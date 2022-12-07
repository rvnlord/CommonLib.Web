using System;
using System.Linq;
using AutoMapper;
using CommonLib.Web.Source.ViewModels.Account;
using CommonLib.Web.Source.ViewModels.Admin;
using Microsoft.AspNetCore.Identity;

namespace CommonLib.Web.Source.MappingProfiles.Admin
{
    public class RoleMappingProfile : Profile
    {
        public RoleMappingProfile()
        {
            CreateMap<IdentityRole<Guid>, FindRoleVM>();
            CreateMap<AdminEditRoleVM, IdentityRole<Guid>>();
            CreateMap<FindRoleVM, AdminEditRoleVM>()
                .ForMember(d => d.UserNames, o => o.Condition(s => s.UserNames?.Any() == true))
                .ForMember(d => d.UserNames, o => o.MapFrom(s => s.UserNames.ToList()));
            CreateMap<AdminEditRoleVM, FindRoleVM>()
                .ForMember(d => d.UserNames, o => o.Condition(s => s.UserNames?.Any() == true))
                .ForMember(d => d.UserNames, o => o.MapFrom(s => s.UserNames.ToList()));
        }
    }
}
