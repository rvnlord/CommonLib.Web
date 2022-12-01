using System;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Converters;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;

namespace CommonLib.Web.Source.Common.Components.MyCheckBoxComponent
{
    public class MyCheckBoxBase : MyInputBase<bool>
    {
        [Parameter]
        public string Description { get; set; }

        [Parameter]
        public BlazorParameter<bool?> DisplayLabel { get; set; }

        [Parameter]
        public MyAsyncEventHandler<MyCheckBoxBase, ChangeEventArgs> Check { get; set; }

        protected override async Task OnInitializedAsync() => await Task.CompletedTask.ConfigureAwait(false);
        protected override async Task OnAfterFirstRenderAsync() => await Task.CompletedTask.ConfigureAwait(false);

        protected override async Task OnParametersSetAsync()
        {
            if (IsFirstParamSetup())
            {
                SetMainAndUserDefinedClasses("my-checkbox");
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
            }

            Model ??= CascadedEditContext?.ParameterValue?.Model;

            string displayName = null;
            if (For != null && Model != null)
                (_, _propName, Value, displayName) = For.GetModelAndProperty();
           
            Text = Value.ToStringInvariant();

            Description ??= Model.GetPropertyDescriptionOrNull(_propName) ?? _propName.AddSpacesToPascalCase();

            Placeholder = !Placeholder.IsNullOrWhiteSpace()
                ? Placeholder
                : !displayName.IsNullOrWhiteSpace()
                    ? $"{displayName}..."
                    : null;

            if (DisplayLabel.HasChanged())
                DisplayLabel.ParameterValue ??= true;

            if (Validate.HasChanged())
                Validate.ParameterValue ??= true; 
            
            CascadedEditContext.BindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync); 

            await Task.CompletedTask;
        }
        
        protected async Task CheckBox_Checked(ChangeEventArgs e)
        {
            if (e == null)
                throw new NullReferenceException(nameof(e));

            if (Model is not null && _propName is not null)
            {
                Model.SetProperty(_propName, e.Value);
                if (CascadedEditContext?.V is not null)
                    await CascadedEditContext.V.NotifyFieldChangedAsync(new FieldIdentifier(Model, _propName), Validate.V == true);
            }

            Value = e.Value.ToBool();
            Text = Value.ToStringInvariant();

            await Check.InvokeAsync(this, e);
        }
    }
}
