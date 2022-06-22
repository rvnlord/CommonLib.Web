using System;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyEditContextComponent;
using CommonLib.Web.Source.Common.Components.MyEditFormComponent;
using CommonLib.Web.Source.Common.Components.MyFluentValidatorComponent;
using CommonLib.Web.Source.Common.Components.MyPromptComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.ViewModels.Account;
using CommonLib.Source.Common.Converters;
using Microsoft.AspNetCore.Components;
using Truncon.Collections;

namespace CommonLib.Web.Source.Common.Pages.Account
{
    public class RegisterBase : MyComponentBase
    {
        protected MyFluentValidator _validator;
        protected MyEditForm _editForm;
        protected ButtonState? _btnRegisterState;
        protected MyEditContext _editContext;
        protected MyButtonBase _btnRegister;

        [Inject]
        public IServiceProvider ServiceProvider { get; set; }

        public RegisterUserVM RegisterUserVM { get; set; }

        protected override async Task OnInitializedAsync()
        {
            _btnRegisterState = ButtonState.Loading; // ButtonState.Loading breaks async validation model for some reason, TODO figure it out
            RegisterUserVM = new()
            {
                ReturnUrl = NavigationManager.GetQueryString<string>("returnUrl")?.Base58ToUTF8() ?? "/Account/Login?keepPrompt=true"
            };
            _editContext = new MyEditContext(RegisterUserVM);
            //_validator = await new MyFluentValidator().InitAsync(_editContext, ServiceProvider); // included using @ref instead
            await Task.CompletedTask;
        }

        protected override async Task OnParametersSetAsync() => await Task.CompletedTask;

        protected override async Task OnAfterFirstRenderAsync()
        {
            //await (await ComponentBaseModuleAsync).InvokeVoidAsync("blazor_MyComponentBase_RefreshLayout");
            //await Task.Delay(10000);
            _btnRegisterState = ButtonState.Enabled;
            await StateHasChangedAsync();
        }

        protected async Task FormRegister_ValidSubmitAsync() // no full validation on submit, simply never call this method if validator contains invalid fields from per field validation
        {
            _btnRegisterState = ButtonState.Loading;
            await StateHasChangedAsync();
            var registrationResult = await AccountClient.RegisterAsync(RegisterUserVM);
            if (registrationResult.IsError)
            {
                if (registrationResult.Result?.ReturnUrl != null && RegisterUserVM.ReturnUrl != registrationResult.Result.ReturnUrl)
                    NavigationManager.NavigateTo(registrationResult.Result.ReturnUrl); // redirect to `ResendEmailConfirmation` on successful registration but when email couldn't be deployed

                _validator.AddValidationMessages(registrationResult.ValidationMessages).NotifyValidationStateChanged(_validator);
                await PromptMessageAsync(NotificationType.Error, registrationResult.Message);
                _btnRegisterState = ButtonState.Enabled;
                await StateHasChangedAsync();
                return;
            }

            var registeredUser = registrationResult.Result;
            _btnRegisterState = ButtonState.Disabled;
            await StateHasChangedAsync();
            await PromptMessageAsync(NotificationType.Success, registrationResult.Message);
            NavigationManager.NavigateTo(registeredUser.ReturnUrl);
        }

        protected async Task BtnSubmit_ClickAsync() => await _editForm.SubmitAsync();

        protected string GetNavQueryStrings()
        {
            return new OrderedDictionary<string, string>
            {
                [nameof(RegisterUserVM.Email).PascalCaseToCamelCase()] = RegisterUserVM.Email,
                [nameof(RegisterUserVM.ReturnUrl).PascalCaseToCamelCase()] = RegisterUserVM.ReturnUrl.UTF8ToBase58()
            }.ToQueryString();
        }
    }
}