using System;
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
            CreateMap<FindRoleVM, AdminEditRoleVM>();
            CreateMap<AdminEditRoleVM, IdentityRole<Guid>>();
        }
    }
}
