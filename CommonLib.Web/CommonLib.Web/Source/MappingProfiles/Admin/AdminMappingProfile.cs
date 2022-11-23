using System.Linq;
using AutoMapper;
using CommonLib.Web.Source.DbContext.Models.Account;
using CommonLib.Web.Source.ViewModels.Account;
using CommonLib.Web.Source.ViewModels.Admin;

namespace CommonLib.Web.Source.MappingProfiles.Admin
{
    public class AdminMappingProfile : Profile
    {
        public AdminMappingProfile()
        {            
            CreateMap<DbUser, AdminEditUserVM>()
                .ForMember(d => d.IsConfirmed, o => o.MapFrom(s => s.EmailConfirmed));
            CreateMap<AdminEditUserVM, DbUser>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.EmailConfirmed, o => o.MapFrom(s => s.IsConfirmed));
            CreateMap<FindUserVM, AdminEditUserVM>()
                .ForMember(d => d.Roles, o => o.Condition(s => s.Roles?.Any() == true))
                .ForMember(d => d.Roles, o => o.MapFrom(s => s.Roles.ToList()))
                .ForMember(d => d.Claims, o => o.Condition(s => s.Claims?.Any() == true))
                .ForMember(d => d.Claims, o => o.MapFrom(s => s.Claims.ToList()));;
            CreateMap<AdminEditUserVM, FindUserVM>()
                .ForMember(d => d.Roles, o => o.Condition(s => s.Roles?.Any() == true))
                .ForMember(d => d.Roles, o => o.MapFrom(s => s.Roles.ToList()))
                .ForMember(d => d.Claims, o => o.Condition(s => s.Claims?.Any() == true))
                .ForMember(d => d.Claims, o => o.MapFrom(s => s.Claims.ToList()));;
        }
    }
}
