using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MiniCSharpCompiler.Core.Lexer;

public readonly struct Token(SyntaxKind kind, string value)
{
    public SyntaxKind Kind { get; } = kind;
    public string Value { get; } = value;
    public SyntaxTriviaList LeadingTrivia { get; init; }
    public SyntaxTriviaList TrailingTrivia { get; init; }
}
