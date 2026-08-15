using System.Reflection;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Tests.Exports;

/// <summary>
/// The structural guard for issue #69. The per-exporter format tests assert representative cells, but a
/// hand-enumerated list checked by hand-enumerated assertions shares its own blind spot: an omission in
/// the list is invisible to assertions derived from the same list, so a missed <c>int</c> cell (as
/// happened with three draft ages) passes at full green.
///
/// The real defense against that is the type system, not a list: integer cells route through
/// <see cref="IntegerFormat"/>, which has no decimal member, and each exporter declares an
/// <c>[Obsolete(error: true)]</c> <c>(string, int, uint)</c> overload of its number-cell factory whose
/// only purpose is to be un-callable. Without that poison overload, <c>int</c> would widen implicitly to
/// <c>double</c> and a raw style index would silently reintroduce the defect.
///
/// This test enforces the mechanism itself: every exporter that builds numeric cells must keep the
/// poison overload, marked as an error. If someone deletes it — reopening the implicit-widening hole —
/// this fails, even though no individual cell assertion would notice.
/// </summary>
public sealed class IntegerCellFactoryGuardTests
{
    public static IEnumerable<object[]> Exporters()
    {
        var assembly = typeof(WorkbookStyles).Assembly;
        foreach (var type in assembly.GetTypes())
        {
            if (type.Namespace != "MyFireNumber.Core.Exports" || !type.Name.EndsWith("Workbook", StringComparison.Ordinal))
            {
                continue;
            }

            // Only exporters that actually build numeric cells need the guard.
            if (NumberFactories(type).Any())
            {
                yield return [type];
            }
        }
    }

    [Theory]
    [MemberData(nameof(Exporters))]
    public void EveryExporterKeepsAnUncallablePoisonOverloadForIntStyleIndexPairs(Type exporter)
    {
        var poison = NumberFactories(exporter)
            .Where(method => HasSignature(method, typeof(string), typeof(int), typeof(uint)))
            .ToArray();

        Assert.True(
            poison.Length == 1,
            $"{exporter.Name} must declare exactly one (string, int, uint) number-cell overload to poison the int+raw-style shape; found {poison.Length}.");

        var obsolete = poison[0].GetCustomAttribute<ObsoleteAttribute>();
        Assert.NotNull(obsolete);
        Assert.True(obsolete!.IsError, $"{exporter.Name}'s poison overload must set error: true so an int+raw-style call fails to compile.");
    }

    [Theory]
    [MemberData(nameof(Exporters))]
    public void EveryExporterExposesAnIntegerFormatOverload(Type exporter)
    {
        var safe = NumberFactories(exporter)
            .Any(method => HasSignature(method, typeof(string), typeof(int), typeof(IntegerFormat)));

        Assert.True(safe, $"{exporter.Name} must offer a (string, int, IntegerFormat) overload so integer cells have a type-safe path.");
    }

    private static IEnumerable<MethodInfo> NumberFactories(Type type) => type
        .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
        .Where(method => method.Name is "CreateNumberCell" or "Number");

    private static bool HasSignature(MethodInfo method, params Type[] parameterTypes)
    {
        var parameters = method.GetParameters();
        return parameters.Length == parameterTypes.Length
            && parameters.Select(parameter => parameter.ParameterType).SequenceEqual(parameterTypes);
    }
}
