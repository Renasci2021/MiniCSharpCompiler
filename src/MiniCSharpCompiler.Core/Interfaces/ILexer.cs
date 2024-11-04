using Microsoft.CodeAnalysis;

namespace MiniCSharpCompiler.Core.Interfaces;

public interface ILexer
{
    IEnumerable<SyntaxToken> Tokenize(string sourceCode);
}
