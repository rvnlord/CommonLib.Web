using System.Linq;
using AutoMapper;
using CommonLib.Web.Source.Common.Converters;
using CommonLib.Web.Source.DbContext.Models.Account;
using CommonLib.Web.Source.ViewModels.Account;
using CommonLib.Web.Source.ViewModels.Admin;

namespace CommonLib.Web.Source.MappingProfiles.Account
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<DbUser, DbUser>();
            CreateMap<DbUser, RegisterUserVM>();
            CreateMap<DbUser, ConfirmUserVM>().ForAllMembers(o => o.Condition((_, _, m) => m is not null));
            CreateMap<DbUser, LoginUserVM>()
                .ForMember(d => d.IsConfirmed, o => o.MapFrom(s => s.EmailConfirmed));
            CreateMap<LoginUserVM, LoginUserVM>()
                .ForMember(d => d.ExternalLogins, o => o.Condition(s => s.ExternalLogins?.Any() == true))
                .ForMember(d => d.ExternalLogins, o => o.MapFrom(s => s.ExternalLogins.ToList()))
                .ForMember(d => d.WalletLogins, o => o.Condition(s => s.WalletLogins?.Any() == true))
                .ForMember(d => d.WalletLogins, o => o.MapFrom(s => s.WalletLogins.ToList()));
            CreateMap<AuthenticateUserVM, AuthenticateUserVM>()
                .ForMember(d => d.Avatar, o => o.Condition(s => s.Avatar is not null))
                .ForMember(d => d.Roles, o => o.Condition(s => s.Roles?.Any() == true))
                .ForMember(d => d.Roles, o => o.MapFrom(s => s.Roles.ToList()))
                .ForMember(d => d.Claims, o => o.Condition(s => s.Claims?.Any() == true))
                .ForMember(d => d.Claims, o => o.MapFrom(s => s.Claims.ToList()));
            CreateMap<AuthenticateUserVM, LoginUserVM>();
            CreateMap<DbUser, AuthenticateUserVM>();
            CreateMap<DbUser, ForgotPasswordUserVM>();
            CreateMap<ForgotPasswordUserVM, ForgotPasswordUserVM>();
            CreateMap<ConfirmUserVM, ConfirmUserVM>();
            CreateMap<DbUser, ResetPasswordUserVM>();
            CreateMap<ResetPasswordUserVM, ResetPasswordUserVM>();
            CreateMap<DbUser, ResendConfirmationEmailUserVM>();
            CreateMap<EditUserVM, ResendConfirmationEmailUserVM>();
            CreateMap<DbUser, EditUserVM>();
            CreateMap<EditUserVM, DbUser>();
            CreateMap<AuthenticateUserVM, EditUserVM>();
            CreateMap<EditUserVM, EditUserVM>() // if this is not set the mapping for same objects will work but will not update the existing dest object only create a new one
                .ForMember(d => d.ExternalLogins, o => o.Condition(s => s.ExternalLogins?.Any() == true))
                .ForMember(d => d.ExternalLogins, o => o.MapFrom(s => s.ExternalLogins.ToList()))
                .ForMember(d => d.Wallets, o => o.Condition(s => s.Wallets?.Any() == true))
                .ForMember(d => d.Wallets, o => o.MapFrom(s => s.Wallets.ToList()));
            CreateMap<DbUser, FindUserVM>()
                .ForMember(d => d.IsConfirmed, o => o.MapFrom(s => s.EmailConfirmed))
                .ForMember(d => d.Avatar, o => o.MapFrom(s => s.Avatar.ToFileDataOrNull()));
            CreateMap<DbUserLogin, ExternalLoginVM>()
                .ForMember(d => d.Provider, o => o.MapFrom(s => s.LoginProvider))
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.ExternalUserName))
                .ForMember(d => d.IsConnected, o => o.MapFrom(s => true));
            CreateMap<DbWallet, WalletVM>();
        }
    }
}
