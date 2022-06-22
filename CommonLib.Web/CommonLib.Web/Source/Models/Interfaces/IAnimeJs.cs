using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Models.Interfaces
{
    public interface IAnimeJs
    {
        bool CompleteCallbackFinished { get; set; }
        public Guid Guid { get; set; }
        public IJSRuntime JsRuntime { get; set; }
        public TimeSpan Duration { get; set; }

        public async Task PlayAsync()
        {
            await JsRuntime.InvokeVoidAsync("BlazorAnimeJsUtils.Play", Guid).ConfigureAwait(false);
        }
    }
}
