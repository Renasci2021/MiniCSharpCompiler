# MiniCSharpCompiler

A simple C# compiler implementation for learning compiler principles. This project is a course assignment that demonstrates the basic compilation process including lexical analysis, syntax analysis, semantic analysis, and code generation.

## Project Structure

```
src/
├── MiniCSharpCompiler.Core/       # Core library for the compiler
│   ├── Interfaces/                # Interfaces for Lexer and Parser
│   │   ├── ILexer.cs              # Interface for Lexer
│   │   └── IParser.cs             # Interface for Parser
│   ├── Lexer/                     # Lexer implementation
│   │   ├── Lexer.cs               # Implementation of Lexer
│   │   └── StandardLexer.cs       # Standard implementation with Roslyn API
│   ├── Parser/                    # Parser implementation
│   │   ├── Parser.cs              # Implementation of Parser
│   │   └── StandardParser.cs      # Standard implementation with Roslyn API
├── MiniCSharpCompiler.Main/       # Main project for the compiler
│   ├── Utilities/                 # Utility classes
│   │   └── SyntaxPrinter.cs       # Helper class for printing syntax tree
│   └── Program.cs                 # Entry point of the compiler
└── README.md                      # Project documentation
```

## Features (In Progress)

- [ ] Lexical Analysis: Convert source code into token stream
- [ ] Syntax Analysis: Build Abstract Syntax Tree (AST)
- [ ] Semantic Analysis: Type checking and symbol resolution
- [ ] Code Generation: Generate IL code

## Usage

```sh
cd src
dotnet run --project MiniCSharpCompiler [<source-file>]
```

You can run the compiler with the source file as an argument. If you don't provide the source file, the compiler will read the source code from the console input.

## Dependencies

- [.NET 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Microsoft.CodeAnalysis](https://www.nuget.org/packages/Microsoft.CodeAnalysis/)
- [Microsoft.CodeAnalysis.CSharp](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp/)
