using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using CommonLib.Web.Source.DbContext;
using CommonLib.Web.Source.Models;
using CommonLib.Web.Source.Models.Interfaces;
using CommonLib.Web.Source.Services.Account.Interfaces;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils;
using CommonLib.Source.Models;
using CommonLib.Source.Models.Interfaces;
using CommonLib.Web.Source.DbContext.Models.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CommonLib.Web.Source.Services.Account
{
    public class EmailSender : IEmailSender
    {
        private readonly UserManager<DbUser> _userManager;
        private readonly IConfiguration _config;
        private readonly AccountDbContext _db;

        public EmailSender(UserManager<DbUser> userManager, IConfiguration config, AccountDbContext db)
        {
            _userManager = userManager;
            _config = config;
            _db = db;
        }

        public async Task<IApiResponse> SendConfirmationEmailAsync(string email, string code, string returnUrl)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            var verifyUrl = $"{ConfigUtils.FrontendBaseUrl}Account/ConfirmEmail?email={email.UTF8ToBase58()}&code={code.UTF8ToBase58(false)}&returnUrl={returnUrl.UTF8ToBase58()}"; // can't be `{_http.HttpContext.Request.Scheme}://{_http.HttpContext.Request.Host}{_http.HttpContext.Request.PathBase}` because the backend address is different, on local machine it is just a port, on a server it might be a completely different domain

            var sbEmailBody = new StringBuilder();
            sbEmailBody.Append("Hello " + user.UserName + ",<br/><br/>");
            sbEmailBody.Append("You have asked for activation in our Web App. You can activate your account either by providing the activation code directly or by clicking the following link.");
            sbEmailBody.Append("<br/><br/>");
            sbEmailBody.Append("Activation Link:");
            sbEmailBody.Append("<br/>");
            sbEmailBody.Append($"<a href='{verifyUrl}'>{verifyUrl}</a></b>");
            sbEmailBody.Append("<br/><br/>");
            sbEmailBody.Append("Activation Code:");
            sbEmailBody.Append("<br/>");
            sbEmailBody.Append("<b>" + code.UTF8ToBase58(false) + "</b>");
            sbEmailBody.Append("<br/><br/>");
            sbEmailBody.Append("Cheers");
            sbEmailBody.Append("<br/>");
            sbEmailBody.Append("Crimson Relays");

            return await SendEmailAsync(user, "Crimson Relays - Confirmation Email", sbEmailBody.ToString());
        }

        public async Task<IApiResponse> SendPasswordResetEmailAsync(string email, string code, string returnUrl)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            var resetPasswordurl = $"{ConfigUtils.FrontendBaseUrl}Account/ResetPassword?email={email.UTF8ToBase58()}&code={code.UTF8ToBase58(false)}&returnUrl={returnUrl.UTF8ToBase58()}";

            var sbEmailBody = new StringBuilder();
            sbEmailBody.Append("Hello " + user.UserName + ",<br/><br/>");
            sbEmailBody.Append("You have asked for password reset in our Web App. You can do so with the code below or by following the Password Reset Link.");
            sbEmailBody.Append("<br/><br/>");
            sbEmailBody.Append("Reset Password Link:");
            sbEmailBody.Append("<br/>");
            sbEmailBody.Append($"<a href='{resetPasswordurl}'>{resetPasswordurl}</a></b>");
            sbEmailBody.Append("<br/><br/>");
            sbEmailBody.Append("Reset Password Code:");
            sbEmailBody.Append("<br/>");
            sbEmailBody.Append("<b>" + code.UTF8ToBase58(false) + "</b>");
            sbEmailBody.Append("<br/><br/>");
            sbEmailBody.Append("Cheers");
            sbEmailBody.Append("<br/>");
            sbEmailBody.Append("Crimson Relays");

            return await SendEmailAsync(user, "Crimson Relays - Reset Password", sbEmailBody.ToString());
        }

        private async Task<IApiResponse> SendEmailAsync(DbUser user, string subject, string content)
        {
            try
            {
                var emailConfig = _config.GetSection("Email");
                var from = emailConfig.GetSection("From").Value;
                var host = emailConfig.GetSection("Host").Value;
                var port = Convert.ToInt32(emailConfig.GetSection("Port").Value);
                var userName = emailConfig.GetSection("UserName").Value;
                var password = emailConfig.GetSection("Password").Value;
                var ssl = Convert.ToBoolean(emailConfig.GetSection("SSL").Value);

                var passwordKey = (await _db.CryptographyKeys.AsNoTracking().FirstOrDefaultAsync(k => k.Name == "EmailPassword"))?.Value;
                var decryptedPassword = passwordKey == null 
                    ? password 
                    : password.Base58ToByteArray().DecryptCamellia(passwordKey.Base58ToByteArray()).ToUTF8String();

                var key = CryptoUtils.GenerateCamelliaKey();
                var encryptedPassword = decryptedPassword.UTF8ToByteArray().EncryptCamellia(key).ToBase58String();
                _db.CryptographyKeys.AddOrUpdate(new DbCryptographyKey { Name = "EmailPassword", Value = key.ToBase58String() }, e => e.Name);
                await _db.SaveChangesAsync();
                await ConfigUtils.SetAppSettingValueAsync("Email:Password", encryptedPassword);

                var mailMessage = new MailMessage(from, user.Email)
                {
                    IsBodyHtml = true,
                    Body = content,
                    Subject = subject
                };

                var smtp = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential { UserName = userName, Password = decryptedPassword },
                    EnableSsl = ssl
                };

                await smtp.SendMailAsync(mailMessage); // if this throws 5.7.0, go here: https://g.co/allowaccess; enable less secure apps, click critical alert - it was me

                return new ApiResponse(StatusCodeType.Status200OK, "Email Sent Successfully", null);
            }
            catch (Exception ex)
            {
                return new ApiResponse(StatusCodeType.Status500InternalServerError, "Sending Email Failed", null, null, ex);
            }
        }
    }
}
