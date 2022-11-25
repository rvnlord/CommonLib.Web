using System;
using CommonLib.Source.Common.Converters;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;

namespace CommonLib.Web.Source.Common.Utils.UtilClasses
{
    public class ComponentState : IEquatable<ComponentState>
    {
        public bool IsForced { get; set; }
        public ComponentStateKind? State { get; set; }

        public bool IsDisabledOrForceDisabled => State == ComponentStateKind.Disabled;
        public bool IsEnabledOrForceEnabled => State == ComponentStateKind.Enabled;
        public bool IsLoadingOrForceLoading => State == ComponentStateKind.Loading;
        public bool IsDisabledButNotForced => State == ComponentStateKind.Disabled && !IsForced;
        public bool IsEnabledButNotForced => State == ComponentStateKind.Enabled && !IsForced;
        public bool IsLoadingButNotForced => State == ComponentStateKind.Loading && !IsForced;
        public bool IsForceDisabled => State == ComponentStateKind.Disabled && IsForced;
        public bool IsForceEnabled => State == ComponentStateKind.Enabled && IsForced;
        public bool IsForceLoading => State == ComponentStateKind.Loading && IsForced;

        public static ComponentState Disabled => new(ComponentStateKind.Disabled);
        public static ComponentState Enabled => new(ComponentStateKind.Enabled);
        public static ComponentState Loading => new(ComponentStateKind.Loading);
        public static ComponentState ForceDisabled => new(ComponentStateKind.Disabled, true);
        public static ComponentState ForceEnabled => new(ComponentStateKind.Enabled, true);
        public static ComponentState ForceLoading => new(ComponentStateKind.Loading, true);
        public static ComponentState Empty => new(null);

        public ComponentState(ComponentStateKind? state, bool isForced = false)
        {
            State = state;
            IsForced = isForced;
        }
        
        public bool Equals(ComponentState other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return IsForced == other.IsForced && State == other.State;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((ComponentState)obj);
        }

        public override int GetHashCode() => HashCode.Combine(IsForced, State);
        public static bool operator ==(ComponentState left, ComponentState right) => Equals(left, right);
        public static bool operator !=(ComponentState left, ComponentState right) => !Equals(left, right);

        //public static explicit operator ComponentState(InputState state)
        //{
        //    if (state is null) 
        //        return new ComponentState(null);
        //    if (state.State == InputStateKind.Enabled && state.IsForced)
        //        return ForceEnabled;
        //    if (state.State == InputStateKind.Disabled && state.IsForced)
        //        return ForceDisabled;
        //    if (state.State == InputStateKind.Enabled && !state.IsForced)
        //        return Enabled;
        //    if (state.State == InputStateKind.Disabled && !state.IsForced)
        //        return Disabled;
        //    throw new ArgumentOutOfRangeException(null, "Conversion not supported");
        //}

        //public static explicit operator ComponentState(ButtonState? state)
        //{
        //    if (state is null) 
        //        return new ComponentState(null);
        //    if (state == ButtonState.Enabled)
        //        return Enabled;
        //    if (state == ButtonState.Disabled)
        //        return Disabled;
        //    if (state == ButtonState.Loading)
        //        return Loading;
        //    throw new ArgumentOutOfRangeException(null, "Conversion not supported");
        //}

        public override string ToString() => $"{(State?.EnumToString() ?? "< no state >")}, {(IsForced ? "forced" : "not forced")}";
    }

    public enum ComponentStateKind
    {
        Enabled,
        Disabled,
        Loading
    }
}
