// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildMagic.BuiltinTaskGenerator;

public class AppSettings
{
    public Dictionary<string /* SetterExpression */, ApiOptions> Apis { get; set; } = new();
    public string[] DictionaryKeyTypes { get; set; }

    public ApiOptions? GetApiOptions(ApiData api)
    {
        // appsettings.json cannot contain global:: prefix because Microsoft.Extensions.Configuration interprets colons as a separator.
        // details: https://github.com/dotnet/runtime/issues/67616

        var setterExpression = api.SetterExpression;
        if (setterExpression.StartsWith("global::", StringComparison.Ordinal))
            setterExpression = setterExpression["global::".Length..];

        if (Apis.TryGetValue(setterExpression, out var result) && result.MatchParameterTypes(api))
            return result;

        return null;
    }
}

public record ApiOptions
{
    public string[] ParameterTypes { get; set; }
    public bool? Ignored { get; set; }
    public string? OverrideDisplayName { get; set; }

    public bool MatchParameterTypes(ApiData api)
    {
        return ParameterTypes.SequenceEqual(api.Parameters.Select(p => p.TypeExpression));
    }
}
