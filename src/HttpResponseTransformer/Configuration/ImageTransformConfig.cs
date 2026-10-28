using System;
using System.Collections.Generic;

using HttpResponseTransformer.Configuration.Enums;
using HttpResponseTransformer.Transforms;

namespace HttpResponseTransformer.Configuration;

public record ImageTransformConfig
{
    internal Func<ImageTransformInfo, bool> Predicate { get; init; } = _ => true;

    internal IEnumerable<Func<RawImage, RawImage>> Operations { get; init; } = [];

    internal ImageFormat? Format { get; init; }

    internal int Quality { get; init; } = 90;
}
