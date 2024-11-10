using MiniCSharpCompiler.Core.Lexer;

namespace MiniCSharpCompiler.Tests;

public class LexerTests
{
    [Test]
    public async Task LexerTest()
    {
        // * Get all the files in the TestFiles directory
        string[] files = Directory.GetFiles("../../../../MiniCSharpCompiler.Test/TestFiles", "*.cs");

        foreach (var file in files)
        {
            await TestFile(file);
        }
    }

    private async Task TestFile(string filePath)
    {
        string sourceCode = await File.ReadAllTextAsync(filePath);

        var lexer = new Lexer();
        var standardLexer = new StandardLexer();
        var tokens = lexer.Tokenize(sourceCode).ToList();
        var standardTokens = standardLexer.Tokenize(sourceCode).ToList();

        await Assert.That(tokens.Count).IsEqualTo(standardTokens.Count);

        for (int i = 0; i < tokens.Count; i++)
        {
            await Assert.That(tokens[i].Kind).IsEqualTo(standardTokens[i].Kind);
            await Assert.That(tokens[i].Value).IsEqualTo(standardTokens[i].Value);
        }
    }
}
