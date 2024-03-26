namespace Recyclarr.Notifications.Events;

public record ErrorEvent(string Error) : INotificationEvent
{
    public string Category => "Errors";
    public string Render() => $"- {Error}";
}
