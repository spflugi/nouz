using Mediator;

namespace Nouz.Application.Notebooks;

public static class NotebookCommands
{
    public sealed record LoadAllNotebooks() : ICommand;

    public sealed record CreateNotebook(string Title) : ICommand;

    public sealed record SelectNotebook(Guid Id) : ICommand;
}