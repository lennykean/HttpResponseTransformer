using System;
using System.Collections.Generic;
using System.Threading;

using HttpResponseTransformer;
using HttpResponseTransformer.Configuration;
using HttpResponseTransformer.Configuration.Builders;
using HttpResponseTransformer.Middleware;
using HttpResponseTransformer.Transforms;

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
    public static IServiceCollection AddResponseTransformer(this IServiceCollection services, Func<ResponseTransformBuilder, ResponseTransformBuilder>? configure = default)
    {
        services.TryAddTransient<GlobalResponseTransformerMiddleware>();
        services.AddTransient<IStartupFilter, GlobalStartupFilter>();

        if (configure is not null)
        {
            services.AddTransient<IStartupFilter>(_ =>
            {
                var resourceManager = new EmbeddedResourceManager();
                var builder = configure(new(resourceManager));

                return new ScopedStartupFilter(resourceManager, builder.Transforms, builder.Config);
            });
        }
        return services;
    }


    private class GlobalStartupFilter : IStartupFilter
    {
        private static int _registered = 0;

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder =>
            {
                if (Interlocked.CompareExchange(ref _registered, value: 1, comparand: 0) == 0)
                {
                    builder.UseMiddleware<GlobalResponseTransformerMiddleware>();
                }
                next(builder);
            };
        }
    }

    private class ScopedStartupFilter(IEmbeddedResourceManager resourceManager, IEnumerable<IResponseTransform> transforms, ResponseTransformerConfig config) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder =>
            {
                builder.UseMiddleware<ScopedResponseTransformerMiddleware>(transforms, config);
                builder.UseMiddleware<EmbeddedResourceMiddleware>(resourceManager);

                next(builder);
            };
        }
    }
}

