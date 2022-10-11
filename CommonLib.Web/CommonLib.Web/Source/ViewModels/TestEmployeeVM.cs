using System.ComponentModel;

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
        public string Avatar { get; set; }
        public double Progress { get; set; }
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
