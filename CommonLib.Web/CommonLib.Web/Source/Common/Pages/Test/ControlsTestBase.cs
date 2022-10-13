using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components;
using CommonLib.Web.Source.Common.Components.MyButtonComponent;
using CommonLib.Web.Source.Common.Components.MyCssGridComponent;
using CommonLib.Web.Source.Common.Components.MyMediaQueryComponent;
using CommonLib.Web.Source.ViewModels;
using MoreLinq;
using Org.BouncyCastle.Security;

namespace CommonLib.Web.Source.Common.Pages.Test
{
    public class ControlsTestBase : MyComponentBase
    {
        private MyComponentBase[] _allControls;
        private MyButtonBase _btnSave;

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
            Files = new List<FileData>
            {
                new() { Path = @"C:\Test1.png", Data = RandomUtils.RandomBytes(4), Position = 3 },
                new() { Path = @"C:\Test2.png", Data = RandomUtils.RandomBytes(5), Position = 3 }
            }
        };

        public string MediaQueryMessage { get; set; } = "Device Size not changed yet";
        
        protected override async Task OnAfterFirstRenderAsync()
        {
            _allControls = GetInputControls();
            _btnSave = _allControls.OfType<MyButtonBase>().SingleOrDefault(b => b.SubmitsForm.V == true);
            await SetControlStatesAsync(ButtonState.Enabled, _allControls);
        }

        protected async Task MediaQuery_ChangedAsync(MyMediaQueryChangedEventArgs e)
        {
            MediaQueryMessage = $"Device Size = {e.DeviceSize.EnumToString()}";
            await StateHasChangedAsync();
        }
    }
}
