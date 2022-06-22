using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Web.Source.Models;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using CommonLib.Web.Source.Common.Utils;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Services;
using CommonLib.Web.Source.Services.Interfaces;
using Microsoft.AspNetCore.Components.Web;

namespace CommonLib.Web.Source.Common.Components.MyPromptComponent
{
    public class MyPromptBase : MyComponentBase
    {
        private SemaphoreSlim _syncNotifications;
        private static Dictionary<string, List<Notification>> _notificationsToShow;
        private TimeSpan _newFor;
        private TimeSpan _removeAfter;
        private int _max;

        protected ElementReference _jsPrompt { get; set; }
        protected static Dictionary<string, ComponentsCacheService.NotificationRenderingData> _previouslyPostProcessedNotificationsRenderingData;
        protected static Dictionary<string, Queue<ComponentsCacheService.NotificationRenderingData>> _notificationDataToRenderQueue;
        protected static Dictionary<string, Queue<ComponentsCacheService.NotificationRenderingData>> _notificationDataToPostProcessQueue;

        [Parameter] 
        public BlazorParameter<List<Notification>> Notifications { get; set; }

        [Parameter] 
        public BlazorParameter<TimeSpan?> NewFor { get; set; }

        [Parameter] 
        public BlazorParameter<TimeSpan?> RemoveAfter { get; set; }

        [Parameter] 
        public BlazorParameter<int?> Max { get; set; }
        
        protected override async Task OnInitializedAsync()
        {
            _syncNotifications ??= new SemaphoreSlim(1, 1);
            _notificationsToShow ??= new Dictionary<string, List<Notification>>();
            Notifications.ParameterValue ??= new List<Notification>();

            _previouslyPostProcessedNotificationsRenderingData ??= ComponentsCache.NotificationsData.PreviouslyPostProcessedNotificationsRenderingData;
            _notificationDataToRenderQueue ??= ComponentsCache.NotificationsData.NotificationDataToRenderQueue;
            _notificationDataToPostProcessQueue ??= ComponentsCache.NotificationsData.NotificationDataToPostProcessQueue;

            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            await _syncNotifications.WaitAsync();
            
            if (IsFirstParamSetup())
            {
                SetMainAndUserDefinedClasses("my-prompt");
                SetCustomAndUserDefinedStyles(new Dictionary<string, string>
                {
                    ["margin-top"] = "0",
                    ["margin-bottom"] = "0"
                });
                SetUserDefinedAttributes();

                if (_id != null)
                {
                    if (_notificationsToShow.VorN(_id) == null)
                        _notificationsToShow[_id] = new List<Notification>();
                    _notificationDataToRenderQueue ??= new Dictionary<string, Queue<ComponentsCacheService.NotificationRenderingData>>();
                    if (_notificationDataToRenderQueue.VorN(_id) == null)
                        _notificationDataToRenderQueue[_id] = new Queue<ComponentsCacheService.NotificationRenderingData>();
                    _notificationDataToPostProcessQueue ??= new Dictionary<string, Queue<ComponentsCacheService.NotificationRenderingData>>();
                    if (_notificationDataToPostProcessQueue.VorN(_id) == null)
                        _notificationDataToPostProcessQueue[_id] = new Queue<ComponentsCacheService.NotificationRenderingData>();
                    _previouslyPostProcessedNotificationsRenderingData ??= new Dictionary<string, ComponentsCacheService.NotificationRenderingData>();
                    if (_previouslyPostProcessedNotificationsRenderingData.VorN(_id) == null)
                        _previouslyPostProcessedNotificationsRenderingData[_id] = null;
                }
            }

            if (Notifications.HasChanged() && Notifications.HasValue() && Notifications.ParameterValue.Any())
                SetNotificationsToRender(Notifications.ParameterValue);

            if (NewFor.HasChanged())
                _newFor = NewFor.ParameterValue ?? TimeSpan.FromMinutes(1);;

            if (RemoveAfter.HasChanged())
                _removeAfter = RemoveAfter.ParameterValue ?? TimeSpan.Zero;

            if (Max.HasChanged())
                _max = Max.ParameterValue ?? 0;
            
            //var promptMesage = (ParametersCache.Params.VorN("PromptMessage") as string).NullifyIf(m => m.IsNullOrWhiteSpace());
            //Message.ParameterValue = promptMesage ?? Message.ParameterValue;

            //var promptType = ParametersCache.Params.VorN("PromptType") as NotificationType?;
            //PromptType.ParameterValue = promptType ?? PromptType.ParameterValue ?? NotificationType.Error;
            //RemoveClasses("my-prompt-success", "my-prompt-danger");
            //if (PromptType.ParameterValue == NotificationType.Success)
            //    AddClass("my-prompt-success");
            //else if (PromptType.ParameterValue == NotificationType.Error)
            //    AddClass("my-prompt-danger");

            _syncNotifications.Release();
        }
        
        protected override async Task OnAfterRenderAsync(bool _)
        {
            await _syncNotifications.WaitAsync();

            await (await ModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_Prompt_AfterRenderAsync", _jsPrompt);

            if (_notificationDataToPostProcessQueue[_id].Count == 0)
            {
                _syncNotifications.Release();
                return;
            }

            var nrd = _notificationDataToPostProcessQueue[_id].Dequeue();
            if (_previouslyPostProcessedNotificationsRenderingData.VorN(_id)?.Equals(nrd) == true)
            {
                var shownNotificationsGuids = nrd.NotificationsToShowGuids.Concat(nrd.NotificationsAlreadyShownGuids).ToArray();
                if (shownNotificationsGuids.Any())
                    await (await ModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_Prompt_SetNotificationsDisplayIfDataDidntChangeAsync", _jsPrompt, shownNotificationsGuids);
                _syncNotifications.Release();
                return;
            }

            _previouslyPostProcessedNotificationsRenderingData[_id] = nrd;

            if (nrd.NotificationsToShowGuids.Any() || nrd.NotificationsAlreadyShownGuids.Any() || nrd.NotificationsToHideGuids.Any())
            {
                foreach (var n in nrd.NotificationsToShowFromPage)
                {
                    if (!n.In(nrd.TotalRemovedNotifications))
                    {
                        n.RemoveAfter = _removeAfter; // this triggers the actual con.NotificationDelayElapsed += Notification_DelayElapsed;
                        n.NewFor = _newFor;
                    }
                }

                await (await ModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_Prompt_ShowNotificationsAsync", nrd.NotificationsToShowGuids, nrd.NotificationsAlreadyShownGuids, nrd.NotificationsToHideGuids);

                Logger.For<MyPromptBase>().Info("After Render for:\n" +
                                                $"          `To Show`: [{nrd.NotificationsToShowFromPage.Select(n => n.Type).JoinAsString(", ")}],\n" +
                                                $"          `Already Shown`: [{nrd.NotificationsAlreadyShownFromPage.Select(n => n.Type).JoinAsString(", ")}],\n" +
                                                $"          `To Hide`: [{nrd.NotificationsToHide.Select(n => n.Type).JoinAsString(", ")}],\n" + 
                                                $"          `To Remove`: [{nrd.NewNotificationsToRemove.Select(n => n.Type).JoinAsString(", ")}]");
            }
            
            _syncNotifications.Release();
        }

        private async Task Notification_NewBadgeExpired(Notification sender, EventArgs e, CancellationToken _)
        {
            SetNotificationsToRender();
            await StateHasChangedAsync();
        }

        private async Task Notification_DelayElapsed(Notification sender, NotificationDelayElapsedEventArgs e, CancellationToken _)
        {
            //Logger.For<MyPromptBase>().Info($"Notification_DelayElapsed for {sender}");)
            await RemoveNotificationAsync(sender);
        }

        protected async Task BtnCloseNotification_ClickAsync(MouseEventArgs e, Notification notification)
        {
            await RemoveNotificationAsync(notification);
        }

        protected async Task DivClearVisible_ClickAsync(MouseEventArgs e)
        {
            await RemoveNotificationsAsync((_notificationDataToPostProcessQueue[_id].LastOrDefault() ?? _previouslyPostProcessedNotificationsRenderingData[_id]).AllNotificationsToShowOrAlreadyShown.Take(_max).ToList());
        }

        protected async Task DivClearAll_ClickAsync(MouseEventArgs e)
        {
            await RemoveNotificationsAsync((_notificationDataToPostProcessQueue[_id].LastOrDefault() ?? _previouslyPostProcessedNotificationsRenderingData[_id]).AllNotificationsToShowOrAlreadyShown.ToList());
        }
        
        private async Task RemoveNotificationsAsync(List<Notification> notificationsToRemove)
        {
            if (notificationsToRemove.Any())
            {
                await _syncNotifications.WaitAsync();

                foreach (var notificationToRemove in notificationsToRemove)
                    notificationToRemove.NotificationDelayElapsed -= Notification_DelayElapsed;
                
                SetNotificationsToRender(notificationsToRemove);
                await StateHasChangedAsync(true);

                _syncNotifications.Release();
            }
        }

        private async Task RemoveNotificationAsync(Notification notification) => await RemoveNotificationsAsync(new List<Notification> { notification });

        public async Task ShowNotificationAsync(NotificationType type, IconType icon, string message, bool updatePromptState = true)
        {
            //await _syncNotifications.WaitAsync();

            var notification = new Notification // set remove after directly before the actual showing in after render, not here
            {
                Type = type,
                Icon = icon,
                Message = message
            };
           
            //Notifications.ParameterValue.Add(notification);
            _notificationsToShow[_id].Add(notification);

            if (updatePromptState)
            {
                //Notifications.SetAsChanged();
                SetNotificationsToRender((Notification) null, _notificationsToShow[_id]);
                _notificationsToShow[_id].Clear();
                await StateHasChangedAsync(true);
            }

            //_syncNotifications.Release();
        }

        public Task ShowNotificationAsync(NotificationType type, string message, bool updatePromptState = true) => ShowNotificationAsync(type, null, message, updatePromptState);
        
        private void SetNotificationsToRender(List<Notification> notificationsToRemove, List<Notification> notificationsToAdd)
        {
            notificationsToRemove ??= new List<Notification>();
            notificationsToAdd ??= new List<Notification>();
            
            var previousNrd = _notificationDataToPostProcessQueue[_id].Count > 0 ? _notificationDataToPostProcessQueue[_id].Last() : _previouslyPostProcessedNotificationsRenderingData[_id];
            var previousNrdExists = previousNrd != null;
            if (previousNrdExists)
            {
                notificationsToRemove.RemoveAll(n => n.In(previousNrd.TotalRemovedNotifications));
                notificationsToAdd.RemoveAll(n => n.In(previousNrd.TotalAddedNotifications));
            }
           
            var allNotificationsAfterCurrentChanges = (previousNrdExists ? previousNrd.AllNotificationsAfterCurrentChanges : Array.Empty<Notification>()).Except(notificationsToRemove).Concat(notificationsToAdd).OrderByDescending(n => n.TimeStamp).ToArray();
            var allNotificationsToShow = allNotificationsAfterCurrentChanges.Where(n => n.In(notificationsToAdd) || previousNrd != null && n.In(previousNrd.AllNotificationsToShowOrAlreadyShown.Skip(_max)) && !n.In(notificationsToRemove)).ToArray();
            var allNotificationsAlreadyShown = allNotificationsAfterCurrentChanges.Except(allNotificationsToShow).ToArray();
            var allNotificationsToShowOrAlreadyShown = allNotificationsToShow.Concat(allNotificationsAlreadyShown).OrderByDescending(n => n.TimeStamp).ToArray();
            var notificationsToShowOrAlreadyShownFromPage = allNotificationsToShowOrAlreadyShown.Take(_max).ToArray(); // take max to show and fill with already shown if not enough
            var notificationsToShowOrAlreadyShownNotFromPage = allNotificationsToShowOrAlreadyShown.Skip(_max).ToArray();
            var notificationsToShowFromPage = notificationsToShowOrAlreadyShownFromPage.Where(n => n.In(allNotificationsToShow)).ToArray();
            var notificationsAlreadyShownFromPage = notificationsToShowOrAlreadyShownFromPage.Where(n => n.In(allNotificationsAlreadyShown)).ToArray();
            var notificationsToShowNotFromPage = notificationsToShowOrAlreadyShownNotFromPage.Where(n => n.In(allNotificationsToShow)).ToArray();
            var notificationsAlreadyShownNotFromPage = notificationsToShowOrAlreadyShownNotFromPage.Where(n => n.In(allNotificationsAlreadyShown)).ToArray();
            var notificationsToHide = notificationsToRemove.ConcatMany(notificationsAlreadyShownNotFromPage).Take(_max).OrderByDescending(n => n.TimeStamp).ToArray();
            var totalRemovedNotifications = (previousNrdExists ? previousNrd.TotalRemovedNotifications : Array.Empty<Notification>()).Concat(notificationsToRemove).ToArray();
            var totalAddedNotifications = (previousNrdExists ? previousNrd.TotalAddedNotifications : Array.Empty<Notification>()).Concat(notificationsToAdd).ToArray();

            var notificationsToShowGuids = notificationsToShowFromPage.Select(n => n.Guid).ToArray();
            var notificationsAlreadyShownGuids = notificationsAlreadyShownFromPage.Select(n => n.Guid).ToArray();
            var notificationsToHideGuids = notificationsToHide.Select(n => n.Guid).ToArray();
            
            var notificationsToRender = notificationsToShowFromPage.Concat(notificationsAlreadyShownFromPage).Concat(notificationsToHide).OrderByDescending(n => n.TimeStamp).ToArray();
            
            var nrd = new ComponentsCacheService.NotificationRenderingData
            {
                AllNotificationsAfterCurrentChanges = allNotificationsAfterCurrentChanges,
                AllNotificationsToShowOrAlreadyShown = allNotificationsToShowOrAlreadyShown,
                NotificationsToShowFromPage = notificationsToShowFromPage,
                NotificationsToHide = notificationsToHide,
                NotificationsAlreadyShownFromPage = notificationsAlreadyShownFromPage,
                NotificationsToShowGuids = notificationsToShowGuids,
                NotificationsAlreadyShownGuids = notificationsAlreadyShownGuids,
                NotificationsToHideGuids = notificationsToHideGuids,
                NotificationsToRender = notificationsToRender,
                TotalAddedNotifications = totalAddedNotifications,
                TotalRemovedNotifications = totalRemovedNotifications,
                NewNotificationsToAdd = notificationsToAdd.ToArray(),
                NewNotificationsToRemove = notificationsToRemove.ToArray()
            };

            foreach (var n in nrd.NotificationsToHide.ToArray())
            {
                n.IsRendered = false;
                n.RemoveAfter = TimeSpan.Zero;
                n.NotificationDelayElapsed -= Notification_DelayElapsed;
                n.NewBadgeExpired -= Notification_NewBadgeExpired;
            }

            foreach (var n in nrd.NotificationsToShowFromPage)
            {
                n.IsRendered = true;
                n.NotificationDelayElapsed += Notification_DelayElapsed;
                n.NewBadgeExpired += Notification_NewBadgeExpired;
            }

            Logger.For<MyPromptBase>().Info("SetNotificationsToRender for:\n" +
                                            $"          `To Show`: [{notificationsToShowFromPage.Select(n => n.Type).JoinAsString(", ")}],\n" +
                                            $"          `Already Shown`: [{notificationsAlreadyShownFromPage.Select(n => n.Type).JoinAsString(", ")}],\n" +
                                            $"          `To Hide`: [{notificationsToHide.Select(n => n.Type).JoinAsString(", ")}],\n" + 
                                            $"          `To Remove`: [{notificationsToRemove.Select(n => n.Type).JoinAsString(", ")}]");
            
            _notificationDataToRenderQueue[_id].Enqueue(nrd);
        }

        private void SetNotificationsToRender() => SetNotificationsToRender(new List<Notification>(), new List<Notification>());
        private void SetNotificationsToRender(Notification notificationToRemove, List<Notification> notificationsToAdd) => SetNotificationsToRender(notificationToRemove == null ? new List<Notification>() : new List<Notification> { notificationToRemove }, notificationsToAdd);
        private void SetNotificationsToRender(List<Notification> notificationsToRemove, Notification notificationToAdd = null) => SetNotificationsToRender(notificationsToRemove, notificationToAdd == null ? new List<Notification>() : new List<Notification> { notificationToAdd });
        private void SetNotificationsToRender(Notification notificationToRemove, Notification notificationToAdd = null) => SetNotificationsToRender(notificationToRemove == null ? new List<Notification>() : new List<Notification> { notificationToRemove }, notificationToAdd == null ? new List<Notification>() : new List<Notification> { notificationToAdd });
        
        [JSInvokable]
        public static async Task ShowTestNotificationsAsync()
        {
            var cache = WebUtils.GetService<IComponentsCacheService>();
            var prompt = cache.Components.Values.OfType<MyPromptBase>().Single(p => p._id.EqualsInvariant("promptMain"));

            await prompt.ShowNotificationAsync(NotificationType.Success, $"Test {NotificationType.Success.EnumToString()} Message", false);
            await prompt.ShowNotificationAsync(NotificationType.Warning, $"Test {NotificationType.Warning.EnumToString()} Message", false);
            await prompt.ShowNotificationAsync(NotificationType.Error, $"Test {NotificationType.Error.EnumToString()} Message", false);
            await prompt.ShowNotificationAsync(NotificationType.Primary, $"Test {NotificationType.Primary.EnumToString()} Message", false);
            await prompt.ShowNotificationAsync(NotificationType.Info, $"Test {NotificationType.Info.EnumToString()} Message - asdg sdfg sr srht sth sfth rdsth srth srt hsrth rsth rset hsrt hdf sdfg sr srht sth sfth rdsth srth srt hsrth rsth rset hsrt h sdfg sr srht sth sfth rdsth srth srt hsrth rsth rset hsrt h", false);
            await prompt.ShowNotificationAsync(NotificationType.Loading, $"Test {NotificationType.Loading.EnumToString()} Message");
        }

    }
}
