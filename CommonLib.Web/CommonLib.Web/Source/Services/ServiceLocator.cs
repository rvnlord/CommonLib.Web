using CommonLib.Web.Source.Services.Interfaces;

namespace CommonLib.Web.Source.Services
{
    public static class ServiceLocator
    {
        private static IServiceProviderProxy diProxy;

        public static IServiceProviderProxy ServiceProvider => diProxy;

        public static void Initialize(IServiceProviderProxy proxy)
        {
            diProxy = proxy;
        }
    }
}