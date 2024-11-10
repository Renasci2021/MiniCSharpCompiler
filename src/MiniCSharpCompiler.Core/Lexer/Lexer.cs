using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MiniCSharpCompiler.Core.Interfaces;

namespace MiniCSharpCompiler.Core.Lexer;

public class Lexer : ILexer
{
    public IEnumerable<Token> Tokenize(string sourceCode)
    {
        // TODO: Implement the lexer

        Token[] tokens = [
            new Token(SyntaxKind.IntKeyword, "int"),
            new Token(SyntaxKind.IdentifierToken, "a"),
            new Token(SyntaxKind.EqualsToken, "=")
            {
                // 添加前导和尾随 Trivia
                LeadingTrivia = new SyntaxTriviaList(SyntaxFactory.Whitespace("  ")),
                TrailingTrivia = new SyntaxTriviaList(SyntaxFactory.LineFeed)
            },
            new Token(SyntaxKind.NumericLiteralToken, "12"),
            new Token(SyntaxKind.SemicolonToken, ";"),
            new Token(SyntaxKind.IdentifierToken, "Console"),
            new Token(SyntaxKind.DotToken, "."),
            new Token(SyntaxKind.IdentifierToken, "WriteLine"),
            new Token(SyntaxKind.OpenParenToken, "("),
            new Token(SyntaxKind.IdentifierToken, "a"),
            new Token(SyntaxKind.CloseParenToken, ")"),
            new Token(SyntaxKind.SemicolonToken, ";"),
            new Token(SyntaxKind.EndOfFileToken, "")
        ];

        return tokens;
    }
}
