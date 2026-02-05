using Microsoft.AspNetCore.Components;
using Nouz.Domain.Entities;

namespace Nouz.Components.Notes;

public partial class TableBlock
{
    [Parameter, EditorRequired]
    public Block Block { get; set; } = null!;

    [Parameter, EditorRequired]
    public Guid NoteId { get; set; }

    [Parameter]
    public EventCallback OnAddRow { get; set; }

    [Parameter]
    public EventCallback<int> OnRemoveRow { get; set; }

    [Parameter]
    public EventCallback OnAddColumn { get; set; }

    [Parameter]
    public EventCallback<int> OnRemoveColumn { get; set; }

    [Parameter]
    public EventCallback<(int RowIndex, int ColIndex, string Content)> OnCellChanged { get; set; }

    [Parameter]
    public EventCallback OnInsertBlockAfter { get; set; }

    private List<List<string>> TableData => GetTableData();
    private int Columns => GetColumns();
    private bool HasHeader => GetHasHeader();

    private List<List<string>> GetTableData()
    {
        if (Block.Metadata.TryGetValue("data", out var dataObj))
        {
            return dataObj switch
            {
                List<List<string>> list => list,
                System.Text.Json.JsonElement jsonElement => ParseJsonTableData(jsonElement),
                _ => CreateDefaultTableData()
            };
        }
        return CreateDefaultTableData();
    }

    private static List<List<string>> ParseJsonTableData(System.Text.Json.JsonElement jsonElement)
    {
        var result = new List<List<string>>();
        if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var rowElement in jsonElement.EnumerateArray())
            {
                var row = new List<string>();
                if (rowElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var cellElement in rowElement.EnumerateArray())
                    {
                        row.Add(cellElement.GetString() ?? string.Empty);
                    }
                }
                result.Add(row);
            }
        }
        return result.Count > 0 ? result : CreateDefaultTableData();
    }

    private static List<List<string>> CreateDefaultTableData()
    {
        return new List<List<string>>
        {
            new() { "Column 1", "Column 2", "Column 3" },
            new() { "", "", "" },
            new() { "", "", "" }
        };
    }

    private int GetColumns()
    {
        if (Block.Metadata.TryGetValue("columns", out var columnsObj))
        {
            return columnsObj switch
            {
                int intValue => intValue,
                long longValue => (int)longValue,
                System.Text.Json.JsonElement jsonElement => jsonElement.GetInt32(),
                _ => 3
            };
        }
        return 3;
    }

    private bool GetHasHeader()
    {
        if (Block.Metadata.TryGetValue("hasHeader", out var hasHeaderObj))
        {
            return hasHeaderObj switch
            {
                bool boolValue => boolValue,
                System.Text.Json.JsonElement jsonElement => jsonElement.GetBoolean(),
                _ => true
            };
        }
        return true;
    }

    private async Task HandleAddRow()
    {
        await OnAddRow.InvokeAsync();
    }

    private async Task HandleRemoveRow(int rowIndex)
    {
        await OnRemoveRow.InvokeAsync(rowIndex);
    }

    private async Task HandleAddColumn()
    {
        await OnAddColumn.InvokeAsync();
    }

    private async Task HandleRemoveColumn(int colIndex)
    {
        await OnRemoveColumn.InvokeAsync(colIndex);
    }

    private async Task HandleCellChange(int rowIndex, int colIndex, ChangeEventArgs e)
    {
        var content = e.Value?.ToString() ?? string.Empty;
        await OnCellChanged.InvokeAsync((rowIndex, colIndex, content));
    }

    private async Task HandleInsertBlockAfter()
    {
        await OnInsertBlockAfter.InvokeAsync();
    }
}
