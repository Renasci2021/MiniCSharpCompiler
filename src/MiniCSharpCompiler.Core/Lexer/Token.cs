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
        // 赵培源对照库函数定义修改
        switch (Kind)
        {
            case SyntaxKind.IdentifierToken:
                return SyntaxFactory.Identifier(LeadingTrivia, Value, TrailingTrivia);
            case SyntaxKind.CharacterLiteralToken:
                return SyntaxFactory.Literal(LeadingTrivia, Value, Value[1], TrailingTrivia); // Lexer使用Regex(@"^'.'")匹配
            case SyntaxKind.NumericLiteralToken:
                return SyntaxFactory.Literal(LeadingTrivia, Value, int.Parse(Value), TrailingTrivia); // Lexer使用Regex(@"^\d+")匹配
            default:
                break;
        }

        // FIXME: 标识符不能用原有方式创建
        return SyntaxFactory.Token(LeadingTrivia, Kind, Value, Value, TrailingTrivia);
    }
}
