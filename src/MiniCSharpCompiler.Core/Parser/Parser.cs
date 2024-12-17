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
        var modifiers = ParseModifiers();

        if (Current.IsKind(SyntaxKind.ClassKeyword))
        {
            return ParseClassDeclaration(modifiers);
        }

        TypeSyntax type = ParseType();
        var name = MatchToken(SyntaxKind.IdentifierToken);
        if (Current.IsKind(SyntaxKind.EqualsToken))
        {
            return ParseFieldDeclaration(modifiers, type, name);
        }
        if (Current.IsKind(SyntaxKind.OpenParenToken))
        {
            return ParseMethodDeclaration(modifiers, type, name);
        }

        throw new NotImplementedException("Only field, method, property are supported");
    }

    private TypeSyntax ParseType()
    {
        var currentKind = Current.Kind();
        if (SyntaxFacts.IsPredefinedType(currentKind))
        {
            var type = SyntaxFactory.PredefinedType(Current);
            _position++;
            return type;
        }
        if (currentKind == SyntaxKind.IdentifierToken)
        {
            return SyntaxFactory.IdentifierName(MatchToken(SyntaxKind.IdentifierToken));
        }
        throw new NotImplementedException("Only predefiend type and identifier supported");
    }

    private SyntaxTokenList ParseModifiers()
    {
        List<SyntaxKind> allowed = [SyntaxKind.PublicKeyword, SyntaxKind.PrivateKeyword, SyntaxKind.StaticKeyword];

        List<SyntaxToken> modifiers = [];
        while (allowed.Contains(Current.Kind()))
        {
            modifiers.Add(Current);
            _position++;
        }
        return new SyntaxTokenList(modifiers);
    }

    private ClassDeclarationSyntax ParseClassDeclaration(SyntaxTokenList modifiers) // TODO
    {
        var keyword = MatchToken(SyntaxKind.ClassKeyword);
        var name = MatchToken(SyntaxKind.IdentifierToken);
        TypeParameterListSyntax? typeParamList = null;

        // TODO: class templates
        if (Current.IsKind(SyntaxKind.LessThanToken))
        {
            throw new NotImplementedException("Class template");
            //var lessThan = MatchToken(SyntaxKind.LessThanToken);
            //var param = SyntaxFactory.TypeParameter(MatchToken(SyntaxKind.IdentifierToken));
            //SeparatedSyntaxList<TypeParameterSyntax> typeParams = [];
            //typeParamList = SyntaxFactory.TypeParameterList(lessThan, typeParams, MatchToken(SyntaxKind.GreaterThanToken));
        }

        var lbrace = MatchToken(SyntaxKind.OpenBraceToken);
        List<MemberDeclarationSyntax> members = [];// parse members
        while (!Current.IsKind(SyntaxKind.CloseBraceToken) && !IsAtEnd)
        {
            members.Add(ParseMemberDeclaration());
        }
        var rbrace = MatchToken(SyntaxKind.CloseBraceToken);
        var semicolon = MatchToken(SyntaxKind.SemicolonToken);
        return SyntaxFactory.ClassDeclaration(
            attributeLists: default,
            modifiers: modifiers,
            keyword: keyword,
            identifier: name,
            typeParameterList: typeParamList,
            baseList: null,
            constraintClauses: default,
            openBraceToken: lbrace,
            members: new SyntaxList<MemberDeclarationSyntax>(members),
            closeBraceToken: rbrace,
            semicolonToken: semicolon);
    }

    private FieldDeclarationSyntax ParseFieldDeclaration(SyntaxTokenList modifiers, TypeSyntax type, SyntaxToken name)
    {
        var equals = MatchToken(SyntaxKind.EqualsToken);
        SeparatedSyntaxList<VariableDeclaratorSyntax> decls = [];
        while (!Current.IsKind(SyntaxKind.SemicolonToken))
        {
            var expression = ParseExpression();
            var declarator = SyntaxFactory.VariableDeclarator(
                name, null,
                SyntaxFactory.EqualsValueClause(equals, expression)
                );
            decls.Add(declarator);
        }
        var declaration = SyntaxFactory.VariableDeclaration(type, decls);
        return SyntaxFactory.FieldDeclaration(default, modifiers, declaration, MatchToken(SyntaxKind.SemicolonToken));
    }

    private MethodDeclarationSyntax ParseMethodDeclaration(SyntaxTokenList modifiers, TypeSyntax type, SyntaxToken name)
    {
        var paramList = ParseParameterList();
        var body = ParseBlock();
        return SyntaxFactory.MethodDeclaration(
            attributeLists: default,
            modifiers: modifiers,
            returnType: type,
            explicitInterfaceSpecifier: null,
            identifier: name,
            typeParameterList: null,
            parameterList: paramList,
            constraintClauses: default,
            body: body,
            semicolonToken: MatchToken(SyntaxKind.SemicolonToken));
    }

    private ParameterListSyntax ParseParameterList()
    {
        var lparen = MatchToken(SyntaxKind.OpenParenToken);
        List<ParameterSyntax> parameters = [];
        List<SyntaxToken> commas = [];
        
        var type = ParseType();
        var name = MatchToken(SyntaxKind.IdentifierToken);
        parameters.Add(SyntaxFactory.Parameter(
            attributeLists: default,
            modifiers: default,
            type: type,
            identifier: name,
            @default: null));
        while (Current.IsKind(SyntaxKind.CommaToken))
        {
            commas.Add(MatchToken(SyntaxKind.CommaToken));
            type = ParseType();
            name = MatchToken(SyntaxKind.IdentifierToken);
            parameters.Add(SyntaxFactory.Parameter(
                attributeLists: default,
                modifiers: default,
                type: type,
                identifier: name,
                @default: null));
        }
        return SyntaxFactory.ParameterList(
            lparen, 
            (new SeparatedSyntaxList<ParameterSyntax>()).AddRange(parameters), 
            MatchToken(SyntaxKind.CloseParenToken));
    }

    private BlockSyntax ParseBlock()
    {
        var lparen = MatchToken(SyntaxKind.OpenBraceToken);
        List<StatementSyntax> statements = [];
        while (!Current.IsKind(SyntaxKind.CloseBraceToken) && !IsAtEnd)
        {
            statements.Add(ParseStatement());
        }
        return SyntaxFactory.Block(
            lparen,
            new SyntaxList<StatementSyntax>(statements),
            MatchToken(SyntaxKind.CloseBraceToken));
    }

    private StatementSyntax ParseStatement()
    {
        throw new NotImplementedException();
    }

    private ExpressionSyntax ParseExpression()
    {
        throw new NotImplementedException();
    }
}
