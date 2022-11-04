using Microsoft.AspNetCore.Components;

namespace CommonLib.Web.Source.Models
{
    public abstract class BlazorParameter
    {
        public static BlazorParameter<TParameter> BP<TParameter>(TParameter parameterValue) => new(parameterValue);
    }

    public class BlazorParameter<T> : BlazorParameter   
    {
        private T _parameterValue;
        private T _previousParameterValue;
        private bool _hasChanged;

        public T ParameterValue
        {
            get => _parameterValue;
            set
            {
                _previousParameterValue = _parameterValue;
                _parameterValue = value;
                SetAsChanged();
            }
        }

        public T V => ParameterValue;

        public T PreviousParameterValue => _previousParameterValue;

        public BlazorParameter(T parameterValue)
        {
            _previousParameterValue = default;
            _parameterValue = parameterValue;
            SetAsChanged();
        }

        public bool HasChanged() => _hasChanged;
        public bool HasValue() => _parameterValue != null;
        public bool HasPreviousValue() => _previousParameterValue != null;

        public T SetAsUnchanged()
        {
            _hasChanged = false;
            return _parameterValue;
        }

        public T SetAsChanged()
        {
            _hasChanged = true;
            return _parameterValue;
        }

        public static implicit operator BlazorParameter<T>(T parameterValue) => new(parameterValue);
    }
}
