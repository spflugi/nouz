using Microsoft.SemanticKernel;

namespace Nouz.Infrastructure.Chat;

/// <summary>
/// Semantic Kernel function invocation filter that notifies callers when tool calls start and complete.
/// </summary>
internal sealed class ToolCallProgressFilter : IFunctionInvocationFilter
{
    private static readonly Dictionary<string, string> FunctionLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ListNotebooks"] = "Listed notebooks",
        ["CreateNotebook"] = "Created notebook",
        ["GetNotebookNotes"] = "Read notebook",
        ["CreateNote"] = "Created note",
        ["GetNoteContent"] = "Read note",
        ["EditNote"] = "Updated note",
        ["DeleteNote"] = "Deleted note",
        ["SearchNotes"] = "Searched notes"
    };

    private readonly Action<string, bool> _onToolCall;

    public ToolCallProgressFilter(Action<string, bool> onToolCall)
    {
        _onToolCall = onToolCall;
    }

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        var functionName = context.Function.Name;
        _onToolCall(functionName, false);

        await next(context).ConfigureAwait(false);

        _onToolCall(functionName, true);
    }

    public static string GetLabel(string functionName)
    {
        return FunctionLabels.TryGetValue(functionName, out var label) ? label : functionName;
    }
}
