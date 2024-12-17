using Microsoft.CodeAnalysis;
using MiniCSharpCompiler.Core.Lexer;
using MiniCSharpCompiler.Core.Parser;
using MiniCSharpCompiler.Utilities;

namespace MiniCSharpCompiler;

class Program
{
    static void Main(string[] args)
    {
        string sourceCode;

        if (args.Length == 0)
        {
            Console.WriteLine("请从标准输入中输入源代码，结束输入请按 Ctrl+D (Unix) 或 Ctrl+Z (Windows)：");
            using var reader = new StreamReader(Console.OpenStandardInput());
            sourceCode = reader.ReadToEndAsync().Result;
        }
        else
        {
            sourceCode = File.ReadAllTextAsync(args[0]).Result;
        }

        // 词法分析
        var lexer = new Lexer();
        var tokens = lexer.Tokenize(sourceCode);

        // 将 Token 转换为 Roslyn 的 SyntaxToken
        var syntaxTokenList = tokens.Select(token => token.ToSyntaxToken()).ToList();

        // 语法分析
        //var parser = new Parser(syntaxTokenList);
        var stdParser = new StandardParser();
        var syntaxTree = stdParser.Parse(sourceCode);
        SyntaxPrinter.PrintSyntaxTree(syntaxTree, printTrivia: false);
    }
}