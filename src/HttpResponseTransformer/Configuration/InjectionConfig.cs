using System;
using System.Collections.Immutable;

using Microsoft.AspNetCore.Http;

namespace HttpResponseTransformer.Configuration;

public record InjectionConfig
{
    public Func<HttpContext, bool> Predicate { get; init; } = _ => true;

    public ImmutableArray<ContentInjectionConfig> ContentInjectionConfigs { get; init; } = [];
}
