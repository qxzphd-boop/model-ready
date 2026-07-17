using System;
using System.Collections.Generic;

namespace ModelReady.Core;

public static class StudioSubmissionRuleSet
{
    public static IReadOnlyList<IModelReadyRule> Create()
    {
        return Array.AsReadOnly<IModelReadyRule>(new IModelReadyRule[]
        {
            new ExpectedUnitsRule(),
            new AbsoluteToleranceRule(),
            new InvalidGeometryRule(),
            new ExactDuplicatesRule(),
            new FarFromOriginRule(),
            new TinyGeometryRule(),
            new DefaultLayerRule(),
        });
    }
}
