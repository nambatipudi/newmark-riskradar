namespace Newmark.RiskRadar.Domain.Exceptions;

/// <summary>
/// Raised when a financial calculation is handed an input that has no economic meaning,
/// such as a zero or negative denominator. Callers get a named parameter and a reason
/// rather than a <see cref="DivideByZeroException"/> from deep inside the math.
/// </summary>
public sealed class InvalidFinancialInputException : Exception
{
    public InvalidFinancialInputException(string parameterName, string message)
        : base(message) => ParameterName = parameterName;

    public InvalidFinancialInputException(string parameterName, string message, Exception innerException)
        : base(message, innerException) => ParameterName = parameterName;

    /// <summary>Name of the offending argument, mirroring <see cref="ArgumentException.ParamName"/>.</summary>
    public string ParameterName { get; }

    /// <summary>Guards a denominator that must be strictly greater than zero.</summary>
    public static void ThrowIfNotPositive(decimal value, string parameterName, string subject)
    {
        if (value <= 0m)
        {
            throw new InvalidFinancialInputException(
                parameterName,
                $"{subject} must be greater than zero, but was {value:0.##}.");
        }
    }

    /// <summary>Guards a value that may be zero but never negative.</summary>
    public static void ThrowIfNegative(decimal value, string parameterName, string subject)
    {
        if (value < 0m)
        {
            throw new InvalidFinancialInputException(
                parameterName,
                $"{subject} cannot be negative, but was {value:0.##}.");
        }
    }
}
