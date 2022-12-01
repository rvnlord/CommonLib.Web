using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Extensions.Collections;
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
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace CommonLib.Web.Source.Common.Pages.Admin
{
    public class EditUserBase : MyComponentBase
    {
        private MyComponentBase[] _allControls;
        private MyButtonBase _btnSave;

        protected MyFluentValidator _validator { get; set; }
        protected MyEditForm _editForm { get; set; }
        protected AdminEditUserVM _adminEditUserVM { get; set; }
        protected MyEditContext _editContext { get; set; }
        protected MyCssGridItem _giRoles { get; set; }
        protected MyCssGridItem _giClaims { get; set; }
        protected List<FindRoleVM> _roles { get; set; }
        protected List<FindClaimVM> _claims { get; set; }
        
        public override bool IsAuthorized => AuthenticatedUser?.IsAuthenticated == true && AuthenticatedUser.HasRole("Admin");
        
        [Parameter]
        public Guid Id { get; set; }
        
        protected override async Task OnInitializedAsync()
        {
            _adminEditUserVM = new();
            _editContext = new MyEditContext(_adminEditUserVM);
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

            var userResp = await AccountClient.FindUserByIdAsync(Id);
            if (userResp.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, userResp.Message);
                return;
            }

            var userToAdminEdit = userResp.Result;
            if (userToAdminEdit is null)
            {
                await PromptMessageAsync(NotificationType.Error, $"There is no User with the following id: {Id}");
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

            Mapper.Map(userToAdminEdit, _adminEditUserVM);
            var gisRolesClaims = new[] { _giRoles, _giClaims };
            await SetControlStatesAsync(ComponentState.Enabled, gisRolesClaims);
            _adminEditUserVM.ReturnUrl = "/Admin/Users";
            _adminEditUserVM.Avatar = (await AccountClient.GetUserAvatarByNameAsync(_adminEditUserVM.UserName)).Result;
            _allControls = GetInputControls().Concat(gisRolesClaims).ToArray();
            _btnSave = _allControls.OfType<MyButtonBase>().Single(b => b.SubmitsForm.V == true);
            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
        }

        public async Task BtnSaveUser_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken token)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, _btnSave);

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
            
            var editResponse = await AdminClient.EditUserAsync(_adminEditUserVM);
            if (editResponse.IsError)
            {
                await _validator.AddValidationMessages(editResponse.ValidationMessages).NotifyValidationStateChangedAsync(_validator);
                await PromptMessageAsync(NotificationType.Error, editResponse.Message);
                await SetControlStatesAsync(ComponentState.Enabled, _allControls);
                return;
            }

            Mapper.Map(editResponse.Result, _adminEditUserVM);
            await PromptMessageAsync(NotificationType.Success, editResponse.Message);

            AuthenticatedUser.Avatar = (await AccountClient.GetUserAvatarByNameAsync(AuthenticatedUser.UserName))?.Result;

            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
        }

        protected async Task CbRole_CheckedAsync(MyCheckBoxBase sender, FindRoleVM role, bool isChecked)
        {
            if (isChecked)
                _adminEditUserVM.Roles.Add(role);
            else
                _adminEditUserVM.Roles.Remove(role);
           
            await sender.Ancestors.OfType<MyCssGridItem>().First().StateHasChangedAsync(true);
        }

        protected async Task CbClaim_CheckedAsync(MyCheckBoxBase sender, FindClaimVM claim, bool isChecked)
        {
            if (isChecked)
                _adminEditUserVM.Claims.Add(claim); // for simplicity consider only claim type and omit the value, so we can take any (first) value, which is a key in this keys because values of the nested dict are UserNames
            else
                _adminEditUserVM.Claims.Remove(claim);

            await sender.Ancestors.OfType<MyCssGridItem>().First().StateHasChangedAsync(true);
        }

        protected async Task BtnBackToListUsers_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken token)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, sender);
            await NavigateAndUpdateActiveNavLinksAsync(_adminEditUserVM.ReturnUrl);
        }
    }
}
