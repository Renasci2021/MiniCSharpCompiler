using MiniCSharpCompiler.Core.Lexer;

namespace MiniCSharpCompiler.Core.Interfaces;

public interface ILexer
{
    IEnumerable<Token> Tokenize(string sourceCode);
}
