namespace Recyclarr.Notifications.Events;

public interface INotificationEvent
{
    public string Category { get; }
    public string Render();
}
