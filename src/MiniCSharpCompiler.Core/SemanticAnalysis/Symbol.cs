using Microsoft.CodeAnalysis.CSharp;

namespace MiniCSharpCompiler.Core.SemanticAnalysis;

public enum SymbolKind
{
    Namespace,
    Class,
    Method,
    Variable,
    Parameter
}

public class Symbol
{
    public string Name { get; }
    public SymbolKind Kind { get; } // 符号类别，如命名空间、类、方法、变量等
    public SyntaxKind Type { get; } // 符号类型，如 int、string、void 等
    public Symbol? Parent { get; }
    private readonly List<Symbol> _children = [];

    public string FullName => Parent is null ? Name : $"{Parent.FullName}.{Name}";
    public IReadOnlyList<Symbol> Children => _children;

    public Symbol(string name, SymbolKind kind, SyntaxKind type, Symbol? parent = null)
    {
        Name = name;
        Kind = kind;
        Type = type;
        Parent = parent;

        Parent?.AddChild(this);
    }

    public void AddChild(Symbol child)
    {
        _children.Add(child);
    }

    public override string ToString()
    {
        // 输出符号的完整信息
        return $"{Kind} {FullName}: {Type}";
    }
}
