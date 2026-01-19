using Nouz.Domain.Entities;
using Nouz.Domain.Extensions;
using Shouldly;

namespace Nouz.Infrastructure.Unit.Tests.Extensions;

public class BlockExtensionsTests
{
    #region GetFormatting Tests

    [Fact]
    public void GetFormatting_WhenNoFormatting_ShouldReturnEmptyList()
    {
        // Arrange
        var block = CreateBlock("Hello World");

        // Act
        var formatting = block.GetFormatting();

        // Assert
        formatting.ShouldBeEmpty();
    }

    [Fact]
    public void GetFormatting_WhenHasFormatting_ShouldReturnFormats()
    {
        // Arrange
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 0, End = 5, Type = TextFormatType.Bold });

        // Act
        var formatting = block.GetFormatting();

        // Assert
        formatting.Count.ShouldBe(1);
        formatting[0].Type.ShouldBe(TextFormatType.Bold);
        formatting[0].Start.ShouldBe(0);
        formatting[0].End.ShouldBe(5);
    }

    #endregion

    #region AddFormat Tests

    [Fact]
    public void AddFormat_ShouldAddBoldFormat()
    {
        // Arrange
        var block = CreateBlock("Hello World");

        // Act
        var result = block.AddFormat(new TextFormat
        {
            Start = 0,
            End = 5,
            Type = TextFormatType.Bold
        });

        // Assert
        var formatting = result.GetFormatting();
        formatting.Count.ShouldBe(1);
        formatting[0].Type.ShouldBe(TextFormatType.Bold);
    }

    [Fact]
    public void AddFormat_ShouldAddItalicFormat()
    {
        // Arrange
        var block = CreateBlock("Hello World");

        // Act
        var result = block.AddFormat(new TextFormat
        {
            Start = 6,
            End = 11,
            Type = TextFormatType.Italic
        });

        // Assert
        var formatting = result.GetFormatting();
        formatting.Count.ShouldBe(1);
        formatting[0].Type.ShouldBe(TextFormatType.Italic);
    }

    [Fact]
    public void AddFormat_ShouldAddColorFormat()
    {
        // Arrange
        var block = CreateBlock("Hello World");

        // Act
        var result = block.AddFormat(new TextFormat
        {
            Start = 0,
            End = 5,
            Type = TextFormatType.Color,
            Value = "#ff0000"
        });

        // Assert
        var formatting = result.GetFormatting();
        formatting.Count.ShouldBe(1);
        formatting[0].Type.ShouldBe(TextFormatType.Color);
        formatting[0].Value.ShouldBe("#ff0000");
    }

    [Fact]
    public void AddFormat_ShouldMergeOverlappingFormats()
    {
        // Arrange
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 0, End = 5, Type = TextFormatType.Bold });

        // Act - Add overlapping bold format
        var result = block.AddFormat(new TextFormat
        {
            Start = 3,
            End = 8,
            Type = TextFormatType.Bold
        });

        // Assert - Should merge into single format
        var formatting = result.GetFormatting();
        formatting.Count.ShouldBe(1);
        formatting[0].Start.ShouldBe(0);
        formatting[0].End.ShouldBe(8);
    }

    [Fact]
    public void AddFormat_ShouldAllowMultipleFormatTypes()
    {
        // Arrange
        var block = CreateBlock("Hello World");

        // Act
        var result = block
            .AddFormat(new TextFormat { Start = 0, End = 5, Type = TextFormatType.Bold })
            .AddFormat(new TextFormat { Start = 0, End = 5, Type = TextFormatType.Italic });

        // Assert
        var formatting = result.GetFormatting();
        formatting.Count.ShouldBe(2);
        formatting.ShouldContain(f => f.Type == TextFormatType.Bold);
        formatting.ShouldContain(f => f.Type == TextFormatType.Italic);
    }

    #endregion

    #region RemoveFormat Tests

    [Fact]
    public void RemoveFormat_ShouldRemoveEntireFormat()
    {
        // Arrange
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 0, End = 5, Type = TextFormatType.Bold });

        // Act
        var result = block.RemoveFormat(0, 5, TextFormatType.Bold);

        // Assert
        result.GetFormatting().ShouldBeEmpty();
    }

    [Fact]
    public void RemoveFormat_ShouldSplitFormatWhenRemovingMiddle()
    {
        // Arrange
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 0, End = 11, Type = TextFormatType.Bold });

        // Act - Remove format from middle (indices 3-7)
        var result = block.RemoveFormat(3, 7, TextFormatType.Bold);

        // Assert - Should have two formats: 0-3 and 7-11
        var formatting = result.GetFormatting();
        formatting.Count.ShouldBe(2);
        formatting.ShouldContain(f => f.Start == 0 && f.End == 3);
        formatting.ShouldContain(f => f.Start == 7 && f.End == 11);
    }

    [Fact]
    public void RemoveFormat_ShouldNotAffectOtherFormatTypes()
    {
        // Arrange
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 0, End = 5, Type = TextFormatType.Bold })
            .AddFormat(new TextFormat { Start = 0, End = 5, Type = TextFormatType.Italic });

        // Act
        var result = block.RemoveFormat(0, 5, TextFormatType.Bold);

        // Assert
        var formatting = result.GetFormatting();
        formatting.Count.ShouldBe(1);
        formatting[0].Type.ShouldBe(TextFormatType.Italic);
    }

    #endregion

    #region ToggleFormat Tests

    [Fact]
    public void ToggleFormat_ShouldAddFormatWhenNotPresent()
    {
        // Arrange
        var block = CreateBlock("Hello World");

        // Act
        var result = block.ToggleFormat(0, 5, TextFormatType.Bold);

        // Assert
        result.HasFormat(0, 5, TextFormatType.Bold).ShouldBeTrue();
    }

    [Fact]
    public void ToggleFormat_ShouldRemoveFormatWhenPresent()
    {
        // Arrange
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 0, End = 5, Type = TextFormatType.Bold });

        // Act
        var result = block.ToggleFormat(0, 5, TextFormatType.Bold);

        // Assert
        result.HasFormat(0, 5, TextFormatType.Bold).ShouldBeFalse();
    }

    #endregion

    #region HasFormat Tests

    [Fact]
    public void HasFormat_ShouldReturnTrueWhenFormatCoversSelection()
    {
        // Arrange
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 0, End = 11, Type = TextFormatType.Bold });

        // Act & Assert
        block.HasFormat(0, 5, TextFormatType.Bold).ShouldBeTrue();
        block.HasFormat(3, 8, TextFormatType.Bold).ShouldBeTrue();
        block.HasFormat(0, 11, TextFormatType.Bold).ShouldBeTrue();
    }

    [Fact]
    public void HasFormat_ShouldReturnFalseWhenFormatDoesNotCoverSelection()
    {
        // Arrange
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 0, End = 5, Type = TextFormatType.Bold });

        // Act & Assert
        block.HasFormat(3, 8, TextFormatType.Bold).ShouldBeFalse(); // Extends beyond format
        block.HasFormat(6, 11, TextFormatType.Bold).ShouldBeFalse(); // Outside format
    }

    #endregion

    #region RenderFormattedContent Tests

    [Fact]
    public void RenderFormattedContent_WithNoFormatting_ShouldReturnPlainText()
    {
        // Arrange
        var block = CreateBlock("Hello World");

        // Act
        var html = block.RenderFormattedContent();

        // Assert
        html.ShouldBe("Hello World");
    }

    [Fact]
    public void RenderFormattedContent_WithBold_ShouldWrapInStrongTag()
    {
        // Arrange
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 0, End = 5, Type = TextFormatType.Bold });

        // Act
        var html = block.RenderFormattedContent();

        // Assert
        html.ShouldBe("<strong>Hello</strong> World");
    }

    [Fact]
    public void RenderFormattedContent_WithItalic_ShouldWrapInEmTag()
    {
        // Arrange
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 6, End = 11, Type = TextFormatType.Italic });

        // Act
        var html = block.RenderFormattedContent();

        // Assert
        html.ShouldBe("Hello <em>World</em>");
    }

    [Fact]
    public void RenderFormattedContent_WithColor_ShouldWrapInSpanWithStyle()
    {
        // Arrange
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 0, End = 5, Type = TextFormatType.Color, Value = "#ff0000" });

        // Act
        var html = block.RenderFormattedContent();

        // Assert
        html.ShouldBe("<span style=\"color:#ff0000\">Hello</span> World");
    }

    [Fact]
    public void RenderFormattedContent_WithMultipleFormats_ShouldRenderAll()
    {
        // Arrange
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 0, End = 5, Type = TextFormatType.Bold })
            .AddFormat(new TextFormat { Start = 6, End = 11, Type = TextFormatType.Italic });

        // Act
        var html = block.RenderFormattedContent();

        // Assert
        html.ShouldBe("<strong>Hello</strong> <em>World</em>");
    }

    [Fact]
    public void RenderFormattedContent_ShouldEscapeHtmlEntities()
    {
        // Arrange
        var block = CreateBlock("<script>alert('xss')</script>");

        // Act
        var html = block.RenderFormattedContent();

        // Assert
        html.ShouldContain("&lt;script&gt;");
        html.ShouldNotContain("<script>");
    }

    #endregion

    #region ClearFormatting Tests

    [Fact]
    public void ClearFormatting_ShouldRemoveAllFormatting()
    {
        // Arrange
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 0, End = 5, Type = TextFormatType.Bold })
            .AddFormat(new TextFormat { Start = 6, End = 11, Type = TextFormatType.Italic });

        // Act
        var result = block.ClearFormatting();

        // Assert
        result.GetFormatting().ShouldBeEmpty();
    }

    #endregion

    #region AdjustFormattingForContentChange Tests

    [Fact]
    public void AdjustFormattingForContentChange_ShouldClampFormatsToNewLength()
    {
        // Arrange
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 0, End = 11, Type = TextFormatType.Bold });

        // Act - Shorten content
        var result = block.AdjustFormattingForContentChange("Hello");

        // Assert
        var formatting = result.GetFormatting();
        formatting.Count.ShouldBe(1);
        formatting[0].End.ShouldBe(5);
    }

    [Fact]
    public void AdjustFormattingForContentChange_ShouldRemoveInvalidFormats()
    {
        // Arrange
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 6, End = 11, Type = TextFormatType.Bold });

        // Act - Shorten content so format is outside
        var result = block.AdjustFormattingForContentChange("Hello");

        // Assert - Format should be removed since start >= length
        result.GetFormatting().ShouldBeEmpty();
    }

    #endregion

    #region Search Compatibility Tests

    [Fact]
    public void FormattedBlock_ContentShouldRemainPlainText()
    {
        // Arrange
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 0, End = 5, Type = TextFormatType.Bold });

        // Assert - Content should still be plain text (searchable)
        block.Content.ShouldBe("Hello World");
        block.Content.ShouldNotContain("<strong>");
        block.Content.ShouldNotContain("</strong>");
    }

    [Fact]
    public void Search_ShouldNotMatchFormattingTags()
    {
        // Arrange
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 0, End = 5, Type = TextFormatType.Bold });

        // Assert - Content should not contain "strong" (the HTML tag name)
        block.Content.ShouldNotContain("strong");
        block.Content.ShouldNotContain("em");
        block.Content.ShouldNotContain("span");
    }

    #endregion

    private static Block CreateBlock(string content) => new()
    {
        Id = Guid.NewGuid(),
        Type = BlockType.Paragraph,
        Content = content
    };
}
