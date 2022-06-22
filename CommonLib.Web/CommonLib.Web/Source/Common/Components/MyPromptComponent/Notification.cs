using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Utils.UtilClasses;

namespace CommonLib.Web.Source.Common.Components.MyPromptComponent
{
    public class Notification : IDisposable
    {
        private IconType _icon;
        private TimeSpan _removeAfter = TimeSpan.Zero;
        private TimeSpan _newFor = TimeSpan.FromMinutes(1);
       
        public bool HasDelayElapsed { get; private set; }
        public bool HasNewBadgeExpired { get; private set; }
        public string RenderCssClasses => $"my-notification {Type.EnumToString().ToLower()}";
        public Guid Guid { get; }
        public NotificationType Type { get; set; } = NotificationType.Info;
        public string Message { get; set; }
        public IconType Icon
        {
            get => _icon ?? Type switch
            {
                NotificationType.Success => IconType.From(LightIconType.BadgeCheck),
                NotificationType.Error => IconType.From(LightIconType.DoNotEnter),
                NotificationType.Warning => IconType.From(LightIconType.ExclamationTriangle),
                NotificationType.Info => IconType.From(LightIconType.InfoSquare),
                NotificationType.Primary => IconType.From(LightIconType.CommentLines),
                _ => null
            };
            set => _icon = value;
        }

        public Visibility Visibility { get; set; } = Visibility.Shown;
        public bool IsRendered { get; set; }
        public ExtendedTime TimeStamp { get; set; }

        public TimeSpan NewFor
        {
            get => _newFor;
            set
            {
                _newFor = value;
                HasNewBadgeExpired = true;

                if (value > TimeSpan.Zero)
                {
                    HasNewBadgeExpired = false;
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(value);
                        if (!HasNewBadgeExpired)
                            await OnNewBadgeExpiringAsync();
                        HasNewBadgeExpired = true;
                    });
                }
            }
        }

        public TimeSpan RemoveAfter
        {
            get => _removeAfter;
            set
            {
                _removeAfter = value;
                HasDelayElapsed = true;

                if (value > TimeSpan.Zero)
                {
                    HasDelayElapsed = false;
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(value);
                        if (!HasDelayElapsed)
                            await OnNotificationDelayElapsingAsync();
                        HasDelayElapsed = true;
                    });
                }
            }
        }

        public Notification()
        {
            Guid = Guid.NewGuid();
            TimeStamp = ExtendedTime.UtcNow;
        }

        public static List<Notification> SingleNotificationList(NotificationType type, string message)
        {
            return new List<Notification>
            {
                new()
                {
                    Type = NotificationType.Error,
                    Message = message
                }
            };
        }

        public event MyAsyncEventHandler<Notification, NotificationDelayElapsedEventArgs> NotificationDelayElapsed;
        public event MyAsyncEventHandler<Notification, EventArgs> NewBadgeExpired;

        private async Task OnNotificationDelayElapsingAsync(NotificationDelayElapsedEventArgs e) => await NotificationDelayElapsed.InvokeAsync(this, e);
        private async Task OnNotificationDelayElapsingAsync() => await OnNotificationDelayElapsingAsync(new NotificationDelayElapsedEventArgs());
        private async Task OnNewBadgeExpiringAsync(EventArgs e) => await NewBadgeExpired.InvokeAsync(this, e);
        private async Task OnNewBadgeExpiringAsync() => await OnNewBadgeExpiringAsync(EventArgs.Empty);

        public override string ToString()
        {
            return $"[{Guid.ToString().Take(4)}..{Guid.ToString().TakeLast(4)}] [{TimeStamp.ToTimeDateString()}] [{Visibility.EnumToString()}] [{(IsRendered ? "Rendered" : "Not Rendered")}] [{Type}] [{Icon}] \"{Message}\"";
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;
        }
    }

    //public delegate void NotificationDelayElapsedEventHandler(Notification sender, NotificationDelayElapsedEventArgs e);

    public class NotificationDelayElapsedEventArgs : EventArgs
    {
        public NotificationDelayElapsedEventArgs() { }
    }

    public enum NotificationType
    {
        Success,
        Error,
        Warning,
        Info,
        Primary,
        Loading
    }

    public enum Visibility
    {
        Shown,
        Hidden
    }
}
