using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Converters;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Utils.TypeUtils;
using CommonLib.Source.Common.Utils.UtilClasses;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace CommonLib.Web.Source.Common.Components.MyDropDownComponent
{
    public class MyDropDownBase<TProperty> : MyComponentBase
    {
        protected Guid? _ddlGuid { get; set; }
        protected MyEditContext _previousEditContext { get; set; }
        protected string _propName { get; set; }
        protected string _validationClass { get; set; }
        protected List<DdlItem> _ddlItems { get; set; }

        [Parameter] 
        public Expression<Func<TProperty>> For { get; set; }

        [CascadingParameter(Name = "Model")] public object Model { get; set; }

        [Parameter]
        public IEnumerable<TProperty> PossibleValues { get; set; }

        [Parameter]
        public Func<TProperty, DdlItem> PossibleValuesConverter { get; set; }

        [Parameter] 
        public IconType Icon { get; set; }

        [Parameter]
        public DdlItem SelectedItem { get; set; }

        [Parameter] 
        public DdlItem EmptyItem { get; set; }

        [Parameter]
        public List<DdlItem> Items { get; set; } = new();

        protected override async Task OnParametersSetAsync()
        {
            _ddlGuid ??= Guid.NewGuid();
            Model ??= CascadedEditContext.ParameterValue.Model;
            CascadedEditContext.ParameterValue ??= new MyEditContext(Model);

            if (For == null || Model == null)
                return;

            var (_, propName, propValue, displayName) = For.GetModelAndProperty();
            _propName = propName;

            var i = 1;
            _ddlItems = Items.Concat(PossibleValues?.Select(PossibleValuesConverter ?? (p => new DdlItem(i++, p.ToString())))
                 ?? typeof(TProperty).EnumToDdlItems()
                 ?? Enumerable.Empty<DdlItem>()).ToList();

            if (EmptyItem == null)
                EmptyItem = new DdlItem(null, $"(Select {displayName})");

            if (EnumUtils.IsEnum<TProperty>())
                SelectedItem = _ddlItems.SingleOrDefault(item => item.Index == propValue?.ToIntN());
            else
                SelectedItem = _ddlItems.SingleOrDefault(item => item.Text == (PossibleValuesConverter != null ? PossibleValuesConverter(propValue).Text : propValue?.ToString()));

            if (SelectedItem == null)
                SelectedItem = EmptyItem;

            CascadedEditContext.BindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync);

            await Task.CompletedTask;
        }

        protected override async Task OnInitializedAsync() => await Task.CompletedTask;

        protected override async Task OnAfterFirstRenderAsync()
        {
            await ModuleAsync; // there is no need to call any function on after render but the module needs to be initally loaded
        }

        public async Task DdlOption_ClickAsync(MouseEventArgs e, int? index, Guid? ddlGuid)
        {
            await JsRuntime.InvokeVoidAsync("blazorDdlOptionOnClick", e, index, ddlGuid).ConfigureAwait(false);
            var selectedItemWithMappedindex = _ddlItems.SingleOrDefault(i => i.Index == index);
            SelectedItem = selectedItemWithMappedindex ?? EmptyItem;
            if (Model != null && For != null)
            {
                var (_, propName, oldValue, _) = For.GetModelAndProperty();
                var possibleVals = (PossibleValues ?? EnumUtils.GetValuesOrNull<TProperty>() ?? Enumerable.Empty<TProperty>()).ToArray();

                TProperty newValue;
                if (index == null || index == EmptyItem.Index)
                    newValue = default;
                else if (EnumUtils.IsEnum<TProperty>())
                    newValue = possibleVals.FirstOrDefault(en => en.ToInt() == index);
                else
                {
                    var ddlItemWithIndex = _ddlItems.Single(di => di.Index == index);
                    newValue = possibleVals.SingleOrDefault(item => (PossibleValuesConverter != null
                        ? PossibleValuesConverter(item).Text
                        : item.ToString()) == ddlItemWithIndex.Text);
                }

                if (!Equals(oldValue, newValue))
                {
                    For.SetPropertyValue(newValue);
                    CascadedEditContext.ParameterValue.NotifyFieldChanged(new FieldIdentifier(Model, propName));
                }
            }
        }

        private async Task CurrentEditContext_ValidationStateChangedAsync(object sender, MyValidationStateChangedEventArgs e)
        {
            if (e == null)
                throw new NullReferenceException(nameof(e));
            if (e.ValidationMode == ValidationMode.Property && e.ValidatedFields == null)
                throw new NullReferenceException(nameof(e.ValidatedFields));
            if (!e.ValidatedFields.Any(f => f.FieldName.EqualsInvariant(_propName))) // do nothing if identifier is is not propName (if validation is triggered for another field, go ahead if it is propName or if it is null which means we are validating model so there is only one validation changed for all props)
                return;

            _validationClass = null;

            if (CascadedEditContext == null || e.ValidationStatus == ValidationStatus.Pending)
            {
                await StateHasChangedAsync().ConfigureAwait(false);
                return;
            }
            
            var wasCurrentFieldValidated = _propName.In(e.ValidatedFields.Select(f => f.FieldName));
            var isCurrentFieldValid = !_propName.In(e.InvalidFields.Select(fi => fi.FieldName));
            var wasValidationSuccessful = e.ValidationStatus == ValidationStatus.Success;
            var validationFailed = e.ValidationStatus == ValidationStatus.Failure;

            if ((wasValidationSuccessful || isCurrentFieldValid) && wasCurrentFieldValidated)
                AddClasses("my-dropdown-valid");
            else if (validationFailed && wasCurrentFieldValidated)
                AddClasses("my-dropdown-invalid");
            
            await StateHasChangedAsync().ConfigureAwait(false);
        }
    }
}