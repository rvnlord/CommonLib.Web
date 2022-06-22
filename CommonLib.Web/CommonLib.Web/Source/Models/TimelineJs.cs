using System;
using System.Collections.Generic;
using System.Linq;
using CommonLib.Web.Source.Models.Interfaces;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Models
{
    public class TimelineJs : IAnimeJs
    {
        public bool CompleteCallbackFinished
        {
            get => Animations.All(c => c.CompleteCallbackFinished);
            set => throw new NotImplementedException();
        }

        public Guid Guid { get; set; }
        public IJSRuntime JsRuntime { get; set; }
        public TimeSpan Duration { get; set; }

        public List<AnimationJs> Animations { get; } = new List<AnimationJs>();
    }
}
