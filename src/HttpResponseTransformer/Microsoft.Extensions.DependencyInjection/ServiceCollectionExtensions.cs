using System;
using System.Threading;

using HttpResponseTransformer;
using HttpResponseTransformer.Configuration.Builders;
using HttpResponseTransformer.Middleware;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;

#pragma warning disable IDE0130
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add HTTP response transform services to the dependency injection container
    /// </summary>
    /// <param name="configure">Optional configuration function for setting up response transforms.</param>
    public static IServiceCollection AddResponseTransformer(this IServiceCollection services, Func<ResponseTransformBuilder, ResponseTransformBuilder>? configure)
    {
        var embeddedResourceManager = new EmbeddedResourceManager();

        services.TryAddSingleton<IEmbeddedResourceManager>(embeddedResourceManager);

        services.TryAddTransient<ResponseTransformerMiddleware>();
        services.TryAddTransient<EmbeddedResourceMiddleware>();

        services.AddTransient<IStartupFilter, StartupFilter>();

        if (configure is not null)
        {
            var builder = configure(new(embeddedResourceManager));
            services.AddSingleton(builder.Config);
            foreach (var transform in builder.Transforms)
            {
                services.AddSingleton(transform);
            }
        }
        return services;
    }

    private class StartupFilter : IStartupFilter
    {
        private int _isConfigured = 0;

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder =>
            {
                if (Interlocked.CompareExchange(ref _isConfigured, value: 1, comparand: 0) == 0)
                {
                    builder
                       .UseMiddleware<ResponseTransformerMiddleware>()
                       .MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/_"), b => b.UseMiddleware<EmbeddedResourceMiddleware>());
                }
                next(builder);
            };
        }
    }
}

