using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MiniCSharpCompiler.Core.Interfaces;
using MiniCSharpCompiler.Core.Lexer;
using MiniCSharpCompiler.Core.Parser;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.Json;
using System.Linq;
using System.Reflection;





namespace MiniCSharpCompiler.Core.Parser;
public class Parser : IParser
{

    private Lexer.Lexer? _lexer;
    private Dictionary<(string, string), List<string>>? _parseTable;

    private Dictionary<string, string> _terminalSymbolsMap = [];


    public static Parser CreateParser()
    {
        var rules = GrammarConfigLoader.LoadFromEmbeddedResource("MiniCSharpCompiler.Core.Config.grammar_rules.json");

        ParserTableGenerator parserTableGenerator = new ParserTableGenerator();

        Parser parser = new Parser();
        parser.LoadFromJson("MiniCSharpCompiler.Core.Config.terminal_symbols.json");
        parserTableGenerator.LoadTerminalSymbolsMap(parser._terminalSymbolsMap);

        var table = parserTableGenerator.GenerateTable(rules);
        parser.LoadParseTable(table);
        parser.PrintParseTable();
        return parser;
    }



    public void LoadParseTable(Dictionary<(string, string), List<string>> parseTable)
    {
        _parseTable = parseTable;
    }

    public void LoadLexer(Lexer.Lexer lexer)
    {
        _lexer = lexer;
    }


    public void LoadTerminalSymbolsMap(Dictionary<string, string> terminalSymbolsMap)
    {
        _terminalSymbolsMap = terminalSymbolsMap;
    }

    public void Init_load(Lexer.Lexer lexer, Dictionary<(string, string), List<string>> parseTable, Dictionary<string, string> terminalSymbolsMap)
    {
        _lexer = lexer;
        _parseTable = parseTable;
        _terminalSymbolsMap = terminalSymbolsMap;
    }

    public void LoadFromJson(string resourceName)
    {
        // 获取当前程序集
        var assembly = Assembly.GetExecutingAssembly();

        // 获取嵌入资源流
        using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                throw new FileNotFoundException($"Resource '{resourceName}' not found.");
            }

            // 读取 JSON 内容
            using (StreamReader reader = new StreamReader(stream))
            {
                string jsonContent = reader.ReadToEnd();

                // 使用 JsonDocument 解析 JSON
                using (JsonDocument document = JsonDocument.Parse(jsonContent))
                {
                    // 获取 "terminal_symbols" 部分
                    JsonElement terminalSymbols = document.RootElement.GetProperty("terminal_symbols");

                    // 遍历 JSON 对象并将数据填充到字典中
                    foreach (var item in terminalSymbols.EnumerateObject())
                    {
                        // 确保值是字符串类型
                        if (item.Value.ValueKind == JsonValueKind.String)
                        {
                            _terminalSymbolsMap[item.Name] = item.Value.GetString() ?? string.Empty;
                        }
                    }
                }
            }
        }
    }



    public SyntaxTree Parse(IEnumerable<Token> tokens)
    {
        var tokenList = tokens.ToList();
        var currentTokenIndex = 0;

        // 初始化栈，开始符号是文法的开始符号 (假设是 "S")
        var stack = new Stack<string>();
        stack.Push("COMPILATION_UNIT");

        // 使用 Roslyn 构造语法树根节点
        var syntaxTreeRoot = SyntaxFactory.CompilationUnit();

        // 当前节点，使用语法节点
        var currentNode = syntaxTreeRoot;

        // 解析过程
        while (stack.Count > 0)
        {
            var stackTop = stack.Peek();
            Console.WriteLine("stackTop: " + stackTop);

            var currentNode_tree = SyntaxFactory.SyntaxTree(currentNode);
            Console.WriteLine("currentNode_tree: " + currentNode_tree);
            SyntaxPrinter_inCore.PrintSyntaxTree(currentNode_tree, printTrivia: true);

            if (IsTerminal(stackTop)) // 如果栈顶是终结符
            {
                if (currentTokenIndex >= tokenList.Count)
                    throw new InvalidOperationException("Unexpected end of input");

                var currentToken = tokenList[currentTokenIndex];

                if (ConvertStringToSyntaxKind(stackTop) == currentToken.Kind)
                {
                    // TODO: 匹配成功，构造叶子节点

                    currentTokenIndex++;
                    stack.Pop();
                }
                else
                {
                    throw new InvalidOperationException($"Syntax error: expected '{stackTop}' but found '{currentToken.Value}'");
                }
            }
            else // 如果栈顶是非终结符
            {
                var currentToken = tokenList[currentTokenIndex];
                var productionRule = GetProductionRule(stackTop, currentToken.Kind.ToString());


                if (productionRule != null && productionRule.Count != 0)
                {
                    // 获取产生式规则，可能有多个选项
                    stack.Pop();

                    // 将产生式右侧的符号按逆序推入栈中
                    for (int i = productionRule.Count - 1; i >= 0; i--)
                    {
                        stack.Push(productionRule[i]);
                    }

                    // TODO: 构建语法节点（非终结符节点）

                }
                else
                {
                    throw new InvalidOperationException($"Syntax error: no production rule for {stackTop} and {currentToken.Kind}");
                }
            }
        }

        // 如果已经解析完所有token，但栈中仍有符号，说明解析失败
        if (currentTokenIndex < tokenList.Count)
        {
            throw new InvalidOperationException("Unexpected extra tokens in input");
        }

        // 返回构建的语法树
        return SyntaxFactory.SyntaxTree(syntaxTreeRoot);
    }

    // 从解析表获取对应的产生式


    public SyntaxTree Parse(string sourceCode)
    {
        return Parse(new Lexer.Lexer(), sourceCode);
    }

    public SyntaxTree Parse(ILexer lexer, string sourceCode)
    {
        return Parse(lexer.Tokenize(sourceCode));
    }


    // 从解析表获取对应的产生式
    private List<string>? GetProductionRule(string nonTerminal, string terminal)
    {
        var key = (nonTerminal, terminal);
        return _parseTable?.GetValueOrDefault(key) ?? new List<string>();
    }

    // 判断是否为终结符 
    private bool IsTerminal(string symbol)
    {

        return _terminalSymbolsMap!.ContainsKey(symbol);
    }


// 将字符串转换为 SyntaxKind 枚举
    public static SyntaxKind ConvertStringToSyntaxKind(string syntaxKindString)
    {
        // 转换字符串为大写并尝试解析为 SyntaxKind 枚举
        if (Enum.TryParse(syntaxKindString, out SyntaxKind syntaxKind))
        {
            return syntaxKind;
        }
        else
        {
            throw new ArgumentException($"Invalid SyntaxKind string: {syntaxKindString}", nameof(syntaxKindString));
        }
    }


    // 打印加载的数据
    public void PrintTerminalSymbols()
    {
        Console.WriteLine("Terminal Symbols:");
        foreach (var item in _terminalSymbolsMap)
        {
            Console.WriteLine($"{item.Key}: {item.Value}");
        }
    }
    
// 打印解析表
    public void PrintParseTable()
    {
        if (_parseTable == null || _parseTable.Count == 0)
        {
            Console.WriteLine("Parse table is empty or not initialized.");
            return;
        }

        foreach (var entry in _parseTable)
        {
            var key = entry.Key;  // (nonTerminal, terminal)
            var productionRules = entry.Value;  // List<string> representing production rules

            // Print the key (non-terminal, terminal pair)
            Console.WriteLine($"Key: ({key.Item1}, {key.Item2})");

            // Print the corresponding production rules
            Console.WriteLine("Production Rules:");
            foreach (var rule in productionRules)
            {
                Console.WriteLine($"  - {rule}");
            }

            Console.WriteLine();  // Add a blank line between entries for readability
        }
    }

}
