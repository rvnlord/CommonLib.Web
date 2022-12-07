using System;
using System.Linq;
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
            CreateMap<AdminEditClaimVM, IdentityUserClaim<Guid>>();
            CreateMap<FindClaimValueVM, AdminEditClaimValueVM>()
                .ForMember(d => d.UserNames, o => o.Condition(s => s.UserNames?.Any() == true))
                .ForMember(d => d.UserNames, o => o.MapFrom(s => s.UserNames.ToList()));
            CreateMap<AdminEditClaimValueVM, FindClaimValueVM>()
                .ForMember(d => d.UserNames, o => o.Condition(s => s.UserNames?.Any() == true))
                .ForMember(d => d.UserNames, o => o.MapFrom(s => s.UserNames.ToList()));
            CreateMap<FindClaimVM, AdminEditClaimVM>()
                .ForMember(d => d.Values, o => o.Condition(s => s.Values?.Any() == true))
                .ForMember(d => d.Values, o => o.MapFrom(s => s.Values.ToList()));
            CreateMap<AdminEditClaimVM, FindClaimVM>()
                .ForMember(d => d.Values, o => o.Condition(s => s.Values?.Any() == true))
                .ForMember(d => d.Values, o => o.MapFrom(s => s.Values.ToList()));
        }
    }
}
