using System.Reflection;
using System.Text.Json;

namespace MiniCSharpCompiler.Core.Parser;
public  class GrammarConfigLoader
{
    public static List<GrammarRule> LoadFromEmbeddedResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new Exception($"Resource '{resourceName}' not found");

        using var reader = new StreamReader(stream);
        var jsonContent = reader.ReadToEnd();

        using var jsonDocument = JsonDocument.Parse(jsonContent);
        var grammarRules = new List<GrammarRule>();

        foreach (var element in jsonDocument.RootElement.EnumerateArray())
        {
            var left = element.GetProperty("left").GetString();
            var right = new List<List<string>>();
            foreach (var rightElement in element.GetProperty("right").EnumerateArray())
            {
                var production = new List<string>();
                foreach (var symbol in rightElement.EnumerateArray())
                {
                    production.Add(symbol.GetString()!);
                }
                right.Add(production);
            }
            var grammarRule = new GrammarRule(left!, right);
            grammarRules.Add(grammarRule);
        }

        return grammarRules;
    }

    
}
