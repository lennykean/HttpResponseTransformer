using System;
using System.Linq;
using System.Threading;

using HttpResponseTransformer;
using HttpResponseTransformer.Configuration.Builders;
using HttpResponseTransformer.Middleware;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

#pragma warning disable IDE0130
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add HTTP response transform services to the dependency injection container
    /// </summary>
    /// <param name="configure">Optional configuration function for setting up response transforms.</param>
    public static IServiceCollection AddResponseTransforms(this IServiceCollection services, Func<ResponseTransformBuilder, ResponseTransformBuilder>? configure)
    {
        var embeddedResourceManager = new EmbeddedResourceManager();

        services.AddSingleton<IStartupFilter, StartupFilter>(); ;
        services.AddSingleton<IEmbeddedResourceManager>(embeddedResourceManager);

        services.AddTransient<RequestTransformerMiddleware>();
        services.AddTransient<EmbeddedResourceMiddleware>();

        if (configure is not null)
        {
            configure(new(embeddedResourceManager)).Transforms.ToList().ForEach(t => services.AddSingleton(t));
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
                    next(builder.UseMiddleware<RequestTransformerMiddleware>());
                    builder.Map("/_", b => b.UseMiddleware<EmbeddedResourceMiddleware>());
                }
                else
                {
                    next(builder);
                }
            };
        }
    }
}

