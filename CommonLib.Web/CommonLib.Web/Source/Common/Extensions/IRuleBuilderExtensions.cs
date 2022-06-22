using CommonLib.Source.Common.Extensions;
using FluentValidation;

namespace CommonLib.Web.Source.Common.Extensions
{
    public static class IRuleBuilderExtensions
    {
        public static string GetPropertyDisplayName<TModel, TProperty>(this IRuleBuilder<TModel, TProperty> rb)
        {
            var rule = rb.GetType().GetProperty("Rule")?.GetValue(rb);
            var member = rule?.GetType().GetProperty("Member")?.GetValue(rule);
            var name = member?.GetType().GetProperty("Name")?.GetValue(member)?.ToString();
            return typeof(TModel).GetPropertyDisplayName(name);
        }
    }
}
