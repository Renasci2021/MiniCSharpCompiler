using MiniCSharpCompiler.Core.Interfaces;
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
            // 创建 Lexer 和 Parser 实例
            ILexer lexer = new StandardLexer();
            IParser parser = new StandardParser();

            // 打印 Token 流
            SyntaxPrinter.PrintTokens(lexer, sourceCode);

            // 打印抽象语法树
            SyntaxPrinter.PrintSyntaxTree(parser, sourceCode);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"编译错误：{ex.Message}");
        }
    }
}
