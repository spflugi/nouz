using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Nouz.Domain.Entities;
using Nouz.Domain.Repositories;

namespace Nouz.Application.Notes;

/// <summary>
/// Service for exporting notes to HTML format.
/// </summary>
internal sealed partial class NoteExportService
{
    private readonly IAttachmentRepository _attachmentRepository;

    public NoteExportService(IAttachmentRepository attachmentRepository)
    {
        _attachmentRepository = attachmentRepository;
    }

    /// <summary>
    /// Generates a self-contained HTML document from a note.
    /// Images are embedded as base64 data URLs.
    /// </summary>
    public async Task<string> GenerateHtmlAsync(Note note, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        // HTML document structure
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"UTF-8\">");
        sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"    <title>Note - {HtmlEncode(note.CreatedAt.LocalDateTime.ToString("dd MMM yyyy"))}</title>");
        sb.AppendLine("    <style>");
        sb.AppendLine(GetEmbeddedStyles());
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <article class=\"note\">");
        sb.AppendLine($"        <header class=\"note-header\">");
        sb.AppendLine($"            <time datetime=\"{note.CreatedAt:O}\">{HtmlEncode(note.CreatedAt.LocalDateTime.ToString("dd MMMM yyyy, HH:mm"))}</time>");
        sb.AppendLine($"        </header>");
        sb.AppendLine("        <div class=\"note-content\">");

        // Track list context for proper nesting
        BlockType? previousBlockType = null;
        var inList = false;

        foreach (var block in note.Blocks.OrderBy(b => b.Order))
        {
            var isListType = block.Type is BlockType.ListItem or BlockType.AgendaItem;

            // Close list if transitioning away from list items
            if (inList && !isListType)
            {
                sb.AppendLine("            </ul>");
                inList = false;
            }

            // Open list if starting list items
            if (isListType && !inList)
            {
                var listClass = block.Type == BlockType.AgendaItem ? "agenda-list" : "bullet-list";
                sb.AppendLine($"            <ul class=\"{listClass}\">");
                inList = true;
            }

            var blockHtml = await RenderBlockAsync(block, cancellationToken).ConfigureAwait(false);
            sb.AppendLine(blockHtml);

            previousBlockType = block.Type;
        }

        // Close any remaining list
        if (inList)
        {
            sb.AppendLine("            </ul>");
        }

        sb.AppendLine("        </div>");
        sb.AppendLine("    </article>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private async Task<string> RenderBlockAsync(Block block, CancellationToken cancellationToken)
    {
        var content = FormatInlineContent(block.Content);

        return block.Type switch
        {
            BlockType.Paragraph => $"            <p>{content}</p>",
            BlockType.H1 => $"            <h1>{content}</h1>",
            BlockType.H2 => $"            <h2>{content}</h2>",
            BlockType.H3 => $"            <h3>{content}</h3>",
            BlockType.H4 => $"            <h4>{content}</h4>",
            BlockType.ListItem => RenderListItem(block),
            BlockType.AgendaItem => RenderAgendaItem(block),
            BlockType.TodoItem => RenderTodoItem(block),
            BlockType.Code => RenderCodeBlock(block),
            BlockType.Quote => $"            <blockquote>{content}</blockquote>",
            BlockType.Decision => RenderDecisionBlock(block),
            BlockType.Warning => RenderWarningBlock(block),
            BlockType.Divider => "            <hr>",
            BlockType.Image => await RenderImageBlockAsync(block, cancellationToken).ConfigureAwait(false),
            BlockType.Table => RenderTableBlock(block),
            BlockType.Mermaid => RenderMermaidBlock(block),
            _ => $"            <p>{content}</p>"
        };
    }

    private string RenderListItem(Block block)
    {
        var content = FormatInlineContent(block.Content);
        var indent = GetIndentLevel(block);
        var paddingStyle = indent > 0 ? $" style=\"padding-left: {indent * 20}px\"" : "";
        return $"                <li{paddingStyle}>{content}</li>";
    }

    private string RenderAgendaItem(Block block)
    {
        var content = FormatInlineContent(block.Content);
        var indent = GetIndentLevel(block);
        var paddingStyle = indent > 0 ? $" style=\"padding-left: {indent * 20}px\"" : "";
        return $"                <li class=\"agenda-item\"{paddingStyle}><span class=\"agenda-chevron\">&#8250;</span>{content}</li>";
    }

    private string RenderTodoItem(Block block)
    {
        var content = FormatInlineContent(block.Content);
        var isChecked = GetMetadataValue<bool>(block, "checked");
        var checkboxHtml = isChecked
            ? "<span class=\"todo-checkbox checked\">&#10003;</span>"
            : "<span class=\"todo-checkbox\"></span>";
        var checkedClass = isChecked ? " class=\"todo-checked\"" : "";
        return $"            <div class=\"todo-item\"{checkedClass}>{checkboxHtml}<span class=\"todo-text\">{content}</span></div>";
    }

    private static string RenderCodeBlock(Block block)
    {
        // For code blocks, don't apply inline formatting - escape and preserve whitespace
        var escapedContent = HtmlEncode(block.Content);
        return $"            <pre><code>{escapedContent}</code></pre>";
    }

    private string RenderDecisionBlock(Block block)
    {
        var content = FormatInlineContent(block.Content);
        return $"            <div class=\"decision-box\"><span class=\"decision-icon\">&#10003;</span><span class=\"decision-text\">{content}</span></div>";
    }

    private string RenderWarningBlock(Block block)
    {
        var content = FormatInlineContent(block.Content);
        return $"            <div class=\"warning-box\"><span class=\"warning-icon\">&#9888;</span><span class=\"warning-text\">{content}</span></div>";
    }

    private async Task<string> RenderImageBlockAsync(Block block, CancellationToken cancellationToken)
    {
        var caption = GetMetadataValue<string>(block, "caption") ?? "";
        var mimeType = GetMetadataValue<string>(block, "mimeType") ?? "image/png";
        var widthPercent = GetMetadataValue<int>(block, "widthPercent");
        if (widthPercent <= 0) widthPercent = 50;

        var attachmentIdStr = GetMetadataValue<string>(block, "attachmentId");
        if (string.IsNullOrEmpty(attachmentIdStr) || !Guid.TryParse(attachmentIdStr, out var attachmentId))
        {
            return "            <figure><p><em>[Image not found]</em></p></figure>";
        }

        var imageData = await _attachmentRepository.LoadAsync(attachmentId, cancellationToken).ConfigureAwait(false);
        if (imageData is null)
        {
            return "            <figure><p><em>[Image not found]</em></p></figure>";
        }

        var base64 = Convert.ToBase64String(imageData);
        var dataUrl = $"data:{mimeType};base64,{base64}";

        var sb = new StringBuilder();
        sb.AppendLine($"            <figure style=\"width: {widthPercent}%;\">");
        sb.AppendLine($"                <img src=\"{dataUrl}\" alt=\"{HtmlEncode(caption)}\">");
        if (!string.IsNullOrWhiteSpace(caption))
        {
            sb.AppendLine($"                <figcaption>{HtmlEncode(caption)}</figcaption>");
        }
        sb.Append("            </figure>");
        return sb.ToString();
    }

    private static string RenderTableBlock(Block block)
    {
        var sb = new StringBuilder();
        sb.AppendLine("            <table class=\"export-table\">");

        var data = GetTableData(block);
        var hasHeader = GetMetadataValue<bool>(block, "hasHeader");

        for (var rowIndex = 0; rowIndex < data.Count; rowIndex++)
        {
            var row = data[rowIndex];
            var isHeader = hasHeader && rowIndex == 0;
            sb.AppendLine("                <tr>");
            foreach (var cell in row)
            {
                var tag = isHeader ? "th" : "td";
                sb.AppendLine($"                    <{tag}>{HtmlEncode(cell)}</{tag}>");
            }
            sb.AppendLine("                </tr>");
        }

        sb.Append("            </table>");
        return sb.ToString();
    }

    private static List<List<string>> GetTableData(Block block)
    {
        if (!block.Metadata.TryGetValue("data", out var dataObj))
        {
            return [];
        }

        if (dataObj is List<List<string>> list)
        {
            return list;
        }

        if (dataObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            var result = new List<List<string>>();
            foreach (var rowElement in jsonElement.EnumerateArray())
            {
                var row = new List<string>();
                if (rowElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var cellElement in rowElement.EnumerateArray())
                    {
                        row.Add(cellElement.GetString() ?? string.Empty);
                    }
                }
                result.Add(row);
            }
            return result;
        }

        return [];
    }

    private static string RenderMermaidBlock(Block block)
    {
        var escapedContent = HtmlEncode(block.Content);
        return $"            <div class=\"mermaid-export\"><pre><code class=\"language-mermaid\">{escapedContent}</code></pre></div>";
    }

    private static string FormatInlineContent(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return string.Empty;
        }

        // First, HTML-encode the base content
        var result = HtmlEncode(content);

        // Then apply inline formatting patterns
        // Bold: **text** or __text__
        result = BoldPatternStars().Replace(result, "<strong>$1</strong>");
        result = BoldPatternUnderscores().Replace(result, "<strong>$1</strong>");

        // Italic: *text* or _text_ (but not inside bold)
        result = ItalicPatternStars().Replace(result, "<em>$1</em>");
        result = ItalicPatternUnderscores().Replace(result, "<em>$1</em>");

        // Strikethrough: ~~text~~
        result = StrikethroughPattern().Replace(result, "<del>$1</del>");

        // Code: `text`
        result = InlineCodePattern().Replace(result, "<code>$1</code>");

        // Color: {color:hex}text{/color}
        result = ColorPattern().Replace(result, "<span style=\"color: $1;\">$2</span>");

        return result;
    }

    [GeneratedRegex(@"\*\*(.+?)\*\*")]
    private static partial Regex BoldPatternStars();

    [GeneratedRegex(@"__(.+?)__")]
    private static partial Regex BoldPatternUnderscores();

    [GeneratedRegex(@"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)")]
    private static partial Regex ItalicPatternStars();

    [GeneratedRegex(@"(?<!_)_(?!_)(.+?)(?<!_)_(?!_)")]
    private static partial Regex ItalicPatternUnderscores();

    [GeneratedRegex(@"~~(.+?)~~")]
    private static partial Regex StrikethroughPattern();

    [GeneratedRegex(@"`(.+?)`")]
    private static partial Regex InlineCodePattern();

    [GeneratedRegex(@"\{color:(#[0-9A-Fa-f]{6})\}(.+?)\{/color\}")]
    private static partial Regex ColorPattern();

    private static T? GetMetadataValue<T>(Block block, string key)
    {
        if (!block.Metadata.TryGetValue(key, out var value))
        {
            return default;
        }

        // Handle different value types from JSON deserialization
        if (value is T typedValue)
        {
            return typedValue;
        }

        if (value is JsonElement jsonElement)
        {
            return typeof(T) switch
            {
                Type t when t == typeof(bool) => (T)(object)jsonElement.GetBoolean(),
                Type t when t == typeof(int) => (T)(object)jsonElement.GetInt32(),
                Type t when t == typeof(string) => (T)(object)(jsonElement.GetString() ?? string.Empty),
                _ => default
            };
        }

        // Try string conversion for booleans
        if (typeof(T) == typeof(bool) && value is string str)
        {
            return (T)(object)(str.Equals("True", StringComparison.OrdinalIgnoreCase) || str == "true");
        }

        // Try conversion for int
        if (typeof(T) == typeof(int) && value is string intStr && int.TryParse(intStr, out var intValue))
        {
            return (T)(object)intValue;
        }

        return default;
    }

    private static int GetIndentLevel(Block block)
    {
        if (block.Metadata.TryGetValue("indent", out var indentValue))
        {
            return indentValue switch
            {
                int i => i,
                JsonElement je => je.GetInt32(),
                string s when int.TryParse(s, out var parsed) => parsed,
                _ => 0
            };
        }
        return 0;
    }

    private static string HtmlEncode(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    private static string GetEmbeddedStyles()
    {
        return """
                * {
                    box-sizing: border-box;
                    margin: 0;
                    padding: 0;
                }

                body {
                    font-family: "Inter", -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
                    font-size: 14px;
                    line-height: 1.6;
                    color: #1A1A1A;
                    background-color: #FFFFFF;
                    padding: 40px;
                    max-width: 800px;
                    margin: 0 auto;
                }

                .note {
                    background-color: #FFFFFF;
                }

                .note-header {
                    margin-bottom: 24px;
                    padding-bottom: 16px;
                    border-bottom: 1px solid #E0DFDD;
                }

                .note-header time {
                    color: #5C5C5C;
                    font-size: 13px;
                }

                .note-content {
                    display: flex;
                    flex-direction: column;
                    gap: 12px;
                }

                h1, h2, h3, h4 {
                    color: #1A1A1A;
                    font-weight: 600;
                    margin-top: 8px;
                }

                h1 { font-size: 24px; }
                h2 { font-size: 20px; }
                h3 { font-size: 17px; }
                h4 { font-size: 15px; }

                p {
                    color: #1A1A1A;
                }

                ul.bullet-list,
                ul.agenda-list {
                    list-style: none;
                    padding-left: 0;
                }

                ul.bullet-list li {
                    position: relative;
                    padding-left: 20px;
                }

                ul.bullet-list li::before {
                    content: "•";
                    position: absolute;
                    left: 4px;
                    color: #5C5C5C;
                }

                ul.agenda-list li {
                    padding-left: 0;
                }

                .agenda-chevron {
                    color: #5C5C5C;
                    margin-right: 8px;
                    font-weight: bold;
                }

                .todo-item {
                    display: flex;
                    align-items: flex-start;
                    gap: 8px;
                }

                .todo-checkbox {
                    display: inline-flex;
                    align-items: center;
                    justify-content: center;
                    width: 16px;
                    height: 16px;
                    border: 1px solid #8A8A8A;
                    border-radius: 3px;
                    font-size: 11px;
                    flex-shrink: 0;
                    margin-top: 3px;
                }

                .todo-checkbox.checked {
                    background-color: #3D3D3D;
                    border-color: #3D3D3D;
                    color: #FFFFFF;
                }

                .todo-checked .todo-text {
                    text-decoration: line-through;
                    color: #8A8A8A;
                }

                pre {
                    background-color: #F7F6F4;
                    border: 1px solid #E0DFDD;
                    border-radius: 6px;
                    padding: 12px 16px;
                    overflow-x: auto;
                }

                pre code {
                    font-family: "SF Mono", "Consolas", "Monaco", monospace;
                    font-size: 13px;
                    color: #1A1A1A;
                    background: none;
                    padding: 0;
                }

                code {
                    font-family: "SF Mono", "Consolas", "Monaco", monospace;
                    font-size: 13px;
                    background-color: #F0EFED;
                    padding: 2px 6px;
                    border-radius: 4px;
                }

                blockquote {
                    border-left: 3px solid #E0DFDD;
                    padding-left: 16px;
                    color: #5C5C5C;
                    font-style: italic;
                }

                .decision-box,
                .warning-box {
                    display: flex;
                    align-items: flex-start;
                    gap: 12px;
                    padding: 12px 16px;
                    border-radius: 6px;
                }

                .decision-box {
                    background-color: rgba(110, 158, 124, 0.12);
                    border: 1px solid rgba(110, 158, 124, 0.3);
                }

                .decision-icon {
                    color: #6E9E7C;
                    font-weight: bold;
                    font-size: 16px;
                }

                .warning-box {
                    background-color: rgba(201, 162, 77, 0.14);
                    border: 1px solid rgba(201, 162, 77, 0.3);
                }

                .warning-icon {
                    color: #C9A24D;
                    font-size: 16px;
                }

                hr {
                    border: none;
                    border-top: 1px solid #E0DFDD;
                    margin: 8px 0;
                }

                .export-table {
                    width: 100%;
                    border-collapse: collapse;
                    font-size: 13px;
                }

                .export-table th,
                .export-table td {
                    border: 1px solid #E0DFDD;
                    padding: 8px 12px;
                    text-align: left;
                }

                .export-table th {
                    background-color: #F7F6F4;
                    font-weight: 600;
                }

                .mermaid-export {
                    background-color: #F7F6F4;
                    border: 1px solid #E0DFDD;
                    border-radius: 6px;
                    padding: 12px 16px;
                }

                .mermaid-export pre {
                    background: none;
                    border: none;
                    padding: 0;
                    margin: 0;
                }

                .mermaid-export code {
                    background: none;
                    padding: 0;
                }

                figure {
                    margin: 0;
                }

                figure img {
                    max-width: 100%;
                    height: auto;
                    border-radius: 6px;
                    display: block;
                }

                figcaption {
                    color: #5C5C5C;
                    font-size: 12px;
                    text-align: center;
                    margin-top: 8px;
                }

                strong {
                    font-weight: 600;
                }

                em {
                    font-style: italic;
                }

                del {
                    text-decoration: line-through;
                    color: #8A8A8A;
                }

                @media print {
                    body {
                        padding: 20px;
                        font-size: 12px;
                    }

                    .note-header {
                        margin-bottom: 16px;
                        padding-bottom: 12px;
                    }

                    pre {
                        white-space: pre-wrap;
                        word-break: break-word;
                    }

                    figure {
                        break-inside: avoid;
                    }
                }
        """;
    }
}
