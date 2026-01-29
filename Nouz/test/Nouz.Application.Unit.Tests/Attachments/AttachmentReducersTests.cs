using System.Collections.Immutable;
using Nouz.Application.Attachments;
using Nouz.Application.Notes;
using Nouz.Domain.Entities;
using Shouldly;

namespace Nouz.Application.Unit.Tests.Attachments;

public class AttachmentReducersTests
{
    private readonly IEnumerable<Nouz.ReduxSimple.On<TestState>> _reducers;

    public AttachmentReducersTests()
    {
        _reducers = AttachmentReducers.Create<TestState>(s => s.Notes);
    }

    private TestState ApplyAction(TestState state, object action)
    {
        foreach (var reducer in _reducers)
        {
            if (reducer.Reduce != null)
            {
                state = reducer.Reduce(state, action);
            }
        }
        return state;
    }

    #region AttachmentsLoaded Tests

    [Fact]
    public void AttachmentsLoaded_ShouldSetAttachmentsForNote()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var attachments = ImmutableList.Create(
            CreateAttachment(noteId, "file1.pdf"),
            CreateAttachment(noteId, "file2.pdf")
        );
        var state = new TestState();

        // Act
        var newState = ApplyAction(state, new AttachmentActions.AttachmentsLoaded(noteId, attachments));

        // Assert
        newState.Notes.AttachmentsByNoteId.ContainsKey(noteId).ShouldBeTrue();
        newState.Notes.AttachmentsByNoteId[noteId].Count.ShouldBe(2);
    }

    [Fact]
    public void AttachmentsLoaded_ShouldReplaceExistingAttachments()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var oldAttachments = ImmutableList.Create(CreateAttachment(noteId, "old.pdf"));
        var newAttachments = ImmutableList.Create(CreateAttachment(noteId, "new.pdf"));
        var state = new TestState
        {
            Notes = new NoteState
            {
                AttachmentsByNoteId = ImmutableDictionary<Guid, ImmutableList<NoteAttachment>>.Empty
                    .Add(noteId, oldAttachments)
            }
        };

        // Act
        var newState = ApplyAction(state, new AttachmentActions.AttachmentsLoaded(noteId, newAttachments));

        // Assert
        newState.Notes.AttachmentsByNoteId[noteId].Count.ShouldBe(1);
        newState.Notes.AttachmentsByNoteId[noteId][0].FileName.ShouldBe("new.pdf");
    }

    [Fact]
    public void AttachmentsLoaded_WithEmptyList_ShouldSetEmptyList()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var state = new TestState();

        // Act
        var newState = ApplyAction(state, new AttachmentActions.AttachmentsLoaded(noteId, ImmutableList<NoteAttachment>.Empty));

        // Assert
        newState.Notes.AttachmentsByNoteId.ContainsKey(noteId).ShouldBeTrue();
        newState.Notes.AttachmentsByNoteId[noteId].ShouldBeEmpty();
    }

    [Fact]
    public void AttachmentsLoaded_ShouldNotAffectOtherNotes()
    {
        // Arrange
        var noteId1 = Guid.NewGuid();
        var noteId2 = Guid.NewGuid();
        var attachments1 = ImmutableList.Create(CreateAttachment(noteId1, "file1.pdf"));
        var attachments2 = ImmutableList.Create(CreateAttachment(noteId2, "file2.pdf"));
        var state = new TestState
        {
            Notes = new NoteState
            {
                AttachmentsByNoteId = ImmutableDictionary<Guid, ImmutableList<NoteAttachment>>.Empty
                    .Add(noteId1, attachments1)
            }
        };

        // Act
        var newState = ApplyAction(state, new AttachmentActions.AttachmentsLoaded(noteId2, attachments2));

        // Assert
        newState.Notes.AttachmentsByNoteId[noteId1].Count.ShouldBe(1);
        newState.Notes.AttachmentsByNoteId[noteId2].Count.ShouldBe(1);
    }

    #endregion

    #region AttachmentAdded Tests

    [Fact]
    public void AttachmentAdded_ShouldInsertAtBeginning()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var existingAttachment = CreateAttachment(noteId, "existing.pdf");
        var newAttachment = CreateAttachment(noteId, "new.pdf");
        var state = new TestState
        {
            Notes = new NoteState
            {
                AttachmentsByNoteId = ImmutableDictionary<Guid, ImmutableList<NoteAttachment>>.Empty
                    .Add(noteId, ImmutableList.Create(existingAttachment))
            }
        };

        // Act
        var newState = ApplyAction(state, new AttachmentActions.AttachmentAdded(noteId, newAttachment));

        // Assert
        newState.Notes.AttachmentsByNoteId[noteId].Count.ShouldBe(2);
        newState.Notes.AttachmentsByNoteId[noteId][0].ShouldBe(newAttachment);
        newState.Notes.AttachmentsByNoteId[noteId][1].ShouldBe(existingAttachment);
    }

    [Fact]
    public void AttachmentAdded_WhenNoExistingAttachments_ShouldCreateList()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var attachment = CreateAttachment(noteId, "new.pdf");
        var state = new TestState();

        // Act
        var newState = ApplyAction(state, new AttachmentActions.AttachmentAdded(noteId, attachment));

        // Assert
        newState.Notes.AttachmentsByNoteId.ContainsKey(noteId).ShouldBeTrue();
        newState.Notes.AttachmentsByNoteId[noteId].Count.ShouldBe(1);
        newState.Notes.AttachmentsByNoteId[noteId][0].ShouldBe(attachment);
    }

    [Fact]
    public void AttachmentAdded_ShouldNotAffectOtherNotes()
    {
        // Arrange
        var noteId1 = Guid.NewGuid();
        var noteId2 = Guid.NewGuid();
        var attachment1 = CreateAttachment(noteId1, "file1.pdf");
        var newAttachment2 = CreateAttachment(noteId2, "file2.pdf");
        var state = new TestState
        {
            Notes = new NoteState
            {
                AttachmentsByNoteId = ImmutableDictionary<Guid, ImmutableList<NoteAttachment>>.Empty
                    .Add(noteId1, ImmutableList.Create(attachment1))
            }
        };

        // Act
        var newState = ApplyAction(state, new AttachmentActions.AttachmentAdded(noteId2, newAttachment2));

        // Assert
        newState.Notes.AttachmentsByNoteId[noteId1].Count.ShouldBe(1);
        newState.Notes.AttachmentsByNoteId[noteId2].Count.ShouldBe(1);
    }

    #endregion

    #region AttachmentDeleted Tests

    [Fact]
    public void AttachmentDeleted_ShouldRemoveAttachment()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var attachment1 = CreateAttachment(noteId, "file1.pdf");
        var attachment2 = CreateAttachment(noteId, "file2.pdf");
        var state = new TestState
        {
            Notes = new NoteState
            {
                AttachmentsByNoteId = ImmutableDictionary<Guid, ImmutableList<NoteAttachment>>.Empty
                    .Add(noteId, ImmutableList.Create(attachment1, attachment2))
            }
        };

        // Act
        var newState = ApplyAction(state, new AttachmentActions.AttachmentDeleted(noteId, attachment1.Id));

        // Assert
        newState.Notes.AttachmentsByNoteId[noteId].Count.ShouldBe(1);
        newState.Notes.AttachmentsByNoteId[noteId][0].Id.ShouldBe(attachment2.Id);
    }

    [Fact]
    public void AttachmentDeleted_WhenNoteNotFound_ShouldReturnUnchangedState()
    {
        // Arrange
        var state = new TestState();
        var noteId = Guid.NewGuid();
        var attachmentId = Guid.NewGuid();

        // Act
        var newState = ApplyAction(state, new AttachmentActions.AttachmentDeleted(noteId, attachmentId));

        // Assert
        newState.Notes.AttachmentsByNoteId.ContainsKey(noteId).ShouldBeFalse();
    }

    [Fact]
    public void AttachmentDeleted_WhenAttachmentNotFound_ShouldReturnUnchangedList()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var attachment = CreateAttachment(noteId, "file.pdf");
        var state = new TestState
        {
            Notes = new NoteState
            {
                AttachmentsByNoteId = ImmutableDictionary<Guid, ImmutableList<NoteAttachment>>.Empty
                    .Add(noteId, ImmutableList.Create(attachment))
            }
        };

        // Act
        var newState = ApplyAction(state, new AttachmentActions.AttachmentDeleted(noteId, Guid.NewGuid()));

        // Assert
        newState.Notes.AttachmentsByNoteId[noteId].Count.ShouldBe(1);
    }

    [Fact]
    public void AttachmentDeleted_ShouldNotAffectOtherNotes()
    {
        // Arrange
        var noteId1 = Guid.NewGuid();
        var noteId2 = Guid.NewGuid();
        var attachment1 = CreateAttachment(noteId1, "file1.pdf");
        var attachment2 = CreateAttachment(noteId2, "file2.pdf");
        var state = new TestState
        {
            Notes = new NoteState
            {
                AttachmentsByNoteId = ImmutableDictionary<Guid, ImmutableList<NoteAttachment>>.Empty
                    .Add(noteId1, ImmutableList.Create(attachment1))
                    .Add(noteId2, ImmutableList.Create(attachment2))
            }
        };

        // Act
        var newState = ApplyAction(state, new AttachmentActions.AttachmentDeleted(noteId1, attachment1.Id));

        // Assert
        newState.Notes.AttachmentsByNoteId[noteId1].ShouldBeEmpty();
        newState.Notes.AttachmentsByNoteId[noteId2].Count.ShouldBe(1);
    }

    #endregion

    #region AttachmentsCleared Tests

    [Fact]
    public void AttachmentsCleared_ShouldRemoveNoteFromDictionary()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var attachments = ImmutableList.Create(CreateAttachment(noteId, "file.pdf"));
        var state = new TestState
        {
            Notes = new NoteState
            {
                AttachmentsByNoteId = ImmutableDictionary<Guid, ImmutableList<NoteAttachment>>.Empty
                    .Add(noteId, attachments)
            }
        };

        // Act
        var newState = ApplyAction(state, new AttachmentActions.AttachmentsCleared(noteId));

        // Assert
        newState.Notes.AttachmentsByNoteId.ContainsKey(noteId).ShouldBeFalse();
    }

    [Fact]
    public void AttachmentsCleared_WhenNoteNotInDictionary_ShouldReturnUnchangedState()
    {
        // Arrange
        var state = new TestState();

        // Act
        var newState = ApplyAction(state, new AttachmentActions.AttachmentsCleared(Guid.NewGuid()));

        // Assert
        newState.Notes.AttachmentsByNoteId.ShouldBeEmpty();
    }

    [Fact]
    public void AttachmentsCleared_ShouldNotAffectOtherNotes()
    {
        // Arrange
        var noteId1 = Guid.NewGuid();
        var noteId2 = Guid.NewGuid();
        var attachments1 = ImmutableList.Create(CreateAttachment(noteId1, "file1.pdf"));
        var attachments2 = ImmutableList.Create(CreateAttachment(noteId2, "file2.pdf"));
        var state = new TestState
        {
            Notes = new NoteState
            {
                AttachmentsByNoteId = ImmutableDictionary<Guid, ImmutableList<NoteAttachment>>.Empty
                    .Add(noteId1, attachments1)
                    .Add(noteId2, attachments2)
            }
        };

        // Act
        var newState = ApplyAction(state, new AttachmentActions.AttachmentsCleared(noteId1));

        // Assert
        newState.Notes.AttachmentsByNoteId.ContainsKey(noteId1).ShouldBeFalse();
        newState.Notes.AttachmentsByNoteId[noteId2].Count.ShouldBe(1);
    }

    #endregion

    private static NoteAttachment CreateAttachment(Guid noteId, string fileName)
    {
        return new NoteAttachment
        {
            Id = Guid.NewGuid(),
            NoteId = noteId,
            FileName = fileName,
            Extension = ".pdf",
            FileSizeBytes = 1024,
            ContentHash = "abc123",
            AddedAt = DateTimeOffset.UtcNow
        };
    }

    private sealed record TestState
    {
        public NoteState Notes { get; init; } = new();
    }
}
