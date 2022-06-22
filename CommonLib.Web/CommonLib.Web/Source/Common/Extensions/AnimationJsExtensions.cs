using System;
using System.Threading.Tasks;
using CommonLib.Web.Source.Models;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class AnimationJsExtensions
    {
        public static async Task<bool> BeganAsync(this Task<AnimationJs> anim)
        {
            return await (await (anim ?? throw new NullReferenceException(nameof(anim))).ConfigureAwait(false)).BeganAsync().ConfigureAwait(false);
        }

        public static async Task SeekAsync(this Task<AnimationJs> anim, TimeSpan time)
        {
            await (await (anim ?? throw new NullReferenceException(nameof(anim))).ConfigureAwait(false)).SeekAsync(time).ConfigureAwait(false);
        }
    }
}
