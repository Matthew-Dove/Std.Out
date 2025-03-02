using Microsoft.Extensions.DependencyInjection;

namespace Std.Out
{
    public static class StdOutExtensions
    {
        /// <summary>Adds stdout services to the host's DI container.</summary>
        public static IServiceCollection AddStdOut(this IServiceCollection services)
        {
            services.AddSingleton<IStdOut, StdOut>();
            return services;
        }
    }
}
