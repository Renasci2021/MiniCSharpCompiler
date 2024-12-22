namespace MiniCSharpCompiler.Core.SemanticAnalysis;

public class SymbolTable
{
    private readonly Dictionary<string, Symbol> _symbols = [];
    private readonly HashSet<string> _usingNamespaces = [];

    public void AddSymbol(Symbol symbol)
    {
        var name = symbol.FullName;
        _symbols.Add(name, symbol);
    }

    public bool TryAddSymbol(Symbol symbol)
    {
        // 检查当前作用域是否已存在同名符号
        if (IsSymbolDeclaredInScope(symbol.Name, symbol.Parent))
        {
            return false;
        }

        AddSymbol(symbol);
        return true;
    }

    public bool TryGetSymbol(string name, out Symbol? symbol)
        => _symbols.TryGetValue(name, out symbol);

    public void AddNamespace(string ns)
        => _usingNamespaces.Add(ns);

    public bool TryResolveSymbol(string name, Symbol? currentScope, out Symbol? symbol)
    {
        // // 作用域查找逻辑
        // return TryResolveInScope(name, currentScope, out symbol)
        //     || TryResolveInNamespace(name, out symbol);

        // 优先从当前作用域查找
        if (TryResolveInScope(name, currentScope, out symbol))
            return true;

        // 其次从全局命名空间查找
        return TryResolveInNamespace(name, out symbol);
    }

    private bool IsSymbolDeclaredInScope(string name, Symbol? scope)
    {
        if (scope == null) return false;

        foreach (var symbol in _symbols.Values)
        {
            if (symbol.Parent == scope && symbol.Name == name)
            {
                return true;
            }
        }
        return false;
    }

    private bool TryResolveInScope(string name, Symbol? currentScope, out Symbol? symbol)
    {
        // 1. 检查当前作用域
        if (currentScope != null)
        {
            var scopedName = $"{currentScope.FullName}.{name}";
            if (TryGetSymbol(scopedName, out symbol))
                return true;

            // 2. 递归检查父作用域
            return TryResolveInScope(name, currentScope.Parent, out symbol);
        }

        // 3. 全局作用域查找
        return _symbols.TryGetValue(name, out symbol);
    }

    private bool TryResolveInNamespace(string name, out Symbol? symbol)
    {
        // 在已导入的命名空间中查找
        foreach (var ns in _usingNamespaces)
        {
            var fullName = $"{ns}.{name}";
            if (_symbols.TryGetValue(fullName, out symbol))
                return true;
        }

        symbol = null;
        return false;
    }
}
