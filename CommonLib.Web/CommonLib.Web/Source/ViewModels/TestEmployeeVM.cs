﻿using System.Collections.Generic;
using System.ComponentModel;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.Common.Components.MyImageComponent;

namespace CommonLib.Web.Source.ViewModels
{
    public class TestEmployeeVM
    {
        private ExtendedImage _avatar;

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
        public ExtendedImage Avatar => _avatar ??= ExtendedImage.Load(PathUtils.Combine(PathSeparator.BSlash, FileUtils.GetAspNetWwwRootDir<MyImageBase>(), "images/test-avatar.png"));

        public double Progress { get; set; }
        public List<FileData> Files { get; set; }
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
