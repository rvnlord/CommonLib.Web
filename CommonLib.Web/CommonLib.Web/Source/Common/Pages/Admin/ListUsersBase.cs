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
    public class ListUsersBase : MyComponentBase
    {
        private MyComponentBase[] _allControls;
        private MyButtonBase[] _modalBtns;

        protected string _deleteMessage => $"Are you sure you want to delete user \"{_userWaitingForDeleteConfirmation?.UserName}\"?";
        protected AdminEditUserVM _userWaitingForDeleteConfirmation { get; set; }
        protected MyModalBase _modalConfirmDeletingUser { get; set; }
        protected List<FindUserVM> _users { get; set; }
        protected MyEditForm _editForm { get; set; }
        protected MyEditContext _editContext { get; set; }
        
        public override bool IsAuthorized => AuthenticatedUser?.IsAuthenticated == true && AuthenticatedUser?.HasRole("Admin") == true;
        
        protected override async Task OnInitializedAsync()
        {
            _users = new(); 
            _editContext = new MyEditContext(_users);
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

            var foundUsersResp = await AdminClient.GetAllUsersAsync();
            if (foundUsersResp.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, foundUsersResp.Message);
                return;
            }

            _users = foundUsersResp.Result;

            await StateHasChangedAsync(true);
        }

        protected override async Task OnAfterFirstRenderAfterAutthorizationAsync()
        {
            _allControls = GetInputControls();
            _modalBtns = _modalConfirmDeletingUser.Descendants.OfType<MyButtonBase>().ToArray();
            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
        }

        protected async Task BtnDeleteUser_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _, FindUserVM userToDelete)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, sender);
            _userWaitingForDeleteConfirmation = Mapper.Map(userToDelete, new AdminEditUserVM());
            await SetControlStatesAsync(ComponentState.Enabled, _modalBtns, null, ChangeRenderingStateMode.None);
            await _modalConfirmDeletingUser.NotifyParametersChangedAsync().StateHasChangedAsync(true);
            await _modalConfirmDeletingUser.ShowModalAsync();
        }

        protected async Task BtnConfirmUserDelete_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _modalBtns, sender, ChangeRenderingStateMode.None);
            await _modalConfirmDeletingUser.StateHasChangedAsync(true);
            var editResponse = await AdminClient.DeleteUserAsync(_userWaitingForDeleteConfirmation);
            if (editResponse.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, editResponse.Message);
                _userWaitingForDeleteConfirmation = null;
                await SetControlStatesAsync(ComponentState.Enabled, _allControls);
                return;
            }

            _users.Remove(Mapper.Map(_userWaitingForDeleteConfirmation, new FindUserVM()));

            await PromptMessageAsync(NotificationType.Success, editResponse.Message);
            _userWaitingForDeleteConfirmation = null;
            await _modalConfirmDeletingUser.HideModalAsync();
        }

        protected async Task Modal_HideAsync(MyModalBase sender, EventArgs e, CancellationToken _)
        {
            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
        }

        protected async Task BtnEditUser_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _, FindUserVM userToEdit)
        {
            //SetButtonStates(ButtonState.Disabled);
            //_btnEditUserStates[userToEdit.Id] = ButtonState.Loading;
            //NavigationManager.NavigateTo($"admin/edituser/{userToEdit.Id}");
        }

        protected async Task BtnAddUser_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _)
        {
            //SetButtonStates(ButtonState.Disabled);
            //_btnAddUserState = ButtonState.Loading;
            //NavigationManager.NavigateTo("admin/adduser/");
        }
    }
}
