using System;
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
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace CommonLib.Web.Source.Common.Pages.Admin
{
    public class EditRoleBase : MyComponentBase
    {
        private MyComponentBase[] _allControls => GetInputControls();
        private MyButtonBase _btnSave;

        protected MyFluentValidator _validator { get; set; }
        protected MyEditForm _editForm { get; set; }
        protected AdminEditRoleVM _adminEditRoleVM { get; set; }
        protected MyEditContext _editContext { get; set; }
        protected MyCssGridItem _giUsers { get; set; }
        protected List<FindUserVM> _users { get; set; }
        
        public override bool IsAuthorized => AuthenticatedUser?.IsAuthenticated == true && AuthenticatedUser.HasRole("Admin");
        
        [Parameter]
        public Guid Id { get; set; }
        
        protected override async Task OnInitializedAsync()
        {
            _adminEditRoleVM = new();
            _editContext = new MyEditContext(_adminEditRoleVM);
            _users = new();
            await Task.CompletedTask;
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            if (!await EnsureAuthenticatedAsync(true, true))
                return;
            
            if (!IsAuthorized)
            {
                await PromptMessageAsync(NotificationType.Error, "Roles can only be accessed by an Admin");
                return;
            }

            var roleResp = await AdminClient.FindRoleByIdAsync(Id);
            if (roleResp.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, roleResp.Message);
                return;
            }

            var roleToAdminEdit = roleResp.Result;
            if (roleToAdminEdit is null)
            {
                await PromptMessageAsync(NotificationType.Error, $"There is no Role with the following id: {Id}");
                return;
            }

            var usersResp = await AdminClient.GetAllUsersAsync();
            if (usersResp.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, usersResp.Message);
                return;
            }

            _users = usersResp.Result;

            Mapper.Map(roleToAdminEdit, _adminEditRoleVM);
            await StateHasChangedAsync(true);
            _btnSave = _allControls.OfType<MyButtonBase>().Single(b => b.SubmitsForm.V == true);
            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
        }

        public async Task BtnSaveRole_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken token)
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
                await PromptMessageAsync(NotificationType.Error, "Only Admin can Edit Roles");
                return;
            }

            if (!await _editContext.ValidateAsync())
                return;
            
            var editResponse = await AdminClient.EditRoleAsync(_adminEditRoleVM);
            if (editResponse.IsError)
            {
                await _validator.AddValidationMessages(editResponse.ValidationMessages).NotifyValidationStateChangedAsync(_validator);
                await PromptMessageAsync(NotificationType.Error, editResponse.Message);
                await SetControlStatesAsync(ComponentState.Enabled, _allControls);
                return;
            }

            Mapper.Map(editResponse.Result, _adminEditRoleVM);
            await PromptMessageAsync(NotificationType.Success, editResponse.Message);
            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
        }

        protected async Task CbUser_CheckedAsync(MyCheckBoxBase sender, FindUserVM user, bool isChecked)
        {
            if (isChecked)
                _adminEditRoleVM.UserNames.Add(user.UserName);
            else
                _adminEditRoleVM.UserNames.Remove(user.UserName);

            await sender.StateHasChangedAsync(true);
        }
        
        protected async Task BtnBackToListRoles_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken token)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, sender);
            await NavigateAndUpdateActiveNavLinksAsync("/Admin/Roles");
        }
    }
}
