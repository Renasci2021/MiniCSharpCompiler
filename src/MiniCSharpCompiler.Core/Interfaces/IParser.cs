using Microsoft.CodeAnalysis;

namespace MiniCSharpCompiler.Core.Interfaces;

public interface IParser
{
    SyntaxTree Parse(string sourceCode);
}
