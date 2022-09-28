using System.Linq;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Web.Source.Models;
using CommonLib.Source.Common.Utils.TypeUtils;
using Microsoft.AspNetCore.Components;

namespace CommonLib.Web.Source.Common.Components.MyBadgeComponent
{
    public class MyBadgeBase : MyComponentBase
    {
        [Parameter]
        public BlazorParameter<BadgeKind?> Badge { get; set; }
        
        protected override async Task OnInitializedAsync()
        {
            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                SetMainAndUserDefinedClasses("my-badge");
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
            }

            if (Badge.HasChanged())
            {
                Badge.ParameterValue ??= BadgeKind.Primary;
                RemoveClasses(EnumUtils.GetValues<BadgeKind>().Select(b => b.EnumToString().PascalCaseToKebabCase()).ToArray());
                AddClass(Badge.V.EnumToString().PascalCaseToKebabCase());
            }
            
            await Task.CompletedTask;
        }
    }

    public enum BadgeKind
    {
        Success,
        Error,
        Warning,
        Info,
        Primary
    }
}
