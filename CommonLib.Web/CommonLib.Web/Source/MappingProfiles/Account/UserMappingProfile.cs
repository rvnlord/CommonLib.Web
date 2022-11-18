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
                .ForMember(d => d.ExternalLogins, o => o.MapFrom(s => s.ExternalLogins.ToList()));
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
            CreateMap<EditUserVM, EditUserVM>(); // if this is not set the mapping for same objects will work but will not update the existing dest object only create a new one
            CreateMap<DbUser, AdminEditUserVM>()
                .ForMember(d => d.IsConfirmed, o => o.MapFrom(s => s.EmailConfirmed));
            CreateMap<AdminEditUserVM, DbUser>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.EmailConfirmed, o => o.MapFrom(s => s.IsConfirmed));
            CreateMap<DbUser, FindUserVM>()
                .ForMember(d => d.IsConfirmed, o => o.MapFrom(s => s.EmailConfirmed))
                .ForMember(d => d.Avatar, o => o.MapFrom(s => s.Avatar.ToFileDataOrNull()));
            CreateMap<FindUserVM, AdminEditUserVM>();
        }
    }
}
