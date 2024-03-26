using Autofac;
using Recyclarr.Notifications.Apprise;

namespace Recyclarr.Notifications;

public class NotificationsAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterType<NotificationService>().InstancePerLifetimeScope();
        builder.RegisterType<NotificationEmitter>().InstancePerLifetimeScope();

        // Apprise
        builder.RegisterType<AppriseNotificationApiService>().As<IAppriseNotificationApiService>();
        builder.RegisterType<AppriseRequestBuilder>().As<IAppriseRequestBuilder>();
    }
}
