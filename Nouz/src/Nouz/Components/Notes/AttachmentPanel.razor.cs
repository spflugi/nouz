using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Nouz.Domain.Entities;

namespace Nouz.Components.Notes;

public partial class AttachmentPanel
{
    private bool _showDropdown;
    private ImmutableList<NoteAttachment> _attachments = [];

    [Parameter, EditorRequired]
    public Guid NoteId { get; set; }

    [Parameter]
    public ImmutableList<NoteAttachment> Attachments { get; set; } = [];

    [Parameter]
    public EventCallback<(Guid NoteId, Stream FileStream, string FileName)> OnAddAttachment { get; set; }

    [Parameter]
    public EventCallback<(Guid NoteId, Guid AttachmentId)> OnDeleteAttachment { get; set; }

    [Parameter]
    public EventCallback<Guid> OnOpenAttachment { get; set; }

    protected override void OnParametersSet()
    {
        _attachments = Attachments;
    }

    private void ToggleDropdown()
    {
        _showDropdown = !_showDropdown;
    }

    private void CloseDropdown()
    {
        _showDropdown = false;
    }

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file is null)
        {
            return;
        }

        // Max 50MB
        const long maxFileSize = 50 * 1024 * 1024;
        var stream = file.OpenReadStream(maxFileSize);

        await OnAddAttachment.InvokeAsync((NoteId, stream, file.Name));
        _showDropdown = false;
    }

    private async Task HandleDeleteAttachment(Guid attachmentId)
    {
        await OnDeleteAttachment.InvokeAsync((NoteId, attachmentId));
    }

    private async Task HandleOpenAttachment(Guid attachmentId)
    {
        await OnOpenAttachment.InvokeAsync(attachmentId);
        _showDropdown = false;
    }

    private static string GetFileIcon(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "picture_as_pdf",
            ".doc" or ".docx" => "description",
            ".xls" or ".xlsx" => "grid_on",
            ".ppt" or ".pptx" => "slideshow",
            ".txt" => "text_snippet",
            ".zip" or ".rar" or ".7z" => "folder_zip",
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp" => "image",
            ".mp3" or ".wav" or ".ogg" or ".m4a" => "audio_file",
            ".mp4" or ".avi" or ".mov" or ".mkv" => "video_file",
            ".html" or ".css" or ".js" or ".ts" or ".cs" or ".py" => "code",
            _ => "insert_drive_file"
        };
    }

    private static string FormatFileSize(long bytes)
    {
        const long kb = 1024;
        const long mb = kb * 1024;
        const long gb = mb * 1024;

        return bytes switch
        {
            < kb => $"{bytes} B",
            < mb => $"{bytes / kb:F1} KB",
            < gb => $"{bytes / mb:F1} MB",
            _ => $"{bytes / gb:F1} GB"
        };
    }

    private static string TruncateFileName(string fileName, int maxLength = 25)
    {
        if (fileName.Length <= maxLength)
        {
            return fileName;
        }

        var extension = Path.GetExtension(fileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var availableLength = maxLength - extension.Length - 3; // -3 for "..."

        if (availableLength <= 0)
        {
            return fileName[..(maxLength - 3)] + "...";
        }

        return nameWithoutExtension[..availableLength] + "..." + extension;
    }
}
