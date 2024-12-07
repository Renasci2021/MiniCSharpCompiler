using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MiniCSharpCompiler.Core.Interfaces;
using MiniCSharpCompiler.Core.Lexer;

namespace MiniCSharpCompiler.Core.Parser;

public class StandardParser : IParser
{
    public SyntaxTree Parse(IEnumerable<Token> tokens)
    {
        // 将 Token 转换为源代码字符串
        StringBuilder sourceCode = new();

        foreach (var token in tokens)
        {
            foreach (var trivia in token.LeadingTrivia)
            {
                sourceCode.Append(trivia.ToString());
            }
            sourceCode.Append(token.Value);
            foreach (var trivia in token.TrailingTrivia)
            {
                sourceCode.Append(trivia.ToString());
            }
        }

        return Parse(sourceCode.ToString());
    }

    public SyntaxTree Parse(string sourceCode)
    {
        // 使用 Roslyn 的解析器来解析源代码
        var syntaxTree = CSharpSyntaxTree.ParseText(
            sourceCode,
            new CSharpParseOptions(
                languageVersion: LanguageVersion.Latest,
                kind: SourceCodeKind.Regular
            )
        );

        return syntaxTree;
    }

    public SyntaxTree Parse(ILexer lexer, string sourceCode)
    {
        return Parse(lexer.Tokenize(sourceCode));
    }
}
