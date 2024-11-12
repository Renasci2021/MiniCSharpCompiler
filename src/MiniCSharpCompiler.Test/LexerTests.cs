using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

            checkTrivia(tokens[i].LeadingTrivia, standardTokens[i].LeadingTrivia);
            checkTrivia(tokens[i].TrailingTrivia, standardTokens[i].TrailingTrivia);

            async void checkTrivia(SyntaxTriviaList trivia, SyntaxTriviaList standardTrivia)
            {
                await Assert.That(trivia.Count).IsEqualTo(standardTrivia.Count);

                for (int j = 0; j < trivia.Count; j++)
                {
                    try
                    {
                        await Assert.That(trivia[j].Kind()).IsEqualTo(standardTrivia[j].Kind());
                        await Assert.That(trivia[j].Span.Length).IsEqualTo(standardTrivia[j].Span.Length);
                    }
                    catch
                    {
                        Console.WriteLine($"Trivia {j}: {trivia[j].Kind()} - {trivia[j].Span.Length}");
                        Console.WriteLine($"Standard Trivia {j}: {standardTrivia[j].Kind()} - {standardTrivia[j].Span.Length}");
                        throw new Exception($"Error at trivia {j} of token {i}");
                    }
                }
            }
        }
    }
}
