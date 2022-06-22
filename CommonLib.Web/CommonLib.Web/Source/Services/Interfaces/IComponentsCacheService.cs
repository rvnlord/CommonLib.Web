using System;
using System.Collections.Generic;
using CommonLib.Web.Source.Common.Components;

namespace CommonLib.Web.Source.Services.Interfaces
{
    public interface IComponentsCacheService
    {
        Dictionary<Guid, MyComponentBase> Components { get; set; }
        ComponentsCacheService.NotificationsCache NotificationsData { get; set; }
    }
}
