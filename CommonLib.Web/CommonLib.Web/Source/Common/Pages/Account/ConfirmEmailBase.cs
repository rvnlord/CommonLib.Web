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
using CommonLib.Web.Source.Common.Components.MyModalComponent;
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
    public class ConfirmEmailBase : MyComponentBase
    {
        private MyComponentBase[] _allControls;
        private MyButtonBase _btnConfirmEmail;

        protected MyFluentValidator _validator;
        protected MyEditForm _editForm;
        protected MyEditContext _editContext;
        protected ConfirmUserVM _confirmEmailUserVM;

        protected override async Task OnInitializedAsync()
        {
            _confirmEmailUserVM = new()
            {
                Email = NavigationManager.GetQueryString<string>("email")?.Base58ToUTF8(),
                ConfirmationCode = NavigationManager.GetQueryString<string>("code"),
                ReturnUrl = NavigationManager.GetQueryString<string>("returnUrl")?.Base58ToUTF8() ?? "/account/login"
            };
            _editContext = new MyEditContext(_confirmEmailUserVM);
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
            _allControls = Descendants.Where(c => c is MyTextInput or MyPasswordInput or MyButton && !c.Ancestors.Any(a => a is MyInputBase)).ToArray();
            _btnConfirmEmail = Descendants.OfType<MyButtonBase>().Single(b => b.SubmitsForm.V == true);
            
            if (!_confirmEmailUserVM.Email.IsNullOrWhiteSpace() && !_confirmEmailUserVM.ConfirmationCode.IsNullOrWhiteSpace())
            {
                var isValid = await _editContext.ValidateAsync(); // validate manually if code and email are directly within the url --> validation will trigger showing the messages and disabling the inputs on success
                if (isValid)
                    await ConfirmEmailAsync(); // changing state is taken care of in this method as well
                else
                {
                    await PromptMessageAsync(NotificationType.Error, "Account cannot be activated, please check the validation Messages");
                    await SetControlStatesAsync(ButtonState.Enabled, _allControls);
                }
                return;
            }
           
            await SetControlStatesAsync(ButtonState.Enabled, _allControls);
        }

        protected async Task FormConfirmEmail_ValidSubmitAsync() => await ConfirmEmailAsync();

        protected async Task BtnSubmit_ClickAsync() => await _editForm.SubmitAsync();

        private async Task ConfirmEmailAsync()
        {
            await SetControlStatesAsync(ButtonState.Disabled, _allControls, _btnConfirmEmail);
            var confirmUserToSend = Mapper.Map(_confirmEmailUserVM, new ConfirmUserVM());
            confirmUserToSend.ConfirmationCode = confirmUserToSend.ConfirmationCode.Base58ToUTF8();
            var emailConfirmationResponse = await AccountClient.ConfirmEmailAsync(confirmUserToSend);
            if (emailConfirmationResponse.IsError)
            {
                await _validator.AddValidationMessages(emailConfirmationResponse.ValidationMessages).NotifyValidationStateChangedAsync(_validator); // since field validation never updates the whole model, adding errors here would cause all other fields to be valid (the ones that were never validated) but technically if even one field was never validated Confirm email method should not be reached be code
                await PromptMessageAsync(NotificationType.Error, emailConfirmationResponse.Message);
                await SetControlStatesAsync(ButtonState.Enabled, _allControls);
                return;
            }

            Mapper.Map(emailConfirmationResponse.Result, _confirmEmailUserVM);
            _confirmEmailUserVM.ConfirmationCode = _confirmEmailUserVM.ConfirmationCode.UTF8ToBase58(false);
            await ComponentByClassAsync<MyModalBase>("my-login-modal").ShowModalAsync();
            await PromptMessageAsync(NotificationType.Success, $"Email for user: \"{_confirmEmailUserVM.UserName}\" has been confirmed successfully"); // can't update state on afterrender because it would cause an infinite loop
            await SetControlStatesAsync(ButtonState.Disabled, _allControls);

        } // https://localhost:44396/Account/ConfirmEmail?email=rvnlord@gmail.com&code=xxx

        private async Task CurrentEditContext_ValidationStateChangedAsync(MyEditContext sender, MyValidationStateChangedEventArgs e, CancellationToken _)
        {
            if (e == null)
                throw new NullReferenceException(nameof(e));
            if (e.ValidationMode == ValidationMode.Property && e.ValidatedFields == null)
                throw new NullReferenceException(nameof(e.ValidatedFields));
            if (e.ValidationStatus == ValidationStatus.Pending)
                return;

            if (new FieldIdentifier(_confirmEmailUserVM, nameof(_confirmEmailUserVM.Email)).In(e.ValidFields)) // doesn't matter if we are validating model or property - as long as email is valid
                await SetUserNameAsync();
            else
                _confirmEmailUserVM.UserName = null;

            await StateHasChangedAsync();
        }

        private async Task SetUserNameAsync() => _confirmEmailUserVM.UserName = (await AccountClient.FindUserByEmailAsync(_confirmEmailUserVM.Email)).Result?.UserName;

        protected string GetNavQueryStrings()
        {
            return new OrderedDictionary<string, string>
            {
                [nameof(_confirmEmailUserVM.Email).PascalCaseToCamelCase()] = _confirmEmailUserVM.Email?.UTF8ToBase58(false),
                [nameof(_confirmEmailUserVM.ReturnUrl).PascalCaseToCamelCase()] = _confirmEmailUserVM.ReturnUrl?.UTF8ToBase58()
            }.Where(kvp => kvp.Value != null).ToQueryString();
        }
    }
}
