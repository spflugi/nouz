using Nouz.ReduxSimple;
using System;

namespace Nouz.Application.Notebooks;

public static class NotebookReducers
{
    public static IEnumerable<On<T>> Create<T>(Func<T, NotebookState> selector) where T : class, new()
    {
        return Reducers.CreateSubReducers(selector)
            .On<NotebookActions.NotebookSelected>((state, action) =>
                state with { SelectedNotebook = action.Id })
            .On<NotebookActions.NotebookAdded>((state, action) =>
            {
                var updatedNotebooks = state.Notebooks.Add(action.Notebook);
                return state with { Notebooks = updatedNotebooks };
            })
            .On<NotebookActions.NotebookUpdated>((state, action) =>
            {
                var oldNotebook = state.Notebooks.Find(n => n.Id == action.Notebook.Id);

                if (oldNotebook is null)
                {
                    return state;
                }

                var updatedNotebooks = state.Notebooks.Replace(oldNotebook, action.Notebook);
                return state with { Notebooks = updatedNotebooks };
            })
            .On<NotebookActions.NotebookDeleted>((state, action) =>
            {
                var updatedNotebooks = state.Notebooks.RemoveAll(n => n.Id == action.Id);
                return state with { Notebooks = updatedNotebooks };
            })
            .On<NotebookActions.AllNotebooksLoaded>((state, action) =>
                state with { Notebooks = action.Notebooks })
            .ToList();
    }
}