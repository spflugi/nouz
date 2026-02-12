using System.Collections.Immutable;
using Nouz.Application.Chat;
using Nouz.Application.Chat.Models;
using Nouz.Application.Logger;
using Nouz.Domain.Entities;
using Nouz.Infrastructure.Chat.Plugins;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Nouz.Infrastructure.Unit.Tests.Chat;

public class NoteManagementPluginTests
{
    private readonly INoteManagementService _noteManagementService = Substitute.For<INoteManagementService>();
    private readonly ILoggerAdapter<NoteManagementPlugin> _logger = Substitute.For<ILoggerAdapter<NoteManagementPlugin>>();
    private readonly NoteManagementPlugin _plugin;

    public NoteManagementPluginTests()
    {
        _plugin = new NoteManagementPlugin(_noteManagementService, _logger);
    }

    #region ListNotebooksAsync Tests

    [Fact]
    public async Task ListNotebooksAsync_WhenNoNotebooks_ShouldReturnHelpfulMessage()
    {
        // Arrange
        _noteManagementService.GetAllNotebooksAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Notebook>());

        // Act
        var result = await _plugin.ListNotebooksAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain("No notebooks found");
    }

    [Fact]
    public async Task ListNotebooksAsync_WhenNotebooksExist_ShouldListThem()
    {
        // Arrange
        var notebooks = new List<Notebook>
        {
            CreateNotebook("Work"),
            CreateNotebook("Personal")
        };
        _noteManagementService.GetAllNotebooksAsync(Arg.Any<CancellationToken>())
            .Returns(notebooks);

        // Act
        var result = await _plugin.ListNotebooksAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain("Work");
        result.ShouldContain("Personal");
        result.ShouldContain("2 notebook(s)");
    }

    #endregion

    #region CreateNotebookAsync Tests

    [Fact]
    public async Task CreateNotebookAsync_WhenNameIsEmpty_ShouldReturnError()
    {
        // Act
        var result = await _plugin.CreateNotebookAsync("", TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain("Error");
        result.ShouldContain("cannot be empty");
    }

    [Fact]
    public async Task CreateNotebookAsync_WhenSuccessful_ShouldReturnConfirmation()
    {
        // Arrange
        var notebook = CreateNotebook("Test");
        _noteManagementService.CreateNotebookAsync("Test", Arg.Any<CancellationToken>())
            .Returns(notebook);

        // Act
        var result = await _plugin.CreateNotebookAsync("Test", TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain("Successfully created");
        result.ShouldContain("Test");
    }

    [Fact]
    public async Task CreateNotebookAsync_WhenException_ShouldReturnError()
    {
        // Arrange
        _noteManagementService.CreateNotebookAsync("Test", Arg.Any<CancellationToken>())
            .Throws(new Exception("Database error"));

        // Act
        var result = await _plugin.CreateNotebookAsync("Test", TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain("Error");
        result.ShouldContain("Database error");
    }

    #endregion

    #region GetNotebookNotesAsync Tests

    [Fact]
    public async Task GetNotebookNotesAsync_WhenNoNotes_ShouldReturnMessage()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        _noteManagementService.GetNotebookNotesAsync(notebookId, Arg.Any<CancellationToken>())
            .Returns(new List<Note>());

        // Act
        var result = await _plugin.GetNotebookNotesAsync(notebookId, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain("no notes");
    }

    [Fact]
    public async Task GetNotebookNotesAsync_WhenNotesExist_ShouldListThem()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        var notes = new List<Note>
        {
            CreateNoteWithContent(notebookId, "Meeting Notes"),
            CreateNoteWithContent(notebookId, "Project Ideas")
        };
        _noteManagementService.GetNotebookNotesAsync(notebookId, Arg.Any<CancellationToken>())
            .Returns(notes);

        // Act
        var result = await _plugin.GetNotebookNotesAsync(notebookId, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain("2 note(s)");
    }

    #endregion

    #region CreateNoteAsync Tests

    [Fact]
    public async Task CreateNoteAsync_WhenSuccessful_ShouldReturnConfirmation()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        var note = CreateNoteWithContent(notebookId, "Test Title");
        _noteManagementService.CreateNoteAsync(notebookId, Arg.Any<IReadOnlyList<BlockDefinition>>(), Arg.Any<CancellationToken>())
            .Returns(note);

        // Act
        var result = await _plugin.CreateNoteAsync(notebookId, "[{\"type\":\"h1\",\"content\":\"Test Title\"}]", TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain("Successfully created");
    }

    [Fact]
    public async Task CreateNoteAsync_WhenInvalidJson_ShouldReturnError()
    {
        // Act
        var result = await _plugin.CreateNoteAsync(Guid.NewGuid(), "invalid json", TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain("Error");
        result.ShouldContain("Invalid blocks JSON");
    }

    [Fact]
    public async Task CreateNoteAsync_WhenEmptyBlocks_ShouldReturnError()
    {
        // Act
        var result = await _plugin.CreateNoteAsync(Guid.NewGuid(), "[]", TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain("Error");
        result.ShouldContain("At least one block is required");
    }

    [Fact]
    public async Task CreateNoteAsync_WhenJsonHasTrailingText_ShouldExtractJsonAndSucceed()
    {
        // Arrange - simulates AI appending text after JSON
        var notebookId = Guid.NewGuid();
        var note = CreateNoteWithContent(notebookId, "Test Title");
        _noteManagementService.CreateNoteAsync(notebookId, Arg.Any<IReadOnlyList<BlockDefinition>>(), Arg.Any<CancellationToken>())
            .Returns(note);

        var jsonWithTrailingText = "[{\"type\":\"h1\",\"content\":\"Test Title\"}] I have created the note for you.";

        // Act
        var result = await _plugin.CreateNoteAsync(notebookId, jsonWithTrailingText, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain("Successfully created");
    }

    [Fact]
    public async Task CreateNoteAsync_WhenJsonHasLeadingText_ShouldExtractJsonAndSucceed()
    {
        // Arrange - simulates AI prepending text before JSON
        var notebookId = Guid.NewGuid();
        var note = CreateNoteWithContent(notebookId, "Test Title");
        _noteManagementService.CreateNoteAsync(notebookId, Arg.Any<IReadOnlyList<BlockDefinition>>(), Arg.Any<CancellationToken>())
            .Returns(note);

        var jsonWithLeadingText = "Here is the JSON: [{\"type\":\"h1\",\"content\":\"Test Title\"}]";

        // Act
        var result = await _plugin.CreateNoteAsync(notebookId, jsonWithLeadingText, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain("Successfully created");
    }

    [Fact]
    public async Task CreateNoteAsync_WhenJsonHasNestedArrays_ShouldParseCorrectly()
    {
        // Arrange - ensures bracket matching handles nested structures
        var notebookId = Guid.NewGuid();
        var note = CreateNoteWithContent(notebookId, "Test [with brackets]");
        _noteManagementService.CreateNoteAsync(notebookId, Arg.Any<IReadOnlyList<BlockDefinition>>(), Arg.Any<CancellationToken>())
            .Returns(note);

        var jsonWithNestedBrackets = "[{\"type\":\"h1\",\"content\":\"Test [with brackets]\"}] Done!";

        // Act
        var result = await _plugin.CreateNoteAsync(notebookId, jsonWithNestedBrackets, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain("Successfully created");
    }

    #endregion

    #region GetNoteContentAsync Tests

    [Fact]
    public async Task GetNoteContentAsync_WhenNoteExists_ShouldReturnContent()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var note = CreateNoteWithContent(Guid.NewGuid(), "Test Content", noteId);
        _noteManagementService.GetNoteAsync(noteId, Arg.Any<CancellationToken>())
            .Returns(note);

        // Act
        var result = await _plugin.GetNoteContentAsync(noteId, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain("Test Content");
        result.ShouldContain(noteId.ToString());
    }

    [Fact]
    public async Task GetNoteContentAsync_WhenNoteNotFound_ShouldReturnError()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        _noteManagementService.GetNoteAsync(noteId, Arg.Any<CancellationToken>())
            .Returns((Note?)null);

        // Act
        var result = await _plugin.GetNoteContentAsync(noteId, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain("not found");
    }

    #endregion

    #region EditNoteAsync Tests

    [Fact]
    public async Task EditNoteAsync_WhenSuccessful_ShouldReturnConfirmation()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var note = CreateNoteWithContent(Guid.NewGuid(), "Updated", noteId);
        _noteManagementService.EditNoteAsync(noteId, Arg.Any<IReadOnlyList<BlockDefinition>>(), Arg.Any<CancellationToken>())
            .Returns(note);

        // Act
        var result = await _plugin.EditNoteAsync(noteId, "[{\"type\":\"paragraph\",\"content\":\"Updated\"}]", TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain("Successfully updated");
    }

    [Fact]
    public async Task EditNoteAsync_WhenNoteNotFound_ShouldReturnError()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        _noteManagementService.EditNoteAsync(noteId, Arg.Any<IReadOnlyList<BlockDefinition>>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException($"Note with ID {noteId} not found."));

        // Act
        var result = await _plugin.EditNoteAsync(noteId, "[{\"type\":\"paragraph\",\"content\":\"Test\"}]", TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain("not found");
    }

    [Fact]
    public async Task EditNoteAsync_WhenInvalidJson_ShouldReturnError()
    {
        // Act
        var result = await _plugin.EditNoteAsync(Guid.NewGuid(), "invalid json", TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain("Error");
        result.ShouldContain("Invalid blocks JSON");
    }

    #endregion

    #region DeleteNoteAsync Tests

    [Fact]
    public async Task DeleteNoteAsync_WhenSuccessful_ShouldReturnConfirmation()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var note = CreateNoteWithContent(Guid.NewGuid(), "Note to Delete", noteId);
        _noteManagementService.GetNoteAsync(noteId, Arg.Any<CancellationToken>())
            .Returns(note);

        // Act
        var result = await _plugin.DeleteNoteAsync(noteId, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain("Successfully deleted");
    }

    [Fact]
    public async Task DeleteNoteAsync_WhenNoteNotFound_ShouldReturnError()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        _noteManagementService.GetNoteAsync(noteId, Arg.Any<CancellationToken>())
            .Returns((Note?)null);

        // Act
        var result = await _plugin.DeleteNoteAsync(noteId, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain("not found");
    }

    #endregion

    #region SearchNotesAsync Tests

    [Fact]
    public async Task SearchNotesAsync_WhenQueryIsEmpty_ShouldReturnError()
    {
        // Act
        var result = await _plugin.SearchNotesAsync("", TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain("Error");
        result.ShouldContain("cannot be empty");
    }

    [Fact]
    public async Task SearchNotesAsync_WhenNoResults_ShouldReturnMessage()
    {
        // Arrange
        _noteManagementService.SearchNotesAsync("test", Arg.Any<CancellationToken>())
            .Returns(new List<Note>());

        // Act
        var result = await _plugin.SearchNotesAsync("test", TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain("No notes found");
    }

    [Fact]
    public async Task SearchNotesAsync_WhenResultsFound_ShouldListThem()
    {
        // Arrange
        var notes = new List<Note>
        {
            CreateNoteWithContent(Guid.NewGuid(), "Meeting notes about project"),
            CreateNoteWithContent(Guid.NewGuid(), "Project planning document")
        };
        _noteManagementService.SearchNotesAsync("project", Arg.Any<CancellationToken>())
            .Returns(notes);

        // Act
        var result = await _plugin.SearchNotesAsync("project", TestContext.Current.CancellationToken);

        // Assert
        result.ShouldContain("2 relevant note(s)");
    }

    #endregion

    #region Helper Methods

    private static Notebook CreateNotebook(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        CreatedAt = DateTimeOffset.UtcNow,
        LastModifiedAt = DateTimeOffset.UtcNow
    };

    private static Note CreateNoteWithContent(Guid notebookId, string content, Guid? noteId = null)
    {
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.H1,
            Content = content,
            Order = 0
        };

        return new Note
        {
            Id = noteId ?? Guid.NewGuid(),
            NotebookId = notebookId,
            CreatedAt = DateTimeOffset.UtcNow,
            LastModifiedAt = DateTimeOffset.UtcNow,
            Blocks = [block]
        };
    }

    #endregion
}
