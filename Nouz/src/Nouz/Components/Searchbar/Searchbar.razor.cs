using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.AspNetCore.Components;
using Nouz.Application.Notes;
using Nouz.Extensions;

namespace Nouz.Components.Searchbar;

public partial class Searchbar
{
    private string _query = string.Empty;
    private Guid? _selectedNotebookId;
    private readonly Subject<string> _searchSubject = new();

    protected override void OnInitialized()
    {
        StateProvider.StateObservable
            .Select(s => s.Notebooks.SelectedNotebook)
            .DistinctUntilChanged()
            .TakeUntilDisappearing(this)
            .Subscribe(notebookId =>
            {
                _selectedNotebookId = notebookId == Guid.Empty ? null : notebookId;
                // Clear search when notebook changes
                if (!string.IsNullOrEmpty(_query))
                {
                    _query = string.Empty;
                    _ = Mediator.Send(new NoteCommands.ClearSearch());
                }
                StateHasChanged();
            });

        // Debounce search input to avoid too many queries
        _searchSubject
            .Throttle(TimeSpan.FromMilliseconds(300))
            .DistinctUntilChanged()
            .TakeUntilDisappearing(this)
            .Subscribe(async query =>
            {
                if (_selectedNotebookId is null)
                {
                    return;
                }

                if (query.Length >= 2)
                {
                    await Mediator.Send(new NoteCommands.SearchNotes(_selectedNotebookId.Value, query));
                }
                else
                {
                    await Mediator.Send(new NoteCommands.ClearSearch());
                }
            });
    }

    private void HandleSearchInput(ChangeEventArgs e)
    {
        var value = e.Value?.ToString() ?? string.Empty;
        _query = value;
        _searchSubject.OnNext(value);
    }

    private async Task ClearSearchInput()
    {
        _query = string.Empty;
        _searchSubject.OnNext(string.Empty);
        await Mediator.Send(new NoteCommands.ClearSearch());
    }

    private async Task CreateNote()
    {
        if (_selectedNotebookId is not null)
        {
            await Mediator.Send(new NoteCommands.CreateNote(_selectedNotebookId.Value));
        }
    }

    public override void Dispose()
    {
        _searchSubject.Dispose();
        base.Dispose();
    }
}