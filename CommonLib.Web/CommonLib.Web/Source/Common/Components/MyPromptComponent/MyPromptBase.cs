using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blazored.SessionStorage;
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
using Microsoft.Extensions.DependencyInjection;

namespace CommonLib.Web.Source.Common.Components.MyPromptComponent
{
    public class MyPromptBase : MyComponentBase
    {
        private TimeSpan _newFor;
        private TimeSpan _removeAfter;
        private int _max;
        
        [Parameter]
        public BlazorParameter<TimeSpan?> NewFor { get; set; }

        [Parameter]
        public BlazorParameter<TimeSpan?> RemoveAfter { get; set; }

        [Parameter]
        public BlazorParameter<int?> Max { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (IsFirstParamSetup())
            {
                SetMainAndUserDefinedClasses("my-prompt");
                SetCustomAndUserDefinedStyles(new Dictionary<string, string>
                {
                    ["margin-top"] = "0",
                    ["margin-bottom"] = "0"
                });
                SetUserDefinedAttributes();
            }
            
            if (NewFor.HasChanged())
                _newFor = NewFor.ParameterValue ?? TimeSpan.FromMinutes(1); ;

            if (RemoveAfter.HasChanged())
                _removeAfter = RemoveAfter.ParameterValue ?? TimeSpan.Zero;

            if (Max.HasChanged())
                _max = Max.ParameterValue ?? 0;

            await Task.CompletedTask;
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            await (await ModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_Prompt_AfterFirstRenderAsync", _guid, _id, _newFor.TotalSeconds, _removeAfter.TotalSeconds, _max, _renderClasses, _renderStyle, _renderAttributes);
        }

        public async Task AddNotificationAsync(NotificationType type, IconType icon, string message)
        {
            await (await ModuleAsync).InvokeVoidAndCatchCancellationAsync("blazor_Prompt_AddNotificationAsync", _id, type.EnumToString(), icon?.GetIconSetName.ToLowerInvariant(), icon?.ToString().PascalCaseToKebabCase(), message, _newFor.TotalSeconds, _removeAfter.TotalSeconds, _max);
        }

        public Task AddNotificationAsync(NotificationType type, string message) => AddNotificationAsync(type, null, message);

    }
}
