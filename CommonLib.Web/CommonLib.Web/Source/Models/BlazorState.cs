namespace CommonLib.Web.Source.Models
{
    public class BlazorState<T>  
    {
        private T _stateValue;
        private T _previousStateValue;
        private bool _hasChanged;

        public T StateValue
        {
            get => _stateValue;
            set
            {
                _previousStateValue = _stateValue;
                _stateValue = value;
                SetAsChanged();
            }
        }

        public T V => StateValue;

        public T PreviousStateValue => _previousStateValue;

        public BlazorState() { }

        public BlazorState(T stateValue)
        {
            _previousStateValue = default;
            _stateValue = stateValue;
            SetAsChanged();
        }

        public bool HasChanged() => _hasChanged;
        public bool HasValue() => _stateValue != null;
        public bool HasPreviousValue() => _previousStateValue != null;

        public T SetAsUnchanged()
        {
            _hasChanged = false;
            return _stateValue;
        }

        public T SetAsChanged()
        {
            _hasChanged = true;
            return _stateValue;
        }

        public override string ToString()
        {
            return $"C: {(_stateValue?.ToString() ?? "(null)")}, P: {(_previousStateValue?.ToString() ?? "(null)")}, Has Changed? {_hasChanged.ToString().ToLowerInvariant()}";
        }
    }
}
