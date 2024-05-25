using Microsoft.Extensions.DependencyInjection;

namespace ContainerFileSystemWatcher
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddContainerFileSystemWatcher(this IServiceCollection services)
        {
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<IContainerFileWatcher, ContainerFileWatcher>();
            return services;
        }
    }
}
