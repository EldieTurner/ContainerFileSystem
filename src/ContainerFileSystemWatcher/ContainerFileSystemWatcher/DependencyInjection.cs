using Microsoft.Extensions.DependencyInjection;

namespace ContainerFileSystemWatcher
{
    /// <summary>
    /// Provides extension methods for dependency injection.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds the container file system watcher services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddContainerFileSystemWatcher(this IServiceCollection services)
        {
            services.AddSingleton<IFileSystemShim, FileSystemShim>();
            services.AddSingleton<IContainerFileWatcher, ContainerFileWatcher>();
            return services;
        }
    }
}
