using System;
using AutoMapper;
using CommonLib.Web.Source.ViewModels.Account;
using CommonLib.Web.Source.ViewModels.Admin;
using Microsoft.AspNetCore.Identity;

namespace CommonLib.Web.Source.MappingProfiles.Admin
{
    public class ClaimMappingProfile : Profile
    {
        public ClaimMappingProfile()
        {
            CreateMap<IdentityUserClaim<Guid>, FindClaimVM>();
            CreateMap<FindClaimValueVM, AdminEditClaimValueVM>();
            CreateMap<AdminEditClaimVM, IdentityUserClaim<Guid>>();
            CreateMap<FindClaimVM, AdminEditClaimVM>();
            CreateMap<AdminEditClaimVM, FindClaimVM>();
        }
    }
}
