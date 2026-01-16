using System.Collections.Immutable;
using Nouz.Application.Store;
using Nouz.Domain.Entities;

namespace Nouz.Application.Notebooks;

public static class NotebookActions
{
    /// <summary>
    /// Triggered when a new notebook gets added.
    /// </summary>
    public sealed record NotebookAdded(Notebook Notebook) : IAction;

    /// <summary>
    /// Triggered when a notebook is updated.
    /// </summary>
    /// <param name="Notebook"></param>
    public sealed record NotebookUpdated(Notebook Notebook) : IAction;

    /// <summary>
    /// Triggered when a notebook was deleted.
    /// </summary>
    public sealed record NotebookDeleted(Guid Id) : IAction;

    /// <summary>
    /// Triggered when all notebooks are loaded/refreshed.
    /// </summary>
    public sealed record AllNotebooksLoaded(ImmutableList<Notebook> Notebooks) : IAction;

    /// <summary>
    /// Triggered when a new notebook was selected.
    /// </summary>
    public sealed record NotebookSelected(Guid Id) : IAction;
}