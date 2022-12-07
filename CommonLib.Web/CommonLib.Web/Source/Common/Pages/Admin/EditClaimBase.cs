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
    public class EditClaimBase : MyComponentBase
    {
        private MyComponentBase[] _allControls => GetInputControls();
        private MyButtonBase _btnSave;

        protected MyFluentValidator _validator { get; set; }
        protected MyEditForm _editForm { get; set; }
        protected AdminEditClaimVM _adminEditClaimVM { get; set; }
        protected MyEditContext _editContext { get; set; }
        protected MyCssGridItem _giUsers { get; set; }
        protected List<FindUserVM> _users { get; set; }
        
        public override bool IsAuthorized => AuthenticatedUser?.IsAuthenticated == true && AuthenticatedUser.HasRole("Admin");
        
        [Parameter]
        public string OriginalName { get; set; }
        
        protected override async Task OnInitializedAsync()
        {
            _adminEditClaimVM = new();
            _editContext = new MyEditContext(_adminEditClaimVM);
            _users = new();
            await Task.CompletedTask;
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            if (!await EnsureAuthenticatedAsync(true, true))
                return;
            
            if (!IsAuthorized)
            {
                await PromptMessageAsync(NotificationType.Error, "Claims can only be accessed by an Admin");
                return;
            }

            var claimResp = await AccountClient.FindClaimByNameAsync(OriginalName);
            if (claimResp.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, claimResp.Message);
                return;
            }

            var claimToAdminEdit = claimResp.Result;
            if (claimToAdminEdit is null)
            {
                await PromptMessageAsync(NotificationType.Error, $"There is no Claim with the following Name: {OriginalName}");
                return;
            }

            var usersResp = await AdminClient.GetAllUsersAsync();
            if (usersResp.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, usersResp.Message);
                return;
            }

            _users = usersResp.Result;

            Mapper.Map(claimToAdminEdit, _adminEditClaimVM);
            await StateHasChangedAsync(true);
            _btnSave = _allControls.OfType<MyButtonBase>().Single(b => b.SubmitsForm.V == true);
            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
        }

        public async Task BtnSaveClaim_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken token)
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
                await PromptMessageAsync(NotificationType.Error, "Only Admin can Edit Claims");
                return;
            }

            if (!await _editContext.ValidateAsync())
                return;
            
            var editResponse = await AdminClient.EditClaimAsync(_adminEditClaimVM);
            if (editResponse.IsError)
            {
                await _validator.AddValidationMessages(editResponse.ValidationMessages).NotifyValidationStateChangedAsync(_validator);
                await PromptMessageAsync(NotificationType.Error, editResponse.Message);
                await SetControlStatesAsync(ComponentState.Enabled, _allControls);
                return;
            }

            Mapper.Map(editResponse.Result, _adminEditClaimVM);
            await PromptMessageAsync(NotificationType.Success, editResponse.Message);
            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
        }

        protected async Task CbUser_CheckedAsync(MyCheckBoxBase sender, FindUserVM user, bool isChecked)
        {
            if (!_adminEditClaimVM.Values.Any()) // since we are creating a new claim it has no values, since we don't care about values, only claim names, we can create a dummy value. Also this value cannot be null because _userManager will refuse to add Claim with null value
                _adminEditClaimVM.Values.Add(new AdminEditClaimValueVM { Value = "true" });

            if (isChecked)
                _adminEditClaimVM.Values.First().UserNames.Add(user.UserName);
            else
                _adminEditClaimVM.Values.First().UserNames.Remove(user.UserName);

            await sender.StateHasChangedAsync(true);
        }
        
        protected async Task BtnBackToListClaims_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken token)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, sender);
            await NavigateAndUpdateActiveNavLinksAsync("/Admin/Claims");
        }
    }
}
