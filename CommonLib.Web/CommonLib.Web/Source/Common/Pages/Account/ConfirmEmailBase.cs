using System;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyEditContextComponent;
using CommonLib.Web.Source.Common.Components.MyEditFormComponent;
using CommonLib.Web.Source.Common.Components.MyFluentValidatorComponent;
using CommonLib.Web.Source.Common.Components.MyModalComponent;
using CommonLib.Web.Source.Common.Components.MyPromptComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.AspNetCore.Components.Forms;
using Truncon.Collections;

namespace CommonLib.Web.Source.Common.Pages.Account
{
    public class ConfirmEmailBase : MyComponentBase
    {
        protected MyFluentValidator _validator;
        protected ButtonState _btnConfirmEmailState;
        protected MyButtonBase _btnConfirmEmail;
        protected MyEditForm _editForm;
        protected MyEditContext _editContext;
        protected ConfirmUserVM _confirmUserVM;

        protected override async Task OnInitializedAsync()
        {
            _btnConfirmEmailState = ButtonState.Loading;
            _confirmUserVM = new()
            {
                Email = NavigationManager.GetQueryString<string>("email"),
                ConfirmationCode = NavigationManager.GetQueryString<string>("code"),
                ReturnUrl = NavigationManager.GetQueryString<string>("returnUrl")?.Base58ToUTF8() ?? "/account/login"
            };
            _editContext = new MyEditContext(_confirmUserVM);
            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (IsFirstParamSetup())
            {
                await SetUserNameAsync();
                _editContext.ReBindValidationStateChanged(CurrentEditContext_ValidationStateChangedAsync);
            }
            await Task.CompletedTask;
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            if (!_confirmUserVM.Email.IsNullOrWhiteSpace() && !_confirmUserVM.ConfirmationCode.IsNullOrWhiteSpace())
            {
                var isValid = await _editContext.ValidateAsync(); // validate manually if code and email are directly within the url --> validation will trigger showing the messages and disabling the inputs on success
                if (isValid)
                    await ConfirmEmailAsync(); // changing state is taken care of in this method as well
                else
                {
                    await PromptMessageAsync(NotificationType.Error, "Account cannot be activated, please check the validation Messages");
                    _btnConfirmEmailState = ButtonState.Enabled;
                    await StateHasChangedAsync();
                }
                return;
            }
           
            _btnConfirmEmailState = ButtonState.Enabled;
            await StateHasChangedAsync();
        }

        protected async Task FormConfirmEmail_ValidSubmitAsync() => await ConfirmEmailAsync();

        protected async Task BtnSubmit_ClickAsync() => await _editForm.SubmitAsync();

        private async Task ConfirmEmailAsync()
        {
            _btnConfirmEmailState = ButtonState.Loading;
            await StateHasChangedAsync();
            var emailConfirmationResponse = await AccountClient.ConfirmEmailAsync(_confirmUserVM);
            if (emailConfirmationResponse.IsError)
            {
                _validator.AddValidationMessages(emailConfirmationResponse.ValidationMessages).NotifyValidationStateChanged(_validator); // since field validation never updates the whole model, adding errors here would cause all other fields to be valid (the ones that were never validated) but technically if even one field was never validated Confirm email method should not be reached be code
                await PromptMessageAsync(NotificationType.Error, emailConfirmationResponse.Message);
                _btnConfirmEmailState = ButtonState.Enabled;
                await StateHasChangedAsync();
                return;
            }

            var confirmedUser = emailConfirmationResponse.Result;
            _confirmUserVM.UserName = confirmedUser.UserName;
            //NavigationManager.NavigateTo($"/Account/Login/?returnUrl={_confirmUserVM.ReturnUrl?.UTF8ToBase58()}");
            await ComponentByClass<MyModalBase>("my-login-modal").ShowModalAsync();
            await PromptMessageAsync(NotificationType.Success, $"Email for user: \"{_confirmUserVM.UserName}\" has been confirmed successfully"); // can't update state on afterrender because it would cause an infinite loop
            _btnConfirmEmailState = ButtonState.Disabled;
            await StateHasChangedAsync();

        } // https://localhost:44396/Account/ConfirmEmail?email=rvnlord@gmail.com&code=xxx

        private async Task CurrentEditContext_ValidationStateChangedAsync(object sender, MyValidationStateChangedEventArgs e)
        {
            if (e == null)
                throw new NullReferenceException(nameof(e));
            if (e.ValidationMode == ValidationMode.Property && e.ValidatedFields == null)
                throw new NullReferenceException(nameof(e.ValidatedFields));
            if (e.ValidationStatus == ValidationStatus.Pending)
                return;

            if (new FieldIdentifier(_confirmUserVM, nameof(_confirmUserVM.Email)).In(e.ValidFields)) // doesn't matter if we are validating model or property - as long as email is valid
                await SetUserNameAsync();
            else
                _confirmUserVM.UserName = null;

            await StateHasChangedAsync();
        }

        private async Task SetUserNameAsync() => _confirmUserVM.UserName = (await AccountClient.FindUserByEmailAsync(_confirmUserVM.Email)).Result?.UserName;

        protected string GetNavQueryStrings()
        {
            return new OrderedDictionary<string, string>
            {
                [nameof(_confirmUserVM.Email).PascalCaseToCamelCase()] = _confirmUserVM.Email,
                [nameof(_confirmUserVM.ReturnUrl).PascalCaseToCamelCase()] = _confirmUserVM.ReturnUrl.UTF8ToBase58()
            }.ToQueryString();
        }
    }
}
