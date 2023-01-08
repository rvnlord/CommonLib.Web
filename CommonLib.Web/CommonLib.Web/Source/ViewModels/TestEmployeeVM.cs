using System;
using System.Collections.Generic;
using System.ComponentModel;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.MyImageComponent;

namespace CommonLib.Web.Source.ViewModels
{
    public class TestEmployeeVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhotoPath { get; set; }
        public Dept? Department { get; set; }
        public string Domain { get; set; }
        public string Password { get; set; }
        public Gender Gender { get; set; }
        [DisplayName("Terms of Use")]
        [Description("I accept the Terms of use")]
        public bool TermsAccepted { get; set; }
        public FileData Avatar { get; set; }
        public double Progress { get; set; }
        public FileDataList Files { get; set; }
        public decimal? Salary { get; set; }
        [DisplayName("Date of Birth")]
        public DateTime? DateOfBirth { get; set; }
        [DisplayName("Available From")]
        public DateTime? AvailableFrom { get; set; }
        public NameWithImage Asset { get; set; }
        public List<NameWithImage> AvailableAssets { get; set; }
        public string Message { get; set; }
    }

    public enum Gender
    {
        Male,
        Female
    }

    public enum Dept
    {
        None,
        IT,
        HR,
        Payroll
    }
}
