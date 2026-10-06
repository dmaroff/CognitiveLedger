using CognitiveLedger.Common;
using CognitiveLedger.Parser.PDF;
using CognitiveLedger.Parser.PDF.Request;
using CognitiveLedger.Services.Importer.Services;
using CognitiveLedger.Statements.Abstractions;
using CognitiveLedger.Statements.Definitions.CapitalOneBjs;
using CognitiveLedger.Statements.Definitions.CapitalOneDiscover;
using CognitiveLedger.Statements.Definitions.SynchronyAmazon;

namespace CognitiveLedger.Testing.Import;

public sealed class StatementDefinitionDetectorTests
{
    [TestCase(
        "Synchrony-Amazon.pdf",
        typeof(SynchronyAmazonStatementDefinition))]
    [TestCase(
        "Mastercard_Statement_082026_1309.pdf",
        typeof(CapitalOneBjsStatementDefinition))]
    [TestCase(
        "Discover_Statement_082026_7513.pdf",
        typeof(CapitalOneDiscoverStatementDefinition))]
    public void Detect_WithSupportedStatementPdf_ReturnsExpectedDefinition(
        string fileName,
        Type expectedDefinitionType)
    {
        var pdfData = File.ReadAllBytes(FindTestFile(fileName));
        var extractor = new PdfPigTextExtractor(
            new NoOpAppLog<PdfPigTextExtractor>());
        var extractedText = extractor.ExtractPdfText(new ExtractPdfTextRequest
        {
            UserId = 1,
            PdfData = pdfData
        });
        var detector = new StatementDefinitionDetector(
        [
            new SynchronyAmazonStatementDefinition(),
            new CapitalOneBjsStatementDefinition(),
            new CapitalOneDiscoverStatementDefinition()
        ]);

        var result = detector.Detect(
            extractedText.FullText,
            StatementKind.CreditCard);

        Assert.That(result, Is.TypeOf(expectedDefinitionType));
    }

    [Test]
    public void Detect_WithMarkersFromMultipleDefinitions_ReturnsNull()
    {
        var detector = new StatementDefinitionDetector(
        [
            new CapitalOneBjsStatementDefinition(),
            new CapitalOneDiscoverStatementDefinition()
        ]);

        var result = detector.Detect(
            "BJ's One Mastercard and Discover More Card",
            StatementKind.CreditCard);

        Assert.That(result, Is.Null);
    }

    private static string FindTestFile(string fileName)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Test", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Could not find test statement '{fileName}'.");
    }
}
