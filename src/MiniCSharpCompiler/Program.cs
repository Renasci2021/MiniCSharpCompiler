using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MiniCSharpCompiler.Core.Lexer;
using MiniCSharpCompiler.Core.Parser;
using MiniCSharpCompiler.Core.SemanticAnalysis;
using MiniCSharpCompiler.Utilities;

namespace MiniCSharpCompiler;

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning disable IL2075 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.
#pragma warning disable IL3000 // Avoid accessing Assembly file path when publishing as a single file

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

        SyntaxPrinter.PrintSyntaxTree(syntaxTree, printTrivia: false);
        Console.WriteLine();

        // 语义分析
        var semanticAnalyzer = new SemanticAnalyzer();
        var diagnostics = semanticAnalyzer.Analyze(syntaxTree.GetCompilationUnitRoot());

        Console.WriteLine($"共发现 {diagnostics.Count} 个语义错误：");
        diagnostics.Select(diagnostic => diagnostic.Message).ToList().ForEach(Console.WriteLine);
        Console.WriteLine();

        // 使用 Roslyn 进行编译
        var syntaxTreeRoslyn = CSharpSyntaxTree.ParseText(sourceCode);
        var assemblyName = "CompiledOutput";

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location));

        var compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: [syntaxTreeRoslyn],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // 生成 IL 代码
        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);
        if (!result.Success)
        {
            Console.WriteLine("编译失败：");
            result.Diagnostics.Select(diagnostic => diagnostic.ToString()).ToList().ForEach(Console.WriteLine);
            return;
        }

        // 将 IL 代码写入文件
        var outputPath = Path.Combine(Directory.GetCurrentDirectory(), $"{assemblyName}.dll");
        File.WriteAllBytesAsync(outputPath, ms.ToArray()).Wait();
        Console.WriteLine($"编译成功，输出文件：{outputPath}");

        // 运行生成的程序
        var assembly = Assembly.LoadFrom(outputPath);
        var type = assembly.GetType("Program");
        var method = type!.GetMethod("Main");
        method!.Invoke(null, null);
    }
}
