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

        try
        {
            // 词法分析
            var lexer = new Lexer();
            var tokens = lexer.Tokenize(sourceCode);

            // 语法分析
            var parser = new StandardParser();
            var syntaxTree = parser.Parse(lexer, sourceCode);
            SyntaxPrinter.PrintSyntaxTree(syntaxTree, printTrivia: false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"编译错误：{ex.Message}");
        }
    }
}
