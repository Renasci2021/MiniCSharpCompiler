using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MiniCSharpCompiler.Utilities;

public static class SyntaxPrinter
{
    public static void PrintTokens(IEnumerable<SyntaxToken> tokens, bool printTrivia = false)
    {
        Console.WriteLine("--- Tokens ---");
        foreach (var token in tokens)
        {
            if (printTrivia) PrintTrivia(token.LeadingTrivia, 0, "Leading");
            Console.WriteLine($"{token.Kind()}: {token.Text} [{token.Span.Start}, {token.Span.End})");
            if (printTrivia) PrintTrivia(token.TrailingTrivia, 0, "Trailing");
        }
    }

    public static void PrintSyntaxTree(SyntaxTree syntaxTree, bool printTrivia = false)
    {
        Console.WriteLine("--- Syntax Tree ---");
        PrintSyntaxNode(syntaxTree.GetRoot(), 0, printTrivia);
    }

    private static void PrintSyntaxNode(SyntaxNode node, int indentLevel, bool printTrivia)
    {
        var indent = new string(' ', indentLevel * 2);
        Console.WriteLine($"{indent}<{node.Kind()}>");

        foreach (var child in node.ChildNodesAndTokens())
        {
            if (child.IsNode)
            {
                PrintSyntaxNode(child.AsNode()!, indentLevel + 1, printTrivia);
            }
            else
            {
                PrintSyntaxToken(child.AsToken(), indentLevel + 1, printTrivia);
            }
        }

        Console.WriteLine($"{indent}</{node.Kind()}>");
    }

    private static void PrintSyntaxToken(SyntaxToken token, int indentLevel, bool printTrivia)
    {
        if (printTrivia) PrintTrivia(token.LeadingTrivia, indentLevel, "Leading");
        var indent = new string(' ', indentLevel * 2);
        Console.WriteLine($"{indent}<{token.Kind()}> {token} </{token.Kind()}> [{token.Span.Start}, {token.Span.End})");
        if (printTrivia) PrintTrivia(token.TrailingTrivia, indentLevel, "Trailing");
    }

    private static void PrintTrivia(SyntaxTriviaList triviaList, int indentLevel, string triviaType)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        var indent = new string(' ', indentLevel * 2);
        foreach (var trivia in triviaList)
        {
            Console.WriteLine($"{indent}{(triviaType[0] == 'L' ? "↓" : "↑")} {triviaType} " +
                $"Trivia: {trivia.Kind()} [{trivia.Span.Start}, {trivia.Span.End})");
        }
        Console.ResetColor();
    }
}
