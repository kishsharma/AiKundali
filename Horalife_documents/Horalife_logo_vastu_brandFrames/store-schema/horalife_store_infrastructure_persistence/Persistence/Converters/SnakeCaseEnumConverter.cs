using System.Linq.Expressions;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HoraLife.Store.Infrastructure.Persistence.Converters;

/// <summary>
/// Saves .NET enum values as lowercase snake_case strings.
/// Examples:
/// DirectStore -> direct_store
/// PartiallyRefunded -> partially_refunded
/// </summary>
public sealed class SnakeCaseEnumConverter<TEnum> : ValueConverter<TEnum, string>
    where TEnum : struct, Enum
{
    public SnakeCaseEnumConverter()
        : base(ToProviderExpression(), FromProviderExpression())
    {
    }

    private static Expression<Func<TEnum, string>> ToProviderExpression()
        => value => EnumSnakeCase.ToSnakeCase(value);

    private static Expression<Func<string, TEnum>> FromProviderExpression()
        => value => EnumSnakeCase.FromSnakeCase<TEnum>(value);
}

internal static class EnumSnakeCase
{
    public static string ToSnakeCase<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        var name = value.ToString();
        var builder = new StringBuilder(name.Length + 8);

        for (var index = 0; index < name.Length; index++)
        {
            var current = name[index];

            if (char.IsUpper(current) && index > 0)
            {
                var previous = name[index - 1];
                var nextIsLower = index + 1 < name.Length &&
                                  char.IsLower(name[index + 1]);

                if (char.IsLower(previous) ||
                    char.IsDigit(previous) ||
                    nextIsLower)
                {
                    builder.Append('_');
                }
            }

            builder.Append(char.ToLowerInvariant(current));
        }

        return builder.ToString();
    }

    public static TEnum FromSnakeCase<TEnum>(string databaseValue)
        where TEnum : struct, Enum
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseValue);

        foreach (var enumValue in Enum.GetValues<TEnum>())
        {
            if (string.Equals(
                    ToSnakeCase(enumValue),
                    databaseValue,
                    StringComparison.OrdinalIgnoreCase))
            {
                return enumValue;
            }
        }

        throw new InvalidOperationException(
            $"Database value '{databaseValue}' is not valid for enum " +
            $"'{typeof(TEnum).Name}'.");
    }
}
