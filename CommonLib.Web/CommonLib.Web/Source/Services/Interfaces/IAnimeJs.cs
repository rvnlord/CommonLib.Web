using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Models.Interfaces;

namespace CommonLib.Web.Source.Services.Interfaces
{
    public interface IAnimeJs
    {
        List<AnimationJs> Animations { get; }
        dynamic DotNetRef { get; set; }
        Task<T> CreateAsync<T>(T animeJs) where T : Models.Interfaces.IAnimeJs;
    }
}
