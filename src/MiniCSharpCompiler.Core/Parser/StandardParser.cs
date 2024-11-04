using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MiniCSharpCompiler.Core.Interfaces;

namespace MiniCSharpCompiler.Core.Parser;

public class StandardParser : IParser
{
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
}
