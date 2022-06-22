using System.Threading.Tasks;
using CommonLib.Source.Models.Interfaces;
using CommonLib.Web.Source.Models.Interfaces;

namespace CommonLib.Web.Source.Services.Account.Interfaces
{
    public interface IEmailSender
    {
        Task<IApiResponse> SendConfirmationEmailAsync(string email, string code, string returnUrl);
        Task<IApiResponse> SendPasswordResetEmailAsync(string email, string code, string returnUrl);
    }
}
