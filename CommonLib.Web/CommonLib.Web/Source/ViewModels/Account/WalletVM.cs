using System;
using CommonLib.Source.Common.Converters;
using CommonLib.Source.Common.Extensions;

namespace CommonLib.Web.Source.ViewModels.Account
{
    public class WalletVM : IEquatable<WalletVM>
    {
        public string Provider { get; set; }
        public string Address { get; set; }
        public WalletSignatureVM DataSignature { get; set; }
        public int CurreentChainId { get; set; }
        public WalletVMConnectionStatus ConnectionStatus { get; set; }
        public string AddressWithProvider => $"{Address} ({Provider})";
        public string ShortenedAddressWithProvider => $"{(Address.IsHex() ? $"{Address.Take(6)}...{Address.TakeLast(4)}" : Address.IsBech32() ? $"{Address.BeforeLast("1")}1{Address.Take(4)}...{Address.TakeLast(4)}" : Address)} ({Provider})";
        public WalletVM Self => this;

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

    public enum WalletVMConnectionStatus
    {
        None,
        Connected,
        Connecting,
        Disconneting
    }
}
