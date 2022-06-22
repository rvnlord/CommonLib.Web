using System;
using System.Collections.Generic;
using System.Linq;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyPromptComponent;
using CommonLib.Web.Source.Services.Interfaces;

namespace CommonLib.Web.Source.Services
{
    public class ComponentsCacheService : IComponentsCacheService
    {
        public Dictionary<Guid, MyComponentBase> Components { get; set; } = new();
        public NotificationsCache NotificationsData { get; set; } = new();

        public class NotificationsCache
        {
            public Dictionary<string, NotificationRenderingData> PreviouslyPostProcessedNotificationsRenderingData { get; set; } = new();
            public Dictionary<string, Queue<NotificationRenderingData>> NotificationDataToRenderQueue { get; set; } = new();
            public Dictionary<string, Queue<NotificationRenderingData>> NotificationDataToPostProcessQueue { get; set; } = new();
        }

        public class NotificationRenderingData
        {
            public Notification[] AllNotificationsAfterCurrentChanges { get; set; }
            public Notification[] AllNotificationsToShowOrAlreadyShown { get; set; }
            public Notification[] NotificationsToShowFromPage { get; set; }
            public Notification[] NotificationsAlreadyShownFromPage { get; set; }
            public Notification[] NotificationsToHide { get; set; }
            public Guid[] NotificationsToShowGuids { get; set; }
            public Guid[] NotificationsAlreadyShownGuids { get; set; }
            public Guid[] NotificationsToHideGuids { get; set; }
            public Notification[] NotificationsToRender { get; set; }
            public Notification[] TotalAddedNotifications { get; set; }
            public Notification[] TotalRemovedNotifications { get; set; }
            public Notification[] NewNotificationsToRemove { get; set; }
            public Notification[] NewNotificationsToAdd { get; set; }


            public override bool Equals(object obj)
            {
                if (obj is not NotificationRenderingData that)
                    return false;

                return AllNotificationsAfterCurrentChanges.SequenceEqual(that.AllNotificationsAfterCurrentChanges) 
                       && NewNotificationsToRemove.SequenceEqual(that.NewNotificationsToRemove) 
                       && NewNotificationsToAdd.SequenceEqual(that.NewNotificationsToAdd);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(AllNotificationsAfterCurrentChanges, NewNotificationsToRemove, NewNotificationsToAdd);
            }
        }
    }
}
