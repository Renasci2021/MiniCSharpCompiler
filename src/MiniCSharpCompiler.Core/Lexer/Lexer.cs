using Microsoft.CodeAnalysis;
using MiniCSharpCompiler.Core.Interfaces;

namespace MiniCSharpCompiler.Core.Lexer;

public class Lexer : ILexer
{
    public IEnumerable<SyntaxToken> Tokenize(string sourceCode)
    {
        throw new NotImplementedException();
    }
}
