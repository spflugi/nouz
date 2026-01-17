using Microsoft.AspNetCore.Components;
using Nouz.Domain.Entities;

namespace Nouz.Components.Notes;

public partial class BlockContextMenu
{
    [Parameter, EditorRequired]
    public BlockType CurrentType { get; set; }

    [Parameter]
    public EventCallback<BlockType> OnTypeSelected { get; set; }

    [Parameter]
    public EventCallback OnDeleteSelected { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }
}
