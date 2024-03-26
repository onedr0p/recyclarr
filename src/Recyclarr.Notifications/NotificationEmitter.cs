using System.Reactive.Linq;
using System.Reactive.Subjects;
using Recyclarr.Notifications.Events;

namespace Recyclarr.Notifications;

public class NotificationEmitter
{
    private readonly Subject<INotificationEvent> _notifications = new();

    public IObservable<INotificationEvent> OnNotification => _notifications.AsObservable();

    public void NotifyStatistic(string description, string stat)
    {
        _notifications.OnNext(new StatisticEvent(description, stat));
    }

    public void NotifyError(string error)
    {
        _notifications.OnNext(new ErrorEvent(error));
    }
}
