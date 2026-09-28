namespace CognitiveLedger.Privacy;

public interface ITokenMap
{
    string Tokenize(string value, TokenType tokenType);

    string Detokenize(string text);
}