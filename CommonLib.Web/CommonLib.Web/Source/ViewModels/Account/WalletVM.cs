using System;

namespace CommonLib.Web.Source.ViewModels.Account
{
    public class WalletVM : IEquatable<WalletVM>
    {
        public string Provider { get; set; }
        public string Address { get; set; }

        public bool Equals(WalletVM other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Provider, other.Provider, StringComparison.InvariantCultureIgnoreCase) && string.Equals(Address, other.Address, StringComparison.InvariantCultureIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((WalletVM)obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Provider, StringComparer.InvariantCultureIgnoreCase);
            hashCode.Add(Address, StringComparer.InvariantCultureIgnoreCase);
            return hashCode.ToHashCode();
        }

        public static bool operator ==(WalletVM left, WalletVM right) => Equals(left, right);
        public static bool operator !=(WalletVM left, WalletVM right) => !Equals(left, right);
    }
}
