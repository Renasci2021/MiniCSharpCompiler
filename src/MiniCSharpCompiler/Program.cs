using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MiniCSharpCompiler.Core.Lexer;
using MiniCSharpCompiler.Core.Parser;
using MiniCSharpCompiler.Core.SemanticAnalysis;
using MiniCSharpCompiler.Utilities;

namespace MiniCSharpCompiler;

class Program
{
    static void Main(string[] args)
    {
        // 读取源代码
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

        // 语法分析
        var syntaxTokens = tokens.Select(token => token.ToSyntaxToken()).ToList();
        var parser = new Parser(syntaxTokens);
        var syntaxTree = parser.Parse();
        // var parser = new StandardParser();
        // var syntaxTree = parser.Parse(tokens);

        SyntaxPrinter.PrintSyntaxTree(syntaxTree, printTrivia: false);
        Console.WriteLine();

        // 语义分析
        var semanticAnalyzer = new SemanticAnalyzer();
        var diagnostics = semanticAnalyzer.Analyze(syntaxTree.GetCompilationUnitRoot());

        Console.WriteLine($"共发现 {diagnostics.Count} 个语义错误：");
        diagnostics.Select(diagnostic => diagnostic.Message).ToList().ForEach(Console.WriteLine);
        Console.WriteLine();

        // 使用 Roslyn 进行编译
        Console.WriteLine("使用 Roslyn 编译器分析：");
        var syntaxTreeRoslyn = CSharpSyntaxTree.ParseText(sourceCode);
        var assemblyName = "CompiledOutput";

#pragma warning disable IL3000 // Avoid accessing Assembly file path when publishing as a single file
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location)!, "System.Runtime.dll"))
        };
#pragma warning restore IL3000 // Avoid accessing Assembly file path when publishing as a single file

        var compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: [syntaxTreeRoslyn],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // 获取诊断信息
        var roslynDiagnostics = compilation.GetDiagnostics();
        Console.WriteLine($"Roslyn 诊断发现 {roslynDiagnostics.Length} 个问题：");
        foreach (var diagnostic in roslynDiagnostics)
        {
            Console.WriteLine($"{diagnostic.Location}: {diagnostic.GetMessage()}");
        }
        Console.WriteLine();
    }
}
