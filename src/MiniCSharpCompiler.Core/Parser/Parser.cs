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

    /// <summary>
    /// 向前看token的类型，用于LL预测。
    /// </summary>
    /// <param name="k"></param>
    /// <returns></returns>
    /// <exception cref="IndexOutOfRangeException"></exception>
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

    /// <summary>
    /// 语法：
    /// ClassDeclaration -> CLASS_MODIFIER? ClassKeyword IdentifierToken TypeParameterList? OpenBraceToken CLASS_MEMBER_DECLARATION* CloseBraceToken;
    /// CLASS_MEMBER_DECLARATION -> FieldDeclaration | MethodDeclaration | PropertyDeclaration;
    /// </summary>
    /// <returns></returns>
    private ClassDeclarationSyntax ParseClassDeclaration()
    {
        var modifiers = ParseModifiers([SyntaxKind.PublicKeyword]);
        var keyword = MatchToken(SyntaxKind.ClassKeyword);
        var name = MatchToken(SyntaxKind.IdentifierToken);

        TypeParameterListSyntax? typeParamList = null;
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
            semicolonToken: default);
    }

    static bool IsModifier(SyntaxKind kind)
    {
        return (
            kind is SyntaxKind.PublicKeyword
            || kind is SyntaxKind.PrivateKeyword
            || kind is SyntaxKind.StaticKeyword
            );
    }

    /// <summary>
    /// 不同单元允许的修饰符范围可以不同，经allowed传入。
    /// </summary>
    /// <param name="allowed"></param>
    /// <returns></returns>
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

    /// <summary>
    /// 支持的语法：
    /// TypeParameterList -> LessThanToken TypeParameter GreaterThanToken
    /// TypeParameter -> IdentifierToken
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private TypeParameterListSyntax ParseTypeParameterList()
    {
        var langle = MatchToken(SyntaxKind.LessThanToken);

        SeparatedSyntaxList<TypeParameterSyntax> typeParams = [];
        var param = SyntaxFactory.TypeParameter(MatchToken(SyntaxKind.IdentifierToken));
        typeParams.Add(param);

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

    /// <summary>
    /// VariableDeclarator -> IdentifierToken EqualsValueClause;
    /// 逗号分隔。
    /// </summary>
    /// <returns></returns>
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
            semicolonToken: default);
    }

    /// <summary>
    /// ParameterList: OpenParenToken (Parameter (CommaToken Parameter)*)? CloseParenToken
    /// </summary>
    /// <returns></returns>
    private ParameterListSyntax ParseParameterList()
    {
        var lparen = MatchToken(SyntaxKind.OpenParenToken);
        SyntaxList<ParameterSyntax> parameters = [];
        List<SyntaxToken> commas = [];

        if (!Current.IsKind(SyntaxKind.CloseParenToken))
        {
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
        }

        var rparen = MatchToken(SyntaxKind.CloseParenToken);
        return SyntaxFactory.ParameterList(
            lparen, 
            (new SeparatedSyntaxList<ParameterSyntax>()).AddRange(parameters), 
            rparen);
    }

    /// <summary>
    /// 和形参列表类似。
    /// </summary>
    /// <returns></returns>
    private ArgumentListSyntax ParseArgumentList()
    {

        var lparen = MatchToken(SyntaxKind.OpenParenToken);
        SyntaxList<ArgumentSyntax> args = [];

        if (!Current.IsKind(SyntaxKind.CloseParenToken))
        {
            var arg = ParseExpression();
            args.Add(SyntaxFactory.Argument(arg));
            while (Current.IsKind(SyntaxKind.CommaToken))
            {
                MatchToken(SyntaxKind.CommaToken);
                arg = ParseExpression();
                args.Add(SyntaxFactory.Argument(arg));
            } 
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

    /// <summary>
    /// PropertyDeclaration -> PROPERTY_MODIFIERS? TYPE IdentifierToken AccessorList
    /// </summary>
    /// <returns></returns>
    private PropertyDeclarationSyntax ParsePropertyDeclaration()
    {
        var modifiers = ParseModifiers([SyntaxKind.PublicKeyword, SyntaxKind.PrivateKeyword]);
        var type = ParseType();
        var name = MatchToken(SyntaxKind.IdentifierToken);
        var accessors = ParseAccessorList();

        return SyntaxFactory.PropertyDeclaration(default, modifiers, type, null, name, accessors);
    }

    /// <summary>
    /// AccessorList -> OpenBraceToken GetAccessorDeclaration CloseBraceToken
    /// </summary>
    /// <returns></returns>
    private AccessorListSyntax ParseAccessorList()
    {
        var lbrace = MatchToken(SyntaxKind.OpenBraceToken);
        var decls = ParseAccessorDeclarations();
        var rbrace = MatchToken(SyntaxKind.CloseBraceToken);

        return SyntaxFactory.AccessorList(lbrace, decls, rbrace);
    }

    /// <summary>
    /// TODO：需要改进吗？
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
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
    /// 还没有像modifiers那样实现allowed预检查。
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private TypeSyntax ParseType()
    {
        var currentKind = Current.Kind();
        if (PeekTokenKind(1) is SyntaxKind.LessThanToken)
        {
            return ParseGenericName();
        }
        
        if (PeekTokenKind(1) is SyntaxKind.OpenBracketToken)
        {
            return ParseArrayType();
        }
        
        if (SyntaxFacts.IsPredefinedType(currentKind))
        {
            return SyntaxFactory.PredefinedType(MatchToken(currentKind));
        }

        if (currentKind == SyntaxKind.IdentifierToken)
        {
            return SyntaxFactory.IdentifierName(MatchToken(SyntaxKind.IdentifierToken));
        }
        
        throw new NotImplementedException("Only predefiend type and identifier supported");
    }

    /// <summary>
    /// 目前只支持变量作为数组规模。语法：
    /// ArrayType: PredefinedType ArrayRankSpecifier;
    /// ArrayRankSpecifier: OpenBracketToken IdentifierName CloseBracketToken;
    /// 缺一个OmittedArraySizeExpression的支持（int x[];）
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
            SyntaxList<ExpressionSyntax> sizes = [];
            if (Current.IsKind(SyntaxKind.OmittedArraySizeExpressionToken))
            {
                MatchToken(SyntaxKind.OmittedArraySizeExpressionToken);
            }
            else
            {
                sizes.Add(ParseExpression());
                while (!Current.IsKind(SyntaxKind.CloseBracketToken))
                {
                    MatchToken(SyntaxKind.CommaToken);
                    sizes.Add(ParseIdentifierName());
                } 
            }
            var rbrack = MatchToken(SyntaxKind.CloseBracketToken);

            dims.Add(SyntaxFactory.ArrayRankSpecifier(
                lbrack,
                (new SeparatedSyntaxList<ExpressionSyntax>()).AddRange(sizes),
                rbrack));
        }
        return SyntaxFactory.ArrayType(elemType, dims);
    }

    /// <summary>
    /// GenericName -> IdentifierToken TypeArgumentList;
    /// </summary>
    /// <returns></returns>
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

    private bool IsLocalVarDecl()
    {
        var kind = Current.Kind();
        if (SyntaxFacts.IsPredefinedType(kind)) return true;
        if (PeekTokenKind(1) is SyntaxKind.LessThanToken)
        {
            for (int i = 2; PeekTokenKind(i) != SyntaxKind.SemicolonToken; i++)
            {
                if (PeekTokenKind(i) is SyntaxKind.GreaterThanToken
                    && PeekTokenKind(i + 1) is SyntaxKind.IdentifierToken)
                {
                    return true;
                }
            }
            return false;
        }
        if (PeekTokenKind(1) is SyntaxKind.OpenBracketToken)
        {
            for (int i = 2; PeekTokenKind(i) != SyntaxKind.SemicolonToken; i++)
            {
                if (PeekTokenKind(i) is SyntaxKind.CloseBracketToken
                    && PeekTokenKind(i + 1) is SyntaxKind.IdentifierToken)
                {
                    return true;
                }
            }
            return false;
        }
        if (PeekTokenKind(1) is SyntaxKind.IdentifierToken) return true;
        
        return false;
    }

    /// <summary>
    /// 对临时变量，不支持定义。声明语法：
    /// LocalDeclarationStatement -> VariableDeclaration SemicolonToken;
    /// 区分embedded语句是为防止“if (j != 0) int k;”
    /// 同时允许“if (_elements.Count == 0) throw new InvalidOperationException("Stack is empty.");”
    /// 文法接近LL(1)。
    /// </summary>
    /// <returns></returns>
    private StatementSyntax ParseStatement()
    {
        if (Current.IsKind(SyntaxKind.SemicolonToken))
            return SyntaxFactory.EmptyStatement();

        // 筛出LL(1)的embedded语句
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
            case SyntaxKind.BreakKeyword:
                return SyntaxFactory.BreakStatement(
                    MatchToken(SyntaxKind.BreakKeyword),
                    MatchToken(SyntaxKind.SemicolonToken));
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
                break;
        }

        // 下面区分表达式语句和变量名
        if (IsLocalVarDecl())
        {
            var decl = ParseVariableDeclaration();
            var end = MatchToken(SyntaxKind.SemicolonToken);

            return SyntaxFactory.LocalDeclarationStatement(default, decl, end);
        }
        else
        {
            return ParseExpressionStatement();
        }
    }

    /// <summary>
    /// 这么区分是为防止“if (j != 0) int k;”
    /// 同时允许“if (_elements.Count == 0) throw new InvalidOperationException("Stack is empty.");”
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
    /// 仅支持变量作为控制变量，还不能用表达式。语法：
    /// SwitchStatement -> SwitchKeyword OpenParenToken IdentifierName CloseParenToken OpenBraceToken SwitchSection* CloseBraceToken
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
    /// 初始化器必须是变量定义。暂不支持更新部分采用逗号表达式。语法：
    /// ForStatement -> ForKeyword OpenParenToken VariableDeclaration SemicolonToken EXPRESSION SemicolonToken STATEMENT_EXPRESSION CloseParenToken Block
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
    /// Roslyn没有STATEMENT_EXPRESSION这种类型，需要自己辨别。
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private ExpressionStatementSyntax ParseExpressionStatement()
    {
        ExpressionSyntax expr = ParseStatementExpression();
        var end = MatchToken(SyntaxKind.SemicolonToken);

        return SyntaxFactory.ExpressionStatement(expr, end);
    }

    /// <summary>
    /// 支持4种能够单独成句的表达式。
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
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

    /// <summary>
    /// 语法：
    /// SimpleAssignmentExpression -> (IdentifierName | ElementAccessExpression) EqualsToken EXPRESSION;
    /// </summary>
    /// <returns></returns>
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
    /// 不支持嵌入的赋值表达式。
    /// </summary>
    /// <returns></returns>
    private ExpressionSyntax ParseExpression()
    {
        Stack<ExpressionSyntax> stack = [];
        Stack<SyntaxToken> ops = [];

        stack.Push(ParseAtomExpression());
        while (!Current.IsKind(SyntaxKind.SemicolonToken)
            && !Current.IsKind(SyntaxKind.CloseParenToken)
            && !Current.IsKind(SyntaxKind.CloseBraceToken)
            && !Current.IsKind(SyntaxKind.CloseBracketToken)
            && !Current.IsKind(SyntaxKind.CommaToken))
        {
            var opKind = Current.Kind();
            while (ops.Count > 0 && GetPrecedence(ops.Peek().Kind()) > GetPrecedence(opKind))
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
    /// 没查该优先级内结合律具体如何。我实现为左结合。
    /// </summary>
    /// <returns></returns>
    private ExpressionSyntax ParseAtomExpression()
    {
        // 先解决所有的字面值等简单情形
        var kind = Current.Kind();
        switch (kind)
        {
            case SyntaxKind.TrueKeyword:
                return SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression, MatchToken(kind));
            case SyntaxKind.FalseKeyword:
                return SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression, MatchToken(kind));
            case SyntaxKind.NullKeyword:
                return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, MatchToken(kind));
            case SyntaxKind.NumericLiteralToken:
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, MatchToken(kind));
            case SyntaxKind.StringLiteralToken:
                return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, MatchToken(kind));
            case SyntaxKind.CharacterLiteralToken:
                return SyntaxFactory.LiteralExpression(SyntaxKind.CharacterLiteralExpression, MatchToken(kind));

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
            else if (Current.IsKind(SyntaxKind.OpenBracketToken))
            {
                var collection = atoms.Pop();
                var brackedList = ParseBracketedArgumentList();

                atoms.Push(SyntaxFactory.ElementAccessExpression(collection, brackedList));
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

    private ElementAccessExpressionSyntax ParseElementAccessExpression()
    {
        var collection = ParseIdentifierName();
        BracketedArgumentListSyntax brackedList = ParseBracketedArgumentList();

        return SyntaxFactory.ElementAccessExpression(collection, brackedList);
    }

    private BracketedArgumentListSyntax ParseBracketedArgumentList()
    {
        var lbrack = MatchToken(SyntaxKind.OpenBracketToken);

        SyntaxList<ArgumentSyntax> indexes = [];
        var index = SyntaxFactory.Argument(ParseExpression());
        indexes.Add(index);

        var rbrack = MatchToken(SyntaxKind.CloseBracketToken);
        return SyntaxFactory.BracketedArgumentList(
                lbrack,
                (new SeparatedSyntaxList<ArgumentSyntax>()).AddRange(indexes),
                rbrack);
    }
}
