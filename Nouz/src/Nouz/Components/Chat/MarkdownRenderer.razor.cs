using System.Text.RegularExpressions;
using Markdig;
using Microsoft.AspNetCore.Components;

namespace Nouz.Components.Chat;

public partial class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private string _renderedHtml = string.Empty;

    [Parameter]
    public string Content { get; set; } = string.Empty;

    protected override void OnParametersSet()
    {
        _renderedHtml = RenderMarkdown(Content);
    }

    private static string RenderMarkdown(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return string.Empty;
        }

        var html = Markdown.ToHtml(markdown, Pipeline);

        // Sanitize the output to prevent XSS while keeping safe HTML tags
        html = SanitizeHtml(html);

        return html;
    }

    private static string SanitizeHtml(string html)
    {
        // Simple sanitization - allow only safe tags commonly used in markdown
        // This is a basic approach; for production, consider using a library like HtmlSanitizer

        // Define allowed tags and their allowed attributes
        var allowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "p", "br", "strong", "b", "em", "i", "code", "pre",
            "h1", "h2", "h3", "h4", "h5", "h6",
            "ul", "ol", "li",
            "blockquote",
            "a", "span", "div"
        };

        // Remove potentially dangerous attributes and tags
        // Remove script tags and their content
        html = Regex.Replace(html, @"<script[^>]*>.*?</script>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // Remove style tags and their content
        html = Regex.Replace(html, @"<style[^>]*>.*?</style>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // Remove on* event handlers
        html = Regex.Replace(html, @"\s+on\w+\s*=\s*[""'][^""']*[""']", "", RegexOptions.IgnoreCase);

        // Remove javascript: URLs
        html = Regex.Replace(html, @"href\s*=\s*[""']javascript:[^""']*[""']", "href=\"#\"", RegexOptions.IgnoreCase);

        return html;
    }
}
