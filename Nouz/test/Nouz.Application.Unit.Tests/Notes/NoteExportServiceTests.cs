using System.Collections.Immutable;
using Nouz.Application.Notes;
using Nouz.Domain.Entities;
using Nouz.Domain.Repositories;
using NSubstitute;
using Shouldly;

namespace Nouz.Application.Unit.Tests.Notes;

public class NoteExportServiceTests
{
    private readonly IAttachmentRepository _attachmentRepository = Substitute.For<IAttachmentRepository>();
    private readonly NoteExportService _service;

    public NoteExportServiceTests()
    {
        _service = new NoteExportService(_attachmentRepository);
    }

    #region HTML Structure Tests

    [Fact]
    public async Task GenerateHtmlAsync_ShouldGenerateValidHtmlDocument()
    {
        // Arrange
        var note = CreateNote(BlockType.Paragraph, "Test content");

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("<!DOCTYPE html>");
        html.ShouldContain("<html lang=\"en\">");
        html.ShouldContain("<head>");
        html.ShouldContain("<meta charset=\"UTF-8\">");
        html.ShouldContain("</head>");
        html.ShouldContain("<body>");
        html.ShouldContain("<article class=\"note\">");
        html.ShouldContain("</body>");
        html.ShouldContain("</html>");
    }

    [Fact]
    public async Task GenerateHtmlAsync_ShouldIncludeNoteDate()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var note = CreateNote(BlockType.Paragraph, "Test content", createdAt);

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("<header class=\"note-header\">");
        html.ShouldContain($"datetime=\"{createdAt:O}\"");
    }

    [Fact]
    public async Task GenerateHtmlAsync_ShouldIncludeEmbeddedStyles()
    {
        // Arrange
        var note = CreateNote(BlockType.Paragraph, "Test");

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("<style>");
        html.ShouldContain("font-family:");
        html.ShouldContain("</style>");
    }

    #endregion

    #region Block Rendering Tests

    [Fact]
    public async Task GenerateHtmlAsync_Paragraph_ShouldRenderAsPTag()
    {
        // Arrange
        var note = CreateNote(BlockType.Paragraph, "Hello world");

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("<p>Hello world</p>");
    }

    [Theory]
    [InlineData(BlockType.H1, "h1")]
    [InlineData(BlockType.H2, "h2")]
    [InlineData(BlockType.H3, "h3")]
    [InlineData(BlockType.H4, "h4")]
    public async Task GenerateHtmlAsync_Headings_ShouldRenderCorrectly(BlockType blockType, string expectedTag)
    {
        // Arrange
        var note = CreateNote(blockType, "Heading text");

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain($"<{expectedTag}>Heading text</{expectedTag}>");
    }

    [Fact]
    public async Task GenerateHtmlAsync_ListItem_ShouldRenderInUnorderedList()
    {
        // Arrange
        var note = CreateNote(BlockType.ListItem, "List item text");

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("<ul class=\"bullet-list\">");
        html.ShouldContain("<li>List item text</li>");
        html.ShouldContain("</ul>");
    }

    [Fact]
    public async Task GenerateHtmlAsync_AgendaItem_ShouldRenderWithChevron()
    {
        // Arrange
        var note = CreateNote(BlockType.AgendaItem, "Agenda item text");

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("<ul class=\"agenda-list\">");
        html.ShouldContain("class=\"agenda-item\"");
        html.ShouldContain("class=\"agenda-chevron\"");
        html.ShouldContain("Agenda item text");
    }

    [Fact]
    public async Task GenerateHtmlAsync_TodoItem_Unchecked_ShouldRenderUncheckedCheckbox()
    {
        // Arrange
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.TodoItem,
            Content = "Todo item",
            Metadata = new Dictionary<string, object> { ["checked"] = false },
            Order = 0
        };
        var note = CreateNoteWithBlocks(block);

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("<div class=\"todo-item\">");
        html.ShouldContain("<span class=\"todo-checkbox\"></span>");
        html.ShouldContain("<span class=\"todo-text\">Todo item</span>");
    }

    [Fact]
    public async Task GenerateHtmlAsync_TodoItem_Checked_ShouldRenderCheckedCheckbox()
    {
        // Arrange
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.TodoItem,
            Content = "Completed todo",
            Metadata = new Dictionary<string, object> { ["checked"] = true },
            Order = 0
        };
        var note = CreateNoteWithBlocks(block);

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("<span class=\"todo-checkbox checked\">");
        html.ShouldContain("class=\"todo-checked\"");
    }

    [Fact]
    public async Task GenerateHtmlAsync_Code_ShouldRenderAsPreCode()
    {
        // Arrange
        var note = CreateNote(BlockType.Code, "const x = 1;");

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("<pre><code>const x = 1;</code></pre>");
    }

    [Fact]
    public async Task GenerateHtmlAsync_Code_ShouldEscapeHtml()
    {
        // Arrange
        var note = CreateNote(BlockType.Code, "<script>alert('xss')</script>");

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("&lt;script&gt;");
        html.ShouldNotContain("<script>alert");
    }

    [Fact]
    public async Task GenerateHtmlAsync_Quote_ShouldRenderAsBlockquote()
    {
        // Arrange
        var note = CreateNote(BlockType.Quote, "A wise quote");

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("<blockquote>A wise quote</blockquote>");
    }

    [Fact]
    public async Task GenerateHtmlAsync_Decision_ShouldRenderAsStyledBox()
    {
        // Arrange
        var note = CreateNote(BlockType.Decision, "We decided to proceed");

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("<div class=\"decision-box\">");
        html.ShouldContain("<span class=\"decision-icon\">");
        html.ShouldContain("We decided to proceed");
    }

    [Fact]
    public async Task GenerateHtmlAsync_Warning_ShouldRenderAsStyledBox()
    {
        // Arrange
        var note = CreateNote(BlockType.Warning, "Be careful!");

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("<div class=\"warning-box\">");
        html.ShouldContain("<span class=\"warning-icon\">");
        html.ShouldContain("Be careful!");
    }

    [Fact]
    public async Task GenerateHtmlAsync_Divider_ShouldRenderAsHr()
    {
        // Arrange
        var note = CreateNote(BlockType.Divider, "");

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("<hr>");
    }

    #endregion

    #region Inline Formatting Tests

    [Fact]
    public async Task GenerateHtmlAsync_BoldText_ShouldRenderAsStrong()
    {
        // Arrange
        var note = CreateNote(BlockType.Paragraph, "This is **bold** text");

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("<strong>bold</strong>");
    }

    [Fact]
    public async Task GenerateHtmlAsync_ItalicText_ShouldRenderAsEm()
    {
        // Arrange
        var note = CreateNote(BlockType.Paragraph, "This is *italic* text");

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("<em>italic</em>");
    }

    [Fact]
    public async Task GenerateHtmlAsync_StrikethroughText_ShouldRenderAsDel()
    {
        // Arrange
        var note = CreateNote(BlockType.Paragraph, "This is ~~strikethrough~~ text");

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("<del>strikethrough</del>");
    }

    [Fact]
    public async Task GenerateHtmlAsync_InlineCode_ShouldRenderAsCode()
    {
        // Arrange
        var note = CreateNote(BlockType.Paragraph, "Use `console.log()` for debugging");

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("<code>console.log()</code>");
    }

    [Fact]
    public async Task GenerateHtmlAsync_ColoredText_ShouldRenderWithInlineStyle()
    {
        // Arrange
        var note = CreateNote(BlockType.Paragraph, "This is {color:#FF5733}colored{/color} text");

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("style=\"color: #FF5733;\"");
        html.ShouldContain(">colored</span>");
    }

    #endregion

    #region Image Block Tests

    [Fact]
    public async Task GenerateHtmlAsync_Image_ShouldEmbedAsBase64()
    {
        // Arrange
        var attachmentId = Guid.NewGuid();
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header bytes
        _attachmentRepository.LoadAsync(attachmentId, Arg.Any<CancellationToken>())
            .Returns(imageData);

        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.Image,
            Content = "",
            Metadata = new Dictionary<string, object>
            {
                ["attachmentId"] = attachmentId.ToString(),
                ["mimeType"] = "image/png",
                ["caption"] = "Test image",
                ["widthPercent"] = 75
            },
            Order = 0
        };
        var note = CreateNoteWithBlocks(block);

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("<figure");
        html.ShouldContain("width: 75%");
        html.ShouldContain("data:image/png;base64,");
        html.ShouldContain("<figcaption>Test image</figcaption>");
    }

    [Fact]
    public async Task GenerateHtmlAsync_Image_MissingAttachment_ShouldShowPlaceholder()
    {
        // Arrange
        var attachmentId = Guid.NewGuid();
        _attachmentRepository.LoadAsync(attachmentId, Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.Image,
            Content = "",
            Metadata = new Dictionary<string, object>
            {
                ["attachmentId"] = attachmentId.ToString(),
                ["mimeType"] = "image/png"
            },
            Order = 0
        };
        var note = CreateNoteWithBlocks(block);

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("[Image not found]");
    }

    [Fact]
    public async Task GenerateHtmlAsync_Image_NoCaption_ShouldNotIncludeFigcaption()
    {
        // Arrange
        var attachmentId = Guid.NewGuid();
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        _attachmentRepository.LoadAsync(attachmentId, Arg.Any<CancellationToken>())
            .Returns(imageData);

        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.Image,
            Content = "",
            Metadata = new Dictionary<string, object>
            {
                ["attachmentId"] = attachmentId.ToString(),
                ["mimeType"] = "image/png",
                ["caption"] = ""
            },
            Order = 0
        };
        var note = CreateNoteWithBlocks(block);

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldNotContain("<figcaption>");
    }

    [Fact]
    public async Task GenerateHtmlAsync_Table_ShouldRenderAsHtmlTable()
    {
        // Arrange
        var tableData = new List<List<string>>
        {
            new() { "Name", "Age" },
            new() { "Alice", "30" },
            new() { "Bob", "25" }
        };
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.Table,
            Content = string.Empty,
            Metadata = new Dictionary<string, object>
            {
                ["rows"] = 3,
                ["columns"] = 2,
                ["data"] = tableData,
                ["hasHeader"] = true
            },
            Order = 0
        };
        var note = CreateNoteWithBlocks(block);

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("<table class=\"export-table\">");
        html.ShouldContain("<th>Name</th>");
        html.ShouldContain("<th>Age</th>");
        html.ShouldContain("<td>Alice</td>");
        html.ShouldContain("<td>30</td>");
        html.ShouldContain("<td>Bob</td>");
        html.ShouldContain("<td>25</td>");
        html.ShouldContain("</table>");
    }

    [Fact]
    public async Task GenerateHtmlAsync_Table_WithoutHeader_ShouldRenderAllAsTd()
    {
        // Arrange
        var tableData = new List<List<string>>
        {
            new() { "A", "B" },
            new() { "C", "D" }
        };
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.Table,
            Content = string.Empty,
            Metadata = new Dictionary<string, object>
            {
                ["rows"] = 2,
                ["columns"] = 2,
                ["data"] = tableData,
                ["hasHeader"] = false
            },
            Order = 0
        };
        var note = CreateNoteWithBlocks(block);

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldNotContain("<th>");
        html.ShouldContain("<td>A</td>");
        html.ShouldContain("<td>B</td>");
    }

    [Fact]
    public async Task GenerateHtmlAsync_Table_ShouldEscapeHtmlInCells()
    {
        // Arrange
        var tableData = new List<List<string>>
        {
            new() { "<script>alert('xss')</script>" }
        };
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.Table,
            Content = string.Empty,
            Metadata = new Dictionary<string, object>
            {
                ["rows"] = 1,
                ["columns"] = 1,
                ["data"] = tableData,
                ["hasHeader"] = false
            },
            Order = 0
        };
        var note = CreateNoteWithBlocks(block);

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("&lt;script&gt;");
        html.ShouldNotContain("<script>alert");
    }

    [Fact]
    public async Task GenerateHtmlAsync_Mermaid_ShouldRenderAsCodeBlock()
    {
        // Arrange
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.Mermaid,
            Content = "graph TD\n    A --> B",
            Metadata = new Dictionary<string, object> { ["isEditMode"] = false },
            Order = 0
        };
        var note = CreateNoteWithBlocks(block);

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("<div class=\"mermaid-export\">");
        html.ShouldContain("<pre><code class=\"language-mermaid\">");
        html.ShouldContain("graph TD");
        html.ShouldContain("A --&gt; B");
    }

    [Fact]
    public async Task GenerateHtmlAsync_Mermaid_ShouldEscapeHtml()
    {
        // Arrange
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.Mermaid,
            Content = "graph TD\n    A[\"<b>Node</b>\"] --> B",
            Metadata = new Dictionary<string, object> { ["isEditMode"] = true },
            Order = 0
        };
        var note = CreateNoteWithBlocks(block);

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("&lt;b&gt;Node&lt;/b&gt;");
        html.ShouldNotContain("<b>Node</b>");
    }

    #endregion

    #region HTML Escaping Tests

    [Fact]
    public async Task GenerateHtmlAsync_ShouldEscapeHtmlEntities()
    {
        // Arrange
        var note = CreateNote(BlockType.Paragraph, "Use <div> and & symbols");

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("&lt;div&gt;");
        html.ShouldContain("&amp;");
    }

    [Fact]
    public async Task GenerateHtmlAsync_ShouldEscapeQuotes()
    {
        // Arrange
        var note = CreateNote(BlockType.Paragraph, "He said \"hello\" and 'goodbye'");

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        html.ShouldContain("&quot;hello&quot;");
        html.ShouldContain("&#39;goodbye&#39;");
    }

    #endregion

    #region Multiple Blocks Tests

    [Fact]
    public async Task GenerateHtmlAsync_MultipleBlocks_ShouldMaintainOrder()
    {
        // Arrange
        var blocks = new[]
        {
            new Block { Id = Guid.NewGuid(), Type = BlockType.H1, Content = "Title", Order = 0 },
            new Block { Id = Guid.NewGuid(), Type = BlockType.Paragraph, Content = "First paragraph", Order = 1 },
            new Block { Id = Guid.NewGuid(), Type = BlockType.Paragraph, Content = "Second paragraph", Order = 2 }
        };
        var note = CreateNoteWithBlocks(blocks);

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        var h1Index = html.IndexOf("<h1>Title</h1>", StringComparison.Ordinal);
        var firstPIndex = html.IndexOf("<p>First paragraph</p>", StringComparison.Ordinal);
        var secondPIndex = html.IndexOf("<p>Second paragraph</p>", StringComparison.Ordinal);

        h1Index.ShouldBeLessThan(firstPIndex);
        firstPIndex.ShouldBeLessThan(secondPIndex);
    }

    [Fact]
    public async Task GenerateHtmlAsync_ConsecutiveListItems_ShouldBeInSingleList()
    {
        // Arrange
        var blocks = new[]
        {
            new Block { Id = Guid.NewGuid(), Type = BlockType.ListItem, Content = "Item 1", Order = 0 },
            new Block { Id = Guid.NewGuid(), Type = BlockType.ListItem, Content = "Item 2", Order = 1 },
            new Block { Id = Guid.NewGuid(), Type = BlockType.ListItem, Content = "Item 3", Order = 2 }
        };
        var note = CreateNoteWithBlocks(blocks);

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        // Should only have one ul opening and closing
        var ulOpenCount = html.Split("<ul class=\"bullet-list\">").Length - 1;
        var ulCloseCount = html.Split("</ul>").Length - 1;
        ulOpenCount.ShouldBe(1);
        ulCloseCount.ShouldBe(1);
    }

    [Fact]
    public async Task GenerateHtmlAsync_ListInterruptedByParagraph_ShouldCreateSeparateLists()
    {
        // Arrange
        var blocks = new[]
        {
            new Block { Id = Guid.NewGuid(), Type = BlockType.ListItem, Content = "Item 1", Order = 0 },
            new Block { Id = Guid.NewGuid(), Type = BlockType.Paragraph, Content = "Interruption", Order = 1 },
            new Block { Id = Guid.NewGuid(), Type = BlockType.ListItem, Content = "Item 2", Order = 2 }
        };
        var note = CreateNoteWithBlocks(blocks);

        // Act
        var html = await _service.GenerateHtmlAsync(note);

        // Assert
        var ulCount = html.Split("<ul class=\"bullet-list\">").Length - 1;
        ulCount.ShouldBe(2);
    }

    #endregion

    #region Helper Methods

    private static Note CreateNote(BlockType blockType, string content, DateTimeOffset? createdAt = null)
    {
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = blockType,
            Content = content,
            Order = 0
        };
        return CreateNoteWithBlocks(createdAt, block);
    }

    private static Note CreateNoteWithBlocks(params Block[] blocks)
    {
        return CreateNoteWithBlocks(null, blocks);
    }

    private static Note CreateNoteWithBlocks(DateTimeOffset? createdAt, params Block[] blocks)
    {
        var now = createdAt ?? DateTimeOffset.UtcNow;
        return new Note
        {
            Id = Guid.NewGuid(),
            NotebookId = Guid.NewGuid(),
            CreatedAt = now,
            LastModifiedAt = now,
            Blocks = blocks.ToImmutableList()
        };
    }

    #endregion
}
