using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MiniCSharpCompiler.Core.Interfaces;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace MiniCSharpCompiler.Core.Parser;

/*
奇妙的源代码，有离谱的缩进和换行
-------------------
      int a  =   12;
   
   
   Console.WriteLine(a);
   
-------------------
*/

public class SampleParser : IParser
{

    public SyntaxTree Parse(string _)
    {
        return SyntaxTree(
CompilationUnit()
.WithMembers(
    List<MemberDeclarationSyntax>(
        new MemberDeclarationSyntax[]{
            GlobalStatement(
                LocalDeclarationStatement(
                    VariableDeclaration(
                        PredefinedType(
                            Token(
                                TriviaList(
                                    Whitespace("      ")),
                                SyntaxKind.IntKeyword,
                                TriviaList(
                                    Space))))
                    .WithVariables(
                        SingletonSeparatedList<VariableDeclaratorSyntax>(
                            VariableDeclarator(
                                Identifier(
                                    TriviaList(),
                                    "a",
                                    TriviaList(
                                        Whitespace("  "))))
                            .WithInitializer(
                                EqualsValueClause(
                                    LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        Literal(12)))
                                .WithEqualsToken(
                                    Token(
                                        TriviaList(),
                                        SyntaxKind.EqualsToken,
                                        TriviaList(
                                            Whitespace("   "))))))))
                .WithSemicolonToken(
                    Token(
                        TriviaList(),
                        SyntaxKind.SemicolonToken,
                        TriviaList(
                            LineFeed)))),
            GlobalStatement(
                ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(
                                Identifier(
                                    TriviaList(
                                        new []{
                                            Whitespace("   "),
                                            LineFeed,
                                            Whitespace("   "),
                                            LineFeed,
                                            Whitespace("   ")}),
                                    "Console",
                                    TriviaList())),
                            IdentifierName("WriteLine")))
                    .WithArgumentList(
                        ArgumentList(
                            SingletonSeparatedList<ArgumentSyntax>(
                                Argument(
                                    IdentifierName("a"))))))
                .WithSemicolonToken(
                    Token(
                        TriviaList(),
                        SyntaxKind.SemicolonToken,
                        TriviaList(
                            LineFeed))))}))
.WithEndOfFileToken(
    Token(
        TriviaList(
            Whitespace("   ")),
        SyntaxKind.EndOfFileToken,
        TriviaList()))
      );
    }
}