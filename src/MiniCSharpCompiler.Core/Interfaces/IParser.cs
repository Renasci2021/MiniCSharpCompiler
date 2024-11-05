using Microsoft.CodeAnalysis;
using MiniCSharpCompiler.Core.Lexer;

namespace MiniCSharpCompiler.Core.Interfaces;

public interface IParser
{
    SyntaxTree Parse(IEnumerable<Token> tokens);
    SyntaxTree Parse(string sourceCode);
    SyntaxTree Parse(ILexer lexer, string sourceCode);
}
