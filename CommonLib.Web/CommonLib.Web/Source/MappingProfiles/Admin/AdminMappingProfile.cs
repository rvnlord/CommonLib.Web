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
            CreateMap<DbUser, AdminEditUserVM>();
            CreateMap<FindUserVM, AdminEditUserVM>();
        }
    }
}
