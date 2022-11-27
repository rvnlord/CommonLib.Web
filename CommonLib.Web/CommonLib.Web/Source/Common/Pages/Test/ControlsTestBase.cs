﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.TypeUtils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyCssGridComponent;
using CommonLib.Web.Source.Common.Components.MyEditFormComponent;
using CommonLib.Web.Source.Common.Components.MyFileUploadComponent;
using CommonLib.Web.Source.Common.Components.MyFluentValidatorComponent;
using CommonLib.Web.Source.Common.Components.MyImageComponent;
using CommonLib.Web.Source.Common.Components.MyMediaQueryComponent;
using CommonLib.Web.Source.Common.Components.MyProgressBarComponent;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.ViewModels;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using MoreLinq;
using Org.BouncyCastle.Security;

namespace CommonLib.Web.Source.Common.Pages.Test
{
    public class ControlsTestBase : MyComponentBase
    {
        private MyComponentBase[] _allControls;
        private MyButtonBase _btnSave;

        protected MyFluentValidatorBase _validator;
        protected MyEditFormBase _editForm;
        protected MyEditContext _editContext;

        protected MyCssGridBase _cssgridTest { get; set; }

        protected TestEmployeeVM _employee { get; set; } = new()
        {
            Id = 0,
            Name = "Mike",
            Email = "Mike@test.com",
            PhotoPath = null,
            Department = Dept.HR,
            Domain = "test1.com",
            Gender = Gender.Female,
            Progress = 70,
            Files = new FileDataList
            {
                new() { Name = @"Test1", Extension = "png", Data = RandomUtils.RandomBytes(4), Position = 3 },
                new() { Name = @"Test2", Extension = "png", Data = RandomUtils.RandomBytes(5), Position = 3 }
            },
            Avatar = PathUtils.Combine(PathSeparator.BSlash, FileUtils.GetAspNetWwwRootDir<MyImageBase>(), "images/test-avatar.png").PathToFileData(true)
        };

        public string MediaQueryMessage { get; set; } = "Device Size not changed yet";

        protected override async Task OnInitializedAsync()
        {
            _editContext = new MyEditContext(_employee);
            await Task.CompletedTask;
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            _allControls = GetInputControls();
            _btnSave = _allControls.OfType<MyButtonBase>().SingleOrDefault(b => b.SubmitsForm.V == true);
            await SetControlStatesAsync(ComponentState.Enabled, _allControls);
        }

        protected async Task MediaQuery_ChangedAsync(MyMediaQueryChangedEventArgs e)
        {
            MediaQueryMessage = $"Device Size = {e.DeviceSize.EnumToString()}";
            await StateHasChangedAsync();
        }

        protected async Task BtnSubmit_ClickAsync(MyButtonBase sender, MouseEventArgs e, CancellationToken token)
        {
            await SetControlStatesAsync(ComponentState.Disabled, _allControls, _btnSave);
            if (!await _editContext.ValidateAsync())
                return;

            //await Task.Delay(5000);
            //await SetControlStatesAsync(ButtonState.Enabled, _allControls);
        }
    }
}
