using System.Text.Json.Serialization;
using Lupira.Primitives;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Lupira.Hosting.Defaults;

public static class DefaultsHostApplicationBuilderExtensions
{
    /// <summary>JSON contract (enum names, strict numbers), optional <c>ThrowOnBadRequest</c>, persisted
    /// data-protection keys; pair with <see cref="DefaultsApplicationBuilderExtensions.UseLupiraDefaults"/>.</summary>
    public static IHostApplicationBuilder AddLupiraDefaults(this IHostApplicationBuilder builder, Action<LupiraDefaultsOptions>? configure = null)
    {
        var options = new LupiraDefaultsOptions();
        configure?.Invoke(options);
        builder.Services.AddSingleton(Options.Create(options));

        builder.Services.ConfigureHttpJsonOptions(o =>
        {
            if (options.StrictNumbers)
                o.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
            o.SerializerOptions.PropertyNameCaseInsensitive = options.CaseInsensitiveProperties;
            o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            if (options.UtcDateTimeOffsets)
                o.SerializerOptions.Converters.Add(new UtcDateTimeOffsetConverter());
        });

        if (options.ThrowOnBadRequest)
            builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);

        if (options.StatusCodePages)
            builder.Services.AddProblemDetails();

        // Persist data-protection keys so the auth cookie survives container restarts (mount DataProtection:KeyPath).
        var keyPath = builder.Configuration[LupiraDefaultsOptions.DataProtectionKeyPathKey];
        if (!string.IsNullOrWhiteSpace(keyPath))
        {
            builder.Services.AddDataProtection()
                .SetApplicationName(options.DataProtectionApplicationName ?? builder.Environment.ApplicationName)
                .PersistKeysToFileSystem(new DirectoryInfo(keyPath));
        }

        return builder;
    }
}
