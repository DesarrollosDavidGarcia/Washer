using System.Globalization;
using Serilog;
using TallerPro.Api.Middleware;
using TallerPro.Api.Security;
using TallerPro.Infrastructure;
using TallerPro.Infrastructure.Logging;
using TallerPro.Infrastructure.Persistence;
using TallerPro.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .Destructure.With(new PiiMaskingPolicy())
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture));

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ITenantContext, AmbientTenantContext>();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (args.Contains("--seed-dev"))
{
    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("--seed-dev is only allowed in the Development environment.");
    }

    using var scope = app.Services.CreateScope();
    var ctx = scope.ServiceProvider.GetRequiredService<TallerProDbContext>();
    await TallerPro.Infrastructure.Persistence.DevSeeder.SeedAsync(ctx);
    return;
}

app.UseMiddleware<TenantResolutionMiddleware>();

app.Run();
