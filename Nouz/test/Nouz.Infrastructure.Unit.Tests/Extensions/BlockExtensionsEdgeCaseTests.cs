using System.Text.Json;
using Nouz.Domain.Entities;
using Nouz.Domain.Extensions;
using Shouldly;

namespace Nouz.Infrastructure.Unit.Tests.Extensions;

public class BlockExtensionsEdgeCaseTests
{
    #region GetFormatting with JsonElement Tests

    [Fact]
    public void GetFormatting_WithJsonElementArray_ShouldDeserializeFormats()
    {
        // Arrange
        var json = "[{\"Start\":0,\"End\":5,\"Type\":0,\"Value\":null}]";
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
        var block = CreateBlockWithFormattingMetadata(jsonElement);

        // Act
        var formatting = block.GetFormatting();

        // Assert
        formatting.Count.ShouldBe(1);
        formatting[0].Start.ShouldBe(0);
        formatting[0].End.ShouldBe(5);
        formatting[0].Type.ShouldBe(TextFormatType.Bold);
    }

    [Fact]
    public void GetFormatting_WithJsonElementLowercaseProperties_ShouldDeserializeFormats()
    {
        // Arrange - lowercase property names
        var json = "[{\"start\":0,\"end\":5,\"type\":\"Bold\",\"value\":\"#ff0000\"}]";
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
        var block = CreateBlockWithFormattingMetadata(jsonElement);

        // Act
        var formatting = block.GetFormatting();

        // Assert
        formatting.Count.ShouldBe(1);
        formatting[0].Start.ShouldBe(0);
        formatting[0].End.ShouldBe(5);
        formatting[0].Type.ShouldBe(TextFormatType.Bold);
        formatting[0].Value.ShouldBe("#ff0000");
    }

    [Fact]
    public void GetFormatting_WithJsonElementTypeAsString_ShouldParseEnum()
    {
        // Arrange
        var json = "[{\"Start\":0,\"End\":5,\"Type\":\"Italic\",\"Value\":null}]";
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
        var block = CreateBlockWithFormattingMetadata(jsonElement);

        // Act
        var formatting = block.GetFormatting();

        // Assert
        formatting.Count.ShouldBe(1);
        formatting[0].Type.ShouldBe(TextFormatType.Italic);
    }

    [Fact]
    public void GetFormatting_WithJsonElementTypeAsNumber_ShouldParseEnum()
    {
        // Arrange - Type as number (1 = Italic)
        var json = "[{\"Start\":0,\"End\":5,\"Type\":1,\"Value\":null}]";
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
        var block = CreateBlockWithFormattingMetadata(jsonElement);

        // Act
        var formatting = block.GetFormatting();

        // Assert
        formatting.Count.ShouldBe(1);
        formatting[0].Type.ShouldBe(TextFormatType.Italic);
    }

    [Fact]
    public void GetFormatting_WithJsonElementNotArray_ShouldReturnEmpty()
    {
        // Arrange - not an array
        var json = "{\"notAnArray\":true}";
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
        var block = CreateBlockWithFormattingMetadata(jsonElement);

        // Act
        var formatting = block.GetFormatting();

        // Assert
        formatting.ShouldBeEmpty();
    }

    [Fact]
    public void GetFormatting_WithJsonElementMissingStartProperty_ShouldSkipItem()
    {
        // Arrange - missing Start property
        var json = "[{\"End\":5,\"Type\":0}]";
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
        var block = CreateBlockWithFormattingMetadata(jsonElement);

        // Act
        var formatting = block.GetFormatting();

        // Assert
        formatting.ShouldBeEmpty();
    }

    [Fact]
    public void GetFormatting_WithUnexpectedMetadataType_ShouldReturnEmpty()
    {
        // Arrange - unexpected type in metadata
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.Paragraph,
            Content = "Test",
            Metadata = new Dictionary<string, object> { ["formatting"] = "invalid string" }
        };

        // Act
        var formatting = block.GetFormatting();

        // Assert
        formatting.ShouldBeEmpty();
    }

    [Fact]
    public void GetFormatting_WithExistingTextFormatList_ShouldReturnDirectly()
    {
        // Arrange - already a list of TextFormat
        var formats = new List<TextFormat>
        {
            new() { Start = 0, End = 5, Type = TextFormatType.Bold }
        };
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.Paragraph,
            Content = "Test",
            Metadata = new Dictionary<string, object> { ["formatting"] = formats }
        };

        // Act
        var formatting = block.GetFormatting();

        // Assert
        formatting.Count.ShouldBe(1);
    }

    #endregion

    #region RenderFormattedContent Edge Cases

    [Fact]
    public void RenderFormattedContent_WithEmptyContent_ShouldReturnEmpty()
    {
        // Arrange
        var block = CreateBlock("");

        // Act
        var html = block.RenderFormattedContent();

        // Assert
        html.ShouldBe(string.Empty);
    }

    [Fact]
    public void RenderFormattedContent_WithNullContent_ShouldReturnEmpty()
    {
        // Arrange
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.Paragraph,
            Content = null!
        };

        // Act
        var html = block.RenderFormattedContent();

        // Assert
        html.ShouldBe(string.Empty);
    }

    [Fact]
    public void RenderFormattedContent_WithColorFormatNoValue_ShouldSkipTag()
    {
        // Arrange
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 0, End = 5, Type = TextFormatType.Color, Value = null });

        // Act
        var html = block.RenderFormattedContent();

        // Assert
        // Color format without value should not render any tag
        html.ShouldNotContain("<span");
    }

    [Fact]
    public void RenderFormattedContent_WithFormatBeyondContent_ShouldClampToBounds()
    {
        // Arrange - format extends beyond content length
        var block = CreateBlock("Hi")
            .AddFormat(new TextFormat { Start = 0, End = 100, Type = TextFormatType.Bold });

        // Act
        var html = block.RenderFormattedContent();

        // Assert
        html.ShouldBe("<strong>Hi</strong>");
    }

    [Fact]
    public void RenderFormattedContent_WithNegativeFormatStart_ShouldClampToZero()
    {
        // Arrange - format starts before content
        var block = CreateBlock("Hello")
            .WithFormatting(new List<TextFormat>
            {
                new() { Start = -5, End = 3, Type = TextFormatType.Bold }
            });

        // Act
        var html = block.RenderFormattedContent();

        // Assert
        html.ShouldContain("<strong>");
        html.ShouldContain("</strong>");
    }

    [Fact]
    public void RenderFormattedContent_WithNestedFormats_ShouldRenderCorrectly()
    {
        // Arrange - Bold and Italic on same range
        var block = CreateBlock("Hello")
            .AddFormat(new TextFormat { Start = 0, End = 5, Type = TextFormatType.Bold })
            .AddFormat(new TextFormat { Start = 0, End = 5, Type = TextFormatType.Italic });

        // Act
        var html = block.RenderFormattedContent();

        // Assert
        html.ShouldContain("<strong>");
        html.ShouldContain("<em>");
        html.ShouldContain("</strong>");
        html.ShouldContain("</em>");
    }

    [Fact]
    public void RenderFormattedContent_WithOverlappingFormats_ShouldRenderCorrectly()
    {
        // Arrange - Overlapping formats
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 0, End = 7, Type = TextFormatType.Bold })
            .AddFormat(new TextFormat { Start = 4, End = 11, Type = TextFormatType.Italic });

        // Act
        var html = block.RenderFormattedContent();

        // Assert
        html.ShouldContain("<strong>");
        html.ShouldContain("<em>");
    }

    #endregion

    #region RemoveFormat Edge Cases

    [Fact]
    public void RemoveFormat_WithPartialOverlapAtStart_ShouldTrimFormat()
    {
        // Arrange - format 0-10, remove 0-5
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 0, End = 10, Type = TextFormatType.Bold });

        // Act
        var result = block.RemoveFormat(0, 5, TextFormatType.Bold);

        // Assert
        var formatting = result.GetFormatting();
        formatting.Count.ShouldBe(1);
        formatting[0].Start.ShouldBe(5);
        formatting[0].End.ShouldBe(10);
    }

    [Fact]
    public void RemoveFormat_WithPartialOverlapAtEnd_ShouldTrimFormat()
    {
        // Arrange - format 0-10, remove 5-10
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 0, End = 10, Type = TextFormatType.Bold });

        // Act
        var result = block.RemoveFormat(5, 10, TextFormatType.Bold);

        // Assert
        var formatting = result.GetFormatting();
        formatting.Count.ShouldBe(1);
        formatting[0].Start.ShouldBe(0);
        formatting[0].End.ShouldBe(5);
    }

    #endregion

    #region WithFormatting Edge Cases

    [Fact]
    public void WithFormatting_WithEmptyList_ShouldRemoveFormattingKey()
    {
        // Arrange
        var block = CreateBlock("Hello")
            .AddFormat(new TextFormat { Start = 0, End = 5, Type = TextFormatType.Bold });

        // Act
        var result = block.WithFormatting(new List<TextFormat>());

        // Assert
        result.Metadata.ShouldNotContainKey("formatting");
    }

    #endregion

    #region AdjustFormattingForContentChange Edge Cases

    [Fact]
    public void AdjustFormattingForContentChange_WithEmptyFormatting_ShouldJustUpdateContent()
    {
        // Arrange
        var block = CreateBlock("Original");

        // Act
        var result = block.AdjustFormattingForContentChange("New");

        // Assert
        result.Content.ShouldBe("New");
        result.GetFormatting().ShouldBeEmpty();
    }

    [Fact]
    public void AdjustFormattingForContentChange_WhenNewContentShorter_ShouldRemoveInvalidFormats()
    {
        // Arrange
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 8, End = 11, Type = TextFormatType.Bold }); // "rld"

        // Act - shorten to "Hi"
        var result = block.AdjustFormattingForContentChange("Hi");

        // Assert - format at 8-11 should be removed since start >= new length
        result.GetFormatting().ShouldBeEmpty();
    }

    [Fact]
    public void AdjustFormattingForContentChange_WhenNewContentLonger_ShouldPreserveFormats()
    {
        // Arrange
        var block = CreateBlock("Hi")
            .AddFormat(new TextFormat { Start = 0, End = 2, Type = TextFormatType.Bold });

        // Act - extend content
        var result = block.AdjustFormattingForContentChange("Hello World");

        // Assert
        var formatting = result.GetFormatting();
        formatting.Count.ShouldBe(1);
        formatting[0].Start.ShouldBe(0);
        formatting[0].End.ShouldBe(2);
    }

    #endregion

    #region ToggleFormat Edge Cases

    [Fact]
    public void ToggleFormat_WithColorValue_ShouldIncludeValue()
    {
        // Arrange
        var block = CreateBlock("Hello");

        // Act
        var result = block.ToggleFormat(0, 5, TextFormatType.Color, "#ff0000");

        // Assert
        var formatting = result.GetFormatting();
        formatting.Count.ShouldBe(1);
        formatting[0].Value.ShouldBe("#ff0000");
    }

    [Fact]
    public void ToggleFormat_WhenPartiallyOverlapping_ShouldNotToggle()
    {
        // Arrange - bold on 0-5
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 0, End = 5, Type = TextFormatType.Bold });

        // Act - try to toggle 0-10 (extends beyond existing format)
        var result = block.ToggleFormat(0, 10, TextFormatType.Bold);

        // Assert - should add new format since existing doesn't fully cover range
        var formatting = result.GetFormatting();
        formatting.ShouldContain(f => f.End >= 10);
    }

    #endregion

    #region NormalizeFormats Tests

    [Fact]
    public void AddFormat_WithAdjacentFormats_ShouldMerge()
    {
        // Arrange
        var block = CreateBlock("Hello World")
            .AddFormat(new TextFormat { Start = 0, End = 5, Type = TextFormatType.Bold });

        // Act - add adjacent format
        var result = block.AddFormat(new TextFormat { Start = 5, End = 11, Type = TextFormatType.Bold });

        // Assert - should merge into single format
        var formatting = result.GetFormatting();
        formatting.Count.ShouldBe(1);
        formatting[0].Start.ShouldBe(0);
        formatting[0].End.ShouldBe(11);
    }

    [Fact]
    public void AddFormat_WithInvalidRange_ShouldBeFilteredOut()
    {
        // Arrange
        var block = CreateBlock("Hi");

        // Act - add format with start >= end (invalid)
        var result = block.AddFormat(new TextFormat { Start = 5, End = 3, Type = TextFormatType.Bold });

        // Assert
        result.GetFormatting().ShouldBeEmpty();
    }

    [Fact]
    public void AddFormat_WithFormatCompletelyOutsideContent_ShouldBeFilteredOut()
    {
        // Arrange
        var block = CreateBlock("Hi"); // length 2

        // Act - add format completely outside content
        var result = block.AddFormat(new TextFormat { Start = 10, End = 20, Type = TextFormatType.Bold });

        // Assert
        result.GetFormatting().ShouldBeEmpty();
    }

    #endregion

    private static Block CreateBlock(string content) => new()
    {
        Id = Guid.NewGuid(),
        Type = BlockType.Paragraph,
        Content = content
    };

    private static Block CreateBlockWithFormattingMetadata(object formattingValue) => new()
    {
        Id = Guid.NewGuid(),
        Type = BlockType.Paragraph,
        Content = "Hello World",
        Metadata = new Dictionary<string, object> { ["formatting"] = formattingValue }
    };
}
