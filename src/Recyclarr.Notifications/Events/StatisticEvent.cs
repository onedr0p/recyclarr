namespace Recyclarr.Notifications.Events;

public record StatisticEvent(string Description, string Statistic) : INotificationEvent
{
    public string Category => "Statistics";
    public string Render() => $"- {Description}: {Statistic}";
}