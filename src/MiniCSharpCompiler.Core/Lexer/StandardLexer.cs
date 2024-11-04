using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MiniCSharpCompiler.Core.Interfaces;

namespace MiniCSharpCompiler.Core.Lexer;

public class StandardLexer : ILexer
{
    public IEnumerable<SyntaxToken> Tokenize(string sourceCode)
    {
        // 使用 Roslyn 的解析器来解析源代码
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        return syntaxTree.GetRoot().DescendantTokens();
    }
}
