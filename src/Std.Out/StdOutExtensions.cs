using Std.Out.Core.Models.Config;
using Std.Out.Core.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class StdOutExtensions
    {
        /// <summary>Adds stdout services to the host's DI container.</summary>
        public static IServiceCollection AddStdOutServices(this IServiceCollection services, Action<StdConfigOptions> options = null)
        {
            if (options != null) services.Configure(options);

            return services
                .AddSingleton<Std.Out.Infrastructure.IMarker, Std.Out.Infrastructure.Marker>()
                .AddSingleton<Std.Out.Core.IMarker, Std.Out.Core.Marker>()
                .AddSingleton<IStdOut, StdOut>()
                .AddSingleton<IDiskStorage, DiskStorage>()
                .AddSingleton<IS3Storage, S3Storage>()
                .AddSingleton<IS3Service, S3Service>()
                .AddSingleton<IDynamodbStorage, DynamodbStorage>()
                .AddSingleton<IDynamodbService, DynamodbService>()
                ;
        }
    }
}
