using System.Collections.Immutable;
using Nouz.Application.Notes;
using Nouz.Domain.Entities;
using Shouldly;

namespace Nouz.Application.Unit.Tests.Notes;

public class NoteReducersTests
{
    private readonly IEnumerable<Nouz.ReduxSimple.On<TestState>> _reducers;

    public NoteReducersTests()
    {
        _reducers = NoteReducers.Create<TestState>(s => s.Notes);
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

    #region NotesLoaded Tests

    [Fact]
    public void NotesLoaded_ShouldReplaceNotesList()
    {
        // Arrange
        var existingNote = CreateNote(Guid.NewGuid());
        var newNotes = ImmutableList.Create(
            CreateNote(Guid.NewGuid()),
            CreateNote(Guid.NewGuid())
        );
        var state = new TestState
        {
            Notes = new NoteState { Notes = ImmutableList.Create(existingNote) }
        };

        // Act
        var newState = ApplyAction(state, new NoteActions.NotesLoaded(newNotes));

        // Assert
        newState.Notes.Notes.Count.ShouldBe(2);
        newState.Notes.Notes.ShouldNotContain(existingNote);
    }

    [Fact]
    public void NotesLoaded_WithEmptyList_ShouldClearNotes()
    {
        // Arrange
        var existingNote = CreateNote(Guid.NewGuid());
        var state = new TestState
        {
            Notes = new NoteState { Notes = ImmutableList.Create(existingNote) }
        };

        // Act
        var newState = ApplyAction(state, new NoteActions.NotesLoaded(ImmutableList<Note>.Empty));

        // Assert
        newState.Notes.Notes.ShouldBeEmpty();
    }

    #endregion

    #region NoteCreated Tests

    [Fact]
    public void NoteCreated_ShouldAddNoteAtBeginning()
    {
        // Arrange
        var existingNote = CreateNote(Guid.NewGuid());
        var newNote = CreateNote(Guid.NewGuid());
        var state = new TestState
        {
            Notes = new NoteState { Notes = ImmutableList.Create(existingNote) }
        };

        // Act
        var newState = ApplyAction(state, new NoteActions.NoteCreated(newNote));

        // Assert
        newState.Notes.Notes.Count.ShouldBe(2);
        newState.Notes.Notes[0].ShouldBe(newNote);
        newState.Notes.Notes[1].ShouldBe(existingNote);
    }

    [Fact]
    public void NoteCreated_WhenListIsEmpty_ShouldAddNote()
    {
        // Arrange
        var newNote = CreateNote(Guid.NewGuid());
        var state = new TestState();

        // Act
        var newState = ApplyAction(state, new NoteActions.NoteCreated(newNote));

        // Assert
        newState.Notes.Notes.Count.ShouldBe(1);
        newState.Notes.Notes[0].ShouldBe(newNote);
    }

    #endregion

    #region NoteUpdated Tests

    [Fact]
    public void NoteUpdated_ShouldReplaceExistingNote()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        var originalNote = new Note
        {
            Id = noteId,
            NotebookId = notebookId,
            CreatedAt = DateTimeOffset.UtcNow,
            LastModifiedAt = DateTimeOffset.UtcNow,
            Blocks = ImmutableList.Create(CreateBlock("Original"))
        };
        var updatedNote = originalNote with
        {
            Blocks = ImmutableList.Create(CreateBlock("Updated"))
        };
        var state = new TestState
        {
            Notes = new NoteState { Notes = ImmutableList.Create(originalNote) }
        };

        // Act
        var newState = ApplyAction(state, new NoteActions.NoteUpdated(updatedNote));

        // Assert
        newState.Notes.Notes.Count.ShouldBe(1);
        newState.Notes.Notes[0].Blocks[0].Content.ShouldBe("Updated");
    }

    [Fact]
    public void NoteUpdated_WhenNoteNotFound_ShouldReturnUnchangedState()
    {
        // Arrange
        var existingNote = CreateNote(Guid.NewGuid());
        var nonExistingNote = CreateNote(Guid.NewGuid());
        var state = new TestState
        {
            Notes = new NoteState { Notes = ImmutableList.Create(existingNote) }
        };

        // Act
        var newState = ApplyAction(state, new NoteActions.NoteUpdated(nonExistingNote));

        // Assert
        newState.Notes.Notes.Count.ShouldBe(1);
        newState.Notes.Notes[0].ShouldBe(existingNote);
    }

    [Fact]
    public void NoteUpdated_ShouldOnlyUpdateMatchingNote()
    {
        // Arrange
        var noteId1 = Guid.NewGuid();
        var noteId2 = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        var note1 = new Note
        {
            Id = noteId1,
            NotebookId = notebookId,
            CreatedAt = DateTimeOffset.UtcNow,
            LastModifiedAt = DateTimeOffset.UtcNow,
            Blocks = ImmutableList.Create(CreateBlock("Note 1"))
        };
        var note2 = CreateNote(noteId2);
        var updatedNote1 = note1 with
        {
            Blocks = ImmutableList.Create(CreateBlock("Updated Note 1"))
        };
        var state = new TestState
        {
            Notes = new NoteState { Notes = ImmutableList.Create(note1, note2) }
        };

        // Act
        var newState = ApplyAction(state, new NoteActions.NoteUpdated(updatedNote1));

        // Assert
        newState.Notes.Notes.Count.ShouldBe(2);
        newState.Notes.Notes.First(n => n.Id == noteId1).Blocks[0].Content.ShouldBe("Updated Note 1");
        newState.Notes.Notes.First(n => n.Id == noteId2).ShouldBe(note2);
    }

    #endregion

    #region NoteDeleted Tests

    [Fact]
    public void NoteDeleted_ShouldRemoveNoteFromList()
    {
        // Arrange
        var note = CreateNote(Guid.NewGuid());
        var state = new TestState
        {
            Notes = new NoteState { Notes = ImmutableList.Create(note) }
        };

        // Act
        var newState = ApplyAction(state, new NoteActions.NoteDeleted(note.Id));

        // Assert
        newState.Notes.Notes.ShouldBeEmpty();
    }

    [Fact]
    public void NoteDeleted_WhenNoteNotFound_ShouldReturnUnchangedState()
    {
        // Arrange
        var note = CreateNote(Guid.NewGuid());
        var state = new TestState
        {
            Notes = new NoteState { Notes = ImmutableList.Create(note) }
        };

        // Act
        var newState = ApplyAction(state, new NoteActions.NoteDeleted(Guid.NewGuid()));

        // Assert
        newState.Notes.Notes.Count.ShouldBe(1);
        newState.Notes.Notes[0].ShouldBe(note);
    }

    [Fact]
    public void NoteDeleted_ShouldOnlyRemoveMatchingNote()
    {
        // Arrange
        var note1 = CreateNote(Guid.NewGuid());
        var note2 = CreateNote(Guid.NewGuid());
        var state = new TestState
        {
            Notes = new NoteState { Notes = ImmutableList.Create(note1, note2) }
        };

        // Act
        var newState = ApplyAction(state, new NoteActions.NoteDeleted(note1.Id));

        // Assert
        newState.Notes.Notes.Count.ShouldBe(1);
        newState.Notes.Notes[0].ShouldBe(note2);
    }

    #endregion

    #region EditingBlockChanged Tests

    [Fact]
    public void EditingBlockChanged_ShouldUpdateEditingState()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var state = new TestState();

        // Act
        var newState = ApplyAction(state, new NoteActions.EditingBlockChanged(noteId, blockId));

        // Assert
        newState.Notes.EditingNoteId.ShouldBe(noteId);
        newState.Notes.EditingBlockId.ShouldBe(blockId);
    }

    [Fact]
    public void EditingBlockChanged_ShouldNotAffectNotesList()
    {
        // Arrange
        var note = CreateNote(Guid.NewGuid());
        var state = new TestState
        {
            Notes = new NoteState { Notes = ImmutableList.Create(note) }
        };

        // Act
        var newState = ApplyAction(state, new NoteActions.EditingBlockChanged(Guid.NewGuid(), Guid.NewGuid()));

        // Assert
        newState.Notes.Notes.Count.ShouldBe(1);
        newState.Notes.Notes[0].ShouldBe(note);
    }

    #endregion

    #region NotesCleared Tests

    [Fact]
    public void NotesCleared_ShouldResetAllNoteState()
    {
        // Arrange
        var note = CreateNote(Guid.NewGuid());
        var state = new TestState
        {
            Notes = new NoteState
            {
                Notes = ImmutableList.Create(note),
                EditingNoteId = Guid.NewGuid(),
                EditingBlockId = Guid.NewGuid(),
                SearchQuery = "test",
                FilteredNotes = ImmutableList.Create(note)
            }
        };

        // Act
        var newState = ApplyAction(state, new NoteActions.NotesCleared());

        // Assert
        newState.Notes.Notes.ShouldBeEmpty();
        newState.Notes.EditingNoteId.ShouldBeNull();
        newState.Notes.EditingBlockId.ShouldBeNull();
        newState.Notes.SearchQuery.ShouldBe(string.Empty);
        newState.Notes.FilteredNotes.ShouldBeNull();
    }

    #endregion

    #region SearchResultsLoaded Tests

    [Fact]
    public void SearchResultsLoaded_ShouldSetSearchQueryAndFilteredNotes()
    {
        // Arrange
        var note = CreateNote(Guid.NewGuid());
        var filteredNotes = ImmutableList.Create(note);
        var state = new TestState();

        // Act
        var newState = ApplyAction(state, new NoteActions.SearchResultsLoaded("test query", filteredNotes));

        // Assert
        newState.Notes.SearchQuery.ShouldBe("test query");
        newState.Notes.FilteredNotes.ShouldBe(filteredNotes);
    }

    [Fact]
    public void SearchResultsLoaded_ShouldNotAffectMainNotesList()
    {
        // Arrange
        var note1 = CreateNote(Guid.NewGuid());
        var note2 = CreateNote(Guid.NewGuid());
        var state = new TestState
        {
            Notes = new NoteState { Notes = ImmutableList.Create(note1, note2) }
        };
        var filteredNotes = ImmutableList.Create(note1);

        // Act
        var newState = ApplyAction(state, new NoteActions.SearchResultsLoaded("query", filteredNotes));

        // Assert
        newState.Notes.Notes.Count.ShouldBe(2);
        newState.Notes.FilteredNotes!.Count.ShouldBe(1);
    }

    #endregion

    #region SearchCleared Tests

    [Fact]
    public void SearchCleared_ShouldClearSearchQueryAndFilteredNotes()
    {
        // Arrange
        var note = CreateNote(Guid.NewGuid());
        var state = new TestState
        {
            Notes = new NoteState
            {
                Notes = ImmutableList.Create(note),
                SearchQuery = "test",
                FilteredNotes = ImmutableList.Create(note)
            }
        };

        // Act
        var newState = ApplyAction(state, new NoteActions.SearchCleared());

        // Assert
        newState.Notes.SearchQuery.ShouldBe(string.Empty);
        newState.Notes.FilteredNotes.ShouldBeNull();
        newState.Notes.Notes.Count.ShouldBe(1);
    }

    #endregion

    #region NoteMoved Tests

    [Fact]
    public void NoteMoved_ShouldRemoveNoteFromList()
    {
        // Arrange
        var note = CreateNote(Guid.NewGuid());
        var state = new TestState
        {
            Notes = new NoteState { Notes = ImmutableList.Create(note) }
        };

        // Act
        var newState = ApplyAction(state, new NoteActions.NoteMoved(note.Id, Guid.NewGuid()));

        // Assert
        newState.Notes.Notes.ShouldBeEmpty();
    }

    [Fact]
    public void NoteMoved_ShouldOnlyRemoveMovedNote()
    {
        // Arrange
        var note1 = CreateNote(Guid.NewGuid());
        var note2 = CreateNote(Guid.NewGuid());
        var state = new TestState
        {
            Notes = new NoteState { Notes = ImmutableList.Create(note1, note2) }
        };

        // Act
        var newState = ApplyAction(state, new NoteActions.NoteMoved(note1.Id, Guid.NewGuid()));

        // Assert
        newState.Notes.Notes.Count.ShouldBe(1);
        newState.Notes.Notes[0].ShouldBe(note2);
    }

    #endregion

    private static Note CreateNote(Guid noteId) => new()
    {
        Id = noteId,
        NotebookId = Guid.NewGuid(),
        CreatedAt = DateTimeOffset.UtcNow,
        LastModifiedAt = DateTimeOffset.UtcNow,
        Blocks = ImmutableList.Create(CreateBlock("Content"))
    };

    private static Block CreateBlock(string content) => new()
    {
        Id = Guid.NewGuid(),
        Type = BlockType.Paragraph,
        Content = content,
        Order = 0
    };

    private sealed record TestState
    {
        public NoteState Notes { get; init; } = new();
    }
}
