using System.Text.RegularExpressions;

namespace MyFireNumber.Tests.Presentation;

/// <summary>
/// Guards the one line of issue #81 that no other test in this suite can reach:
/// <c>CalculatorPageBase</c> asking the session store about the <b>calculator id</b> rather than the
/// page type.
///
/// <para><b>Why this exists at all.</b> <see cref="AdvancedAssumptionsSessionStateTests"/> proves the
/// store keeps per-key state correctly, and it is thorough — including the standard/lean isolation
/// pair. But it exercises a keyed collection through two string literals, so it is green under
/// <i>every possible keying of the real app</i>. The bug in #81 does not live in the store. It lives
/// at the call site, and <c>MyFireNumber.Tests.csproj</c> references only Core and Storage, so
/// <c>app/MyFireNumber/</c> is never compiled here and <c>CalculatorPageBase</c> is unreachable by
/// any conventional test. Replacing <c>viewModel.CalculatorId</c> with <c>GetType().Name</c>
/// reintroduces #81 in full and leaves the entire suite passing. That was measured, not supposed.</para>
///
/// <para><b>Why the page type is the specific poison.</b> <c>standard-fire</c>, <c>lean-fire</c> and
/// <c>fat-fire</c> all route to <c>FireNumberPage</c> (asserted in
/// <see cref="Calculations.CalculatorCatalogTests"/>). Any key derived from the page therefore
/// collapses those three onto one entry and makes them disclose together, while looking perfectly
/// correct on the other eight calculators — so casual testing, and eight of eleven manual passes,
/// report success.</para>
///
/// <para><b>Why it reads source text.</b> Same trade the route-table oracle already makes: text is a
/// weak instrument, but the alternative is no instrument. It costs no MAUI project reference and no
/// new test target. The weakness of a text guard is <i>vacuity</i> — it stops matching after an
/// innocuous rename and reports success for a file it no longer understands — so the anchors are
/// asserted to exist before anything is concluded from them, and the detector is validated against a
/// known-bad and a known-good sample on every run rather than once by hand at authoring time.</para>
/// </summary>
public class AdvancedAssumptionsCallSiteGuardTests
{
    /// <summary>
    /// The invocation, not the declaration. Requiring a <c>;</c> after the closing parenthesis is what
    /// separates them: the declaration is followed by its body brace.
    /// </summary>
    /// <remarks>
    /// The argument is <c>.*?</c> rather than <c>[^)]*</c>, and that is not incidental. The first
    /// version of this pattern excluded parentheses from the argument, which meant it could not match
    /// <c>RestoreAdvancedAssumptions(GetType().Name);</c> at all — the exact regression it exists to
    /// catch. <see cref="The_detector_actually_detects_the_regression_it_claims_to"/> failed on that,
    /// which is the whole reason the detector is validated against a known-bad sample instead of being
    /// assumed correct. <c>.</c> does not match a newline here, so this stays within one statement.
    /// </remarks>
    private const string RestoreCallPattern =
        """RestoreAdvancedAssumptions\s*\(\s*(?<argument>.*?)\s*\)\s*;""";

    private const string RestoreDeclarationPattern =
        """void\s+RestoreAdvancedAssumptions\s*\(""";

    /// <summary>
    /// Expressions that yield a type name rather than a catalog id. <c>GetType()</c> and
    /// <c>typeof</c> are the direct forms; <c>nameof</c> and a bare <c>.Name</c> are the ways the
    /// same mistake arrives looking tidier.
    /// </summary>
    private static readonly string[] PageTypeDerivedExpressions =
        ["GetType", "typeof", "nameof", ".Name"];

    private static readonly string PageBaseSource = LoadPageBaseSource();

    [Fact]
    public void Advanced_assumptions_state_is_keyed_on_the_calculator_id()
    {
        var argument = RestoreCallArgument();

        Assert.Contains("CalculatorId", argument, StringComparison.Ordinal);
        Assert.EndsWith(".CalculatorId", argument, StringComparison.Ordinal);
    }

    [Fact]
    public void Advanced_assumptions_state_is_not_keyed_on_the_page_type()
    {
        var argument = RestoreCallArgument();

        foreach (var banned in PageTypeDerivedExpressions)
        {
            Assert.False(
                argument.Contains(banned, StringComparison.Ordinal),
                $"CalculatorPageBase restores advanced assumptions using '{argument}', which derives the "
                    + $"key from the page type via '{banned}'. Standard, Lean and Fat FIRE all route to "
                    + "FireNumberPage, so this collapses those three calculators onto one key and makes "
                    + "them disclose together — issue #81. Pass the view model's CalculatorId instead.");
        }
    }

    [Fact]
    public void The_restored_id_is_the_one_handed_to_each_expander()
    {
        // The other half of the round trip. Keying the restore correctly and then binding the expander
        // to something else would defeat it just as completely, and lives in this same unreachable file.
        Assert.Matches(
            """BindSessionState\s*\(\s*advancedAssumptionsState\s*,\s*calculatorId\s*\)""",
            PageBaseSource);
    }

    [Fact]
    public void The_guard_is_reading_real_source_and_its_anchors_still_exist()
    {
        // Vacuity guard. Every assertion above is drawn from a regex over this file; a rename that
        // stopped the patterns matching would turn them all green while checking nothing. The anchors
        // are therefore asserted to exist as facts in their own right.
        Assert.NotEmpty(PageBaseSource);
        Assert.Contains("class CalculatorPageBase", PageBaseSource, StringComparison.Ordinal);
        Assert.Contains("ApplyQueryAttributes", PageBaseSource, StringComparison.Ordinal);

        Assert.Matches(RestoreDeclarationPattern, PageBaseSource);
        Assert.Single(Regex.Matches(PageBaseSource, RestoreCallPattern));
    }

    [Fact]
    public void The_detector_actually_detects_the_regression_it_claims_to()
    {
        // A scanner trusted on real source without ever being shown a positive is how a guard ends up
        // reporting a codebase it cannot read as clean. Both samples are checked, so the guard cannot
        // pass by matching everything or by matching nothing.
        const string Sabotaged = "RestoreAdvancedAssumptions(GetType().Name);";
        const string Correct = "RestoreAdvancedAssumptions(viewModel.CalculatorId);";

        var sabotagedArgument = Regex.Match(Sabotaged, RestoreCallPattern).Groups["argument"].Value;
        var correctArgument = Regex.Match(Correct, RestoreCallPattern).Groups["argument"].Value;

        Assert.Equal("GetType().Name", sabotagedArgument);
        Assert.Equal("viewModel.CalculatorId", correctArgument);

        Assert.Contains(PageTypeDerivedExpressions, banned => sabotagedArgument.Contains(banned, StringComparison.Ordinal));
        Assert.DoesNotContain(PageTypeDerivedExpressions, banned => correctArgument.Contains(banned, StringComparison.Ordinal));
    }

    private static string RestoreCallArgument()
    {
        var match = Regex.Match(PageBaseSource, RestoreCallPattern);

        Assert.True(
            match.Success,
            "No call to RestoreAdvancedAssumptions(...) was found in app/MyFireNumber/Views/CalculatorPageBase.cs. "
                + "Either the restore was removed — which is issue #81 — or it was renamed, in which case this "
                + "guard is no longer reading the thing it claims to and must be updated rather than deleted.");

        return match.Groups["argument"].Value;
    }

    private static string LoadPageBaseSource()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "NativeCallSites", "CalculatorPageBase.cs");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"CalculatorPageBase source not found at '{path}'. It is copied from app/MyFireNumber by "
                    + "MyFireNumber.Tests.csproj so this suite can guard a call site it cannot compile.",
                path);
        }

        return File.ReadAllText(path);
    }
}
