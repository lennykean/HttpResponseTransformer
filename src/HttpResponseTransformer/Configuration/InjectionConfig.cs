using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;

namespace HttpResponseTransformer.Configuration;

public record InjectionConfig
{
    public Func<HttpContext, bool> Predicate { get; init; } = _ => true;

    public IEnumerable<ContentInjectionConfig> ContentInjectionConfigs { get; init; } = [];
}
