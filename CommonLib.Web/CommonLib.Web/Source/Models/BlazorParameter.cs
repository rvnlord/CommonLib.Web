using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components;
using Microsoft.AspNetCore.Components;

namespace CommonLib.Web.Source.Models
{
    public abstract class BlazorParameter
    {
        public static BlazorParameter<Expression<Func<TValue>>> BP<TValue>(Expression<Func<TValue>> parameterValue) => new(parameterValue);
        public static BlazorParameter<TParameter> BP<TParameter>(TParameter parameterValue) => new(parameterValue);
        public static BlazorParameter<MyAsyncEventHandler<TSender, TEventArgs>> BP<TSender, TEventArgs>(MyAsyncEventHandler<TSender, TEventArgs> parameterValue) where TSender : MyComponentBase where TEventArgs : EventArgs => new(parameterValue);
        //public static BlazorParameter<MyAsyncEventHandler<TSender, TEventArgs>> BP<TSender, TEventArgs>(Expression<Func<TSender, TEventArgs, CancellationToken, Task>> parameterValue) where TSender : MyComponentBase where TEventArgs : EventArgs => new(new MyAsyncEventHandler<TSender, TEventArgs>(parameterValue.Compile()));
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

        public static implicit operator BlazorParameter<T>(T parameterValue)
        {
            return new(parameterValue);
        }

        public static implicit operator BlazorParameter<object>(BlazorParameter<T> parameterValue)
        {
            return new(parameterValue.V);
        }
    }
}
