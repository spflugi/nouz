using System.Collections.Immutable;
using Nouz.Application.Notebooks;
using Nouz.Domain.Entities;
using Shouldly;

namespace Nouz.Application.Unit.Tests.Notebooks;

public class NotebookReducersTests
{
    private readonly IEnumerable<Nouz.ReduxSimple.On<TestState>> _reducers;

    public NotebookReducersTests()
    {
        _reducers = NotebookReducers.Create<TestState>(s => s.Notebooks);
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

    #region NotebookSelected Tests

    [Fact]
    public void NotebookSelected_ShouldUpdateSelectedNotebook()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        var state = new TestState();

        // Act
        var newState = ApplyAction(state, new NotebookActions.NotebookSelected(notebookId));

        // Assert
        newState.Notebooks.SelectedNotebook.ShouldBe(notebookId);
    }

    [Fact]
    public void NotebookSelected_ShouldNotModifyNotebooksList()
    {
        // Arrange
        var notebook = CreateNotebook("Test");
        var state = new TestState
        {
            Notebooks = new NotebookState { Notebooks = ImmutableList.Create(notebook) }
        };
        var notebookId = Guid.NewGuid();

        // Act
        var newState = ApplyAction(state, new NotebookActions.NotebookSelected(notebookId));

        // Assert
        newState.Notebooks.Notebooks.Count.ShouldBe(1);
        newState.Notebooks.Notebooks[0].ShouldBe(notebook);
    }

    #endregion

    #region NotebookAdded Tests

    [Fact]
    public void NotebookAdded_ShouldAddNotebookToList()
    {
        // Arrange
        var notebook = CreateNotebook("New Notebook");
        var state = new TestState();

        // Act
        var newState = ApplyAction(state, new NotebookActions.NotebookAdded(notebook));

        // Assert
        newState.Notebooks.Notebooks.Count.ShouldBe(1);
        newState.Notebooks.Notebooks[0].ShouldBe(notebook);
    }

    [Fact]
    public void NotebookAdded_ShouldAppendToExistingNotebooks()
    {
        // Arrange
        var existingNotebook = CreateNotebook("Existing");
        var newNotebook = CreateNotebook("New");
        var state = new TestState
        {
            Notebooks = new NotebookState { Notebooks = ImmutableList.Create(existingNotebook) }
        };

        // Act
        var newState = ApplyAction(state, new NotebookActions.NotebookAdded(newNotebook));

        // Assert
        newState.Notebooks.Notebooks.Count.ShouldBe(2);
        newState.Notebooks.Notebooks.ShouldContain(existingNotebook);
        newState.Notebooks.Notebooks.ShouldContain(newNotebook);
    }

    #endregion

    #region NotebookUpdated Tests

    [Fact]
    public void NotebookUpdated_ShouldReplaceExistingNotebook()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        var originalNotebook = new Notebook
        {
            Id = notebookId,
            Name = "Original",
            CreatedAt = DateTimeOffset.UtcNow,
            LastModifiedAt = DateTimeOffset.UtcNow
        };
        var updatedNotebook = originalNotebook with { Name = "Updated" };
        var state = new TestState
        {
            Notebooks = new NotebookState { Notebooks = ImmutableList.Create(originalNotebook) }
        };

        // Act
        var newState = ApplyAction(state, new NotebookActions.NotebookUpdated(updatedNotebook));

        // Assert
        newState.Notebooks.Notebooks.Count.ShouldBe(1);
        newState.Notebooks.Notebooks[0].Name.ShouldBe("Updated");
        newState.Notebooks.Notebooks[0].Id.ShouldBe(notebookId);
    }

    [Fact]
    public void NotebookUpdated_WhenNotebookNotFound_ShouldReturnUnchangedState()
    {
        // Arrange
        var existingNotebook = CreateNotebook("Existing");
        var nonExistingNotebook = CreateNotebook("Non-existing");
        var state = new TestState
        {
            Notebooks = new NotebookState { Notebooks = ImmutableList.Create(existingNotebook) }
        };

        // Act
        var newState = ApplyAction(state, new NotebookActions.NotebookUpdated(nonExistingNotebook));

        // Assert
        newState.Notebooks.Notebooks.Count.ShouldBe(1);
        newState.Notebooks.Notebooks[0].ShouldBe(existingNotebook);
    }

    [Fact]
    public void NotebookUpdated_ShouldOnlyUpdateMatchingNotebook()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        var notebook1 = new Notebook
        {
            Id = notebookId,
            Name = "Notebook 1",
            CreatedAt = DateTimeOffset.UtcNow,
            LastModifiedAt = DateTimeOffset.UtcNow
        };
        var notebook2 = CreateNotebook("Notebook 2");
        var updatedNotebook1 = notebook1 with { Name = "Updated Notebook 1" };
        var state = new TestState
        {
            Notebooks = new NotebookState { Notebooks = ImmutableList.Create(notebook1, notebook2) }
        };

        // Act
        var newState = ApplyAction(state, new NotebookActions.NotebookUpdated(updatedNotebook1));

        // Assert
        newState.Notebooks.Notebooks.Count.ShouldBe(2);
        newState.Notebooks.Notebooks.First(n => n.Id == notebookId).Name.ShouldBe("Updated Notebook 1");
        newState.Notebooks.Notebooks.First(n => n.Id == notebook2.Id).Name.ShouldBe("Notebook 2");
    }

    #endregion

    #region NotebookDeleted Tests

    [Fact]
    public void NotebookDeleted_ShouldRemoveNotebookFromList()
    {
        // Arrange
        var notebook = CreateNotebook("To Delete");
        var state = new TestState
        {
            Notebooks = new NotebookState { Notebooks = ImmutableList.Create(notebook) }
        };

        // Act
        var newState = ApplyAction(state, new NotebookActions.NotebookDeleted(notebook.Id));

        // Assert
        newState.Notebooks.Notebooks.ShouldBeEmpty();
    }

    [Fact]
    public void NotebookDeleted_WhenNotebookNotFound_ShouldReturnUnchangedState()
    {
        // Arrange
        var notebook = CreateNotebook("Existing");
        var state = new TestState
        {
            Notebooks = new NotebookState { Notebooks = ImmutableList.Create(notebook) }
        };

        // Act
        var newState = ApplyAction(state, new NotebookActions.NotebookDeleted(Guid.NewGuid()));

        // Assert
        newState.Notebooks.Notebooks.Count.ShouldBe(1);
        newState.Notebooks.Notebooks[0].ShouldBe(notebook);
    }

    [Fact]
    public void NotebookDeleted_ShouldOnlyRemoveMatchingNotebook()
    {
        // Arrange
        var notebook1 = CreateNotebook("Notebook 1");
        var notebook2 = CreateNotebook("Notebook 2");
        var state = new TestState
        {
            Notebooks = new NotebookState { Notebooks = ImmutableList.Create(notebook1, notebook2) }
        };

        // Act
        var newState = ApplyAction(state, new NotebookActions.NotebookDeleted(notebook1.Id));

        // Assert
        newState.Notebooks.Notebooks.Count.ShouldBe(1);
        newState.Notebooks.Notebooks[0].ShouldBe(notebook2);
    }

    #endregion

    #region AllNotebooksLoaded Tests

    [Fact]
    public void AllNotebooksLoaded_ShouldReplaceNotebooksList()
    {
        // Arrange
        var existingNotebook = CreateNotebook("Existing");
        var newNotebooks = ImmutableList.Create(
            CreateNotebook("New 1"),
            CreateNotebook("New 2")
        );
        var state = new TestState
        {
            Notebooks = new NotebookState { Notebooks = ImmutableList.Create(existingNotebook) }
        };

        // Act
        var newState = ApplyAction(state, new NotebookActions.AllNotebooksLoaded(newNotebooks));

        // Assert
        newState.Notebooks.Notebooks.Count.ShouldBe(2);
        newState.Notebooks.Notebooks.ShouldNotContain(existingNotebook);
    }

    [Fact]
    public void AllNotebooksLoaded_WithEmptyList_ShouldClearNotebooks()
    {
        // Arrange
        var existingNotebook = CreateNotebook("Existing");
        var state = new TestState
        {
            Notebooks = new NotebookState { Notebooks = ImmutableList.Create(existingNotebook) }
        };

        // Act
        var newState = ApplyAction(state, new NotebookActions.AllNotebooksLoaded(ImmutableList<Notebook>.Empty));

        // Assert
        newState.Notebooks.Notebooks.ShouldBeEmpty();
    }

    #endregion

    private static Notebook CreateNotebook(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        CreatedAt = DateTimeOffset.UtcNow,
        LastModifiedAt = DateTimeOffset.UtcNow
    };

    private sealed record TestState
    {
        public NotebookState Notebooks { get; init; } = new();
    }
}
