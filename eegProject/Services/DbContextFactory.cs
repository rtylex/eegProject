using System.Data.Entity;

namespace eegProject.Services
{
    internal static class DbContextFactory
    {
        public static eegDBEntities Create()
        {
            var context = new eegDBEntities();
            context.Configuration.LazyLoadingEnabled = false;
            context.Configuration.ProxyCreationEnabled = false;
            return context;
        }
    }
}
