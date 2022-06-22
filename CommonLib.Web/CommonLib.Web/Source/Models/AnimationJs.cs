using System;
using System.Threading.Tasks;
using CommonLib.Web.Source.Models.Interfaces;
using CommonLib.Source.Common.Utils.UtilClasses;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Models
{
    public class AnimationJs : IAnimeJs
    {
        public bool CompleteCallbackFinished { get; set; }
        public Guid Guid { get; set; }
        public IJSRuntime JsRuntime { get; set; }
        public TimeSpan Duration { get; set; }

        public JQueryCollection Targets { get; } = new JQueryCollection(null);
        public EasingType Easing { get; set; }
        public bool Autoplay { get; set; }
        public Func<Guid, Task> BeginAsync { get; set; }
        public Func<Guid, Task> CompleteAsync { get; set; }

        public DoubleRange Opacity { get; set; }
        public StringRange ClipPath { get; set; }
        public StringRange Height { get; set; }

        public async Task<bool> BeganAsync()
        {
            return await JsRuntime.InvokeAsync<bool>("BlazorAnimeJsUtils.Began", Guid).ConfigureAwait(false);
        }

        public async Task SeekAsync(TimeSpan time)
        {
            await JsRuntime.InvokeVoidAsync("BlazorAnimeJsUtils.Seek", Guid, time).ConfigureAwait(false);
        }
    }

    public enum EasingType
    {
        EaseOutExpo,
        EaseOutCirc,
        EaseInOutSine
    }
}
