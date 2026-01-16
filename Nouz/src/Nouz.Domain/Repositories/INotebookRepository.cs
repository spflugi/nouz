using Nouz.Domain.Entities;

namespace Nouz.Domain.Repositories;

public interface INotebookRepository
{
    /// <summary>
    /// Return all available notebooks.
    /// </summary>
    Task<IReadOnlyList<Notebook>> GetAll(CancellationToken token = default);

    /// <summary>
    /// Return the notebook with the given id - null otherwise.
    /// </summary>
    Task<Notebook?> GetById(Guid id, CancellationToken token = default);

    /// <summary>
    /// Add a new notebook.
    /// </summary>
    Task Add(Notebook notebook, CancellationToken token = default);

    /// <summary>
    /// Update an existing notebook.
    /// </summary>
    Task Update(Notebook notebook, CancellationToken token = default);
}