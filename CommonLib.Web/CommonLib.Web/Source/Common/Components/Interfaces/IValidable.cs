using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;

namespace CommonLib.Web.Source.Common.Components.Interfaces
{
    public interface IValidable
    {
        [Parameter]
        public BlazorParameter<bool?> Validate { get; set; }
    }
}
