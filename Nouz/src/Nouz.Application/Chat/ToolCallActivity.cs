namespace Nouz.Application.Chat;

/// <summary>
/// Represents a tool/function call made by the AI assistant during response generation.
/// </summary>
public sealed record ToolCallActivity(
    string FunctionName,
    string Label,
    bool IsCompleted);
