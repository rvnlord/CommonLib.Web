﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyFileUploadComponent;
using CommonLib.Web.Source.Common.Components.MyIconComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MoreLinq;

namespace CommonLib.Web.Source.Common.Components.MyInputComponent
{
    public abstract class MyInputBase<TProperty> : MyInputBase
    {
        [Parameter]
        public Expression<Func<TProperty>> For { get; set; }
        
        [Parameter]
        public TProperty Value { get; set; }
    }

    public abstract class MyInputBase : MyComponentBase
    {
        protected string _propName { get; set; }
        protected Task<IJSObjectReference> _inputModuleAsync;

        public List<MyButtonBase> InputGroupButtons { get; set; }
        public List<MyIconBase> InputGroupIcons { get; set; }
        public virtual string Text { get; protected set; }

        [Parameter]
        public object Model { get; set; }

        [Parameter]
        public string Placeholder { get; set; }

        [Parameter]
        public string SyncPaddingGroup { get; set; }

        [Parameter]
        public BlazorParameter<IconType> Icon { get; set; }

        [Parameter]
        public BlazorParameter<bool?> Validate { get; set; }

        protected async Task CurrentEditContext_ValidationStateChangedAsync(MyEditContext sender, MyValidationStateChangedEventArgs e, CancellationToken _)
        {
            if (e == null)
                throw new NullReferenceException(nameof(e));
            if (e.ValidationMode == ValidationMode.Property && e.ValidatedFields == null)
                throw new NullReferenceException(nameof(e.ValidatedFields));
            if (Ancestors.Any(a => a is MyInputBase))
                return;
            if (Validate.V != true)
                return;
            if (InteractivityState.V?.IsForced == true)
                return;

            var fi = Model is not null && !_propName.IsNullOrWhiteSpace() ? new FieldIdentifier(Model, _propName) : (FieldIdentifier?)null;
            if (e.ValidationMode == ValidationMode.Model || fi?.In(e.NotValidatedFields) == true || fi?.In(e.ValidatedFields) == true)
                RemoveClasses("my-valid", "my-invalid");

            if (e.ValidationMode == ValidationMode.Property && fi is not null && !((FieldIdentifier)fi).In(e.ValidatedFields)) // do nothing if identifier is not propName (if validation is triggered for another field, go ahead if it is propName or if it is null which means we are validating model so there is only one validation changed for all props)
            {
                await NotifyParametersChangedAsync().StateHasChangedAsync(true);
                return;
            }
            
            if (CascadedEditContext is null || e.ValidationMode == ValidationMode.Model && e.ValidationStatus.In(ValidationStatus.Pending, ValidationStatus.Success))
            {
                await SetControlStateAsync(ComponentState.Disabled, this);
                return;
            }

            if (e.ValidationMode == ValidationMode.Model && e.ValidationStatus == ValidationStatus.Failure)
            {
                await SetControlStateAsync(ComponentState.Enabled, this);
                //if (this is MyFileUploadBase fileUpload)
                //{
                //    var btnsForManyFiles = fileUpload.Children?.OfType<MyButtonBase>().Where(b => b.Model?.V is null).ToArray();
                //    btnsForManyFiles?.ForEach(b =>
                //    {
                //        b.State.SetAsChanged();
                //        b._prevParentState = ButtonState.Enabled;
                //    });
                //    await fileUpload.SetMultipleFileBtnsStateAsync(null);
                //}
            }

            if (fi is null)
                return;

            var wasCurrentFieldValidated = _propName.In(e.ValidatedFields.Select(f => f.FieldName));
            var isCurrentFieldValid = !_propName.In(e.InvalidFields.Select(f => f.FieldName));
            var wasValidationSuccessful = e.ValidationStatus == ValidationStatus.Success;
            var validationFailed = e.ValidationStatus == ValidationStatus.Failure;

            if ((wasValidationSuccessful || isCurrentFieldValid) && wasCurrentFieldValidated)
                AddClasses("my-valid");
            else if (validationFailed && wasCurrentFieldValidated)
                AddClasses("my-invalid");

            await NotifyParametersChangedAsync().StateHasChangedAsync(true);
        }
    }

    //public class InputState : IEquatable<InputState>
    //{
    //    public bool IsForced { get; set; }
    //    public InputStateKind? State { get; set; }

    //    public bool IsDisabledOrForceDisabled => State == InputStateKind.Disabled;
    //    public bool IsEnabledOrForceEnabled => State == InputStateKind.Enabled;

    //    public static InputState Disabled => new(InputStateKind.Disabled);
    //    public static InputState Enabled => new(InputStateKind.Enabled);
    //    public static InputState ForceDisabled => new(InputStateKind.Disabled, true);
    //    public static InputState ForceEnabled => new(InputStateKind.Enabled, true);

    //    public InputState(InputStateKind? state, bool isForced = false)
    //    {
    //        State = state;
    //        IsForced = isForced;
    //    }

    //    public bool Equals(InputState other)
    //    {
    //        if (ReferenceEquals(null, other)) return false;
    //        if (ReferenceEquals(this, other)) return true;
    //        return IsForced == other.IsForced && State == other.State;
    //    }

    //    public override bool Equals(object obj)
    //    {
    //        if (ReferenceEquals(null, obj)) return false;
    //        if (ReferenceEquals(this, obj)) return true;
    //        return obj.GetType() == GetType() && Equals((InputState)obj);
    //    }

    //    public override int GetHashCode() => HashCode.Combine(IsForced, State);
    //    public static bool operator ==(InputState left, InputState right) => Equals(left, right);
    //    public static bool operator !=(InputState left, InputState right) => !Equals(left, right);
    //}

    //public enum InputStateKind
    //{
    //    Enabled,
    //    Disabled
    //}
}
