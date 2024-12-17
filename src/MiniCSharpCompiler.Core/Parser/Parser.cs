using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MiniCSharpCompiler.Core.Parser;

public class Parser(List<SyntaxToken> tokens)
{
    private readonly List<SyntaxToken> _tokens = tokens;
    private int _position = 0;

    private bool IsAtEnd => Current.IsKind(SyntaxKind.EndOfFileToken); // _position == _tokens.Count;
    private SyntaxToken Current => _tokens[_position];

    private SyntaxToken MatchToken(SyntaxKind kind)
    {
        if (Current.IsKind(kind))
        {
            var token = Current;
            _position++;
            return token;
        }

        throw new Exception($"Expected token of kind {kind}, but got {Current.Text}.");
    }

    public SyntaxTree Parse()
    {
        var compilationUnit = ParseCompilationUnit();
        return SyntaxFactory.SyntaxTree(compilationUnit);
    }

    private CompilationUnitSyntax ParseCompilationUnit()
    {
        var usings = new List<UsingDirectiveSyntax>();
        var members = new List<MemberDeclarationSyntax>();

        while (Current.IsKind(SyntaxKind.UsingKeyword))
        {
            usings.Add(ParseUsingDirective());
        }

        while (!IsAtEnd)
        {
            members.Add(ParseMemberDeclaration());
        }

        return SyntaxFactory.CompilationUnit()
            .WithUsings(SyntaxFactory.List(usings))
            .WithMembers(SyntaxFactory.List(members));
    }

    private UsingDirectiveSyntax ParseUsingDirective()
    {
        var usingKeyword = MatchToken(SyntaxKind.UsingKeyword);
        var name = ParseQualifiedName();
        var semicolon = MatchToken(SyntaxKind.SemicolonToken);

        return SyntaxFactory.UsingDirective(name)
            .WithUsingKeyword(usingKeyword)
            .WithSemicolonToken(semicolon);
    }

    /// <summary>
    /// 并不严格返回QualifiedName。
    /// 若无DotToken，则返回一个IdentifierName。
    /// </summary>
    private NameSyntax ParseQualifiedName()
    {
        var left = ParseSimpleName();
        if (!Current.IsKind(SyntaxKind.DotToken))
        {
            return left;
        }

        MatchToken(SyntaxKind.DotToken);
        IdentifierNameSyntax right = ParseSimpleName();
        QualifiedNameSyntax result = SyntaxFactory.QualifiedName(left, right);
        while (Current.IsKind(SyntaxKind.DotToken))
        {
            MatchToken(SyntaxKind.DotToken); // 赵培源修改
            right = ParseSimpleName();
            result = SyntaxFactory.QualifiedName(result, right);
        }

        return result;
    }

    private IdentifierNameSyntax ParseSimpleName()
    {
        var identifier = MatchToken(SyntaxKind.IdentifierToken);
        return SyntaxFactory.IdentifierName(identifier.Text);
    }

    private MemberDeclarationSyntax ParseMemberDeclaration()
    {
        //throw new NotImplementedException();
        var modifiers = ParseModifiers();
        var classDeclaration = ParseClassDeclaration(modifiers);
        return classDeclaration;
    }

    private SyntaxTokenList ParseModifiers() // TODO
    {
        List<SyntaxKind> allowed = [SyntaxKind.PublicKeyword, SyntaxKind.PrivateKeyword, SyntaxKind.StaticKeyword];

        SyntaxTokenList modifiers = new SyntaxTokenList();
        while (allowed.Contains(Current.Kind()))
        {
            modifiers.Add(Current);
            _position++;
        }
        return modifiers;
    }

    private MemberDeclarationSyntax ParseClassDeclaration(SyntaxTokenList modifiers) // TODO
    {
        var keyword = MatchToken(SyntaxKind.ClassKeyword);
        var name = MatchToken(SyntaxKind.IdentifierToken);
        TypeParameterListSyntax typeParamList = null;
        if (Current.IsKind(SyntaxKind.LessThanToken)) // is a template
        {
            ;
        }
        var openBrace = MatchToken(SyntaxKind.OpenBraceToken);
        SyntaxList<MemberDeclarationSyntax> members = [];// parse members
        var closeBrace = MatchToken(SyntaxKind.CloseBraceToken);
        var semicolon = MatchToken(SyntaxKind.SemicolonToken);
        return SyntaxFactory.ClassDeclaration(default, modifiers, keyword, name, typeParamList, null, default, openBrace, members, closeBrace, semicolon);
    }
}
