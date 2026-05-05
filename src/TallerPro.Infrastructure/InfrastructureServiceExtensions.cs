using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TallerPro.Infrastructure.Persistence;
using TallerPro.Infrastructure.Persistence.Interceptors;

namespace TallerPro.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Stateless interceptors registered as singletons — one instance shared across all DbContext lifetimes.
        services.AddSingleton<SoftDeleteInterceptor>();
        services.AddSingleton<FundationalTenantGuard>();
        services.AddSingleton<TenantStampInterceptor>();

        // AuditingInterceptor depends on ICurrentUser which is Scoped, so it must be Scoped as well.
        services.AddScoped<AuditingInterceptor>();

        services.AddDbContext<TallerProDbContext>((sp, opts) =>
        {
            var connStr = configuration.GetConnectionString("Default")
                ?? Environment.GetEnvironmentVariable("TALLERPRO_DB_CONNECTION")
                ?? throw new InvalidOperationException(
                    "Connection string not configured. Set ConnectionStrings:Default or TALLERPRO_DB_CONNECTION.");

            opts.UseSqlServer(connStr);
            opts.AddInterceptors(
                sp.GetRequiredService<SoftDeleteInterceptor>(),
                sp.GetRequiredService<AuditingInterceptor>(),
                sp.GetRequiredService<FundationalTenantGuard>(),
                sp.GetRequiredService<TenantStampInterceptor>());
        });

        return services;
    }
}
