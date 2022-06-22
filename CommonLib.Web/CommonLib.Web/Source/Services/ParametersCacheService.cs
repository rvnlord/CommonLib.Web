using System.Collections.Generic;
using CommonLib.Web.Source.Services.Interfaces;

namespace CommonLib.Web.Source.Services
{
    public class ParametersCacheService : IParametersCacheService
    {
        public Dictionary<string, object> Params { get; set; } = new();
    }
}
