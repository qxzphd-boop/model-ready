using System;
using System.Collections.Generic;

namespace ModelReady.Core;

internal static class Guard
{
    public static string NotBlank(string value, string parameterName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty or whitespace.", parameterName);
        }

        return value;
    }

    public static double NonNegativeFinite(double value, string parameterName)
    {
        if (value < 0 || double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite and non-negative.");
        }

        return value;
    }

    public static Guid NonEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be an empty GUID.", parameterName);
        }

        return value;
    }

    public static IReadOnlyList<T> ReadOnlyCopy<T>(IEnumerable<T> source, string parameterName, bool rejectNullItems)
    {
        if (source is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var copy = new List<T>();
        foreach (var item in source)
        {
            if (rejectNullItems && item is null)
            {
                throw new ArgumentException("Collection cannot contain null values.", parameterName);
            }

            copy.Add(item);
        }

        return copy.AsReadOnly();
    }
}
