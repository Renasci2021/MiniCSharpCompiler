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
            var leadingTrivias = new List<SyntaxTrivia>();
            var trailingTrivias = new List<SyntaxTrivia>();
            var dollor=false;
            var braced=false;
            var openbrace=false;
            var emptyList=false;
            var leadingTriviaDefinitions = new List<(Regex regex, Action<string> createTrivia)>
            {
                (new Regex(@"^ +"), text => leadingTrivias.Add(SyntaxFactory.Whitespace(text))), // Skip whitespace
                (new Regex(@"^\r\n"), text => leadingTrivias.Add(SyntaxFactory.EndOfLine(text))), // Skip new line
                (new Regex(@"^\/\/.*\n"), text =>{leadingTrivias.Add(SyntaxFactory.Comment(text.Substring(0,text.Length-2)));leadingTrivias.Add(SyntaxFactory.EndOfLine("\r\n"));}), // Skip single-line comments
                (new Regex(@"^/\*.*?\*/", RegexOptions.Singleline), text =>leadingTrivias.Add(SyntaxFactory.Comment(text))), // Skip multi-line comments
            };
            var trailingTriviaDefinitions = new List<(Regex regex, Action<string> createTrivia)>
            {
                (new Regex(@"^ +"), text => trailingTrivias.Add(SyntaxFactory.Whitespace(text))), // Skip whitespace
                (new Regex(@"^\r\n"), text => trailingTrivias.Add(SyntaxFactory.EndOfLine(text))), // Skip new line
                (new Regex(@"^\/\/.*\n"), text =>{trailingTrivias.Add(SyntaxFactory.Comment(text.Substring(0,text.Length-2)));}), // Skip single-line comments
                (new Regex(@"^/\*.*?\*/", RegexOptions.Singleline), text =>trailingTrivias.Add(SyntaxFactory.Comment(text))), // Skip multi-line comments
            };
            var tokenDefinitions = new List<(Regex regex, Func<string, Token> createToken)>
            {
                // (new Regex(@"^ "), text => new Token(SyntaxKind.WhitespaceTrivia,text)), // Skip whitespace
                // (new Regex(@"^\n"), text => new Token(SyntaxKind.EndOfLineTrivia, text)), // Skip new line
                (new Regex(@"^\d+"), text => new Token(SyntaxKind.NumericLiteralToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*"), text =>
                {
                    if (Keywords.KeywordDictionary.TryGetValue(text, out var kind))
                    {
                        return new Token(kind, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        };
                    }
                    return new Token(SyntaxKind.IdentifierToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        };
                }),
                (new Regex(@"^\$"""), text => new Token(SyntaxKind.InterpolatedStringStartToken, text)
                {
                    
                    LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                    TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                }),
                (new Regex(@"^'.'"), text => new Token(SyntaxKind.CharacterLiteralToken, text.Substring(1,1)){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^@?""[^""]*"""), text =>
                {
                    if (text.StartsWith("@"))
                    {
                        return new Token(SyntaxKind.StringLiteralToken, text.Substring(2, text.Length - 3)){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        };
                    }
                    return new Token(SyntaxKind.StringLiteralToken, text.Substring(1, text.Length - 2)){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        };
                }),
                (new Regex(@"^"""), text => new Token(SyntaxKind.InterpolatedStringEndToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^\{"), text => new Token(SyntaxKind.OpenBraceToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^\}"), text => new Token(SyntaxKind.CloseBraceToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^[^""]*?\{"), text => new Token(SyntaxKind.InterpolatedStringTextToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),        
                // (new Regex(@"^//.*"), text => new Token(SyntaxKind. SingleLineCommentTrivia, text)), // Skip single-line comments
                // (new Regex(@"^/\*.*?\*/", RegexOptions.Singleline), text => new Token(SyntaxKind.MultiLineCommentTrivia, text)), // Skip multi-line comments
                (new Regex(@"^;"), text => new Token(SyntaxKind.SemicolonToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^:"), text => new Token(SyntaxKind.ColonToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^,"), text => new Token(SyntaxKind.CommaToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^\."), text => new Token(SyntaxKind.DotToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^<="), text => new Token(SyntaxKind.LessThanEqualsToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^>="), text => new Token(SyntaxKind.GreaterThanEqualsToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^=="), text => new Token(SyntaxKind.EqualsEqualsToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^="), text => new Token(SyntaxKind.EqualsToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^\("), text => new Token(SyntaxKind.OpenParenToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^\)"), text => new Token(SyntaxKind.CloseParenToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                
                (new Regex(@"^\["), text => new Token(SyntaxKind.OpenBracketToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^\]"), text => new Token(SyntaxKind.CloseBracketToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^>"), text => new Token(SyntaxKind.GreaterThanToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^<"), text => new Token(SyntaxKind.LessThanToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^\+\+"), text => new Token(SyntaxKind.PlusPlusToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^\+"), text => new Token(SyntaxKind.PlusToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                        (new Regex(@"^--"), text => new Token(SyntaxKind.MinusMinusToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^-"), text => new Token(SyntaxKind.MinusToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                
                (new Regex(@"^\*"), text => new Token(SyntaxKind.AsteriskToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^\/"), text => new Token(SyntaxKind.SlashToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^!="), text => new Token(SyntaxKind.ExclamationEqualsToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^&&"), text => new Token(SyntaxKind.AmpersandAmpersandToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^\|\|"), text => new Token(SyntaxKind.BarBarToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),
                (new Regex(@"^!"), text => new Token(SyntaxKind.ExclamationToken, text){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        }),

                // Add more token definitions as needed
            };

            int position = 0;

            while (position < sourceCode.Length)
            {
                //leading trivia
                var match = false;
                // List<string> stringList = new List<string>();
                var mark = true;
                while (mark)
                {
                    mark = false;
                    foreach (var (regex, createTrivia) in leadingTriviaDefinitions)
                    {
                        var matchResult = regex.Match(sourceCode.Substring(position));
                        if (matchResult.Success)
                        {
                            createTrivia(matchResult.Value);
                            position += matchResult.Length;
                            // match = true;
                            mark = true;
                            break;
                        }
                    }
                }
                // var matched=false;
                foreach (var (regex, createToken) in tokenDefinitions)
                {
                    var matchResult = regex.Match(sourceCode.Substring(position));
                    // Console.WriteLine(sourceCode.Substring(position));
                    if (matchResult.Success)
                    {
                        // matched=true;
                        if(matchResult.Value == "["&&sourceCode[position+1]==']')
                        {
                            emptyList = true;
                        }
                        if(dollor){
                            if(matchResult.Value == @"""")
                            {
                                dollor=false;
                            }
                            if(!braced&&dollor)
                            {
                                if(matchResult.Value[matchResult.Value.Length-1]!='{')
                                    continue;
                                else{
                                    position--;
                                    braced=true;
                                    openbrace = true;
                                }
                            }
                            if(matchResult.Value == "}")
                            {
                                braced=false;
                            }
                        }
                        else{
                            if(matchResult.Value[matchResult.Value.Length-1]=='{'&&matchResult.Value.Length>1)
                            {
                                continue;
                            }
                        }
                        if(matchResult.Value == "$\"")
                        {
                            dollor = true;
                        }
                        
                        position += matchResult.Length;
                        //trailing trivia
                        mark = true;
                        while (mark)
                        {
                            mark = false;
                            foreach (var (regex1, createTrivia) in trailingTriviaDefinitions)
                            {
                                // Console.WriteLine(sourceCode.Substring(position));
                                var str= sourceCode.Substring(position);
                                var matchResult1 = regex1.Match(sourceCode.Substring(position));
                                if (matchResult1.Success)
                                {
                                    createTrivia(matchResult1.Value);
                                    if(matchResult1.Value.Length>1&&matchResult1.Value[1] == '/')
                                        position-=2;
                                    position += matchResult1.Length;
                                    match = true;
                                    if(matchResult1.Value != "\r\n")
                                        mark = true;
                                    break;
                                }
                            }
                        }
                        var token = createToken(openbrace?matchResult.Value.Substring(0,matchResult.Value.Length-1):matchResult.Value);
                        tokens.Add(token);
                        openbrace = false;
                        if(emptyList)
                        {
                            tokens.Add(new Token(SyntaxKind.OmittedArraySizeExpressionToken, ""));
                            emptyList = false;
                        }
                        match = true;
                        break;
                    }
                }

                if (!match)
                {
                    // Handle unexpected characters
                    
                        tokens.Add(new Token(SyntaxKind.EndOfLineTrivia, ""){
                            LeadingTrivia = new SyntaxTriviaList(leadingTrivias),
                            TrailingTrivia = new SyntaxTriviaList(trailingTrivias)
                        });
                    
                    position++;
                }
                leadingTrivias.Clear();
                trailingTrivias.Clear();
            }
            tokens.Add(new Token(SyntaxKind.EndOfFileToken, ""));
            return tokens;
        }
    }
}