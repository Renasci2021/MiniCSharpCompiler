using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MiniCSharpCompiler.Core.Interfaces;

namespace MiniCSharpCompiler.Main.Utilities;

public static class SyntaxPrinter
{
    public static void PrintTokens(ILexer lexer, string sourceCode)
    {
        var tokens = lexer.Tokenize(sourceCode);
        Console.WriteLine("\n--- Tokens ---");
        foreach (var token in tokens)
        {
            if (token is SyntaxToken syntaxToken)
            {
                var lineSpan = syntaxToken.GetLocation().GetLineSpan();

                Console.WriteLine($"{syntaxToken.Kind()}: {syntaxToken.Text} " +
                    $"[{lineSpan.StartLinePosition.Line + 1}:{lineSpan.StartLinePosition.Character + 1}, " +
                    $"{lineSpan.EndLinePosition.Line + 1}:{lineSpan.EndLinePosition.Character + 1})");
            }
        }
    }

    public static void PrintSyntaxTree(IParser parser, string sourceCode)
    {
        var syntaxTree = parser.Parse(sourceCode);
        var root = syntaxTree.GetRoot();
        Console.WriteLine("\n--- Syntax Tree ---");
        PrintSyntaxNode(root, 0);
    }

    private static void PrintSyntaxNode(SyntaxNode node, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 2);
        Console.WriteLine($"{indent}<{node.Kind()}>");

        foreach (var child in node.ChildNodesAndTokens())
        {
            if (child.IsNode)
            {
                var childNode = child.AsNode();
                if (childNode != null)
                {
                    PrintSyntaxNode(childNode, indentLevel + 1);
                }
            }
            else
            {
                PrintSyntaxToken(child.AsToken(), indentLevel + 1);
            }
        }

        Console.WriteLine($"{indent}</{node.Kind()}>");
    }

    private static void PrintSyntaxToken(SyntaxToken token, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 2);
        Console.WriteLine($"{indent}<{token.Kind()}> {token} </{token.Kind()}>");
    }
}
