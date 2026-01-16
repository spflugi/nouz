using Nouz.ReduxSimple;

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
            .On<NotebookActions.AllNotebooksLoaded>((state, action) =>
                state with { Notebooks = action.Notebooks })
            .ToList();
    }
}