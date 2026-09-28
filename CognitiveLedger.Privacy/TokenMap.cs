using System.Globalization;

namespace CognitiveLedger.Privacy;

public sealed class TokenMap : ITokenMap
{
    private readonly Dictionary<TokenType, Dictionary<string, string>> _valueToToken;
    private readonly Dictionary<string, string> _tokenToValue;
    private int _nextIndex;

    public TokenMap()
    {
        _valueToToken = new Dictionary<TokenType, Dictionary<string, string>>();
        _tokenToValue = new Dictionary<string, string>(StringComparer.Ordinal);
        _nextIndex = 1;
    }

    public string Tokenize(string value, TokenType tokenType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A value is required.", nameof(value));
        }

        Dictionary<string, string>? tokensForType;
        if (!_valueToToken.TryGetValue(tokenType, out tokensForType))
        {
            tokensForType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _valueToToken[tokenType] = tokensForType;
        }

        var trimmedValue = value.Trim();

        string? existingToken;
        if (tokensForType.TryGetValue(trimmedValue, out existingToken))
        {
            return existingToken;
        }

        var token = CreateToken(tokenType, _nextIndex);
        _nextIndex++;

        tokensForType[trimmedValue] = token;
        _tokenToValue[token] = trimmedValue;

        return token;
    }

    public string Detokenize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var result = text;

        foreach (var mapping in _tokenToValue)
        {
            result = result.Replace(
                mapping.Key,
                mapping.Value,
                StringComparison.Ordinal);
        }

        return result;
    }

    private static string CreateToken(TokenType tokenType, int index)
    {
        var typeName = tokenType.ToString().ToUpperInvariant();
        var formattedIndex = index.ToString("D4", CultureInfo.InvariantCulture);

        return "CL_" + typeName + "_" + formattedIndex;
    }
}
