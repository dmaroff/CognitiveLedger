using CognitiveLedger.Statements.Abstractions;
using CognitiveLedger.Statements.Definitions;
using CognitiveLedger.Statements.Definitions.CapitalOneBjs;
using CognitiveLedger.Statements.Definitions.CapitalOneDiscover;
using CognitiveLedger.Statements.Definitions.Resolution;
using CognitiveLedger.Statements.Definitions.SynchronyAmazon;

namespace CognitiveLedger.Testing.Statements;

public sealed class StatementDefinitionResolverTests
{
    [Test]
    public void Resolve_WithDefinitionKey_ReturnsMatchingDefinition()
    {
        var synchrony = new SynchronyAmazonStatementDefinition();
        var resolver = new StatementDefinitionResolver([synchrony]);

        var result = resolver.Resolve(new StatementDefinitionSelector(
            StatementDefinitionKeys.SynchronyAmazonStoreCard,
            "Unknown",
            StatementKind.CreditCard));

        Assert.That(result, Is.SameAs(synchrony));
    }

    [TestCase("Synchrony Bank")]
    [TestCase("Synchrony")]
    [TestCase("Amazon Store Card")]
    public void Resolve_WithKnownSource_ReturnsMatchingDefinition(string sourceName)
    {
        var synchrony = new SynchronyAmazonStatementDefinition();
        var resolver = new StatementDefinitionResolver([synchrony]);

        var result = resolver.Resolve(new StatementDefinitionSelector(
            null,
            sourceName,
            StatementKind.CreditCard));

        Assert.That(result, Is.SameAs(synchrony));
    }

    [Test]
    public void Resolve_WithUnknownSourceAndOneCandidate_PreservesExistingBehavior()
    {
        var synchrony = new SynchronyAmazonStatementDefinition();
        var resolver = new StatementDefinitionResolver([synchrony]);

        var result = resolver.Resolve(new StatementDefinitionSelector(
            null,
            "Unknown",
            StatementKind.CreditCard));

        Assert.That(result, Is.SameAs(synchrony));
    }

    [Test]
    public void Resolve_WithUnknownSourceAndMultipleCandidates_ReturnsNull()
    {
        IStatementDefinition[] definitions =
        [
            new SynchronyAmazonStatementDefinition(),
            new CapitalOneBjsStatementDefinition()
        ];
        var resolver = new StatementDefinitionResolver(definitions);

        var result = resolver.Resolve(new StatementDefinitionSelector(
            null,
            "Unknown",
            StatementKind.CreditCard));

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Resolve_CapitalOneProductKeys_ReturnTheirOwnDefinitions()
    {
        var bjs = new CapitalOneBjsStatementDefinition();
        var discover = new CapitalOneDiscoverStatementDefinition();
        var resolver = new StatementDefinitionResolver([bjs, discover]);

        var bjsResult = resolver.Resolve(new StatementDefinitionSelector(
            bjs.Descriptor.Key,
            "Unknown",
            StatementKind.CreditCard));
        var discoverResult = resolver.Resolve(new StatementDefinitionSelector(
            discover.Descriptor.Key,
            "Unknown",
            StatementKind.CreditCard));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(bjsResult, Is.SameAs(bjs));
            Assert.That(discoverResult, Is.SameAs(discover));
            Assert.That(bjsResult, Is.TypeOf<CapitalOneBjsStatementDefinition>());
            Assert.That(discoverResult, Is.TypeOf<CapitalOneDiscoverStatementDefinition>());
        }
    }

    [TestCase("BJ's", typeof(CapitalOneBjsStatementDefinition))]
    [TestCase("Discover", typeof(CapitalOneDiscoverStatementDefinition))]
    public void Resolve_CapitalOneProductAlias_ReturnsExpectedDefinition(
        string sourceName,
        Type expectedType)
    {
        var resolver = new StatementDefinitionResolver(
        [
            new CapitalOneBjsStatementDefinition(),
            new CapitalOneDiscoverStatementDefinition()
        ]);

        var result = resolver.Resolve(new StatementDefinitionSelector(
            null,
            sourceName,
            StatementKind.CreditCard));

        Assert.That(result, Is.TypeOf(expectedType));
    }
}
