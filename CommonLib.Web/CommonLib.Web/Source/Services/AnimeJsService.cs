using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Models.Interfaces;
using CommonLib.Web.Source.Services.Interfaces;
using CommonLib.Source.Common.Converters;
using Microsoft.JSInterop;
using Newtonsoft.Json.Linq;

namespace CommonLib.Web.Source.Services
{
    public class AnimeJsService : IAnimeJsService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly IJQueryService _jqueryService;

        public dynamic DotNetRef { get; set; }
        public List<AnimationJs> Animations { get; }

        public AnimeJsService(IJSRuntime jsRuntime, IJQueryService jqueryService)
        {
            _jsRuntime = jsRuntime;
            _jqueryService = jqueryService;
            Animations = new List<AnimationJs>();
        }

        public async Task<T> CreateAsync<T>(T animeJs) where T : Models.Interfaces.IAnimeJs
        {
            animeJs = animeJs ?? throw new NullReferenceException(nameof(animeJs));
            animeJs.Guid = Guid.NewGuid();
            animeJs.JsRuntime = _jsRuntime;

            var jAnim = new JObject
            {
                ["guid"] = animeJs.Guid.ToStringInvariant(),
                ["duration"] = animeJs.Duration.TotalMilliseconds.ToInt()
            };

            if (animeJs is AnimationJs anim)
            {
                anim.Targets.JsRuntime = _jsRuntime;
                anim.Targets.IjQueryServiceService = _jqueryService;

                jAnim["type"] = "animation";

                jAnim["targetsSelector"] = anim.Targets.GetSelector();
                jAnim["easing"] = anim.Easing.EnumToString().PascalCaseToCamelCase();
                jAnim["autoplay"] = anim.Autoplay;
                jAnim["beginMethodName"] = anim.BeginAsync?.Method.Name;
                jAnim["completeMethodName"] = anim.CompleteAsync?.Method.Name;

                if (anim.Opacity != null)
                    jAnim["opacity"] = new JArray(anim.Opacity.From, anim.Opacity.To);
                if (anim.ClipPath != null)
                    jAnim["clipPath"] = new JArray(anim.ClipPath.From, anim.ClipPath.To);
                if (anim.Height != null)
                    jAnim["height"] = new JArray(anim.Height.From, anim.Height.To);

                Animations.Add(anim);
            }
            else if (animeJs is TimelineJs timeline)
            {
                jAnim["type"] = "timeline";
                var jAnims = new JArray();

                foreach (var tlAnim in timeline.Animations)
                {
                    tlAnim.Guid = Guid.NewGuid();
                    tlAnim.JsRuntime = _jsRuntime;
                    tlAnim.Targets.JsRuntime = _jsRuntime;
                    tlAnim.Targets.IjQueryServiceService = _jqueryService;

                    var jTlAnim = new JObject
                    {
                        ["guid"] = tlAnim.Guid.ToStringInvariant(),
                        ["duration"] = tlAnim.Duration.TotalMilliseconds.ToInt(),

                        ["targetsSelector"] = tlAnim.Targets.GetSelector(),
                        ["easing"] = tlAnim.Easing.EnumToString().PascalCaseToCamelCase(),
                        ["autoplay"] = tlAnim.Autoplay,
                        ["beginMethodName"] = tlAnim.BeginAsync?.Method.Name,
                        ["completeMethodName"] = tlAnim.CompleteAsync?.Method.Name
                    };

                    if (tlAnim.Opacity != null)
                        jTlAnim["opacity"] = new JArray(tlAnim.Opacity.From, tlAnim.Opacity.To);
                    if (tlAnim.ClipPath != null)
                        jTlAnim["clipPath"] = new JArray(tlAnim.ClipPath.From, tlAnim.ClipPath.To);
                    if (tlAnim.Height != null)
                        jTlAnim["height"] = new JArray(tlAnim.Height.From, tlAnim.Height.To);

                    jAnims.Add(jTlAnim);
                    Animations.Add(tlAnim);
                }

                jAnim["animations"] = jAnims;
            }

            await JSRuntimeExtensions.InvokeVoidAsync(_jsRuntime, "BlazorAnimeJsUtils.CreateAnimation", jAnim.ToString(), DotNetRef);
            return animeJs;
        }
    }
}
