using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyCheckBoxComponent;
using CommonLib.Web.Source.Common.Components.MyCssGridItemComponent;
using CommonLib.Web.Source.Common.Components.MyEditFormComponent;
using CommonLib.Web.Source.Common.Components.MyFluentValidatorComponent;
using CommonLib.Web.Source.Common.Components.MyPromptComponent;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.ViewModels.Account;
using CommonLib.Web.Source.ViewModels.Admin;
using Microsoft.AspNetCore.Components.Web;

namespace CommonLib.Web.Source.Common.Pages.Admin
{
    public class AddUserBase : MyComponentBase
    {
        private MyComponentBase[] _allControls => GetInputControls();
        private MyButtonBase _btnAdd;

        protected MyFluentValidator _validator { get; set; }
        protected MyEditForm _editForm { get; set; }
        protected AdminEditUserVM _adminAddUserVM { get; set; }
        protected MyEditContext _editContext { get; set; }
        protected MyCssGridItem _giRoles { get; set; }
        protected MyCssGridItem _giClaims { get; set; }
        protected List<FindRoleVM> _roles { get; set; }
        protected List<FindClaimVM> _claims { get; set; }
        
        public override bool IsAuthorized => AuthenticatedUser?.IsAuthenticated == true && AuthenticatedUser.HasRole("Admin");

        protected override async Task OnInitializedAsync()
        {
            _adminAddUserVM = new();
            _editContext = new MyEditContext(_adminAddUserVM);
            _roles = new();
            _claims = new();
            await Task.CompletedTask;
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            if (!await EnsureAuthenticatedAsync(true, true))
                return;
            
            if (!IsAuthorized)
            {
                await PromptMessageAsync(NotificationType.Error, "Users can only be accessed by an Admin");
                return;
            }

            var rolesResp = await AdminClient.GetRolesAsync();
            if (rolesResp.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, rolesResp.Message);
                return;
            }

            _roles = rolesResp.Result;

            var claimsResp = await AdminClient.GetClaimsAsync();
            if (claimsResp.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, claimsResp.Message);
                return;
            }

            _claims = claimsResp.Result;

            await StateHasChangedAsync(true);
            _adminAddUserVM.ReturnUrl = "/Admin/Users";
            _btnAdd = _allControls.OfType<MyButtonBase>().Single(b => b.SubmitsForm.V == true);
            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
        }

        public async Task BtnAddUser_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken token)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, _btnAdd);

            if (!await EnsureAuthenticatedAsync(true, false))
            {
                await SetControlStatesAsync(ComponentState.Disabled, _allControls);
                await ShowLoginModalAsync();
                return;
            }

            if (!IsAuthorized)
            {
                await PromptMessageAsync(NotificationType.Error, "Only Admin can edit users");
                return;
            }

            if (!await _editContext.ValidateAsync())
                return;
            
            var addResponse = await AdminClient.AddUserAsync(_adminAddUserVM);
            if (addResponse.IsError)
            {
                await _validator.AddValidationMessages(addResponse.ValidationMessages).NotifyValidationStateChangedAsync(_validator);
                await PromptMessageAsync(NotificationType.Error, addResponse.Message);
                await SetControlStatesAsync(ComponentState.Enabled, _allControls);
                return;
            }

            Mapper.Map(addResponse.Result, _adminAddUserVM);
            await PromptMessageAsync(NotificationType.Success, addResponse.Message);
            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
        }

        protected async Task CbRole_CheckedAsync(MyCheckBoxBase sender, FindRoleVM role, bool isChecked)
        {
            if (isChecked)
                _adminAddUserVM.Roles.Add(role);
            else
                _adminAddUserVM.Roles.Remove(role);
           
            await sender.Ancestors.OfType<MyCssGridItem>().First().StateHasChangedAsync(true);
        }

        protected async Task CbClaim_CheckedAsync(MyCheckBoxBase sender, FindClaimVM claim, bool isChecked)
        {
            if (isChecked)
                _adminAddUserVM.Claims.Add(claim); // for simplicity consider only claim type and omit the value, so we can take any (first) value, which is a key in this keys because values of the nested dict are UserNames
            else
                _adminAddUserVM.Claims.Remove(claim);

            await sender.Ancestors.OfType<MyCssGridItem>().First().StateHasChangedAsync(true);
        }

        protected async Task BtnBackToListUsers_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken token)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, sender);
            await NavigateAndUpdateActiveNavLinksAsync(_adminAddUserVM.ReturnUrl);
        }
    }
}
