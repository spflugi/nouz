using System.Collections.Immutable;
using Nouz.Application.Notes;
using Nouz.ReduxSimple;

namespace Nouz.Application.Attachments;

public static class AttachmentReducers
{
    public static IEnumerable<On<T>> Create<T>(Func<T, NoteState> selector) where T : class, new()
    {
        return Reducers.CreateSubReducers(selector)
            .On<AttachmentActions.AttachmentsLoaded>((state, action) =>
            {
                var updatedAttachments = state.AttachmentsByNoteId.SetItem(
                    action.NoteId,
                    action.Attachments);
                return state with { AttachmentsByNoteId = updatedAttachments };
            })
            .On<AttachmentActions.AttachmentAdded>((state, action) =>
            {
                var existingAttachments = state.AttachmentsByNoteId.TryGetValue(action.NoteId, out var list)
                    ? list
                    : ImmutableList<Domain.Entities.NoteAttachment>.Empty;

                var updatedList = existingAttachments.Insert(0, action.Attachment);
                var updatedAttachments = state.AttachmentsByNoteId.SetItem(action.NoteId, updatedList);
                return state with { AttachmentsByNoteId = updatedAttachments };
            })
            .On<AttachmentActions.AttachmentDeleted>((state, action) =>
            {
                if (!state.AttachmentsByNoteId.TryGetValue(action.NoteId, out var list))
                {
                    return state;
                }

                var updatedList = list.RemoveAll(a => a.Id == action.AttachmentId);
                var updatedAttachments = state.AttachmentsByNoteId.SetItem(action.NoteId, updatedList);
                return state with { AttachmentsByNoteId = updatedAttachments };
            })
            .On<AttachmentActions.AttachmentsCleared>((state, action) =>
            {
                var updatedAttachments = state.AttachmentsByNoteId.Remove(action.NoteId);
                return state with { AttachmentsByNoteId = updatedAttachments };
            })
            .ToList();
    }
}
