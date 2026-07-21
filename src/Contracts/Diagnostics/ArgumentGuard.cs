using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace BIMCapabilities.Contracts.Diagnostics;

public static class ArgumentGuard
{
    public static void ThrowIfNull(object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }

    public static void ThrowIfNullOrWhiteSpace(
        string? argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", paramName);
        }
    }
}
