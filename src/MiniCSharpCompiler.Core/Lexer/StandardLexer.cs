using Microsoft.CodeAnalysis.CSharp;
using MiniCSharpCompiler.Core.Interfaces;

namespace MiniCSharpCompiler.Core.Lexer;

public class StandardLexer : ILexer
{
    public IEnumerable<Token> Tokenize(string sourceCode)
    {
        var tokens = new List<Token>();
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var syntaxTokens = syntaxTree.GetRoot().DescendantTokens();
        foreach (var syntaxToken in syntaxTokens)
        {
            var token = new Token(syntaxToken.Kind(), syntaxToken.ValueText)
            {
                LeadingTrivia = syntaxToken.LeadingTrivia,
                TrailingTrivia = syntaxToken.TrailingTrivia
            };
            tokens.Add(token);
        }
        return tokens;
    }
}
