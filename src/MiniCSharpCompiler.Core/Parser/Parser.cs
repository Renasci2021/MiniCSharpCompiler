using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MiniCSharpCompiler.Core.Parser;

public class Parser(List<SyntaxToken> tokens)
{
    private readonly List<SyntaxToken> _tokens = tokens;
    private int _position = 0;

    private bool IsAtEnd => _position == _tokens.Count;
    private SyntaxToken Current => _tokens[_position];

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
        // var name = ParseQualifiedName();
        var name = ParseSimpleName();
        var semicolon = MatchToken(SyntaxKind.SemicolonToken);

        return SyntaxFactory.UsingDirective(name)
            .WithUsingKeyword(usingKeyword)
            .WithSemicolonToken(semicolon);
    }

    private MemberDeclarationSyntax ParseMemberDeclaration()
    {
        throw new NotImplementedException();
    }

    private NameSyntax ParseQualifiedName()
    {
        var left = ParseSimpleName();
        if (!Current.IsKind(SyntaxKind.DotToken))
        {
            return left;
        }

        QualifiedNameSyntax result = null!;
        while (Current.IsKind(SyntaxKind.DotToken))
        {
            var right = ParseSimpleName();
            result = SyntaxFactory.QualifiedName(left, right);
        }

        return result;
    }

    private IdentifierNameSyntax ParseSimpleName()
    {
        var identifier = MatchToken(SyntaxKind.IdentifierToken);
        return SyntaxFactory.IdentifierName(identifier.Text);
    }

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
}
