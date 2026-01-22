namespace Nouz.Components.Modals;

public partial class ImagePreviewModal
{
    private bool _isVisible;
    private string? _imageDataUrl;
    private string? _caption;

    public void Show(string imageDataUrl, string? caption = null)
    {
        _imageDataUrl = imageDataUrl;
        _caption = caption;
        _isVisible = true;
        StateHasChanged();
    }

    private void Close()
    {
        _isVisible = false;
        StateHasChanged();
    }
}
