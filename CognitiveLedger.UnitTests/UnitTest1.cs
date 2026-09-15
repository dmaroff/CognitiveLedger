using System.Security.Cryptography;
using CognitiveLedger.AI.OpenAI;
using CognitiveLedger.AI.OpenAI.Request;
using CognitiveLedger.AI.OpenAI.Response;
using CognitiveLedger.AI.OpenAI.StatementDefinitions;
using CognitiveLedger.Common;
using CognitiveLedger.Data.Database;
using CognitiveLedger.Data.Repositories;
using CognitiveLedger.Parser.PDF;
using CognitiveLedger.Parser.PDF.Dtos;
using CognitiveLedger.Parser.PDF.Interfaces;
using CognitiveLedger.Parser.PDF.Request;
using CognitiveLedger.Parser.PDF.Response;
using CognitiveLedger.Parser.PDF.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DataProcessingAudit = CognitiveLedger.Data.Models.CreditCard.StatementProcessingAudit;
using DataStatement = CognitiveLedger.Data.Models.CreditCard.CreditCardStatement;
using DataTransaction = CognitiveLedger.Data.Models.CreditCard.CreditCardTransaction;

namespace CognitiveLedger.UnitTests;

public class Tests
{
    [Test]
    public async Task SanitizePiiAsync_PdfWithDetectedPii_ReturnsImageOnlyPdf()
    {
        var logger = TestLogging.CreateLogger<Tests>();
        logger.LogInformation("Starting test: {TestName}", nameof(SanitizePiiAsync_PdfWithDetectedPii_ReturnsImageOnlyPdf));
        
        var pdfPath = FindRepositoryFile("Test", "Synchrony-Amazon.pdf");
        var testFolder = Path.GetDirectoryName(pdfPath)
                         ?? throw new DirectoryNotFoundException($"Could not find test folder for '{pdfPath}'.");
        var originalPdf = await File.ReadAllBytesAsync(pdfPath);
        var textExtractor = new PdfPigTextExtractor(
            TestLogging.CreateLogger<PdfPigTextExtractor>());

        var extractPdfText = textExtractor.ExtractPdfText(
            new ExtractPdfTextRequest { PdfData = originalPdf });
        
        logger.LogInformation("Extracted text from PDF: {TextLength}", extractPdfText.FullText.Length);
        
        var piiDetector = new StubPiiDetector();
        IList<string> piiValues =
        [
            "3016",
            "Daniel",
            "Maroff",
            "15824 REYNOLDS",
            "Indian Land"
        ];

        var sanitizer = new PdfPiiSanitizer(
            textExtractor,
            piiDetector,
            new TextPdfRedactor(TestLogging.CreateLogger<TextPdfRedactor>()),
            new PdfRasterizer(TestLogging.CreateLogger<PdfRasterizer>()),
            TestLogging.CreateLogger<PdfPiiSanitizer>());

        var result = await sanitizer.SanitizePiiAsync(
            new SanitizePiiRequest
            {
                PdfData = originalPdf,
                PiiValues = piiValues
            });

        var rasterizedText = textExtractor.ExtractPdfText(
            new ExtractPdfTextRequest { PdfData = result.RasterizedPdfData });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.PiiStatus, Is.EqualTo(PiiDetectionStatus.DetectedAndRemoved));
            Assert.That(result.PiiItems, Is.Not.Empty);
            Assert.That(
                result.PiiItems.Select(item => item.Value),
                Does.Contain("Daniel").IgnoreCase);
            Assert.That(
                result.PiiItems.All(item => piiValues.Contains(
                    item.Value,
                    StringComparer.OrdinalIgnoreCase)),
                Is.True);
            Assert.That(result.RasterizedPdfData, Is.Not.Empty);
            Assert.That(result.RasterizedPdfData, Is.Not.EqualTo(originalPdf));
            Assert.That(result.PageCount, Is.GreaterThan(0));
            Assert.That(result.RasterizationDpi, Is.EqualTo(200));
            Assert.That(result.Sha256Hash,Is.EqualTo(Convert.ToHexString(SHA256.HashData(result.RasterizedPdfData))));
            Assert.That(rasterizedText.FullText, Is.Empty);
            Assert.That(rasterizedText.TextItems, Is.Empty);
        }
        await WriteRepositoryFile(result.RasterizedPdfData, testFolder, "Synchrony-Amazon-Rasterized.pdf");
    }

    [Test]
    public async Task Send_Sanitized_PDF_To_LLM_And_Extract_Statement_Data()
    {
        var pdfPath = FindRepositoryFile("Test", "Synchrony-Amazon-Rasterized.pdf");
        var originalPdf = await File.ReadAllBytesAsync(pdfPath);
        var sourcePdfPath = FindRepositoryFile("Test", "Synchrony-Amazon.pdf");
        var sourcePdf = await File.ReadAllBytesAsync(sourcePdfPath);
        var sourceDocumentSha256 = Convert.ToHexString(SHA256.HashData(sourcePdf));
        
        IAppConfiguration config = new AppConfiguration();
        var apiKey = config.AiApiKey;
        Assert.That(apiKey, Is.Not.Null.And.Not.Empty);

        var requestTimeoutSeconds = config.AiRequestTimeoutSeconds;
        Assert.That(requestTimeoutSeconds, Is.GreaterThan(0));
        
        Assert.That(config.ConnectionString, Is.Not.Null.And.Not.Empty);

        var dbOptions = new DbContextOptionsBuilder<CognitiveLedgerDbContext>()
            .UseNpgsql(config.ConnectionString)
            .Options;
        await using var dbContext = new CognitiveLedgerDbContext(dbOptions);
        var statementRepository = new StatementRepository(dbContext);
        var processingRepository = new StatementProcessingRepository(dbContext);

        var existingStatement = await statementRepository
            .FindBySourceDocumentSha256Async(sourceDocumentSha256);

        if (existingStatement is not null)
        {
            TestLogging.CreateLogger<Tests>().LogInformation(
                "Statement PDF has already been imported as statement ID {StatementId}; " +
                "SHA-256={SourceDocumentSha256}",
                existingStatement.Id,
                sourceDocumentSha256);
            Assert.Pass(
                $"Statement PDF has already been imported as statement ID " +
                $"{existingStatement.Id}.");
        }

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(requestTimeoutSeconds);

        var reader = new OpenAiPdfStatementReader(
            httpClient,
            config,
            TestLogging.CreateLogger<OpenAiPdfStatementReader>());

        var audit = await processingRepository.StartAsync(
            new DataProcessingAudit
            {
                StatementType = "SynchronyAmazon",
                AiProvider = "OpenAI",
                AiModel = config.AiModel
            });

        ExtractPdfStatementResponse result;
        DataStatement savedStatement;

        try
        {
            result = await reader.ExtractAsync(new SynchronyAmazonStatementRequest
            {
                PdfData = originalPdf
            });

            savedStatement = await statementRepository.InsertStatementAsync(
                MapToDataStatement(result, sourceDocumentSha256));

            audit = await processingRepository.CompleteAsync(
                audit.Id,
                savedStatement.Id,
                savedStatement.Transactions.Count);
        }
        catch (Exception exception)
        {
            await processingRepository.FailAsync(audit.Id, exception);
            throw;
        }

        LogExtractedStatement(result);
        
        using (Assert.EnterMultipleScope())
        {
            var stmt = result.Statement;
            Assert.That(stmt.Transactions, Is.Not.Empty);
            Assert.That(stmt.Transactions.Any(t => string.IsNullOrEmpty(t.Description)), Is.False);
            Assert.That(stmt.Transactions.All(t => t.Amount > 0), Is.True);

            var calculatedNewBalance =
                stmt.PreviousBalance +
                stmt.TotalPurchases +
                stmt.Fees +
                stmt.InterestCharged -
                stmt.TotalPayments -
                stmt.TotalOtherCredits;

            Assert.That(calculatedNewBalance, Is.EqualTo(stmt.NewBalance).Within(0.01m));
            Assert.That(
                stmt.Transactions.Where(t => !t.IsCredit).Sum(t => t.Amount),
                Is.EqualTo(stmt.TotalPurchases + stmt.Fees + stmt.InterestCharged).Within(0.01m));
            Assert.That(
                stmt.NetNewSpending,
                Is.EqualTo(stmt.TotalPurchases - stmt.TotalOtherCredits));
            Assert.That(savedStatement.Id, Is.GreaterThan(0));
            Assert.That(audit.StatementId, Is.EqualTo(savedStatement.Id));
            Assert.That(audit.Status, Is.EqualTo("Succeeded"));
        }
    }

    [Test]
    public async Task SanitizePiiAsync_PdfWithoutDetectedPii_StillReturnsImageOnlyPdf()
    {
        var pdfPath = FindRepositoryFile("Test", "Synchrony-Amazon.pdf");
        var testFolder = Path.GetDirectoryName(pdfPath)
                         ?? throw new DirectoryNotFoundException($"Could not find test folder for '{pdfPath}'.");
        
        var originalPdf = await File.ReadAllBytesAsync(pdfPath);
        var textExtractor = new PdfPigTextExtractor(
            TestLogging.CreateLogger<PdfPigTextExtractor>());
        var sanitizer = new PdfPiiSanitizer(
            textExtractor,
            new StubPiiDetector(),
            new TextPdfRedactor(TestLogging.CreateLogger<TextPdfRedactor>()),
            new PdfRasterizer(TestLogging.CreateLogger<PdfRasterizer>()),
            TestLogging.CreateLogger<PdfPiiSanitizer>());

        var result = await sanitizer.SanitizePiiAsync(
            new SanitizePiiRequest { PdfData = originalPdf });

        var rasterizedText = textExtractor.ExtractPdfText(
            new ExtractPdfTextRequest { PdfData = result.RasterizedPdfData });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.PiiStatus, Is.EqualTo(PiiDetectionStatus.NotDetected));
            Assert.That(result.PiiItems, Is.Empty);
            Assert.That(result.RasterizedPdfData, Is.Not.Empty);
            Assert.That(result.RasterizedPdfData, Is.Not.EqualTo(originalPdf));
            Assert.That(result.PageCount, Is.GreaterThan(0));
            Assert.That(rasterizedText.FullText, Is.Empty);
            Assert.That(rasterizedText.TextItems, Is.Empty);
        }
        await WriteRepositoryFile(result.RasterizedPdfData, testFolder, "Synchrony-Amazon-Rasterized.pdf");
    }

    private static void LogExtractedStatement(ExtractPdfStatementResponse result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var logger = TestLogging.CreateLogger<Tests>();
        var statement = result.Statement;

        logger.LogInformation(
            "Logging {TransactionCount} extracted transactions",
            statement.Transactions.Count);

        foreach (var transaction in statement.Transactions)
        {
            logger.LogInformation(
                "Transaction: Date={TransactionDate}, Amount={TransactionAmount:F2}, Description={TransactionDescription}",
                transaction.Date?.ToString("yyyy-MM-dd") ?? "Not provided",
                transaction.Amount,
                transaction.Description);
        }

        logger.LogInformation(
            "Transaction total={TransactionTotal:F2}; Statement balance={StatementBalance:F2}",
            statement.Transactions.Sum(transaction => transaction.Amount),
            statement.NewBalance);
    }

    private static DataStatement MapToDataStatement(
        ExtractPdfStatementResponse result,
        string sourceDocumentSha256)
    {
        ArgumentNullException.ThrowIfNull(result);
        var statement = result.Statement;

        return new DataStatement
        {
            SourceDocumentSha256 = sourceDocumentSha256,
            Issuer = statement.Issuer,
            AccountName = statement.AccountName,
            StatementPeriodStart = statement.StatementPeriodStart,
            StatementPeriodEnd = statement.StatementPeriodEnd,
            PreviousBalance = statement.PreviousBalance,
            NewBalance = statement.NewBalance,
            TotalPurchases = statement.TotalPurchases,
            TotalPayments = statement.TotalPayments,
            TotalOtherCredits = statement.TotalOtherCredits,
            Fees = statement.Fees,
            InterestCharged = statement.InterestCharged,
            Transactions = statement.Transactions
                .Select(transaction => new DataTransaction
                {
                    TransactionDate = transaction.Date,
                    Category = transaction.Category,
                    Merchant = transaction.Merchant,
                    Description = transaction.Description,
                    Amount = transaction.Amount,
                    IsCredit = transaction.IsCredit
                })
                .ToList()
        };
    }

    private static string FindRepositoryFile(params string[] pathParts)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                [directory.FullName, .. pathParts]);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Could not find test PDF '{Path.Combine(pathParts)}'.");
    }

    private static async Task WriteRepositoryFile(byte[] pdfData, params string[] pathParts)
    {
        if (pdfData.Length == 0)
        {
            throw new ArgumentException(
                "PDF data cannot be empty.",
                nameof(pdfData));
        }
        var outputPath = Path.Combine([..pathParts]);
        await File.WriteAllBytesAsync(outputPath, pdfData);
    }

    private sealed class StubPiiDetector(params PiiItem[] piiItems) : IPiiDetector
    {
        public IReadOnlyCollection<PiiItem> PiiItems => piiItems;
        public DetectPiiResponse DetectPii(DetectPiiRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            return new DetectPiiResponse
            {
                PiiItems = piiItems
            };
        }
    }
}
