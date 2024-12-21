using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System.Linq.Expressions;

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

    private SyntaxKind PeekTokenKind(int k)
    {
        if (_position + k >= _tokens.Count)
        {
            throw new IndexOutOfRangeException($"Expect {k} more tokens");
        }
        return _tokens[_position + k].Kind();
    }

    public SyntaxTree Parse()
    {
        var compilationUnit = ParseCompilationUnit();
        return SyntaxFactory.SyntaxTree(compilationUnit);
    }

    /* ************************************************************ */
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
        List<SyntaxKind> modifierKinds = [SyntaxKind.PublicKeyword, SyntaxKind.PrivateKeyword, SyntaxKind.StaticKeyword];
        int k = 0;
        while (modifierKinds.Contains(PeekTokenKind(k))) k++;
        if (PeekTokenKind(k) is SyntaxKind.ClassKeyword)
        {
            return ParseClassDeclaration();
        }

        try
        {
            while (true)
            {
                switch (PeekTokenKind(k++))
                {
                    case SyntaxKind.EqualsToken:
                        return ParseFieldDeclaration();
                    case SyntaxKind.OpenParenToken:
                        return ParseMethodDeclaration();
                    case SyntaxKind.OpenBraceToken:
                        return ParsePropertyDeclaration();
                    default:
                        continue;
                }
            }
        }
        catch (IndexOutOfRangeException)
        {
            throw;
        }

        throw new NotImplementedException("Only field, method, property are supported");
    }

    /* ************************************************************ */

    private ClassDeclarationSyntax ParseClassDeclaration() // TODO
    {
        var modifiers = ParseModifiers([SyntaxKind.PublicKeyword]);
        var keyword = MatchToken(SyntaxKind.ClassKeyword);
        var name = MatchToken(SyntaxKind.IdentifierToken);

        TypeParameterListSyntax? typeParamList = null; // TODO: class templates
        if (Current.IsKind(SyntaxKind.LessThanToken))
        {
            typeParamList = ParseTypeParameterList();
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

    static bool IsModifier(SyntaxKind kind)
    {
        return (
            kind is SyntaxKind.PublicKeyword
            || kind is SyntaxKind.PrivateKeyword
            || kind is SyntaxKind.StaticKeyword
            );
    }
    private SyntaxTokenList ParseModifiers(List<SyntaxKind> allowed)
    { 
        List<SyntaxToken> modifiers = [];
        while (true)
        {
            var kind = Current.Kind();
            if (!IsModifier(kind)) break;
            if (allowed.Contains(kind))
            {
                modifiers.Add(MatchToken(kind));
            }
        }
        return new SyntaxTokenList(modifiers);
    }

    private TypeParameterListSyntax ParseTypeParameterList()
    {
        var langle = MatchToken(SyntaxKind.LessThanToken);

        var param = SyntaxFactory.TypeParameter(MatchToken(SyntaxKind.IdentifierToken));
        SeparatedSyntaxList<TypeParameterSyntax> typeParams = [param];
        if (!Current.IsKind(SyntaxKind.GreaterThanToken))
        {
            throw new NotImplementedException("Only support one type argument");
        }

        var rangle = MatchToken(SyntaxKind.GreaterThanToken);
        
        return SyntaxFactory.TypeParameterList(langle, typeParams, rangle);
    }

    /* ************************************************************ */

    private FieldDeclarationSyntax ParseFieldDeclaration()
    {
        var modifiers = ParseModifiers([SyntaxKind.PrivateKeyword]);
        VariableDeclarationSyntax declaration = ParseVariableDeclaration();

        return SyntaxFactory.FieldDeclaration(default, modifiers, declaration, MatchToken(SyntaxKind.SemicolonToken));
    }

    private VariableDeclarationSyntax ParseVariableDeclaration()
    {
        var type = ParseType();
        SeparatedSyntaxList<VariableDeclaratorSyntax> decls = ParseVariableDeclarators();
        
        return SyntaxFactory.VariableDeclaration(type, decls); ;
    }

    private SeparatedSyntaxList<VariableDeclaratorSyntax> ParseVariableDeclarators()
    {
        SeparatedSyntaxList<VariableDeclaratorSyntax> decls = [];
        var name = MatchToken(SyntaxKind.IdentifierToken);
        EqualsValueClauseSyntax eqClause = ParseEqualsValueClause();
        var declarator = SyntaxFactory.VariableDeclarator(name, null, eqClause);
        decls = decls.Add(declarator);
        while (!Current.IsKind(SyntaxKind.SemicolonToken))
        {
            MatchToken(SyntaxKind.CommaToken);
            name = MatchToken(SyntaxKind.IdentifierToken);
            eqClause = ParseEqualsValueClause();
            declarator = SyntaxFactory.VariableDeclarator(name, null, eqClause);
            decls = decls.Add(declarator);
        }

        return decls;
    }

    private EqualsValueClauseSyntax ParseEqualsValueClause()
    {
        var equals = MatchToken(SyntaxKind.EqualsToken);
        var expression = ParseExpression();        
        return SyntaxFactory.EqualsValueClause(equals, expression); ;
    }

    /* ************************************************************ */

    private MethodDeclarationSyntax ParseMethodDeclaration()
    {
        var modifiers = ParseModifiers([SyntaxKind.PublicKeyword, SyntaxKind.PrivateKeyword, SyntaxKind.StaticKeyword]);
        var type = ParseType();
        var name = MatchToken(SyntaxKind.IdentifierToken);
        var paramList = ParseParameterList();
        var body = ParseBlock();

        return SyntaxFactory.MethodDeclaration(
            attributeLists: default,
            modifiers: modifiers,
            returnType: type,
            explicitInterfaceSpecifier: default,
            identifier: name,
            typeParameterList: default,
            parameterList: paramList,
            constraintClauses: default,
            body: body,
            semicolonToken: MatchToken(SyntaxKind.SemicolonToken));
    }

    private ParameterListSyntax ParseParameterList()
    {
        var lparen = MatchToken(SyntaxKind.OpenParenToken);
        SyntaxList<ParameterSyntax> parameters = [];
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
    private ArgumentListSyntax ParseArgumentList()
    {

        var lparen = MatchToken(SyntaxKind.OpenParenToken);
        SyntaxList<ArgumentSyntax> args = [];

        var arg = ParseExpression();
        args.Add(SyntaxFactory.Argument(arg));
        while (Current.IsKind(SyntaxKind.CommaToken))
        {
            MatchToken(SyntaxKind.CommaToken);
            arg = ParseExpression();
            args.Add(SyntaxFactory.Argument(arg));
        }

        var rparen = MatchToken(SyntaxKind.CloseParenToken);

        return SyntaxFactory.ArgumentList(
            lparen, 
            (new SeparatedSyntaxList<ArgumentSyntax>()).AddRange(args), 
            rparen);
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

    /* ************************************************************ */

    private PropertyDeclarationSyntax ParsePropertyDeclaration()
    {
        var modifiers = ParseModifiers([SyntaxKind.PublicKeyword, SyntaxKind.PrivateKeyword]);
        var type = ParseType();
        var name = MatchToken(SyntaxKind.IdentifierToken);
        var accessors = ParseAccessorList();

        return SyntaxFactory.PropertyDeclaration(default, modifiers, type, null, name, accessors);
    }

    private AccessorListSyntax ParseAccessorList()
    {
        var lbrace = MatchToken(SyntaxKind.OpenBraceToken);
        var decls = ParseAccessorDeclarations();
        var rbrace = MatchToken(SyntaxKind.CloseBraceToken);

        return SyntaxFactory.AccessorList(lbrace, decls, rbrace);
    }

    private SyntaxList<AccessorDeclarationSyntax> ParseAccessorDeclarations()
    {
        List<AccessorDeclarationSyntax> decls = [];
        SyntaxKind kind;
        BlockSyntax? body;
        while (!Current.IsKind(SyntaxKind.CloseBraceToken))
        {
            kind = Current.Kind();
            switch (kind)
            {
                case SyntaxKind.GetKeyword:
                    body = ParseBlock(); break;
                case SyntaxKind.SetKeyword:
                default:
                    throw new NotImplementedException("Only getter is supported");
            }

            decls.Add(SyntaxFactory.AccessorDeclaration(kind, body));
        }

        return (new SyntaxList<AccessorDeclarationSyntax>()).AddRange(decls);
    }

    /* ************************************************************ */

    /// <summary>
    /// 还没有像modifiers那样实现预检查。
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private TypeSyntax ParseType()
    {
        var currentKind = Current.Kind();
        if (SyntaxFacts.IsPredefinedType(currentKind))
        {
            return SyntaxFactory.PredefinedType(MatchToken(currentKind));
        }

        if (PeekTokenKind(1) is SyntaxKind.LessThanToken)
        {
            return ParseGenericName();
        }
        
        if (PeekTokenKind(1) is SyntaxKind.OpenBracketToken)
        {
            return ParseArrayType();
        }
        
        if (currentKind == SyntaxKind.IdentifierToken)
        {
            return SyntaxFactory.IdentifierName(MatchToken(SyntaxKind.IdentifierToken));
        }
        
        throw new NotImplementedException("Only predefiend type and identifier supported");
    }

    /// <summary>
    /// 目前只支持变量作为数组规模。
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private ArrayTypeSyntax ParseArrayType()
    {
        TypeSyntax elemType;
        var currentKind = Current.Kind();
        if (SyntaxFacts.IsPredefinedType(currentKind))
            elemType = SyntaxFactory.PredefinedType(MatchToken(currentKind));
        else if (PeekTokenKind(1) is SyntaxKind.LessThanToken)
            elemType = ParseGenericName();
        else if (currentKind == SyntaxKind.IdentifierToken)
            elemType = SyntaxFactory.IdentifierName(MatchToken(SyntaxKind.IdentifierToken));
        else throw new NotImplementedException("Unable to handle element type.");

        SyntaxList<ArrayRankSpecifierSyntax> dims = [];
        while (Current.IsKind(SyntaxKind.OpenBracketToken))
        {
            var lbrack = MatchToken(SyntaxKind.OpenBracketToken);
            var size = ParseIdentifierName(); // 注意
            var rbrack = MatchToken(SyntaxKind.CloseBracketToken);

            dims.Add(SyntaxFactory.ArrayRankSpecifier(
                lbrack,
                (new SeparatedSyntaxList<ExpressionSyntax>()).Add(size),
                rbrack));
        }
        return SyntaxFactory.ArrayType(elemType, dims);
    }

    private GenericNameSyntax ParseGenericName()
    {
        var name = MatchToken(SyntaxKind.IdentifierToken);

        var langle = MatchToken(SyntaxKind.LessThanToken);
        SyntaxList<TypeSyntax> types = [];
        types.Add(ParseType());
        while (!Current.IsKind(SyntaxKind.GreaterThanToken))
        {
            MatchToken(SyntaxKind.CommaToken);
            var t = ParseType();
            types.Add(t);
        }
        var rangle = MatchToken(SyntaxKind.GreaterThanToken);
        
        return SyntaxFactory.GenericName(
            name, 
            SyntaxFactory.TypeArgumentList(
                langle,
                (new SeparatedSyntaxList<TypeSyntax>()).AddRange(types),
                rangle));
    }

    /* ************************************************************ */

    private bool IsEmbeddedStatement()
    {
        throw new NotImplementedException("Must determine this");
    }
    private StatementSyntax ParseStatement()
    {
        if (IsEmbeddedStatement())
        {
            return ParseEmbeddedStatement();
        }
        else
        {
            throw new NotImplementedException("DECLARATION_STATEMENT");
        }
    }

    /// <summary>
    /// 文法接近LL(1)。
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private StatementSyntax ParseEmbeddedStatement()
    {
        switch (Current.Kind())
        {
            case SyntaxKind.IfKeyword:
                return ParseIfStatement();
            case SyntaxKind.ForKeyword:
                return ParseForStatement();
            case SyntaxKind.WhileKeyword:
                return ParseWhileStatement();
            case SyntaxKind.SwitchKeyword:
                return ParseSwitchStatement();
            case SyntaxKind.ContinueKeyword:
                return SyntaxFactory.ContinueStatement(
                    MatchToken(SyntaxKind.ContinueKeyword), 
                    MatchToken(SyntaxKind.SemicolonToken));
            case SyntaxKind.ThrowKeyword:
                 return SyntaxFactory.ThrowStatement(
                     MatchToken(SyntaxKind.ThrowKeyword),
                     ParseExpression(),
                     MatchToken(SyntaxKind.SemicolonToken));
            case SyntaxKind.ReturnKeyword:
                return SyntaxFactory.ReturnStatement(
                    MatchToken(SyntaxKind.ReturnKeyword),
                    ParseExpression(),
                    MatchToken(SyntaxKind.SemicolonToken));
            case SyntaxKind.OpenBraceToken:
                return ParseBlock();
            default:
                return ParseExpressionStatement();
        }
    }

    /// <summary>
    /// 仅支持变量作为控制变量，还不能用表达式。
    /// 也许以后可以加上重复标签检查？
    /// </summary>
    /// <returns></returns>
    private SwitchStatementSyntax ParseSwitchStatement()
    {
        var keyword = MatchToken(SyntaxKind.SwitchKeyword);
        var lparen = MatchToken(SyntaxKind.OpenParenToken);
        var keyval = ParseIdentifierName();
        var rparen = MatchToken(SyntaxKind.CloseParenToken);
        var lbrace = MatchToken(SyntaxKind.OpenBraceToken);

        List<SwitchSectionSyntax> sections = [];
        bool hasDefault = false;
        while (!Current.IsKind(SyntaxKind.CloseBraceToken))
        {
            List<SwitchLabelSyntax> labels = [];
            while (true)
            {
                if (Current.Kind() is SyntaxKind.CaseKeyword)
                {
                    labels.Add(SyntaxFactory.CaseSwitchLabel(
                        MatchToken(SyntaxKind.CaseKeyword),
                        ParseExpression(),
                        MatchToken(SyntaxKind.ColonToken)));
                }
                else if (Current.Kind() is SyntaxKind.DefaultKeyword)
                {
                    if (hasDefault)
                    {
                        throw new Exception("Multiple 'default's not allowed.");
                    }
                    else
                    {
                        hasDefault = true;
                    }
                    labels.Add(SyntaxFactory.DefaultSwitchLabel(
                        MatchToken(SyntaxKind.DefaultKeyword),
                        MatchToken(SyntaxKind.ColonToken)));
                }
                else
                {
                    break;
                }
            }

            List<StatementSyntax> statements = [];
            while (!Current.IsKind(SyntaxKind.CloseBraceToken)
                && !Current.IsKind(SyntaxKind.CaseKeyword)
                && !Current.IsKind(SyntaxKind.DefaultKeyword))
            {
                statements.Add(ParseStatement());
            }

            sections.Add(SyntaxFactory.SwitchSection(
                (new SyntaxList<SwitchLabelSyntax>()).AddRange(labels),
                (new SyntaxList<StatementSyntax>()).AddRange(statements)
                ));
        }

        var rbrace = MatchToken(SyntaxKind.CloseBraceToken);

        return SyntaxFactory.SwitchStatement(
            keyword, lparen, keyval, rparen, lbrace,
            (new SyntaxList<SwitchSectionSyntax>()).AddRange(sections),
            rbrace);
    }

    private IdentifierNameSyntax ParseIdentifierName()
    {
        var identifier = MatchToken(SyntaxKind.IdentifierToken);
        return SyntaxFactory.IdentifierName(identifier);
    }

    private WhileStatementSyntax ParseWhileStatement()
    {
        var keyword = MatchToken(SyntaxKind.WhileKeyword);
        var lparen = MatchToken(SyntaxKind.OpenParenToken);
        var cond = ParseExpression();
        var rparen = MatchToken(SyntaxKind.CloseParenToken);
        var body = ParseBlock();

        return SyntaxFactory.WhileStatement(keyword, lparen, cond, rparen, body);
    }

    /// <summary>
    /// 初始化器必须是变量定义。
    /// 暂不支持更新部分采用逗号表达式。
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private ForStatementSyntax ParseForStatement()
    {
        var keyword = MatchToken(SyntaxKind.ForKeyword);
        var lparen = MatchToken(SyntaxKind.OpenParenToken);

        var init = ParseVariableDeclaration();

        var sep1 = MatchToken(SyntaxKind.SemicolonToken);
        var condition = ParseExpression();
        var sep2 = MatchToken(SyntaxKind.SemicolonToken);

        var updates = new SeparatedSyntaxList<ExpressionSyntax>();
        updates = updates.Add(ParseStatementExpression());

        var rparen = MatchToken(SyntaxKind.CloseParenToken);
        var body = ParseBlock();

        return SyntaxFactory.ForStatement(
            keyword, lparen,
            init, default, sep1,
            condition, sep2,
            updates, rparen,
            body);
    }

    /// <summary>
    /// if-else悬挂问题!
    /// 本实现使用就近原则。
    /// </summary>
    /// <returns></returns>
    private IfStatementSyntax ParseIfStatement()
    {
        var keyword = MatchToken(SyntaxKind.IfKeyword);
        var lparen = MatchToken(SyntaxKind.OpenParenToken);
        var condition = ParseExpression();
        var rparen = MatchToken(SyntaxKind.CloseParenToken);
        var body = ParseEmbeddedStatement();

        ElseClauseSyntax? elseClause = null;
        if (Current.IsKind(SyntaxKind.ElseKeyword))
        {
            var keywordElse = MatchToken(SyntaxKind.ElseKeyword);
            var bodyElse = ParseEmbeddedStatement();

            elseClause = SyntaxFactory.ElseClause(keywordElse, bodyElse);
        }

        return SyntaxFactory.IfStatement(keyword, lparen, condition, rparen, body, elseClause);
    }

    /// <summary>
    /// 没有STATEMENT_EXPRESSION这种类型，需要自己辨别。
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private ExpressionStatementSyntax ParseExpressionStatement()
    {
        ExpressionSyntax expr = ParseStatementExpression();
        var end = MatchToken(SyntaxKind.SemicolonToken);

        return SyntaxFactory.ExpressionStatement(expr, end);
    }

    private ExpressionSyntax ParseStatementExpression()
    {
        try
        {
            for (int i = 1; ; i++)
            {
                switch (PeekTokenKind(i))
                {
                    case SyntaxKind.PlusPlusToken:
                        return ParsePostIncrementExpression();
                    case SyntaxKind.MinusMinusToken:
                        return ParsePostDecrementExpression();
                    case SyntaxKind.EqualsToken:
                        return ParseSimpleAssignmentExpression();
                    case SyntaxKind.OpenParenToken:
                        return ParseInvocationExpression();
                    case SyntaxKind.SemicolonToken:
                        throw new NotImplementedException("Unsupported statement-expression.");
                    default:
                        break;
                }
            }
        }
        catch (IndexOutOfRangeException)
        {
            throw;
        }
    }

    private InvocationExpressionSyntax ParseInvocationExpression()
    {
        var process = ParseSimpleMemberAccessExpression();
        var args = ParseArgumentList();

        return SyntaxFactory.InvocationExpression(process, args);
    }

    private MemberAccessExpressionSyntax ParseSimpleMemberAccessExpression()
    {
        ExpressionSyntax left;
        if (SyntaxFacts.IsPredefinedType(Current.Kind()))
        {
            left = ParseType();
        }
        else
        {
            left = ParseIdentifierName();
        }
        var dot = MatchToken(SyntaxKind.DotToken);
        var right = ParseIdentifierName();

        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            left, dot, right);
    }

    private AssignmentExpressionSyntax ParseSimpleAssignmentExpression()
    {
        ExpressionSyntax lhs, rhs;
        if (PeekTokenKind(1) is SyntaxKind.EqualsToken)
        {
            lhs = ParseIdentifierName();
        }
        else
        {
            lhs = ParseElementAccessExpression();
        }

        var eq = MatchToken(SyntaxKind.EqualsToken);
        rhs = ParseExpression();
        return SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression, 
            lhs, eq, rhs);
    }

    /// <summary>
    /// 目前仅支持：
    /// ElementAccessExpression -> IdentifierName BracketedArgumentList;
    /// BracketedArgumentList -> OpenBracketToken Argument CloseBracketToken;
    /// </summary>
    /// <returns></returns>
    private ElementAccessExpressionSyntax ParseElementAccessExpression()
    {
        var collection = ParseIdentifierName();

        var lbrack = MatchToken(SyntaxKind.OpenBracketToken);

        SyntaxList<ArgumentSyntax> indexes = [];
        var index = SyntaxFactory.Argument(ParseIdentifierName());
        indexes.Add(index);
        
        var rbrack = MatchToken(SyntaxKind.CloseBracketToken);

        return SyntaxFactory.ElementAccessExpression(
            collection,
            SyntaxFactory.BracketedArgumentList(
                lbrack, 
                (new SeparatedSyntaxList<ArgumentSyntax>()).AddRange(indexes), 
                rbrack));
    }

    private PostfixUnaryExpressionSyntax ParsePostDecrementExpression()
    {
        var operand = ParseIdentifierName();
        var op = MatchToken(SyntaxKind.MinusMinusToken);

        return SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostDecrementExpression, operand, op);
    }

    private PostfixUnaryExpressionSyntax ParsePostIncrementExpression()
    {
        var operand = ParseIdentifierName();
        var op = MatchToken(SyntaxKind.PlusPlusToken);
        
        return SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, operand, op );
    }

    /* ************************************************************ */

    private static int GetPrecedence(SyntaxKind op)
    {
        switch (op)
        {
            case SyntaxKind.BarBarToken:
                return 0;
            case SyntaxKind.AmpersandAmpersandToken:
                return 1;
            case SyntaxKind.EqualsEqualsToken:
            case SyntaxKind.ExclamationEqualsToken:
                return 2;
            case SyntaxKind.LessThanToken:
            case SyntaxKind.GreaterThanToken:
            case SyntaxKind.LessThanEqualsToken:
            case SyntaxKind.GreaterThanEqualsToken:
                return 3;
            case SyntaxKind.PlusToken:
            case SyntaxKind.MinusToken:
                return 4;
            case SyntaxKind.AsteriskToken:
            case SyntaxKind.SlashToken:
                return 5;
            default:
                throw new NotImplementedException("Not supported binary infix operator");
        }
    }
    /// <summary>
    /// 这些中缀表达式是右结合的，方便归约分析。
    /// </summary>
    /// <returns></returns>
    private ExpressionSyntax ParseExpression()
    {
        Stack<ExpressionSyntax> stack = [];
        Stack<SyntaxToken> ops = [];

        stack.Push(ParseAtomExpression());
        while (!Current.IsKind(SyntaxKind.SemicolonToken)
            && !Current.IsKind(SyntaxKind.CloseParenToken)
            && !Current.IsKind(SyntaxKind.CloseBraceToken))
        {
            var opKind = Current.Kind();
            while (GetPrecedence(ops.Peek().Kind()) > GetPrecedence(opKind))
            {
                var rhs = stack.Pop();
                var op = ops.Pop();
                var lhs = stack.Pop();
                stack.Push(MergeInfixExpression(lhs, op, rhs));
            }
            ops.Push(MatchToken(opKind));
            stack.Push(ParseAtomExpression());
        }
        while (stack.Count > 1)
        {
            var rhs = stack.Pop();
            var op = ops.Pop();
            var lhs = stack.Pop();
            stack.Push(MergeInfixExpression(lhs, op, rhs));
        }
        return stack.Pop();
    }

    private static readonly Dictionary<SyntaxKind, SyntaxKind> op2expr = new Dictionary<SyntaxKind, SyntaxKind>()
    {
        [SyntaxKind.BarBarToken] = SyntaxKind.LogicalOrExpression,
        [SyntaxKind.AmpersandAmpersandToken] = SyntaxKind.LogicalAndExpression,
        [SyntaxKind.EqualsEqualsToken] = SyntaxKind.EqualsExpression,
        [SyntaxKind.ExclamationEqualsToken] = SyntaxKind.NotEqualsExpression,
        [SyntaxKind.LessThanToken] = SyntaxKind.LessThanExpression,
        [SyntaxKind.GreaterThanToken] = SyntaxKind.GreaterThanOrEqualExpression,
        [SyntaxKind.LessThanEqualsToken] = SyntaxKind.LessThanOrEqualExpression,
        [SyntaxKind.GreaterThanEqualsToken] = SyntaxKind.GreaterThanOrEqualExpression,
        [SyntaxKind.PlusToken] = SyntaxKind.AddExpression,
        [SyntaxKind.MinusToken] = SyntaxKind.SubtractExpression,
        [SyntaxKind.AsteriskToken] = SyntaxKind.MultiplyExpression,
        [SyntaxKind.SlashToken] = SyntaxKind.DivideExpression,
    };
    private static BinaryExpressionSyntax MergeInfixExpression(ExpressionSyntax lhs, SyntaxToken op, ExpressionSyntax rhs)
    {
        SyntaxKind exprKind = op2expr[op.Kind()];
        return SyntaxFactory.BinaryExpression(exprKind, lhs, op, rhs);        
    }

    /* ************************************************************ */

    /// <summary>
    /// 不知道结合律具体如何。
    /// </summary>
    /// <returns></returns>
    private ExpressionSyntax ParseAtomExpression()
    {
        // 先解决所有的字面值等简单情形
        var kind = Current.Kind();
        switch (kind)
        {
            case SyntaxKind.TrueKeyword:
            case SyntaxKind.FalseKeyword:
            case SyntaxKind.NullKeyword:
            case SyntaxKind.NumericLiteralToken:
            case SyntaxKind.StringLiteralToken:
            case SyntaxKind.CharacterLiteralToken:
                return SyntaxFactory.LiteralExpression(kind, MatchToken(kind));

            case SyntaxKind.InterpolatedStringStartToken:
                return ParseInterpolatedStringExpression();

            case SyntaxKind.OpenParenToken:
                var lparen = MatchToken(SyntaxKind.OpenParenToken);
                var expr = ParseExpression();
                var rparen = MatchToken(SyntaxKind.CloseParenToken);
                return SyntaxFactory.ParenthesizedExpression(lparen, expr, rparen);

            case SyntaxKind.NewKeyword:
                return ParseCreationExpression();

            default:
                break;
        }

        // LR分析解决：成员访问、过程调用、变量名。
        Stack<ExpressionSyntax> atoms = [];
        if (SyntaxFacts.IsPredefinedType(Current.Kind()))
        {
            atoms.Push(ParseType());
        }
        else
        {
            atoms.Push(ParseIdentifierName());
        }

        while (true)
        {
            if (Current.IsKind(SyntaxKind.DotToken))
            {
                var obj = atoms.Pop();
                var dot = MatchToken(SyntaxKind.DotToken);
                var tag = ParseIdentifierName();

                atoms.Push(SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    obj, dot, tag));
            }
            else if (Current.IsKind(SyntaxKind.OpenParenToken))
            {
                var proc = atoms.Pop();
                var args = ParseArgumentList();

                atoms.Push(SyntaxFactory.InvocationExpression(proc, args));
            }
            else
            {
                break;
            }
        }
        return atoms.Pop();
    }

    private InterpolatedStringExpressionSyntax ParseInterpolatedStringExpression()
    {
        var start = MatchToken(SyntaxKind.InterpolatedStringStartToken);
        SyntaxList<InterpolatedStringContentSyntax> list = [];
        while (!Current.IsKind(SyntaxKind.InterpolatedStringEndToken))
        {
            if (Current.IsKind(SyntaxKind.OpenBraceToken))
            {
                var lbrace = MatchToken(SyntaxKind.OpenBraceToken);
                var val = ParseExpression();
                var rbrace = MatchToken(SyntaxKind.CloseBraceToken);
                list.Add(SyntaxFactory.Interpolation(lbrace, val, null, null, rbrace));
            }
            else if (Current.IsKind(SyntaxKind.InterpolatedStringTextToken))
            {
                list.Add(SyntaxFactory.InterpolatedStringText(MatchToken(SyntaxKind.InterpolatedStringTextToken)));
            }
            else
            {
                throw new Exception($"{Current} not allowed within interp-str.");
            }
        }
        var end = MatchToken(SyntaxKind.InterpolatedStringEndToken);
        return SyntaxFactory.InterpolatedStringExpression(start, list, end);
    }

    private ExpressionSyntax ParseCreationExpression()
    {
        var keyword = MatchToken(SyntaxKind.NewKeyword);
        var type = ParseType();
        if (type.IsKind(SyntaxKind.ArrayType))
        {
            return SyntaxFactory.ArrayCreationExpression(keyword, (ArrayTypeSyntax)type, null);
        }
        else
        {
            var argList = ParseArgumentList();
            return SyntaxFactory.ObjectCreationExpression(keyword, type, argList, null);
        }
    }

}
