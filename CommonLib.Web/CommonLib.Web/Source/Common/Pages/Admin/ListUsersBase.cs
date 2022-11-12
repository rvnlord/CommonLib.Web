using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyEditFormComponent;
using CommonLib.Web.Source.Common.Components.MyPromptComponent;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.AspNetCore.Components.Web;

namespace CommonLib.Web.Source.Common.Pages.Admin
{
    public class ListUsersBase : MyComponentBase
    {
        private MyComponentBase[] _allControls;

        protected List<FindUserVM> _users { get; set; }
        protected MyEditForm _editForm { get; set; }
        protected MyEditContext _editContext { get; set; }
        
        public bool IsAuthorized => AuthenticatedUser?.IsAuthenticated == true && AuthenticatedUser?.HasRole("Admin") == true;
        
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

            //var foundUsersResp = await AdminService.GetAllUsersAsync();
            //if (foundUsersResp.IsError)
            //{
            //    await PromptMessageAsync(NotificationType.Error, foundUsersResp.Message);
            //    return;
            //}

            //_users = foundUsersResp.Result;

            _allControls = GetInputControls();

            await SetControlStatesAsync(ButtonState.Enabled, _allControls);
        }
        
        protected async Task BtnDeleteUser_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _, FindUserVM userToDelete)
        {
            //SetButtonStates(ButtonState.Disabled);
            //_btnDeleteUserStates[userToDelete.Id] = ButtonState.Loading;
            //ConfirmationDialog_DeleteUser.Show($"Are you sure you want to delete User \"{userToDelete.UserName}\"?");
            //_userWaitingForDeleteConfirmation = Mapper.Map(userToDelete, new AdminEditUserVM());
        }

        protected async Task BtnConfirmUserDelete_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _, bool isDeleteConfirmed)
        {
            //if (!isDeleteConfirmed)
            //{
            //    SetButtonStates(ButtonState.Enabled);
            //    _userWaitingForDeleteConfirmation = null;
            //    StateHasChanged();
            //    return;
            //}

            //var editResponse = await AdminService.DeleteUserAsync(_userWaitingForDeleteConfirmation);
            //var usersToEditbyAdminResponse = await AdminService.GetAllUsersAsync();
            //Users = usersToEditbyAdminResponse.Result;
            //if (editResponse.IsError || usersToEditbyAdminResponse.IsError)
            //{
            //    SetButtonStates(ButtonState.Enabled);
            //    await Main.PromptMessageAsync(PromptType.Error, editResponse.Message ?? usersToEditbyAdminResponse.Message);
            //    _userWaitingForDeleteConfirmation = null;
            //    StateHasChanged();
            //    return;
            //}

            //SetButtonStates(ButtonState.Enabled);
            //await Main.PromptMessageAsync(PromptType.Success, editResponse.Message);
            //_userWaitingForDeleteConfirmation = null;
            //StateHasChanged();
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
