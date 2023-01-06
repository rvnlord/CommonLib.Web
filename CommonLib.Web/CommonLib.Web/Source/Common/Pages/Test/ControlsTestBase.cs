using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Blazored.FluentValidation;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.TypeUtils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.ExtEditorComponent;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyCssGridComponent;
using CommonLib.Web.Source.Common.Components.MyEditFormComponent;
using CommonLib.Web.Source.Common.Components.MyFileUploadComponent;
using CommonLib.Web.Source.Common.Components.MyFluentValidatorComponent;
using CommonLib.Web.Source.Common.Components.MyImageComponent;
using CommonLib.Web.Source.Common.Components.MyInputGroupComponent;
using CommonLib.Web.Source.Common.Components.MyMediaQueryComponent;
using CommonLib.Web.Source.Common.Components.MyProgressBarComponent;
using CommonLib.Web.Source.Common.Extensions;
using CommonLib.Web.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Services;
using CommonLib.Web.Source.Services.Interfaces;
using CommonLib.Web.Source.Validators;
using CommonLib.Web.Source.ViewModels;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MoreLinq;
using Org.BouncyCastle.Security;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Editor;

namespace CommonLib.Web.Source.Common.Pages.Test
{
    public class ControlsTestBase : MyComponentBase
    {
        private IComponent[] _allControls;
        private MyButtonBase _btnSave;
        private static readonly string _testImgsDir = FileUtils.GetAspNetWwwRootDir<MyImageBase>();

        protected MyFluentValidatorBase _validator;
        protected FluentValidationValidator _fvTestDataValidator;
        protected MyEditFormBase _editForm;
        protected MyEditContext _editContext;
        protected TelerikDatePicker<DateTime?> _tdpDateOfBirth;
        protected Guid _tdpDateOfBirthGuid;
        protected TelerikDateTimePicker<DateTime?> _tdtpAvailableFrom;
        protected Guid _tdtpAvailableFromGuid;
        protected TelerikAutoComplete<TestAsset> _tacAsset;
        protected Guid _tacAssetGuid;
        protected TelerikGrid<TestDataVM> _gvTestData;
        protected TelerikRadialGauge _gTest;
        protected Guid _gTestGuid;
        protected ExtEditor<string> _teMessage;

        protected string _syncPaddingGroup;

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
            Avatar = PathUtils.Combine(PathSeparator.BSlash, _testImgsDir, "images/test-avatar.png").PathToFileData(true),
            AvailableAssets = new()
            {
                new TestAsset { Name = "BTC", Image = PathUtils.Combine(PathSeparator.BSlash, _testImgsDir, "images/test-assets/bitcoin.png").PathToFileData(true) },
                new TestAsset { Name = "ETH", Image = PathUtils.Combine(PathSeparator.BSlash, _testImgsDir, "images/test-assets/ethereum.png").PathToFileData(true) },
                new TestAsset { Name = "LTC", Image = PathUtils.Combine(PathSeparator.BSlash, _testImgsDir, "images/test-assets/litecoin.png").PathToFileData(true) },
                new TestAsset { Name = "NEO", Image = PathUtils.Combine(PathSeparator.BSlash, _testImgsDir, "images/test-assets/neo.png").PathToFileData(true) },
                new TestAsset { Name = "XMR", Image = PathUtils.Combine(PathSeparator.BSlash, _testImgsDir, "images/test-assets/monero.png").PathToFileData(true) },
                new TestAsset { Name = "XTZ", Image = PathUtils.Combine(PathSeparator.BSlash, _testImgsDir, "images/test-assets/tezos.png").PathToFileData(true) },
                new TestAsset { Name = "DOGE", Image = PathUtils.Combine(PathSeparator.BSlash, _testImgsDir, "images/test-assets/dogecoin.png").PathToFileData(true) },
                new TestAsset { Name = "ETC", Image = PathUtils.Combine(PathSeparator.BSlash, _testImgsDir, "images/test-assets/ethereum-classic.png").PathToFileData(true) }
            }
        };

        public string MediaQueryMessage { get; set; } = "Device Size not changed yet";

        [Inject]
        public IJQueryService JQuery { get; set; }

        protected override async Task OnInitializedAsync()
        {
            _editContext = new MyEditContext(_employee);
            _tdpDateOfBirthGuid = _tdpDateOfBirthGuid == Guid.Empty ? Guid.NewGuid() : _tdpDateOfBirthGuid;
            _tdtpAvailableFromGuid = _tdtpAvailableFromGuid == Guid.Empty ? Guid.NewGuid() : _tdtpAvailableFromGuid;
            _tacAssetGuid = _tacAssetGuid == Guid.Empty ? Guid.NewGuid() : _tacAssetGuid;
            _gTestGuid = _gTestGuid == Guid.Empty ? Guid.NewGuid() : _gTestGuid;
            _syncPaddingGroup = "controls-test-panel";
            await UpdateGvTestDataAsync();
            await Task.CompletedTask;
        }

        protected override async Task OnAfterFirstRenderAsync()
        {
            // fix sync padding group for every non-native input
            await FixInputSyncPaddingGroupAsync(_tdpDateOfBirthGuid);
            _editContext.BindValidationStateChangedForNonNativeComponent(_tdpDateOfBirth, () => _employee.DateOfBirth, this);
            await FixInputSyncPaddingGroupAsync(_tdtpAvailableFromGuid);
            _editContext.BindValidationStateChangedForNonNativeComponent(_tdtpAvailableFrom, () => _employee.AvailableFrom, this);
            await FixInputSyncPaddingGroupAsync(_tacAssetGuid);
            _editContext.BindValidationStateChangedForNonNativeComponent(_tacAsset, () => _employee.Asset, this);
            
            _allControls = GetInputControls().Cast<IComponent>().Concat(_tdpDateOfBirth, _tdtpAvailableFrom, _tacAsset).ToArray();
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
            {
                await SetControlStatesAsync(ComponentState.Enabled, _allControls.Where(c => c is MyInputGroupBase)); // only input-groups on validation, validation itsdelf is taking care of non-native components' correct state
                return;
            }

            //await Task.Delay(5000);
            //await SetControlStatesAsync(ButtonState.Enabled, _allControls);
        }


        protected readonly TestDataVMValidator _testDataValidator = new();
        public TestDataManager TestDataManager { get; } = new();
        public List<TestDataVM> MyData { get; set; }

        protected async Task GvTestData_EditAsync(GridCommandEventArgs e)
        {
            var item = (TestDataVM) e.Item;
            
            //if (item.Id < 3) // prevent opening for edit based on condition
            //    e.IsCancelled = true; // the general approach for cancelling an event

            await Task.CompletedTask;
        }

        protected async Task GvTestData_UpdateAsync(GridCommandEventArgs e)
        {
            var item = (TestDataVM)e.Item;
            
            await TestDataManager.UpdateAsync(item);
            await UpdateGvTestDataAsync();

            Console.WriteLine("Update event is fired.");
        }

        protected async Task GvTestData_DeleteAsync(GridCommandEventArgs e)
        {
            var item = (TestDataVM)e.Item;
            
            await TestDataManager.DeleteAsync(item);
            await UpdateGvTestDataAsync();

            Console.WriteLine("Delete event is fired.");
        }

        protected async Task GvTestData_CreateAsync(GridCommandEventArgs e)
        {
            var item = (TestDataVM) e.Item;
            
            await TestDataManager.CreateAsync(item);
            await UpdateGvTestDataAsync();

            Console.WriteLine("Create event is fired.");
        }

        protected async Task GvTestData_CancelAsync(GridCommandEventArgs e)
        {
            var item = (TestDataVM) e.Item;

            // if necessary, perform actual data source operation here through your service
            await Task.CompletedTask;
        }
        
        private async Task UpdateGvTestDataAsync()
        {
            MyData = await TestDataManager.ReadAsync();
        }
    }
}
