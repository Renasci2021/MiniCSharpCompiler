# MiniCSharpCompiler

A simple C# compiler implementation for learning compiler principles. This project is a course assignment that demonstrates the basic compilation process including lexical analysis, syntax analysis, semantic analysis, and code generation.

## Project Structure

```
src/
├── MiniCSharpCompiler.Core/
│   ├── Interfaces/
│   │   ├── ILexer.cs              # Interface for Lexer
│   │   └── IParser.cs             # Interface for Parser
│   ├── Lexer/
│   │   ├── Lexer.cs               # Implementation of Lexer
│   │   ├── StandardLexer.cs       # Standard implementation with Roslyn API
│   │   └── Token.cs               # Token definition
│   ├── Parser/
│   │   ├── Parser.cs              # Implementation of Parser
│   │   └── StandardParser.cs      # Standard implementation with Roslyn API
├── MiniCSharpCompiler/
│   ├── Utilities/
│   │   └── SyntaxPrinter.cs       # Helper class for printing syntax tree
│   └── Program.cs                 # Entry point of the compiler
├── MiniCSharpCompiler.Test/
│   ├── TestFiles/                 # Test source files
│   └── LexerTests.cs              # Unit tests for Lexer
README.md                          # This file
```

## Features (In Progress)

- [ ] Lexical Analysis: Convert source code into token stream
- [ ] Syntax Analysis: Build Abstract Syntax Tree (AST)
- [ ] Semantic Analysis: Type checking and symbol resolution
- [ ] Code Generation: Generate IL code

## Usage

```sh
cd src/MiniCSharpCompiler/
dotnet run -- [source-file]
```

You can run the compiler with the source file as an argument. If you don't provide the source file, the compiler will read the source code from the console input.

## Dependencies

- [.NET 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Microsoft.CodeAnalysis](https://www.nuget.org/packages/Microsoft.CodeAnalysis/)
- [Microsoft.CodeAnalysis.CSharp](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp/)
