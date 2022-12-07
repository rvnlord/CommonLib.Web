using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyEditFormComponent;
using CommonLib.Web.Source.Common.Components.MyModalComponent;
using CommonLib.Web.Source.Common.Components.MyPromptComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.ViewModels.Account;
using CommonLib.Web.Source.ViewModels.Admin;
using Microsoft.AspNetCore.Components.Web;

namespace CommonLib.Web.Source.Common.Pages.Admin
{
    public class ListRolesBase : MyComponentBase
    {
        private MyComponentBase[] _allControls;
        private MyButtonBase[] _modalBtns;

        protected string _deleteMessage => $"Are you sure you want to delete role \"{_roleWaitingForDeleteConfirmation?.Name}\"?";
        protected AdminEditRoleVM _roleWaitingForDeleteConfirmation { get; set; }
        protected MyModalBase _modalConfirmDeletingRole { get; set; }
        protected List<FindRoleVM> _roles { get; set; }
        protected MyEditForm _editForm { get; set; }
        protected MyEditContext _editContext { get; set; }
        
        public override bool IsAuthorized => AuthenticatedUser?.IsAuthenticated == true && AuthenticatedUser?.HasRole("Admin") == true;
        
        protected override async Task OnInitializedAsync()
        {
            _roles = new(); 
            _editContext = new MyEditContext(_roles);
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

            var rolesResp = await AdminClient.GetRolesAsync();
            if (rolesResp.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, rolesResp.Message);
                return;
            }

            _roles = rolesResp.Result;

            await StateHasChangedAsync(true);
        }

        protected override async Task OnAfterFirstRenderAfterAutthorizationAsync()
        {
            _allControls = GetInputControls();
            _modalBtns = _modalConfirmDeletingRole.Descendants.OfType<MyButtonBase>().ToArray();
            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
        }

        protected async Task BtnDeleteRole_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _, FindRoleVM roleToDelete)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, sender);
            _roleWaitingForDeleteConfirmation = Mapper.Map(roleToDelete, new AdminEditRoleVM());
            await SetControlStatesAsync(ComponentState.Enabled, _modalBtns, null, ChangeRenderingStateMode.None);
            await _modalConfirmDeletingRole.NotifyParametersChangedAsync().StateHasChangedAsync(true);
            await _modalConfirmDeletingRole.ShowModalAsync();
        }

        protected async Task BtnConfirmRoleDelete_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _modalBtns, sender, ChangeRenderingStateMode.None);
            await _modalConfirmDeletingRole.StateHasChangedAsync(true);
            var editResponse = await AdminClient.DeleteRoleAsync(_roleWaitingForDeleteConfirmation);
            if (editResponse.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, editResponse.Message);
                _roleWaitingForDeleteConfirmation = null;
                await SetControlStatesAsync(ComponentState.Enabled, _allControls);
                return;
            }

            _roles.Remove(Mapper.Map(_roleWaitingForDeleteConfirmation, new FindRoleVM()));

            await PromptMessageAsync(NotificationType.Success, editResponse.Message);
            _roleWaitingForDeleteConfirmation = null;
            await _modalConfirmDeletingRole.HideModalAsync();
            await StateHasChangedAsync(true);
        }

        protected async Task Modal_HideAsync(MyModalBase sender, EventArgs e, CancellationToken _)
        {
            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
        }

        protected async Task BtnEditRole_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _, FindRoleVM roleToEdit)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, sender);
            await NavigateAndUpdateActiveNavLinksAsync($"admin/editrole/{roleToEdit.Id}");
        }

        protected async Task BtnAddRole_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, sender);
            await NavigateAndUpdateActiveNavLinksAsync("admin/addrole/");
        }
    }
}
