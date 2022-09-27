using System.Linq;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyEditFormComponent;
using CommonLib.Web.Source.Common.Components.MyFluentValidatorComponent;
using CommonLib.Web.Source.Common.Components.MyPromptComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.ViewModels.Account;
using CommonLib.Source.Common.Converters;
using CommonLib.Web.Source.Common.Components.MyInputComponent;
using CommonLib.Web.Source.Common.Components.MyPasswordInputComponent;
using CommonLib.Web.Source.Common.Components.MyTextInputComponent;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using Truncon.Collections;

namespace CommonLib.Web.Source.Common.Pages.Account
{
    public class RegisterBase : MyComponentBase
    {
        private MyButtonBase _btnRegister;
        private MyComponentBase[] _allControls;

        protected MyFluentValidator _validator;
        protected MyEditForm _editForm;
        protected MyEditContext _editContext;
        protected RegisterUserVM _registerUserVM { get; set; }

        protected override async Task OnInitializedAsync()
        {
            _registerUserVM = new()
            {
                ReturnUrl = NavigationManager.GetQueryString<string>("returnUrl")?.Base58ToUTF8() ?? "/Account/Login"
            };
            _editContext = new MyEditContext(_registerUserVM);
            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync() => await Task.CompletedTask;

        protected override async Task OnAfterFirstRenderAsync()
        {
            _allControls = Descendants.Where(c => c is MyTextInput or MyPasswordInput or MyButton && !c.Ancestors.Any(a => a is MyInputBase)).ToArray();
            _btnRegister = Descendants.OfType<MyButtonBase>().Single(b => b.SubmitsForm.V == true);

            await SetControlStatesAsync(ButtonState.Enabled, _allControls);
        }

        protected async Task FormRegister_ValidSubmitAsync() // no full validation on submit, simply never call this method if validator contains invalid fields from per field validation
        {
            await SetControlStatesAsync(ButtonState.Disabled, _allControls, _btnRegister);
            var registrationResult = await AccountClient.RegisterAsync(_registerUserVM);
            if (registrationResult.IsError)
            {
                if (registrationResult.Result?.ReturnUrl != null && _registerUserVM.ReturnUrl != registrationResult.Result.ReturnUrl)
                    NavigationManager.NavigateTo(registrationResult.Result.ReturnUrl); // redirect to `ResendEmailConfirmation` on successful registration but when email couldn't be deployed

                _validator.AddValidationMessages(registrationResult.ValidationMessages).NotifyValidationStateChanged(_validator);
                await PromptMessageAsync(NotificationType.Error, registrationResult.Message);
                await SetControlStatesAsync(ButtonState.Enabled, _allControls);
                return;
            }

            var registeredUser = registrationResult.Result;
            await SetControlStatesAsync(ButtonState.Disabled, _allControls);
            await PromptMessageAsync(NotificationType.Success, registrationResult.Message);
            NavigationManager.NavigateTo(registeredUser.ReturnUrl);
        }

        protected async Task BtnSubmit_ClickAsync() => await _editForm.SubmitAsync();

        protected string GetNavQueryStrings()
        {
            return new OrderedDictionary<string, string>
            {
                [nameof(_registerUserVM.Email).PascalCaseToCamelCase()] = _registerUserVM.Email,
                [nameof(_registerUserVM.ReturnUrl).PascalCaseToCamelCase()] = _registerUserVM.ReturnUrl.UTF8ToBase58()
            }.ToQueryString();
        }
    }
}