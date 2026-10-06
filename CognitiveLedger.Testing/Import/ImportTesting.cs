using System.Security.Cryptography;
using CognitiveLedger.AI.OpenAI;
using CognitiveLedger.AI.OpenAI.Request;
using CognitiveLedger.AI.OpenAI.Response;
using CognitiveLedger.Common;
using CognitiveLedger.Data.Database;
using CognitiveLedger.Data.Models;
using CognitiveLedger.Common.Types;
using CognitiveLedger.Data.Repositories;
using CognitiveLedger.Parser.PDF;
using CognitiveLedger.Parser.PDF.Dtos;
using CognitiveLedger.Parser.PDF.Interfaces;
using CognitiveLedger.Parser.PDF.Request;
using CognitiveLedger.Parser.PDF.Response;
using CognitiveLedger.Parser.PDF.Types;
using CognitiveLedger.Privacy;
using CognitiveLedger.Statements.Definitions.SynchronyAmazon;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DataProcessingAudit = CognitiveLedger.Data.Models.CreditCard.StatementProcessingAudit;
using DataStatement = CognitiveLedger.Data.Models.CreditCard.CreditCardStatement;
using DataTransaction = CognitiveLedger.Data.Models.CreditCard.CreditCardTransaction;

namespace CognitiveLedger.Testing.Import;

public class Tests
{
    private const long TestUserId = UserCatalog.SystemUserId;

    [Test]
    [Explicit("Generates a tokenized PDF preview for manual inspection.")]
    public async Task PreviewTokenizedPdf()
    {
        var pdfPath = FindRepositoryFile("Test", "Synchrony-Amazon.pdf");
        
        var testFolder = Path.GetDirectoryName(pdfPath)
                         ?? throw new DirectoryNotFoundException(
                             $"Could not find test folder for '{pdfPath}'.");
        
        var originalPdf = await File.ReadAllBytesAsync(pdfPath);
        var tokenMap = new TokenMap();

        PdfTextReplacement[] replacements =
        [
            new PdfTextReplacement
            {
                OriginalValue = "3016",
                ReplacementValue = tokenMap.Tokenize("3016", TokenType.Account)
            },
            new PdfTextReplacement
            {
                OriginalValue = "Daniel",
                ReplacementValue = tokenMap.Tokenize("Daniel", TokenType.Person)
            },
            new PdfTextReplacement
            {
                OriginalValue = "Maroff",
                ReplacementValue = tokenMap.Tokenize("Maroff", TokenType.Person)
            },
            new PdfTextReplacement
            {
                OriginalValue = "15824 REYNOLDS",
                ReplacementValue = tokenMap.Tokenize("15824 REYNOLDS", TokenType.Value)
            },
            new PdfTextReplacement
            {
                OriginalValue = "Indian Land",
                ReplacementValue = tokenMap.Tokenize("Indian Land", TokenType.Value)
            }
        ];

        var sanitizer = new PdfPiiSanitizer(
            new PdfPigTextExtractor(new NoOpAppLog<PdfPigTextExtractor>()),
            new StubPiiDetector(),
            new TextPdfRedactor(new NoOpAppLog<TextPdfRedactor>()),
            new PdfRasterizer(new NoOpAppLog<PdfRasterizer>()),
            new NoOpAppLog<PdfPiiSanitizer>());

        var result = await sanitizer.SanitizePiiAsync(
            new SanitizePiiRequest
            {
                UserId = 1,
                PdfData = originalPdf,
                Replacements = replacements
            });

        var outputDirectory = Path.Combine(testFolder, "Output");
        Directory.CreateDirectory(outputDirectory);

        var outputPath = Path.Combine(
            outputDirectory,
            "Synchrony-Amazon-Tokenized-Preview.pdf");

        await File.WriteAllBytesAsync(outputPath, result.RasterizedPdfData);

        TestContext.Progress.WriteLine(
            $"Tokenized PDF preview written to: {outputPath}");

        Assert.That(File.Exists(outputPath), Is.True);
        Assert.That(new FileInfo(outputPath).Length, Is.GreaterThan(0));
    }

    [Test]
    [Explicit("Writes the Discover PDF and sanitized page text supplied to the AI.")]
    public async Task PreviewDiscoverAiInput()
    {
        const int previewUserId = 2;
        var sourcePath = FindRepositoryFile("Test", "Discover-Capital-One.pdf");
        var sourcePdf = await File.ReadAllBytesAsync(sourcePath);
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();
        var config = new AppConfiguration(configuration);
        var dbOptions = new DbContextOptionsBuilder<CognitiveLedgerDbContext>()
            .UseNpgsql(config.ConnectionString)
            .Options;

        await using var dbContext = new CognitiveLedgerDbContext(dbOptions);
        var redactionValues = await new UserRedactionValueRepository(dbContext)
            .GetActiveValuesAsync(previewUserId);
        Assert.That(redactionValues, Is.Not.Empty);

        var tokenMap = new TokenMap();
        PdfTextReplacement[] replacements =
        [
            .. redactionValues.Select(value => new PdfTextReplacement
            {
                OriginalValue = value,
                ReplacementValue = tokenMap.Tokenize(value, TokenType.Value)
            })
        ];
        var rasterizer = new PdfRasterizer(new NoOpAppLog<PdfRasterizer>());
        var sanitizer = new PdfPiiSanitizer(
            new PdfPigTextExtractor(new NoOpAppLog<PdfPigTextExtractor>()),
            new StubPiiDetector(),
            new TextPdfRedactor(
                new NoOpAppLog<TextPdfRedactor>(),
                rasterizer),
            rasterizer,
            new NoOpAppLog<PdfPiiSanitizer>());

        var result = await sanitizer.SanitizePiiAsync(new SanitizePiiRequest
        {
            UserId = previewUserId,
            PdfData = sourcePdf,
            Replacements = replacements
        });

        var outputDirectory = Path.Combine(
            Path.GetDirectoryName(sourcePath)!,
            "Output");
        Directory.CreateDirectory(outputDirectory);
        var pdfOutputPath = Path.Combine(
            outputDirectory,
            "Discover-Capital-One-AI-Input.pdf");
        var textOutputPath = Path.Combine(
            outputDirectory,
            "Discover-Capital-One-AI-Input.txt");
        var splitPages = PdfPageSplitter.SplitPages(result.RasterizedPdfData);
        (int Index, PdfPageText Text)[] pagesToSend =
        [
            .. result.SanitizedPageText
                .Select((text, index) => (Index: index, Text: text))
                .Where(page => !string.IsNullOrWhiteSpace(page.Text.Text))
        ];
        var aiPdfData = PdfPageSplitter.CombinePages(
            [.. pagesToSend.Select(page => splitPages[page.Index])]);
        PdfPageText[] aiPageText = [.. pagesToSend.Select(page => page.Text)];
        var sanitizedText = OpenAiPdfStatementReader.FormatSanitizedPageText(
            aiPageText);

        await File.WriteAllBytesAsync(pdfOutputPath, aiPdfData);
        await File.WriteAllTextAsync(textOutputPath, sanitizedText);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.SanitizedPageText.Count, Is.EqualTo(result.PageCount));
            Assert.That(result.PageCount, Is.EqualTo(6));
            Assert.That(aiPageText, Has.Length.EqualTo(5));
            Assert.That(PdfPageSplitter.SplitPages(aiPdfData), Has.Count.EqualTo(5));
            Assert.That(sanitizedText, Does.Contain("APPLE.COM/BILL 866-712-7753 CA $14.99"));
            Assert.That(redactionValues.All(value =>
                !sanitizedText.Contains(value, StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(new FileInfo(pdfOutputPath).Length, Is.GreaterThan(0));
            Assert.That(new FileInfo(textOutputPath).Length, Is.GreaterThan(0));
        }

        TestContext.Progress.WriteLine($"AI PDF input written to: {pdfOutputPath}");
        TestContext.Progress.WriteLine($"AI text input written to: {textOutputPath}");
    }

    [Test]
    public async Task SanitizePiiAsync_PdfWithDetectedPii_ReturnsImageOnlyPdf()
    {
        var pdfPath = FindRepositoryFile("Test", "Synchrony-Amazon.pdf");
        
        var testFolder = Path.GetDirectoryName(pdfPath)
                         ?? throw new DirectoryNotFoundException($"Could not find test folder for '{pdfPath}'.");
        
        var originalPdf = await File.ReadAllBytesAsync(pdfPath);
        var textExtractor = new PdfPigTextExtractor(
            TestLogging.CreateLogger<PdfPigTextExtractor>());

        var extractPdfText = textExtractor.ExtractPdfText(
            new ExtractPdfTextRequest { UserId = 1, PdfData = originalPdf });
        
        var piiDetector = new StubPiiDetector();
        IList<string> piiValues =
        [
            "3016",
            "Daniel",
            "Maroff",
            "15824 REYNOLDS",
            "Indian Land"
        ];
        var tokenMap = new TokenMap();
        IList<PdfTextReplacement> replacements =
        [
            .. piiValues.Select(value => new PdfTextReplacement
            {
                OriginalValue = value,
                ReplacementValue = tokenMap.Tokenize(value, TokenType.Value)
            })
        ];

        var sanitizer = new PdfPiiSanitizer(
            textExtractor,
            piiDetector,
            new TextPdfRedactor(new NoOpAppLog<TextPdfRedactor>()),
            new PdfRasterizer(new NoOpAppLog<PdfRasterizer>()),
            new NoOpAppLog<PdfPiiSanitizer>());

        var result = await sanitizer.SanitizePiiAsync(
            new SanitizePiiRequest
            {
                UserId = 1,
                PdfData = originalPdf,
                Replacements = replacements
            });

        var rasterizedText = textExtractor.ExtractPdfText(
            new ExtractPdfTextRequest { UserId = 1, PdfData = result.RasterizedPdfData });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.PiiStatus, Is.EqualTo(PiiDetectionStatus.DetectedAndRemoved));
            Assert.That(result.PiiItems, Is.Not.Empty);
            Assert.That(result.PiiItems.Select(item => item.Value), Does.Contain("Daniel").IgnoreCase);
            Assert.That(result.PiiItems.All(item => piiValues.Contains(item.Value, StringComparer.OrdinalIgnoreCase)), Is.True);
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
        var pdfPath = FindRepositoryFile("Test", "Synchrony-Amazon.pdf");
        var sourcePdf = await File.ReadAllBytesAsync(pdfPath);
        var sourceDocumentSha256 = Convert.ToHexString(SHA256.HashData(sourcePdf));
        var tokenMap = new TokenMap();
        string[] piiValues =
        [
            "3016",
            "Daniel",
            "Maroff",
            "15824 REYNOLDS",
            "Indian Land"
        ];
        PdfTextReplacement[] replacements =
        [
            .. piiValues.Select(value => new PdfTextReplacement
            {
                OriginalValue = value,
                ReplacementValue = tokenMap.Tokenize(value, TokenType.Value)
            })
        ];
        var rasterizer = new PdfRasterizer(new NoOpAppLog<PdfRasterizer>());
        var sanitized = await new PdfPiiSanitizer(
            new PdfPigTextExtractor(new NoOpAppLog<PdfPigTextExtractor>()),
            new StubPiiDetector(),
            new TextPdfRedactor(
                new NoOpAppLog<TextPdfRedactor>(),
                rasterizer),
            rasterizer,
            new NoOpAppLog<PdfPiiSanitizer>()).SanitizePiiAsync(
            new SanitizePiiRequest
            {
                UserId = checked((int)TestUserId),
                PdfData = sourcePdf,
                Replacements = replacements
            });
        
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();
        
        var config = new AppConfiguration(configuration);
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
            .FindBySourceDocumentSha256Async(TestUserId, sourceDocumentSha256);

        if (existingStatement is not null)
        {
            Assert.Pass(
                $"Statement PDF has already been imported as statement ID " +
                $"{existingStatement.Id}.");
        }

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(requestTimeoutSeconds);

        var reader = new OpenAiPdfStatementReader(
            httpClient,
            config,
            new NoOpAppLog<OpenAiPdfStatementReader>());

        var audit = await processingRepository.StartAsync(
            new DataProcessingAudit
            {
                UserId = TestUserId,
                Filename = Path.GetFileName(pdfPath),
                StatementTypeId = StatementTypeCatalog.CreditCardId,
                AiProviderId = AiProviderCatalog.OpenAiId,
                AiModelId = AiModelCatalog.Gpt56TerraId
            });

        ExtractPdfStatementResponse result;
        DataStatement savedStatement;

        try
        {
            result = await reader.ExtractAsync(
                new PdfStatementDocument
                {
                    RasterizedPdfData = sanitized.RasterizedPdfData,
                    SanitizedPageText = sanitized.SanitizedPageText
                },
                new SynchronyAmazonStatementDefinition());

            savedStatement = await statementRepository.InsertStatementAsync(
                MapToDataStatement(TestUserId, result, sourceDocumentSha256));

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
        
        using (Assert.EnterMultipleScope())
        {
            var stmt = result.Statement!;
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
            Assert.That(
                savedStatement.CreatedBy,
                Is.EqualTo(checked((int)TestUserId)));
            Assert.That(
                savedStatement.Transactions.All(transaction =>
                    transaction.CreatedBy == checked((int)TestUserId)),
                Is.True);
            Assert.That(audit.StatementId, Is.EqualTo(savedStatement.Id));
            Assert.That(audit.Filename, Is.EqualTo(Path.GetFileName(pdfPath)));
            Assert.That(audit.StatusId, Is.EqualTo(StatusCatalog.SuccessId));
        }
    }

    [Test]
    public async Task SanitizePiiAsync_PdfWithoutDetectedPii_StillReturnsImageOnlyPdf()
    {
        var pdfPath = FindRepositoryFile("Test", "Synchrony-Amazon.pdf");
        
        var testFolder = Path.GetDirectoryName(pdfPath)
                         ?? throw new DirectoryNotFoundException($"Could not find test folder for '{pdfPath}'.");
        
        var originalPdf = await File.ReadAllBytesAsync(pdfPath);
        
        var textExtractor = new PdfPigTextExtractor(new NoOpAppLog<PdfPigTextExtractor>());
        
        var sanitizer = new PdfPiiSanitizer(
            textExtractor,
            new StubPiiDetector(),
            new TextPdfRedactor(new NoOpAppLog<TextPdfRedactor>()),
            new PdfRasterizer(new NoOpAppLog<PdfRasterizer>()),
            new NoOpAppLog<PdfPiiSanitizer>());

        var result = await sanitizer.SanitizePiiAsync(
            new SanitizePiiRequest { UserId = 1, PdfData = originalPdf });

        var rasterizedText = textExtractor.ExtractPdfText(
            new ExtractPdfTextRequest { UserId = 1, PdfData = result.RasterizedPdfData });

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
    
    private static DataStatement MapToDataStatement(
        long userId,
        ExtractPdfStatementResponse result,
        string sourceDocumentSha256)
    {
        ArgumentNullException.ThrowIfNull(result);
        var statement = result.Statement!;

        return new DataStatement
        {
            UserId = userId,
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
            Transactions =
            [
                .. statement.Transactions
                    .Select(transaction => new DataTransaction
                    {
                        TransactionDate = transaction.Date,
                        Category = transaction.Category,
                        Merchant = transaction.Merchant,
                        Description = transaction.Description,
                        Amount = transaction.Amount,
                        IsCredit = transaction.IsCredit
                    })
            ]
        };
    }

    private static string FindRepositoryFile(params string[] pathParts)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine([directory.FullName, .. pathParts]);
            if (File.Exists(candidate))
            {
                return candidate;
            }
            directory = directory.Parent;
        }
        throw new FileNotFoundException($"Could not find test PDF '{Path.Combine(pathParts)}'.");
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
