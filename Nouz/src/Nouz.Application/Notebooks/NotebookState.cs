using System.Collections.Immutable;
using Nouz.Domain.Entities;

namespace Nouz.Application.Notebooks;

public sealed record NotebookState
{
    public Guid SelectedNotebook { get; init; }

    public ImmutableList<Notebook> Notebooks { get; init; } = [];
}