using System;
using System.Collections.Immutable;

using Microsoft.AspNetCore.Http;

namespace HttpResponseTransformer.Configuration;

public record ContentInjectionConfig
{
    public Func<HttpContext, bool> Predicate { get; init; } = _ => true;

    public ImmutableArray<InjectPageContentConfig> InjectContentConfigs { get; init; } = [];
}
