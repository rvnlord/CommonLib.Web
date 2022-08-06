using System;
using System.Collections.Generic;
using System.Linq;
using CommonLib.Source.Common.Extensions;

namespace CommonLib.Web.Source.ViewModels.Account
{
    public class AuthenticateUserVM : IEquatable<AuthenticateUserVM>
    {
        public Guid Id { get; set; }
        public bool IsAuthenticated { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public List<FindRoleVM> Roles { get; set; } = new();
        public List<FindClaimVM> Claims { get; set; } = new();
        public string Ticket { get; set; }
        public bool HasPassword { get; set; }
        public bool RememberMe { get; set; }

        public bool HasRole(string role) => Roles.Any(r => r.Name.EqualsInvariant(role));
        public bool HasClaim(string claim) => Claims.Any(c => c.Name.EqualsInvariant(claim));

        public static AuthenticateUserVM NotAuthenticated => new() { IsAuthenticated = false };

        public override bool Equals(object o)
        {
            return Equals(o as AuthenticateUserVM);
        }

        public bool Equals(AuthenticateUserVM other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id.Equals(other.Id) && UserName == other.UserName && Email == other.Email;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, UserName, Email);
        }

        public static bool operator ==(AuthenticateUserVM left, AuthenticateUserVM right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(AuthenticateUserVM left, AuthenticateUserVM right)
        {
            return !Equals(left, right);
        }
    }
}
