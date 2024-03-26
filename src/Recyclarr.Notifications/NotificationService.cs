using System.Reactive.Disposables;
using System.Text;
using Flurl.Http;
using Recyclarr.Common.Extensions;
using Recyclarr.Http;
using Recyclarr.Notifications.Apprise;
using Recyclarr.Notifications.Apprise.Dto;
using Recyclarr.Notifications.Events;
using Recyclarr.Settings;
using Serilog;

namespace Recyclarr.Notifications;

public sealed class NotificationService(
    ILogger log,
    IAppriseNotificationApiService apprise,
    ISettingsProvider settingsProvider,
    NotificationEmitter notificationEmitter) : IDisposable
{
    private readonly Dictionary<string, List<INotificationEvent>> _events = new();
    private readonly CompositeDisposable _eventConnection = new();
    private string? _activeInstanceName;

    public void Dispose()
    {
        _eventConnection.Dispose();
    }

    public void SetInstanceName(string instanceName)
    {
        _activeInstanceName = instanceName;
    }

    public void BeginWatchEvents()
    {
        _events.Clear();
        _eventConnection.Clear();
        _eventConnection.Add(notificationEmitter.OnNotification.Subscribe(x =>
        {
            ArgumentNullException.ThrowIfNull(_activeInstanceName);
            _events.GetOrCreate(_activeInstanceName).Add(x);
        }));
    }

    public async Task SendNotification(bool succeeded)
    {
        // stop receiving events while we build the report
        _eventConnection.Clear();

        // If the user didn't configure notifications, exit early and do nothing.
        if (settingsProvider.Settings.Notifications is null)
        {
            log.Debug("Notification settings are not present, so this notification will not be sent");
            return;
        }

        var body = new StringBuilder();

        foreach (var (instanceName, notifications) in _events)
        {
            RenderInstanceEvents(body, instanceName, notifications);
        }

        var messageType = AppriseMessageType.Success;
        if (!succeeded)
        {
            messageType = AppriseMessageType.Failure;
        }

        try
        {
            await apprise.Notify("apprise", new AppriseNotification
            {
                Title = $"Recyclarr Sync {(succeeded ? "Completed" : "Failed")}",
                Body = body.ToString(),
                Type = messageType,
                Format = AppriseMessageFormat.Markdown
            });
        }
        catch (FlurlHttpException e)
        {
            log.Error("Failed to send notification: {Msg}", e.SanitizedExceptionMessage());
        }
    }

    private static void RenderInstanceEvents(
        StringBuilder body,
        string instanceName,
        IEnumerable<INotificationEvent> notifications)
    {
        body.AppendLine($"### Instance: `{instanceName}`");

        var groupedEvents = notifications
            .GroupBy(x => x.Category)
            .ToDictionary(x => x.Key, x => x.ToList());

        foreach (var (category, events) in groupedEvents)
        {
            body.AppendLine(
                $"""
                 {category}:
                 {string.Join('\n', events.Select(x => x.Render()))}

                 """);
        }
    }
}
