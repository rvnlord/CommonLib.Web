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
    public class ListClaimsBase : MyComponentBase
    {
        private MyComponentBase[] _allControls;
        private MyButtonBase[] _modalBtns;

        protected string _deleteMessage => $"Are you sure you want to delete claim \"{_claimWaitingForDeleteConfirmation?.Name}\"?";
        protected AdminEditClaimVM _claimWaitingForDeleteConfirmation { get; set; }
        protected MyModalBase _modalConfirmDeletingClaim { get; set; }
        protected List<FindClaimVM> _claims { get; set; }
        protected MyEditForm _editForm { get; set; }
        protected MyEditContext _editContext { get; set; }
        
        public override bool IsAuthorized => AuthenticatedUser?.IsAuthenticated == true && AuthenticatedUser?.HasRole("Admin") == true;
        
        protected override async Task OnInitializedAsync()
        {
            _claims = new(); 
            _editContext = new MyEditContext(_claims);
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

            var claimsResp = await AdminClient.GetClaimsAsync();
            if (claimsResp.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, claimsResp.Message);
                return;
            }

            _claims = claimsResp.Result;

            await StateHasChangedAsync(true);
        }

        protected override async Task OnAfterFirstRenderAfterAutthorizationAsync()
        {
            _allControls = GetInputControls();
            _modalBtns = _modalConfirmDeletingClaim.Descendants.OfType<MyButtonBase>().ToArray();
            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
        }

        protected async Task BtnDeleteClaim_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _, FindClaimVM claimToDelete)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, sender);
            _claimWaitingForDeleteConfirmation = Mapper.Map(claimToDelete, new AdminEditClaimVM());
            await SetControlStatesAsync(ComponentState.Enabled, _modalBtns, null, ChangeRenderingStateMode.None);
            await _modalConfirmDeletingClaim.NotifyParametersChangedAsync().StateHasChangedAsync(true);
            await _modalConfirmDeletingClaim.ShowModalAsync();
        }

        protected async Task BtnConfirmClaimDelete_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _modalBtns, sender, ChangeRenderingStateMode.None);
            await _modalConfirmDeletingClaim.StateHasChangedAsync(true);
            var editResponse = await AdminClient.DeleteClaimAsync(_claimWaitingForDeleteConfirmation);
            if (editResponse.IsError)
            {
                await PromptMessageAsync(NotificationType.Error, editResponse.Message);
                _claimWaitingForDeleteConfirmation = null;
                await SetControlStatesAsync(ComponentState.Enabled, _allControls);
                return;
            }

            _claims.Remove(Mapper.Map(_claimWaitingForDeleteConfirmation, new FindClaimVM()));

            await PromptMessageAsync(NotificationType.Success, editResponse.Message);
            _claimWaitingForDeleteConfirmation = null;
            await _modalConfirmDeletingClaim.HideModalAsync();
            await StateHasChangedAsync(true);
        }

        protected async Task Modal_HideAsync(MyModalBase sender, EventArgs e, CancellationToken _)
        {
            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
        }

        protected async Task BtnEditClaim_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _, FindClaimVM claimToEdit)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, sender);
            await NavigateAndUpdateActiveNavLinksAsync($"admin/editclaim/{claimToEdit.OriginalName}");
        }

        protected async Task BtnAddClaim_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken _)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, sender);
            await NavigateAndUpdateActiveNavLinksAsync("admin/addclaim/");
        }
    }
}
