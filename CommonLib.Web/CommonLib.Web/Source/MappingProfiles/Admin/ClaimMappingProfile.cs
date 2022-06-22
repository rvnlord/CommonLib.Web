using System;
using AutoMapper;
using CommonLib.Web.Source.ViewModels.Account;
using Microsoft.AspNetCore.Identity;

namespace CommonLib.Web.Source.MappingProfiles.Admin
{
    public class ClaimMappingProfile : Profile
    {
        public ClaimMappingProfile()
        {
            CreateMap<IdentityUserClaim<Guid>, FindClaimVM>();
            //CreateMap<FindClaimVM, AdminEditClaimVM>();
            //CreateMap<FindClaimValueVM, AdminEditClaimValueVM>();
            //CreateMap<AdminEditClaimVM, IdentityUserClaim<Guid>>();
        }
    }
}
