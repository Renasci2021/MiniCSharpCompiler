using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MiniCSharpCompiler.Core.Lexer;

public readonly struct Token(SyntaxKind kind, string value)
{
    public SyntaxKind Kind { get; } = kind;
    public string Value { get; } = value;
    public SyntaxTriviaList LeadingTrivia { get; init; }
    public SyntaxTriviaList TrailingTrivia { get; init; }

    public SyntaxToken ToSyntaxToken()
    {
        // FIXME: 标识符不能用原有方式创建
        return SyntaxFactory.Token(LeadingTrivia, Kind, Value, Value, TrailingTrivia);
    }
}
