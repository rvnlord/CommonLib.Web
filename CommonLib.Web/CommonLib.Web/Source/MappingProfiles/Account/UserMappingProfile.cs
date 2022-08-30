using System.Linq;
using AutoMapper;
using CommonLib.Web.Source.Models.Account;
using CommonLib.Web.Source.ViewModels.Account;

namespace CommonLib.Web.Source.MappingProfiles.Account
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<User, User>();
            CreateMap<User, RegisterUserVM>();
            CreateMap<User, ConfirmUserVM>();
            CreateMap<User, LoginUserVM>();
            CreateMap<LoginUserVM, LoginUserVM>()
                .ForMember(d => d.ExternalLogins, o => o.Condition(s => s.ExternalLogins?.Any() == true))
                .ForMember(d => d.ExternalLogins, o => o.MapFrom(s => s.ExternalLogins.ToList()));
            CreateMap<AuthenticateUserVM, LoginUserVM>();
            CreateMap<User, AuthenticateUserVM>();
            //CreateMap<User, ForgotPasswordUserVM>();
            //CreateMap<User, ResetPasswordUserVM>();
            CreateMap<User, ResendConfirmationEmailUserVM>();
            //CreateMap<EditUserVM, ResendConfirmationEmailUserVM>();
            //CreateMap<User, EditUserVM>();
            //CreateMap<EditUserVM, User>();
            //CreateMap<AuthenticateUserVM, EditUserVM>();
            //CreateMap<EditUserVM, EditUserVM>(); // if this is not set the mapping for same objects will work but will not update the existing dest object only create a new one
            //CreateMap<User, AdminEditUserVM>()
            //    .ForMember(d => d.IsConfirmed, o => o.MapFrom(s => s.EmailConfirmed));
            //CreateMap<AdminEditUserVM, User>()
            //    .ForMember(d => d.Id, o => o.Ignore())
            //    .ForMember(d => d.EmailConfirmed, o => o.MapFrom(s => s.IsConfirmed));
            CreateMap<User, FindUserVM>()
                .ForMember(d => d.IsConfirmed, o => o.MapFrom(s => s.EmailConfirmed));
            //CreateMap<FindUserVM, AdminEditUserVM>();
            CreateMap<User, FindUserVM>()
                .ForMember(d => d.IsConfirmed, o => o.MapFrom(s => s.EmailConfirmed));
        }
    }
}
