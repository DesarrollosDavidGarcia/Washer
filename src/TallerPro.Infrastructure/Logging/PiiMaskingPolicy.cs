using System.Collections.Concurrent;
using System.Reflection;
using Serilog.Core;
using Serilog.Events;
using TallerPro.Domain.Common;

namespace TallerPro.Infrastructure.Logging;

/// <summary>
/// Serilog destructuring policy that masks properties decorated with <see cref="PiiDataAttribute"/>.
/// High → ***@***   Low → first character + ***
/// Reflection results are cached per type to avoid repeated scanning.
/// </summary>
public sealed class PiiMaskingPolicy : IDestructuringPolicy
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, PiiLevel>> Cache = new();

    public bool TryDestructure(
        object value,
        ILogEventPropertyValueFactory propertyValueFactory,
        out LogEventPropertyValue result)
    {
        var type = value.GetType();
        var piiMap = Cache.GetOrAdd(type, BuildPiiMap);

        if (piiMap.Count == 0)
        {
            result = null!;
            return false;
        }

        var properties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .Select(p =>
            {
                var rawValue = p.GetValue(value);

                if (piiMap.TryGetValue(p.Name, out var level))
                {
                    var masked = Mask(rawValue?.ToString(), level);
                    return new LogEventProperty(p.Name, new ScalarValue(masked));
                }

                return new LogEventProperty(p.Name, propertyValueFactory.CreatePropertyValue(rawValue));
            })
            .ToArray();

        result = new StructureValue(properties);
        return true;
    }

    private static IReadOnlyDictionary<string, PiiLevel> BuildPiiMap(Type type)
    {
        var dict = new Dictionary<string, PiiLevel>(StringComparer.Ordinal);

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = prop.GetCustomAttribute<PiiDataAttribute>();
            if (attr is not null)
            {
                dict[prop.Name] = attr.Level;
            }
        }

        return dict;
    }

    private static string Mask(string? input, PiiLevel level) => level switch
    {
        PiiLevel.High => "***@***",
        PiiLevel.Low when string.IsNullOrEmpty(input) => string.Empty,
        PiiLevel.Low => string.Concat(input![..1], "***"),
        _ => input ?? string.Empty
    };
}
