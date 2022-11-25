using System;
using CommonLib.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Utils.UtilClasses;

namespace CommonLib.Web.Source.Common.Converters
{
    public static class ComponentStateConverter
    {
        //public static ComponentState ToComponentState(this InputState inputState) => (ComponentState) inputState;
        //public static ComponentState ToComponentState(this ButtonState buttonState) => (ComponentState) buttonState;
        //public static ComponentState ToComponentStateOrNull(this object state)
        //{
        //    if (state is ComponentState componentState)
        //        return componentState;
        //    if (state is InputState inputState)
        //        return inputState.ToComponentState();
        //    if (state is ButtonState buttonState)
        //        return buttonState.ToComponentState();
        //    return null;
        //}

        //public static ComponentState ToComponentStateOrEmpty(this object state) => state.ToComponentStateOrNull() ?? ComponentState.Empty;
        //public static ComponentState ToComponentState(this object state) => state.ToComponentStateOrNull() ?? throw new NullReferenceException("Component State can't be null");

        //public static ButtonState? ToButtonState(this ComponentState componentState)
        //{
        //    if (componentState?.State is null) 
        //        return null;
        //    if (componentState.State == ComponentStateKind.Enabled)
        //        return ButtonState.Enabled;
        //    if (componentState.State == ComponentStateKind.Disabled)
        //        return ButtonState.Disabled;
        //    if (componentState.State == ComponentStateKind.Loading)
        //        return ButtonState.Loading;
        //    throw new ArgumentOutOfRangeException(null, "Conversion not supported");
        //}

        //public static InputState ToinputState(this ComponentState componentState)
        //{
        //    InputState inputState = new InputState(null);
        //    if (componentState?.State is null) 
        //        return inputState;
        //    if (componentState.State == ComponentStateKind.Enabled)
        //        inputState.State = InputStateKind.Enabled;
        //    if (componentState.State.In(ComponentStateKind.Disabled, ComponentStateKind.Loading))
        //        inputState.State = InputStateKind.Disabled;
        //    inputState.IsForced = componentState.IsForced;
        //    return inputState;
        //}
    }
}
