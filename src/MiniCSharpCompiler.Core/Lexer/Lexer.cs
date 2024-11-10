// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp;
// using MiniCSharpCompiler.Core.Interfaces;

// namespace MiniCSharpCompiler.Core.Lexer;

// public class Lexer : ILexer
// {
//     public IEnumerable<Token> Tokenize(string sourceCode)
//     {
//         // TODO: Implement the lexer

//         Token[] tokens = [
//             new Token(SyntaxKind.IntKeyword, "int"),
//             new Token(SyntaxKind.IdentifierToken, "a"),
//             new Token(SyntaxKind.EqualsToken, "=")
//             {
//                 // 添加前导和尾随 Trivia
//                 LeadingTrivia = new SyntaxTriviaList(SyntaxFactory.Whitespace("  ")),
//                 TrailingTrivia = new SyntaxTriviaList(SyntaxFactory.LineFeed)
//             },
//             new Token(SyntaxKind.NumericLiteralToken, "12"),
//             new Token(SyntaxKind.SemicolonToken, ";"),
//             new Token(SyntaxKind.IdentifierToken, "Console"),
//             new Token(SyntaxKind.DotToken, "."),
//             new Token(SyntaxKind.IdentifierToken, "WriteLine"),
//             new Token(SyntaxKind.OpenParenToken, "("),
//             new Token(SyntaxKind.IdentifierToken, "a"),
//             new Token(SyntaxKind.CloseParenToken, ")"),
//             new Token(SyntaxKind.SemicolonToken, ";")
//         ];

//         return tokens;
//     }
// }


using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MiniCSharpCompiler.Core.Interfaces;

namespace MiniCSharpCompiler.Core.Lexer
{
    public class Lexer : ILexer
    {
        public IEnumerable<Token> Tokenize(string sourceCode)
        {
            var tokens = new List<Token>();

            var tokenDefinitions = new List<(Regex regex, Func<string, Token> createToken)>
            {
                (new Regex(@"^ "), text => new Token(SyntaxKind.WhitespaceTrivia,text)), // Skip whitespace
                (new Regex(@"^\n"), text => new Token(SyntaxKind.EndOfLineTrivia, text)), // Skip new line
                (new Regex(@"^\d+"), text => new Token(SyntaxKind.NumericLiteralToken, text)),
                (new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*"), text =>
                {
                    if (Keywords.KeywordDictionary.TryGetValue(text, out var kind))
                    {
                        return new Token(kind, text);
                    }
                    return new Token(SyntaxKind.IdentifierToken, text);
                }),
                (new Regex(@"^@?""[^""]*"""), text => 
                {
                    if (text.StartsWith("@"))
                    {
                        return new Token(SyntaxKind.StringLiteralToken, text.Substring(2, text.Length - 3));
                    }
                    return new Token(SyntaxKind.StringLiteralToken, text.Substring(1, text.Length - 2));
                }),
                (new Regex(@"^//.*"), text => new Token(SyntaxKind. SingleLineCommentTrivia, text)), // Skip single-line comments
                (new Regex(@"^/\*.*?\*/", RegexOptions.Singleline), text => new Token(SyntaxKind.MultiLineCommentTrivia, text)), // Skip multi-line comments
                (new Regex(@"^;"), text => new Token(SyntaxKind.SemicolonToken, text)),
                (new Regex(@"^:"), text => new Token(SyntaxKind.ColonToken, text)),
                (new Regex(@"^,"), text => new Token(SyntaxKind.CommaToken, text)),
                (new Regex(@"^\."), text => new Token(SyntaxKind.DotToken, text)),
                (new Regex(@"^<="), text => new Token(SyntaxKind.LessThanEqualsToken, text)),
                (new Regex(@"^>="), text => new Token(SyntaxKind.GreaterThanEqualsToken, text)),
                (new Regex(@"^=="), text => new Token(SyntaxKind.EqualsEqualsToken, text)),
                (new Regex(@"^="), text => new Token(SyntaxKind.EqualsToken, text)),
                (new Regex(@"^\("), text => new Token(SyntaxKind.OpenParenToken, text)),
                (new Regex(@"^\)"), text => new Token(SyntaxKind.CloseParenToken, text)),
                (new Regex(@"^\{"), text => new Token(SyntaxKind.OpenBraceToken, text)),
                (new Regex(@"^\}"), text => new Token(SyntaxKind.CloseBraceToken, text)),
                (new Regex(@"^\["), text => new Token(SyntaxKind.OpenBracketToken, text)),
                (new Regex(@"^\]"), text => new Token(SyntaxKind.CloseBracketToken, text)),
                (new Regex(@"^>"), text => new Token(SyntaxKind.GreaterThanToken, text)),
                (new Regex(@"^<"), text => new Token(SyntaxKind.LessThanToken, text)),
                (new Regex(@"^\+"), text => new Token(SyntaxKind.PlusToken, text)),
                (new Regex(@"^-"), text => new Token(SyntaxKind.MinusToken, text)),
                (new Regex(@"^\*"), text => new Token(SyntaxKind.AsteriskToken, text)),
                (new Regex(@"^\/"), text => new Token(SyntaxKind.SlashToken, text)),
                (new Regex(@"^!="), text => new Token(SyntaxKind.ExclamationEqualsToken, text)),
                (new Regex(@"^&&"), text => new Token(SyntaxKind.AmpersandAmpersandToken, text)),
                (new Regex(@"^\|\|"), text => new Token(SyntaxKind.BarBarToken, text)),
                (new Regex(@"^!"), text => new Token(SyntaxKind.ExclamationToken, text)),

                // Add more token definitions as needed
            };

            int position = 0;

            while (position < sourceCode.Length)
            {
                var match = false;

                foreach (var (regex, createToken) in tokenDefinitions)
                {
                    var matchResult = regex.Match(sourceCode.Substring(position));
                    if (matchResult.Success)
                    {
                        if (createToken != null)
                        {
                            var token = createToken(matchResult.Value);
                            // if (token.Kind != None)
                            // {
                            //     tokens.Add(token);
                            // }
                            tokens.Add(token);
                        }
                        position += matchResult.Length;
                        match = true;
                        break;
                    }
                }

                if (!match)
                {
                    // Handle unexpected characters
                    position++;
                }
            }
            tokens.Add(new Token(SyntaxKind.EndOfFileToken, ""));
            return tokens;
        }
    }
}