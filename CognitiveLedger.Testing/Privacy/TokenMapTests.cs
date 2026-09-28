using CognitiveLedger.Privacy;

namespace CognitiveLedger.Testing.Privacy;

public class TokenMapTests
{
    [Test]
    public void Tokenize_NewValue_CreatesReadableToken()
    {
        var tokenMap = new TokenMap();

        var token = tokenMap.Tokenize("Starbucks", TokenType.Merchant);

        Assert.That(token, Is.EqualTo("CL_MERCHANT_0001"));
    }

    [Test]
    public void Tokenize_SameValueWithDifferentCase_ReturnsSameToken()
    {
        var tokenMap = new TokenMap();

        var firstToken = tokenMap.Tokenize("Starbucks", TokenType.Merchant);
        var secondToken = tokenMap.Tokenize("starbucks", TokenType.Merchant);

        Assert.That(secondToken, Is.EqualTo(firstToken));
    }

    [Test]
    public void Tokenize_SameValueWithDifferentTypes_ReturnsDifferentTokens()
    {
        var tokenMap = new TokenMap();

        var accountToken = tokenMap.Tokenize("Amazon", TokenType.Account);
        var merchantToken = tokenMap.Tokenize("Amazon", TokenType.Merchant);

        Assert.That(merchantToken, Is.Not.EqualTo(accountToken));
    }

    [Test]
    public void Detokenize_TextContainingTokens_RestoresOriginalValues()
    {
        var tokenMap = new TokenMap();
        var accountToken = tokenMap.Tokenize("Amazon", TokenType.Account);
        var merchantToken = tokenMap.Tokenize("Starbucks", TokenType.Merchant);
        var text = "The " + accountToken + " account contains a purchase from " + merchantToken + ".";

        var result = tokenMap.Detokenize(text);

        Assert.That(
            result,
            Is.EqualTo("The Amazon account contains a purchase from Starbucks."));
    }

    [Test]
    public void Detokenize_UnknownToken_LeavesTextUnchanged()
    {
        var tokenMap = new TokenMap();
        var text = "Purchase from CL_MERCHANT_9999.";

        var result = tokenMap.Detokenize(text);

        Assert.That(result, Is.EqualTo(text));
    }
}
