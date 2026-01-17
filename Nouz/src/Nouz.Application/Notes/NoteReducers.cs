using Nouz.ReduxSimple;

namespace Nouz.Application.Notes;

public static class NoteReducers
{
    public static IEnumerable<On<T>> Create<T>(Func<T, NoteState> selector) where T : class, new()
    {
        return Reducers.CreateSubReducers(selector)
            .On<NoteActions.NotesLoaded>((state, action) =>
                state with { Notes = action.Notes })
            .On<NoteActions.NoteCreated>((state, action) =>
            {
                var updatedNotes = state.Notes.Insert(0, action.Note);
                return state with { Notes = updatedNotes };
            })
            .On<NoteActions.NoteUpdated>((state, action) =>
            {
                var oldNote = state.Notes.Find(n => n.Id == action.Note.Id);

                if (oldNote is null)
                {
                    return state;
                }

                var updatedNotes = state.Notes.Replace(oldNote, action.Note);
                return state with { Notes = updatedNotes };
            })
            .On<NoteActions.NoteDeleted>((state, action) =>
            {
                var updatedNotes = state.Notes.RemoveAll(n => n.Id == action.NoteId);
                return state with { Notes = updatedNotes };
            })
            .On<NoteActions.EditingBlockChanged>((state, action) =>
                state with { EditingNoteId = action.NoteId, EditingBlockId = action.BlockId })
            .On<NoteActions.NotesCleared>((state, _) =>
                state with { Notes = [], FilteredNotes = null, SearchQuery = string.Empty, EditingNoteId = null, EditingBlockId = null })
            .On<NoteActions.SearchResultsLoaded>((state, action) =>
                state with { SearchQuery = action.Query, FilteredNotes = action.FilteredNotes })
            .On<NoteActions.SearchCleared>((state, _) =>
                state with { SearchQuery = string.Empty, FilteredNotes = null })
            .ToList();
    }
}
