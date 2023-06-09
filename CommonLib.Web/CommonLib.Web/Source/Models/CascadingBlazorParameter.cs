using System.Collections.Generic;
using System.Linq;
using CommonLib.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Components;

namespace CommonLib.Web.Source.Models
{
    public class CascadingBlazorParameter<T>
    {
        private T _parameterValue;
        private T _previousParameterValue;
        private readonly Dictionary<string, bool> _hasChanged = new();

        public T V => _parameterValue;
        public T PreviousParameterValue => _previousParameterValue;

        public CascadingBlazorParameter() { }

        public CascadingBlazorParameter(T parameterValue)
        {
            _previousParameterValue = default;
            _parameterValue = parameterValue;
        }

        public T SetParameterValueFor(MyComponentBase c, T value)
        {
            _previousParameterValue = _parameterValue;
            _parameterValue = value;
            SetAsChangedFor(c);
            return value;
        }
        
        public bool HasChangedFor(MyComponentBase c)
        {
            var isStateInDict = _hasChanged.TryGetValue(c.ToTypeAndShortGuidString(), out var hasChanged);
            if (!isStateInDict)
                _hasChanged[c.ToTypeAndShortGuidString()] = true;
            var ancestorsIdentifiers = c.Ancestors.Select(a => a.ToTypeAndShortGuidString()).ToArray();
            var ancestorsHasChanged = _hasChanged.Where(hc => hc.Key.In(ancestorsIdentifiers));
            var hasChangedForAnyAncestor = ancestorsHasChanged.Any(a => a.Value);
            return !isStateInDict || hasChanged || hasChangedForAnyAncestor;
        }

        public bool HasValue() => _parameterValue is not null;
        public bool HasPreviousValue() => _previousParameterValue != null;

        public T SetAsUnchangedFor(MyComponentBase c)
        {
            _hasChanged[c.ToTypeAndShortGuidString()] = false;
            return _parameterValue;
        }

        public T SetAsChangedFor(MyComponentBase c)
        {
            _hasChanged[c.ToTypeAndShortGuidString()] = true;
            return _parameterValue;
        }

        public static implicit operator CascadingBlazorParameter<T>(T parameterValue)
        {
            return new(parameterValue);
        }

        public static implicit operator CascadingBlazorParameter<object>(CascadingBlazorParameter<T> parameterValue)
        {
            return new(parameterValue.V);
        }

        public override string ToString()
        {
            return $"C: {(_parameterValue?.ToString() ?? "(null)")}, P: {(_previousParameterValue?.ToString() ?? "(null)")}, Has Changed for any? {_hasChanged.Any(hc => hc.Value)} (*including descendants)";
        }
    }
}
