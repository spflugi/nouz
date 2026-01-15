using System.Reactive.Linq;
using Nouz.Components;

namespace Nouz.Extensions;

public static class ObservableExtensions
{
    public static IObservable<T> TakeUntilDisappearing<T>(this IObservable<T> observable, SharedComponentBase component)
    {
        var disposing = Observable.FromEventPattern<EventHandler, EventArgs>(
            handler => component.DisposingEvent += handler,
            handler => component.DisposingEvent -= handler);

        return observable.TakeUntil(disposing);
    }
}