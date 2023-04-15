using System;

namespace CommonLib.Web.Source.ViewModels.Account
{
    public class ExternalLoginVM : IEquatable<ExternalLoginVM>
    {
        public string UserName { get; set; }
        public string Provider { get; set; }
        public bool IsConnected { get; set; }

        public bool Equals(ExternalLoginVM other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(UserName, other.UserName, StringComparison.InvariantCultureIgnoreCase) && string.Equals(Provider, other.Provider, StringComparison.InvariantCultureIgnoreCase) && IsConnected == other.IsConnected;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ExternalLoginVM)obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(UserName, StringComparer.InvariantCultureIgnoreCase);
            hashCode.Add(Provider, StringComparer.InvariantCultureIgnoreCase);
            hashCode.Add(IsConnected);
            return hashCode.ToHashCode();
        }

        public static bool operator ==(ExternalLoginVM left, ExternalLoginVM right) => Equals(left, right);
        public static bool operator !=(ExternalLoginVM left, ExternalLoginVM right) => !Equals(left, right);
    }
}
