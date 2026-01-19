using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Nouz.Domain.Entities;

namespace Nouz.Domain.Extensions;

public static class BlockExtensions
{
    private const string FormattingKey = "formatting";

    public static IReadOnlyList<TextFormat> GetFormatting(this Block block)
    {
        if (!block.Metadata.TryGetValue(FormattingKey, out var value))
        {
            return [];
        }

        return value switch
        {
            JsonElement jsonElement => DeserializeFormats(jsonElement),
            IEnumerable<TextFormat> formats => formats.ToList(),
            _ => []
        };
    }

    public static Block WithFormatting(this Block block, IReadOnlyList<TextFormat> formatting)
    {
        var metadata = new Dictionary<string, object>(block.Metadata);

        if (formatting.Count == 0)
        {
            metadata.Remove(FormattingKey);
        }
        else
        {
            metadata[FormattingKey] = formatting.ToList();
        }

        return block with { Metadata = metadata };
    }

    public static Block AddFormat(this Block block, TextFormat format)
    {
        var formats = block.GetFormatting().ToList();
        formats.Add(format);
        return block.WithFormatting(NormalizeFormats(formats, block.Content.Length));
    }

    public static Block RemoveFormat(this Block block, int start, int end, TextFormatType type)
    {
        var formats = block.GetFormatting()
            .Where(f => !(f.Type == type && f.Start < end && f.End > start))
            .ToList();

        // Handle partial overlaps - split formats that partially overlap with the removal range
        var toAdd = new List<TextFormat>();
        var toRemove = new List<TextFormat>();

        foreach (var f in block.GetFormatting().Where(f => f.Type == type))
        {
            if (f.Start < start && f.End > start && f.End <= end)
            {
                // Format extends before the removal range
                toRemove.Add(f);
                toAdd.Add(f with { End = start });
            }
            else if (f.Start >= start && f.Start < end && f.End > end)
            {
                // Format extends after the removal range
                toRemove.Add(f);
                toAdd.Add(f with { Start = end });
            }
            else if (f.Start < start && f.End > end)
            {
                // Format spans the entire removal range - split it
                toRemove.Add(f);
                toAdd.Add(f with { End = start });
                toAdd.Add(f with { Start = end });
            }
        }

        formats = formats.Except(toRemove).Concat(toAdd).ToList();
        return block.WithFormatting(NormalizeFormats(formats, block.Content.Length));
    }

    public static Block ToggleFormat(this Block block, int start, int end, TextFormatType type, string? value = null)
    {
        var formats = block.GetFormatting();
        var existingFormat = formats.FirstOrDefault(f =>
            f.Type == type && f.Start <= start && f.End >= end);

        if (existingFormat is not null)
        {
            return block.RemoveFormat(start, end, type);
        }

        return block.AddFormat(new TextFormat
        {
            Start = start,
            End = end,
            Type = type,
            Value = value
        });
    }

    public static bool HasFormat(this Block block, int start, int end, TextFormatType type)
    {
        return block.GetFormatting().Any(f =>
            f.Type == type && f.Start <= start && f.End >= end);
    }

    public static string RenderFormattedContent(this Block block)
    {
        var content = block.Content;
        if (string.IsNullOrEmpty(content))
        {
            return string.Empty;
        }

        var formats = block.GetFormatting();
        if (formats.Count == 0)
        {
            return System.Web.HttpUtility.HtmlEncode(content);
        }

        // Build a list of format events (start/end points)
        var events = new List<(int Position, bool IsStart, TextFormat Format)>();
        foreach (var format in formats)
        {
            if (format.Start < content.Length && format.End > 0)
            {
                var start = Math.Max(0, format.Start);
                var end = Math.Min(content.Length, format.End);
                events.Add((start, true, format));
                events.Add((end, false, format));
            }
        }

        // Sort events: by position, then ends before starts at same position
        events = events
            .OrderBy(e => e.Position)
            .ThenBy(e => e.IsStart ? 1 : 0)
            .ToList();

        var result = new StringBuilder();
        var activeFormats = new List<TextFormat>();
        var lastPosition = 0;

        foreach (var evt in events)
        {
            // Append text from last position to current position
            if (evt.Position > lastPosition)
            {
                var text = content.Substring(lastPosition, evt.Position - lastPosition);
                result.Append(System.Web.HttpUtility.HtmlEncode(text));
            }

            if (evt.IsStart)
            {
                result.Append(GetOpenTag(evt.Format));
                activeFormats.Add(evt.Format);
            }
            else
            {
                result.Append(GetCloseTag(evt.Format));
                activeFormats.Remove(evt.Format);
            }

            lastPosition = evt.Position;
        }

        // Append remaining text
        if (lastPosition < content.Length)
        {
            result.Append(System.Web.HttpUtility.HtmlEncode(content.Substring(lastPosition)));
        }

        return result.ToString();
    }

    public static Block ClearFormatting(this Block block)
    {
        return block.WithFormatting([]);
    }

    public static Block AdjustFormattingForContentChange(this Block block, string newContent)
    {
        var formats = block.GetFormatting();
        if (formats.Count == 0)
        {
            return block with { Content = newContent };
        }

        // Clamp formats to new content length and remove invalid ones
        var adjustedFormats = formats
            .Select(f => f with
            {
                Start = Math.Min(f.Start, newContent.Length),
                End = Math.Min(f.End, newContent.Length)
            })
            .Where(f => f.Start < f.End)
            .ToList();

        return (block with { Content = newContent }).WithFormatting(adjustedFormats);
    }

    private static IReadOnlyList<TextFormat> NormalizeFormats(List<TextFormat> formats, int contentLength)
    {
        // Remove invalid formats and merge adjacent/overlapping formats of the same type
        var normalized = new List<TextFormat>();

        foreach (var typeGroup in formats.GroupBy(f => (f.Type, f.Value)))
        {
            var sorted = typeGroup
                .Where(f => f.Start < f.End && f.Start < contentLength && f.End > 0)
                .Select(f => f with
                {
                    Start = Math.Max(0, f.Start),
                    End = Math.Min(contentLength, f.End)
                })
                .OrderBy(f => f.Start)
                .ToList();

            if (sorted.Count == 0) continue;

            var current = sorted[0];
            for (var i = 1; i < sorted.Count; i++)
            {
                var next = sorted[i];
                if (next.Start <= current.End)
                {
                    // Merge overlapping or adjacent formats
                    current = current with { End = Math.Max(current.End, next.End) };
                }
                else
                {
                    normalized.Add(current);
                    current = next;
                }
            }
            normalized.Add(current);
        }

        return normalized;
    }

    private static string GetOpenTag(TextFormat format)
    {
        return format.Type switch
        {
            TextFormatType.Bold => "<strong>",
            TextFormatType.Italic => "<em>",
            TextFormatType.Color when !string.IsNullOrEmpty(format.Value) =>
                $"<span style=\"color:{System.Web.HttpUtility.HtmlAttributeEncode(format.Value)}\">",
            _ => string.Empty
        };
    }

    private static string GetCloseTag(TextFormat format)
    {
        return format.Type switch
        {
            TextFormatType.Bold => "</strong>",
            TextFormatType.Italic => "</em>",
            TextFormatType.Color => "</span>",
            _ => string.Empty
        };
    }

    private static IReadOnlyList<TextFormat> DeserializeFormats(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var formats = new List<TextFormat>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.TryGetProperty("Start", out var startProp) ||
                item.TryGetProperty("start", out startProp))
            {
                var start = startProp.GetInt32();

                var end = 0;
                if (item.TryGetProperty("End", out var endProp) ||
                    item.TryGetProperty("end", out endProp))
                {
                    end = endProp.GetInt32();
                }

                var type = TextFormatType.Bold;
                if (item.TryGetProperty("Type", out var typeProp) ||
                    item.TryGetProperty("type", out typeProp))
                {
                    if (typeProp.ValueKind == JsonValueKind.String)
                    {
                        Enum.TryParse<TextFormatType>(typeProp.GetString(), true, out type);
                    }
                    else if (typeProp.ValueKind == JsonValueKind.Number)
                    {
                        type = (TextFormatType)typeProp.GetInt32();
                    }
                }

                string? value = null;
                if (item.TryGetProperty("Value", out var valueProp) ||
                    item.TryGetProperty("value", out valueProp))
                {
                    value = valueProp.GetString();
                }

                formats.Add(new TextFormat
                {
                    Start = start,
                    End = end,
                    Type = type,
                    Value = value
                });
            }
        }

        return formats;
    }
}
