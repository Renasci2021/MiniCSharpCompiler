using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

namespace MiniCSharpCompiler.Core.SemanticAnalysis;

public class SemanticAnalyzer
{
    private readonly SymbolTable _symbolTable = new();
    private readonly List<DiagnosticMessage> _diagnostics = [];
    private Symbol? _currentScope;

    public IReadOnlyList<DiagnosticMessage> Analyze(CompilationUnitSyntax root)
    {
        // 添加内置类型
        AddPredefinedTypes();

        // 处理 using 指令
        foreach (var usingDirective in root.Usings)
        {
            var ns = usingDirective.Name!.ToString();
            _symbolTable.AddNamespace(ns);
        }

        // 分析命名空间成员
        foreach (var member in root.Members)
        {
            Console.WriteLine("分析成员：");
            Console.WriteLine(member);
            AnalyzeMember(member);
        }

        return _diagnostics;
    }

    private void AnalyzeMember(MemberDeclarationSyntax member)
    {
        switch (member)
        {
            case NamespaceDeclarationSyntax ns:
                Console.WriteLine("分析命名空间：");
                Console.WriteLine(ns);
                AnalyzeNamespace(ns);
                break;
            case ClassDeclarationSyntax cls:
                Console.WriteLine("分析类：");
                Console.WriteLine(cls);
                AnalyzeClass(cls);
                break;
        }
    }

    private void AnalyzeNamespace(NamespaceDeclarationSyntax ns)
    {
        var symbol = new Symbol(ns.Name.ToString(), SymbolKind.Namespace, SyntaxKind.NamespaceDeclaration);

        if (!_symbolTable.TryAddSymbol(symbol))
        {
            ReportError($"命名空间 '{symbol.Name}' 已存在", ns.GetLocation());
            return;
        }

        var previousScope = _currentScope;
        _currentScope = symbol;

        foreach (var member in ns.Members)
        {
            AnalyzeMember(member);
        }

        _currentScope = previousScope;
    }

    private void AnalyzeClass(ClassDeclarationSyntax cls)
    {
        var symbol = new Symbol(cls.Identifier.Text, SymbolKind.Class, SyntaxKind.ClassDeclaration, _currentScope);

        if (!_symbolTable.TryAddSymbol(symbol))
        {
            ReportError($"类型 '{symbol.Name}' 已存在", cls.GetLocation());
            return;
        }

        var previousScope = _currentScope;
        _currentScope = symbol;

        foreach (var member in cls.Members)
        {
            if (member is MethodDeclarationSyntax method)
            {
                Console.WriteLine("分析方法：");
                Console.WriteLine(method);
                AnalyzeMethod(method);
            }
        }

        _currentScope = previousScope;
    }

    private void AnalyzeMethod(MethodDeclarationSyntax method)
    {
        var symbol = new Symbol(method.Identifier.Text, SymbolKind.Method, GetTypeKind(method.ReturnType), _currentScope);

        if (!_symbolTable.TryAddSymbol(symbol))
        {
            ReportError($"方法 '{symbol.Name}' 已存在", method.GetLocation());
            return;
        }

        var previousScope = _currentScope;
        _currentScope = symbol;

        // 分析参数
        foreach (var param in method.ParameterList.Parameters)
        {
            Console.WriteLine("分析参数：");
            Console.WriteLine(param);
            AnalyzeParameter(param);
        }

        // 分析方法体
        if (method.Body != null)
        {
            Console.WriteLine("分析方法体：");
            Console.WriteLine(method.Body);
            AnalyzeStatements(method.Body.Statements);
        }

        _currentScope = previousScope;
    }

    private void AnalyzeParameter(ParameterSyntax param)
    {
        var symbol = new Symbol(param.Identifier.Text, SymbolKind.Parameter, GetTypeKind(param.Type!), _currentScope);

        if (!_symbolTable.TryAddSymbol(symbol))
        {
            ReportError($"参数 '{symbol.Name}' 已存在", param.GetLocation());
        }
    }

    private void AnalyzeStatements(SyntaxList<StatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
            if (statement is LocalDeclarationStatementSyntax localDecl)
            {
                Console.WriteLine("分析局部变量声明：");
                Console.WriteLine(localDecl);
                AnalyzeLocalDeclaration(localDecl);
            }
        }
    }

    private void AnalyzeLocalDeclaration(LocalDeclarationStatementSyntax localDecl)
    {
        foreach (var variable in localDecl.Declaration.Variables)
        {
            var symbol = new Symbol(
                variable.Identifier.Text,
                SymbolKind.Variable,
                GetTypeKind(localDecl.Declaration.Type),
                _currentScope
            );

            if (!_symbolTable.TryAddSymbol(symbol))
            {
                ReportError($"变量 '{symbol.Name}' 已存在", variable.GetLocation());
            }
        }
    }

    private void AddPredefinedTypes()
    {
        var types = new[]
        {
            (name: "int", kind: SyntaxKind.IntKeyword),
            (name: "string", kind: SyntaxKind.StringKeyword),
            (name: "void", kind: SyntaxKind.VoidKeyword),
            (name: "bool", kind: SyntaxKind.BoolKeyword)
        };

        foreach (var (name, kind) in types)
        {
            var symbol = new Symbol(name, SymbolKind.Class, kind);
            _symbolTable.AddSymbol(symbol);
        }
    }

    private static SyntaxKind GetTypeKind(TypeSyntax type) => type switch
    {
        PredefinedTypeSyntax p => p.Keyword.Kind(),
        IdentifierNameSyntax => SyntaxKind.IdentifierName,
        _ => SyntaxKind.None
    };

    private void ReportError(string message, Location location)
    {
        _diagnostics.Add(new DiagnosticMessage(message, location));
    }
}
