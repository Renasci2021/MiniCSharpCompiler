// * 这个文件先给大家调试用，随意改动，不用提交

using MiniCSharpCompiler.Core.Lexer;
using MiniCSharpCompiler.Core.Parser;
using MiniCSharpCompiler.Utilities;

namespace MiniCSharpCompiler;

class Program
{
    static async Task Main(string[] args)
    {
        string sourceCode;

        if (args.Length == 0)
        {
            Console.WriteLine("请从标准输入中输入源代码，结束输入请按 Ctrl+D (Unix) 或 Ctrl+Z (Windows)：");
            using var reader = new StreamReader(Console.OpenStandardInput());
            sourceCode = await reader.ReadToEndAsync();
        }
        else
        {
            sourceCode = await File.ReadAllTextAsync(args[0]);
        }

        try
        {
            // 打印 Token 流
            var standardLexer = new StandardLexer();
            var tokens = standardLexer.Tokenize(sourceCode);
            SyntaxPrinter.PrintTokens(tokens, printTrivia: true);

            // 查看标准抽象语法树
            var standardParser = new StandardParser();
            var standardSyntaxTree = standardParser.Parse(sourceCode);
            SyntaxPrinter.PrintSyntaxTree(standardSyntaxTree, printTrivia: true);

            // 组合 Lexer 和 Parser 进行分析
            var lexer = new Lexer();
            var syntaxTree = standardParser.Parse(lexer, sourceCode);
            SyntaxPrinter.PrintSyntaxTree(syntaxTree, printTrivia: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"编译错误：{ex.Message}");
        }
    }
}
