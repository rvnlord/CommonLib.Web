using System;
using CommonLib.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Utils.UtilClasses;

namespace CommonLib.Web.Source.Common.Converters
{
    public static class ComponentStateConverter
    {
        public static ComponentState ToComponentState(this InputState inputState) => (ComponentState) inputState;
        public static ComponentState ToComponentState(this ButtonState buttonState) => (ComponentState) buttonState;
        public static ComponentState ToComponentStateOrNull(this object state)
        {
            if (state is ComponentState componentState)
                return componentState;
            if (state is InputState inputState)
                return inputState.ToComponentState();
            if (state is ButtonState buttonState)
                return buttonState.ToComponentState();
            return null;
        }

        public static ComponentState ToComponentStateOrEmpty(this object state) => state.ToComponentStateOrNull() ?? ComponentState.Empty;
        public static ComponentState ToComponentState(this object state) => state.ToComponentStateOrNull() ?? throw new NullReferenceException("Component State can't be null");

        public static ButtonState? ToButtonState(this ComponentState state)
        {
            if (state?.State is null) 
                return null;
            if (state.State == ComponentStateKind.Enabled)
                return ButtonState.Enabled;
            if (state.State == ComponentStateKind.Disabled)
                return ButtonState.Disabled;
            if (state.State == ComponentStateKind.Loading)
                return ButtonState.Loading;
            throw new ArgumentOutOfRangeException(null, "Conversion not supported");
        }

        public static InputState ToinputState(this ComponentState state)
        {
            InputState inputState = new InputState(null);
            if (state?.State is null) 
                return inputState;
            if (state.State == ComponentStateKind.Enabled)
                inputState.State = InputStateKind.Enabled;
            if (state.State.In(ComponentStateKind.Disabled, ComponentStateKind.Loading))
                inputState.State = InputStateKind.Disabled;
            inputState.IsForced = state.IsForced;
            return inputState;
        }
    }
}
