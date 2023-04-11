namespace CommonLib.Web.Source.ViewModels.Account
{
    public class ExternalLoginVM
    {
        public string ExternalUserName { get; set; }
        public string LoginProvider { get; set; }
        public bool Connected { get; set; } = true;
    }
}
