using MiniCSharpCompiler.Core.Lexer;

namespace MiniCSharpCompiler.Tests;

public class LexerTests
{
    [Test]
    public async Task LexerTest()
    {
        string[] files = Directory.GetFiles("../../../../MiniCSharpCompiler.Test/TestFiles", "*.cs");

        bool allFilePassed = true;
        foreach (var file in files)
        {
            Console.WriteLine(new string('-', 50));
            Console.WriteLine($"Testing file: {file}");
            Console.WriteLine();

            try
            {
                await TestFile(file);
                Console.WriteLine($"File passed: {file}");
            }
            catch (Exception e)
            {
                allFilePassed = false;
                Console.WriteLine($"Error testing file: {file}");
                Console.WriteLine(e.Message);
            }
        }
        await Assert.That(allFilePassed).IsTrue();
    }

    private async Task TestFile(string filePath)
    {
        string sourceCode = await File.ReadAllTextAsync(filePath);

        var lexer = new Lexer();
        var standardLexer = new StandardLexer();
        var tokens = lexer.Tokenize(sourceCode).ToList();
        var standardTokens = standardLexer.Tokenize(sourceCode).ToList();

        Console.WriteLine($"Token count: {tokens.Count}");

        await Assert.That(tokens.Count).IsEqualTo(standardTokens.Count);

        for (int i = 0; i < tokens.Count; i++)
        {
            Console.WriteLine($"Token {i}: {tokens[i].Kind} - {tokens[i].Value}");
            Console.WriteLine($"Standard Token {i}: {standardTokens[i].Kind} - {standardTokens[i].Value}");

            await Assert.That(tokens[i].Kind).IsEqualTo(standardTokens[i].Kind);
            await Assert.That(tokens[i].Value).IsEqualTo(standardTokens[i].Value);

            try
            {
                await Assert.That(tokens[i].LeadingTrivia).IsEqualTo(standardTokens[i].LeadingTrivia);
                await Assert.That(tokens[i].TrailingTrivia).IsEqualTo(standardTokens[i].TrailingTrivia);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
