using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyEditFormComponent;
using CommonLib.Web.Source.Common.Components.MyFluentValidatorComponent;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Components.MyPasswordInputComponent;
using CommonLib.Web.Source.Common.Components.MyPromptComponent;
using CommonLib.Web.Source.Common.Components.MyTextInputComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.AspNetCore.Components.Forms;
using Truncon.Collections;

namespace CommonLib.Web.Source.Common.Pages.Account
{
    public class ForgotPasswordBase : MyComponentBase
    {
        private MyComponentBase[] _allControls;
        private MyButtonBase _btnForgotPassword;

        protected MyFluentValidator _validator { get; set; }
        protected MyEditForm _editForm { get; set; }
        protected MyEditContext _editContext { get; set; }
        protected ForgotPasswordUserVM _forgotPasswordUserVM { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var inheritedReturnUrl = NavigationManager.GetQueryString<string>("returnUrl")?.Base58ToUTF8()?.BeforeFirstOrWhole("?");
            _forgotPasswordUserVM ??= new ForgotPasswordUserVM { ReturnUrl = inheritedReturnUrl ?? NavigationManager.BaseUri };
            _editContext ??= new MyEditContext(_forgotPasswordUserVM);

            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (FirstParamSetup)
            {
                await SetUserNameAsync();
                _editContext.ReBindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync);
            }
            await Task.CompletedTask;
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            _allControls = GetInputControls();
            _btnForgotPassword = Descendants.OfType<MyButtonBase>().Single(b => b.SubmitsForm.V == true);

            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
        }

        protected async Task BtnChangeForgottenPassword_ClickAsync() => await _editForm.SubmitAsync();
        protected async Task FormChangeForgottenPassword_ValidSubmitAsync()
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, _btnForgotPassword);
            var forgotPasswordResponse = await AccountClient.ForgotPasswordAsync(_forgotPasswordUserVM);
            if (forgotPasswordResponse.IsError)
            {
                _btnForgotPassword.InteractivityState.StateValue = ComponentState.Enabled;
                await _validator.AddValidationMessages(forgotPasswordResponse.ValidationMessages).NotifyValidationStateChangedAsync(_validator); ;
                await PromptMessageAsync(NotificationType.Error, forgotPasswordResponse.Message);
                await SetControlStatesAsync(ComponentState.Enabled, _allControls);
                return;
            }

            Mapper.Map(forgotPasswordResponse.Result, _forgotPasswordUserVM);
            await PromptMessageAsync(NotificationType.Success, forgotPasswordResponse.Message);
            NavigationManager.NavigateTo($"/Account/ResetPassword?{GetNavQueryStrings()}");
        }

        private async Task CurrentEditContext_ValidationStateChangedAsync(MyEditContext sender, MyValidationStateChangedEventArgs e, CancellationToken _)
        {
            if (e == null)
                throw new NullReferenceException(nameof(e));
            if (e.ValidationMode == ValidationMode.Property && e.ValidatedFields == null)
                throw new NullReferenceException(nameof(e.ValidatedFields));
            if (e.ValidationStatus == ValidationStatus.Pending)
                return;
            if (Ancestors.Any(a => a is MyInputBase))
                return;

            if (new FieldIdentifier(_forgotPasswordUserVM, nameof(_forgotPasswordUserVM.Email)).In(e.ValidatedFields))
                await SetUserNameAsync();

            await StateHasChangedAsync();
        }

        private async Task SetUserNameAsync() => _forgotPasswordUserVM.UserName = (await AccountClient.FindUserByEmailAsync(_forgotPasswordUserVM.Email))?.Result?.UserName;

        protected string GetNavQueryStrings()
        {
            return new OrderedDictionary<string, string>
            {
                [nameof(_forgotPasswordUserVM.Email).PascalCaseToCamelCase()] = _forgotPasswordUserVM.Email?.UTF8ToBase58(false),
                [nameof(_forgotPasswordUserVM.ReturnUrl).PascalCaseToCamelCase()] = _forgotPasswordUserVM.ReturnUrl?.UTF8ToBase58()
            }.Where(kvp => kvp.Value != null).ToQueryString();
        }
    }
}
