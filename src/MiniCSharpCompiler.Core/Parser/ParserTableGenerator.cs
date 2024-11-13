using System.Text.Json;

namespace MiniCSharpCompiler.Core.Parser;

// 用于生成分析表
public class ParserTableGenerator
{
    private Dictionary<string, string>? _terminalSymbolsMap;


    public bool IsTerminal(string symbol)
    {

        return _terminalSymbolsMap!.ContainsKey(symbol);
    }

    public void LoadTerminalSymbolsMap(Dictionary<string, string> terminalSymbolsMap)
    {
        _terminalSymbolsMap = terminalSymbolsMap;
    }

    public Dictionary<(string, string), List<string>> GenerateTable(List<GrammarRule> rules)
    {
        var firstSets = CalculateFirstSets(rules);
        var followSets = CalculateFollowSets(rules, firstSets);
        var table = new Dictionary<(string, string), List<string>>();

        foreach (var rule in rules)
        {
            string left = rule.Left;
            foreach (var production in rule.Right)
            {
                var firstSet = First(production, firstSets);

                foreach (var terminal in firstSet)
                {
                    if (terminal != "empty")
                    {
                        table[(left, terminal)] = production;
                    }
                }

                // If empty is in the first set, add follow set of the non-terminal
                if (firstSet.Contains("empty"))
                {
                    foreach (var followSymbol in followSets[left])
                    {
                        table[(left, followSymbol)] = production;
                    }
                }
            }
        }

        return table;
    }

    // 计算 First 集
    private Dictionary<string, HashSet<string>> CalculateFirstSets(List<GrammarRule> rules)
    {
        var firstSets = new Dictionary<string, HashSet<string>>();

        foreach (var rule in rules)
        {
            firstSets[rule.Left] = [];
        }

        bool changed;
        do
        {
            changed = false;

            foreach (var rule in rules)
            {
                foreach (var production in rule.Right)
                {
                    var firstSet = First(production, firstSets);
                    int initialCount = firstSets[rule.Left].Count;
                    firstSets[rule.Left].UnionWith(firstSet);

                    // If any new elements were added to the First set, set changed to true
                    if (firstSets[rule.Left].Count > initialCount)
                    {
                        changed = true;
                    }
                }
            }
        } while (changed);

        return firstSets;
    }

    // 计算 First 集
    private HashSet<string> First(List<string> symbols, Dictionary<string, HashSet<string>> firstSets)
    {
        var result = new HashSet<string>();

        foreach (var symbol in symbols)
        {
            // If symbol is a terminal or empty, add it to the result and stop
            if (IsTerminal(symbol) || symbol == "empty")
            {
                result.Add(symbol);
                break;
            }

            // Otherwise, add all non-empty symbols from the first set of the non-terminal
            if (firstSets.TryGetValue(symbol, out HashSet<string>? value))
            {
                result.UnionWith(value.Where(s => s != "empty"));

                // If empty is not in the first set of the current symbol, stop
                if (!value.Contains("empty"))
                {
                    break;
                }
            }
        }

        // If all symbols can derive empty, add empty to the result
        if (symbols.All(s => firstSets.ContainsKey(s) && firstSets[s].Contains("empty")))
        {
            result.Add("empty");
        }

        return result;
    }

// 计算 Follow 集
    private Dictionary<string, HashSet<string>> CalculateFollowSets(
        List<GrammarRule> rules, Dictionary<string, HashSet<string>> firstSets)
    {
        var followSets = new Dictionary<string, HashSet<string>>();

        // 初始化 follow 集，为每个非终结符创建空的 follow 集
        foreach (var rule in rules)
        {
            if (!followSets.ContainsKey(rule.Left))
            {
                followSets[rule.Left] = new HashSet<string>();
            }
        }

        // 为起始符号添加结束符号 $
        followSets["COMPILATION_UNIT"].Add("$");

        bool changed;
        do
        {
            changed = false;

            foreach (var rule in rules)
            {
                string left = rule.Left;

                foreach (var production in rule.Right)
                {
                    for (int i = 0; i < production.Count; i++)
                    {
                        var symbol = production[i];

                        // if (!char.IsLower(symbol[0]))
                        if (!IsTerminal(symbol))
                        {
                            var followSet = new HashSet<string>();

                            // Get first set of the remaining symbols after the current symbol
                            var suffix = production.Skip(i + 1).ToList();
                            var firstSet = First(suffix, firstSets);

                            followSet.UnionWith(firstSet.Where(s => s != "empty"));

                            // If empty is in the first set of the suffix, add follow of the left-hand side
                            if (firstSet.Contains("empty") || suffix.Count == 0)
                            {
                                followSet.UnionWith(followSets[left]);
                            }

                            if (!followSets.TryGetValue(symbol, out HashSet<string>? value))
                            {
                                value = ([]);
                                followSets[symbol] = value;
                            }

                            int initialCount = value.Count;
                            value.UnionWith(followSet);

                            if (value.Count > initialCount)
                            {
                                changed = true;
                            }
                        }
                    }
                }
            }
        } while (changed);

        return followSets;
    }

// 从 JSON 文件中加载终结符映射
    public void LoadFromJson(string jsonFilePath)
    {
        // 读取 JSON 文件内容
        string jsonContent = File.ReadAllText(jsonFilePath);

        // 使用 JsonDocument 解析 JSON
        using (JsonDocument document = JsonDocument.Parse(jsonContent))
        {
            // 获取 terminal_symbols 对应的部分
            JsonElement terminalSymbols = document.RootElement.GetProperty("terminal_symbols");

            // 遍历 JSON 中的键值对并将其填充到字典中
            foreach (var item in terminalSymbols.EnumerateObject())
            {
                // 确保值不为 null，然后再添加到字典中
                if (item.Value.ValueKind == JsonValueKind.String)
                {
                    _terminalSymbolsMap[item.Name] = item.Value.GetString() ?? string.Empty;  // 或者可以使用适当的默认值
                }
            }
        }
    }



}
