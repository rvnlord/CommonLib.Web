using System.Collections.Generic;

namespace CommonLib.Web.Source.Services.Interfaces
{
    public interface IParametersCacheService
    {
        Dictionary<string, object> Params { get; set; }
    }
}
