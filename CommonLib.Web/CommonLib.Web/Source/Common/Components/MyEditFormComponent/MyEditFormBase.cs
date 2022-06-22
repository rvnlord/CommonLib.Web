using System;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Components.MyEditContextComponent;
using CommonLib.Web.Source.Models;
using Microsoft.AspNetCore.Components;

namespace CommonLib.Web.Source.Common.Components.MyEditFormComponent
{
    public class MyEditFormBase : MyComponentBase
    {
        private bool _submitting;

        protected MyEditContext _fixedEditContext;
        protected Func<Task> _handleSubmitDelegate; // Cache to avoid per-render allocations

        protected BlazorParameter<MyEditContext> _fixedEditContextBp;

        [Parameter]
        public BlazorParameter<MyEditContext> EditContext { get; set; }

        [Parameter] 
        public object Model { get; set; }
        
        [Parameter] 
        public EventCallback<MyEditContext> OnSubmit { get; set; }

        [Parameter] 
        public EventCallback<MyEditContext> OnValidSubmit { get; set; }

        [Parameter] 
        public EventCallback<MyEditContext> OnInvalidSubmit { get; set; }

        protected override async Task OnInitializedAsync()
        {
            //await OnParametersSetAsync();
            //if (_fixedEditContextBp == null)
            //    _fixedEditContextBp = new BlazorParameter<MyEditContext>(_fixedEditContext);
            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            _handleSubmitDelegate = SubmitAsync;

            if (IsFirstParamSetup())
            {
                SetMainAndUserDefinedClasses("my-editform");
                SetUserDefinedStyles();
                SetUserDefinedAttributes();
            }

            if (Model == null && !EditContext.HasValue() && !CascadedEditContext.HasValue() // no, no, no
                || Model == null && EditContext.HasValue() && CascadedEditContext.HasValue() // no, yes, yes
                || Model != null && EditContext.HasValue() && !CascadedEditContext.HasValue() // yes, yes, no
                || Model != null && EditContext.HasValue() && CascadedEditContext.HasValue() // yes, yes, yes
                || Model != null && !EditContext.HasValue() && CascadedEditContext.HasValue()) // yes, no, yes
                throw new InvalidOperationException($"{nameof(MyEditFormBase)} requires a {nameof(Model)} parameter, or {nameof(EditContext)} or {nameof(CascadedEditContext)} parameter, but not all.");

            if (OnSubmit.HasDelegate && (OnValidSubmit.HasDelegate || OnInvalidSubmit.HasDelegate))
                throw new InvalidOperationException($"When supplying an {nameof(OnSubmit)} parameter to {nameof(MyEditFormBase)}, do not also supply {nameof(OnValidSubmit)} or {nameof(OnInvalidSubmit)}.");

            if (_fixedEditContext == null || EditContext.HasValue() || CascadedEditContext.HasValue() || Model != _fixedEditContext.Model)
            {
                _fixedEditContext = EditContext.HasValue() 
                    ? EditContext.ParameterValue 
                    : CascadedEditContext.HasValue() 
                        ? CascadedEditContext.ParameterValue 
                        : new MyEditContext(Model);
                if (_fixedEditContextBp == null)
                    _fixedEditContextBp = new BlazorParameter<MyEditContext>(_fixedEditContext);
                else
                    _fixedEditContextBp.ParameterValue = _fixedEditContext;
            }

            await Task.CompletedTask;
        }

        protected override Task OnAfterFirstRenderAsync() => Task.CompletedTask;

        //protected override void BuildRenderTree(RenderTreeBuilder builder)
        //{
        //    if (builder == null)
        //        throw new NullReferenceException(nameof(builder));

        //    builder.OpenRegion(_fixedEditContext.GetHashCode());

        //    builder.OpenElement(0, "form");
        //    builder.AddMultipleAttributes(1, AdditionalAttributes);
        //    builder.AddAttribute(2, "onsubmit", _handleSubmitDelegate);
        //    builder.OpenComponent<CascadingValue<BlazorParameter<MyEditContext>>>(3);
        //    builder.OpenComponent<CascadingValue<MyEditContext>>(4);
        //    builder.AddAttribute(5, "IsFixed", true);
        //    builder.AddAttribute(6, "Value", _fixedEditContext);
        //    builder.AddAttribute(7, "ChildContent", ChildContent?.Invoke(_fixedEditContext));
        //    builder.CloseComponent();
        //    builder.CloseComponent();
        //    builder.CloseElement();

        //    builder.CloseRegion();
        //}

        public async Task SubmitAsync()
        {
            if (_submitting)
                return;

            _submitting = true;

            if (OnSubmit.HasDelegate)
                await OnSubmit.InvokeAsync(_fixedEditContext).ConfigureAwait(false);
            else
            {
                var isValid = await _fixedEditContext.ValidateAsync();
                if (isValid && OnValidSubmit.HasDelegate)
                    await OnValidSubmit.InvokeAsync(_fixedEditContext).ConfigureAwait(false);
                if (!isValid && OnInvalidSubmit.HasDelegate)
                    await OnInvalidSubmit.InvokeAsync(_fixedEditContext).ConfigureAwait(false);
            }

            _submitting = false;
        }
    }
}
