using Microsoft.CodeAnalysis;

namespace MiniCSharpCompiler.Core.SemanticAnalysis;

public record DiagnosticMessage(string Message, Location Location);
