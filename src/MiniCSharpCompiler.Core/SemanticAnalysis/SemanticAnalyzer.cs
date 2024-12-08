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
            switch (statement)
            {
                case LocalDeclarationStatementSyntax localDecl:
                    Console.WriteLine("分析局部变量声明：");
                    Console.WriteLine(localDecl);
                    AnalyzeLocalDeclaration(localDecl);
                    break;

                case ExpressionStatementSyntax expressionStmt:
                    Console.WriteLine("分析表达式语句：");
                    Console.WriteLine(expressionStmt);
                    AnalyzeExpression(expressionStmt.Expression);
                    break;

                //TODO: Uncomment this block
                // case ReturnStatementSyntax returnStmt:
                //     Console.WriteLine("分析返回语句：");
                //     AnalyzeExpression(returnStmt.Expression);
                //     break;

                case ForStatementSyntax forStmt:
                    Console.WriteLine("分析 for 循环：");
                    // AnalyzeForStatement(forStmt);
                    break;

                case WhileStatementSyntax whileStmt:
                    Console.WriteLine("分析 while 循环：");
                    AnalyzeWhileStatement(whileStmt);
                    break;

                case IfStatementSyntax ifStmt:
                    Console.WriteLine("分析 if 语句：");
                    AnalyzeIfStatement(ifStmt);
                    break;
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

        for (var i = 0; i < localDecl.Declaration.Variables.Count; i++)
        {
            if (localDecl.Declaration.Variables[i].Initializer != null)
            {
                Console.WriteLine("分析变量初始化：");
                Console.WriteLine(localDecl.Declaration.Variables[i].Initializer!.Value);
                Console.WriteLine(localDecl.Declaration.Variables[i].Initializer!.Value.GetType());
                AnalyzeExpression(localDecl.Declaration.Variables[i].Initializer!.Value);
            }
        }
    }

    private void AnalyzeExpression(ExpressionSyntax expression)
    {
        switch (expression)
        {
            case IdentifierNameSyntax identifier:
                Console.WriteLine("分析标识符：");
                // 检查变量是否已定义
                if (!_symbolTable.TryResolveSymbol(identifier.Identifier.Text, _currentScope, out _))
                {
                    ReportError($"未定义的变量 '{identifier.Identifier.Text}'", identifier.GetLocation());
                }
                break;

            case InvocationExpressionSyntax invocation:
                Console.WriteLine("分析方法调用：");
                // 检查方法调用
                if (invocation.Expression is IdentifierNameSyntax methodName)
                {
                    if (!_symbolTable.TryResolveSymbol(methodName.Identifier.Text, _currentScope, out var methodSymbol) ||
                        methodSymbol?.Kind != SymbolKind.Method) // Use null-conditional operator
                    {
                        ReportError($"未定义的方法 '{methodName.Identifier.Text}'", methodName.GetLocation());
                    }
                }
                break;

            case AssignmentExpressionSyntax assignment:
                Console.WriteLine("分析赋值表达式：");
                // 检查赋值左右两边类型是否匹配
                AnalyzeExpression(assignment.Left);
                AnalyzeExpression(assignment.Right);
                var leftType = GetExpressionType(assignment.Left);
                var rightType = GetExpressionType(assignment.Right);

                if (leftType != rightType)
                {
                    ReportError($"类型不匹配：'{leftType}' 和 '{rightType}'", assignment.GetLocation());
                }
                break;
            case BinaryExpressionSyntax binaryExpr:
                Console.WriteLine("分析二元表达式：");
                Console.WriteLine(binaryExpr);
                AnalyzeBinaryExpression(binaryExpr);
                break;
            case PrefixUnaryExpressionSyntax prefixUnary:
                Console.WriteLine("分析前缀一元表达式：");
                Console.WriteLine(prefixUnary);
                AnalyzeUnaryExpression(prefixUnary.OperatorToken, prefixUnary.Operand);
                break;
            case PostfixUnaryExpressionSyntax postfixUnary:
                Console.WriteLine("分析后缀一元表达式：");
                Console.WriteLine(postfixUnary);
                AnalyzeUnaryExpression(postfixUnary.OperatorToken, postfixUnary.Operand);
                break;
        }
    }


    private SyntaxKind GetExpressionType(ExpressionSyntax expression)
    {
        Console.WriteLine("分析表达式类型：");
        Console.WriteLine(expression);
        switch (expression)
        {
            case IdentifierNameSyntax identifier:
                if (_symbolTable.TryResolveSymbol(identifier.Identifier.Text, _currentScope, out var symbol))
                {
                    return symbol?.Type ?? SyntaxKind.None;
                }
                break;

            case LiteralExpressionSyntax literal:
                return literal.Kind() switch
                {
                    SyntaxKind.NumericLiteralExpression => SyntaxKind.IntKeyword,
                    SyntaxKind.StringLiteralExpression => SyntaxKind.StringKeyword,
                    SyntaxKind.TrueLiteralExpression or SyntaxKind.FalseLiteralExpression => SyntaxKind.BoolKeyword,
                    _ => SyntaxKind.None
                };

            case PrefixUnaryExpressionSyntax prefixUnary:
                return AnalyzeUnaryExpression(prefixUnary.OperatorToken, prefixUnary.Operand);

            case PostfixUnaryExpressionSyntax postfixUnary:
                return AnalyzeUnaryExpression(postfixUnary.OperatorToken, postfixUnary.Operand);


            case BinaryExpressionSyntax binaryExpr:
                // 简单类型推导：假设操作数类型相同，返回相同类型
                Console.WriteLine("分析二元表达式：");
                Console.WriteLine(binaryExpr);

                return AnalyzeBinaryExpression(binaryExpr);

        }

        return SyntaxKind.None;
    }

    private SyntaxKind AnalyzeBinaryExpression(BinaryExpressionSyntax binaryExpr)
    {
        var leftType = GetExpressionType(binaryExpr.Left); // 左操作数类型
        var rightType = GetExpressionType(binaryExpr.Right); // 右操作数类型
                                                             // 检查运算符类型
        switch (binaryExpr.OperatorToken.Kind())
        {
            // 算术运算符
            case SyntaxKind.PlusToken:
            case SyntaxKind.MinusToken:
            case SyntaxKind.AsteriskToken:
            case SyntaxKind.SlashToken:
                if (leftType == SyntaxKind.IntKeyword && rightType == SyntaxKind.IntKeyword)
                {
                    return SyntaxKind.IntKeyword; // 数值运算返回数值类型
                }
                ReportError($"操作符 '{binaryExpr.OperatorToken}' 不支持操作数类型 '{leftType}' 和 '{rightType}'", binaryExpr.GetLocation());
                break;

            // 比较运算符
            case SyntaxKind.GreaterThanToken:
            case SyntaxKind.LessThanToken:
            case SyntaxKind.GreaterThanEqualsToken:
            case SyntaxKind.LessThanEqualsToken:
            case SyntaxKind.EqualsEqualsToken:
            case SyntaxKind.ExclamationEqualsToken:
                if (leftType == rightType && leftType != SyntaxKind.None)
                {
                    return SyntaxKind.BoolKeyword; // 比较运算返回布尔类型
                }
                ReportError($"操作符 '{binaryExpr.OperatorToken}' 不支持操作数类型 '{leftType}' 和 '{rightType}'", binaryExpr.GetLocation());
                break;

            // 逻辑运算符
            case SyntaxKind.AmpersandAmpersandToken:
            case SyntaxKind.BarBarToken:
                if (leftType == SyntaxKind.BoolKeyword && rightType == SyntaxKind.BoolKeyword)
                {
                    return SyntaxKind.BoolKeyword; // 逻辑运算返回布尔类型
                }
                ReportError($"逻辑操作符 '{binaryExpr.OperatorToken}' 需要布尔类型操作数", binaryExpr.GetLocation());
                break;

            default:
                ReportError($"未知的操作符 '{binaryExpr.OperatorToken}'", binaryExpr.GetLocation());
                break;
        }

        return SyntaxKind.None; // 返回 `None` 表示不支持的表达式类型

    }

    private SyntaxKind AnalyzeUnaryExpression(SyntaxToken operatorToken, ExpressionSyntax operand)
    {
        // 判断变量是否存在
        if (operand is IdentifierNameSyntax identifier)
        {
            if (!_symbolTable.TryResolveSymbol(identifier.Identifier.Text, _currentScope, out _))
            {
                ReportError($"未定义的变量 '{identifier.Identifier.Text}'", identifier.GetLocation());
                return SyntaxKind.None;
            }

        }

        // 获取操作数的类型
        var operandType = GetExpressionType(operand);

        // 检查操作符和操作数类型的匹配性
        switch (operatorToken.Kind())
        {
            case SyntaxKind.PlusToken:  // +
            case SyntaxKind.MinusToken: // -
                if (operandType == SyntaxKind.IntKeyword || operandType == SyntaxKind.DoubleKeyword)
                {
                    return operandType; // 数值类型返回自身
                }
                ReportError($"操作符 '{operatorToken.Text}' 不能用于类型 '{operandType}'", operand.GetLocation());
                break;

            case SyntaxKind.ExclamationToken: // !
                if (operandType == SyntaxKind.BoolKeyword)
                {
                    return SyntaxKind.BoolKeyword; // 逻辑非操作返回布尔类型
                }
                ReportError($"操作符 '{operatorToken.Text}' 不能用于类型 '{operandType}'", operand.GetLocation());
                break;

            case SyntaxKind.TildeToken: // ~
                if (operandType == SyntaxKind.IntKeyword)
                {
                    return SyntaxKind.IntKeyword; // 按位非操作返回整型
                }
                ReportError($"操作符 '{operatorToken.Text}' 不能用于类型 '{operandType}'", operand.GetLocation());
                break;

            case SyntaxKind.PlusPlusToken: // ++
            case SyntaxKind.MinusMinusToken: // --
                if (operandType == SyntaxKind.IntKeyword || operandType == SyntaxKind.DoubleKeyword)
                {
                    return operandType; // 自增/自减返回操作数类型
                }
                ReportError($"操作符 '{operatorToken.Text}' 不能用于类型 '{operandType}'", operand.GetLocation());
                break;

            default:
                ReportError($"未知的一元操作符: {operatorToken.Text}", operatorToken.GetLocation());
                break;
        }

        return SyntaxKind.None; // 如果类型推导失败，返回 None
    }


    private void AnalyzeWhileStatement(WhileStatementSyntax whileStmt)
    {
        // 分析条件表达式
        if (whileStmt.Condition != null)
        {
            AnalyzeExpression(whileStmt.Condition);
            var conditionType = GetExpressionType(whileStmt.Condition);
            Console.WriteLine("以下是 while 循环条件：");
            Console.WriteLine(whileStmt.Condition);
            if (conditionType != SyntaxKind.BoolKeyword)
            {
                Console.WriteLine(conditionType);
                Console.WriteLine("while 循环条件必须是布尔类型");
                ReportError("while 循环条件必须是布尔类型", whileStmt.Condition.GetLocation());
            }
        }

        // 分析循环体
        if (whileStmt.Statement == null)
        {
            Console.WriteLine("Statement is null.");
        }
        else
        {
            Console.WriteLine($"Statement type: {whileStmt.Statement.GetType()}");
            Console.WriteLine($"Statement content: {whileStmt.Statement.ToFullString()}");
        }

        var statementsInWhlie = whileStmt.Statement is BlockSyntax block
            ? block.Statements
            : whileStmt.Statement != null
                ? SyntaxFactory.List(new[] { whileStmt.Statement })
        : default(SyntaxList<StatementSyntax>);

        AnalyzeStatements(statementsInWhlie);
    }

    private void AnalyzeIfStatement(IfStatementSyntax ifStmt)
    {
        // 分析条件表达式
        if (ifStmt.Condition != null)
        {
            AnalyzeExpression(ifStmt.Condition);
            var conditionType = GetExpressionType(ifStmt.Condition);
            if (conditionType != SyntaxKind.BoolKeyword)
            {
                ReportError("if 条件必须是布尔类型", ifStmt.Condition.GetLocation());
            }
        }

        // 分析 then 分支
        AnalyzeStatements(ifStmt.Statement is BlockSyntax block ? block.Statements : SyntaxFactory.List(new[] { ifStmt.Statement }));

        // 分析 else 分支
        if (ifStmt.Else != null)
        {
            AnalyzeStatements(ifStmt.Else.Statement is BlockSyntax elseBlock
                ? elseBlock.Statements
                : SyntaxFactory.List(new[] { ifStmt.Else.Statement }));
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
