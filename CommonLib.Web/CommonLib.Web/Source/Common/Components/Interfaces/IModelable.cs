using System;
using System.Linq.Expressions;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;

namespace CommonLib.Web.Source.Common.Components.Interfaces
{
    public interface IModelable<TProperty>
    {
        [Parameter]
        BlazorParameter<object> Model { get; set; }
        
        [Parameter]
        BlazorParameter<Expression<Func<TProperty>>> For { get; set; }
        
        [Parameter]
        BlazorParameter<TProperty> Value { get; set; }
    }
}
