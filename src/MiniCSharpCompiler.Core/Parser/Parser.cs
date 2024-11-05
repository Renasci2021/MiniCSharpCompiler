using Microsoft.CodeAnalysis;
using MiniCSharpCompiler.Core.Interfaces;
using MiniCSharpCompiler.Core.Lexer;

namespace MiniCSharpCompiler.Core.Parser;

public class Parser : IParser
{
    public SyntaxTree Parse(IEnumerable<Token> tokens)
    {
        // TODO: Implement the parser
        throw new NotImplementedException();
    }

    public SyntaxTree Parse(string sourceCode)
    {
        return Parse(new Lexer.Lexer(), sourceCode);
    }

    public SyntaxTree Parse(ILexer lexer, string sourceCode)
    {
        return Parse(lexer.Tokenize(sourceCode));
    }
}
