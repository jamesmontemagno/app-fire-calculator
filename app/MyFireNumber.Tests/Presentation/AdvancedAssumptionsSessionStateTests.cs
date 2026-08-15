using MyFireNumber.Core.Presentation;

namespace MyFireNumber.Tests.Presentation;

/// <summary>
/// Covers the disclosure state that outlives a transient calculator page.
/// </summary>
/// <remarks>
/// These are necessary but not sufficient: the actual bug is about object lifetime across Shell
/// navigation, which only the app on a device can prove. What they do pin down is the part that is
/// easy to get silently wrong — that the store is keyed per calculator rather than globally, and
/// that a fresh store starts empty.
/// </remarks>
public class AdvancedAssumptionsSessionStateTests
{
    /// <summary>
    /// Every calculator reachable through a Shell route. Written out rather than read from the
    /// catalog so the isolation sweep below keeps meaning what it says even if the catalog moves.
    /// </summary>
    public static readonly string[] AllCalculatorIds =
    [
        "standard-fire",
        "lean-fire",
        "fat-fire",
        "withdrawal-rate",
        "savings-rate",
        "healthcare-gap",
        "coast-fire",
        "barista-fire",
        "reverse-fire",
        "debt-payoff",
        "retirement-cash-flow"
    ];

    public static TheoryData<string> CalculatorIds()
    {
        var data = new TheoryData<string>();
        foreach (var id in AllCalculatorIds)
        {
            data.Add(id);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(CalculatorIds))]
    public void FirstVisitIsCollapsed(string calculatorId)
    {
        var state = new AdvancedAssumptionsSessionState();

        Assert.False(state.IsExpanded(calculatorId));
    }

    [Fact]
    public void ExpandingIsRemembered()
    {
        var state = new AdvancedAssumptionsSessionState();

        state.SetExpanded("coast-fire", true);

        Assert.True(state.IsExpanded("coast-fire"));
    }

    [Fact]
    public void CollapsingAgainIsRemembered()
    {
        var state = new AdvancedAssumptionsSessionState();

        state.SetExpanded("coast-fire", true);
        state.SetExpanded("coast-fire", false);

        Assert.False(state.IsExpanded("coast-fire"));
    }

    /// <summary>
    /// The one pair in the app that catches a key derived from the page type. Standard, Lean, and
    /// Fat FIRE all route to <c>FireNumberPage</c>, so a page-typed key looks correct on the other
    /// eight calculators and only leaks inside this trio.
    /// </summary>
    [Fact]
    public void ExpandingStandardFireLeavesLeanFireCollapsed()
    {
        var state = new AdvancedAssumptionsSessionState();

        state.SetExpanded("standard-fire", true);

        Assert.True(state.IsExpanded("standard-fire"));
        Assert.False(state.IsExpanded("lean-fire"));
        Assert.False(state.IsExpanded("fat-fire"));
    }

    [Fact]
    public void EachSharedPageVariantKeepsItsOwnState()
    {
        var state = new AdvancedAssumptionsSessionState();

        state.SetExpanded("lean-fire", true);

        Assert.False(state.IsExpanded("standard-fire"));
        Assert.True(state.IsExpanded("lean-fire"));
        Assert.False(state.IsExpanded("fat-fire"));
    }

    /// <summary>
    /// Sweeps every pair, so a single shared key cannot pass by luck of which two calculators a
    /// narrower test happened to pick.
    /// </summary>
    [Theory]
    [MemberData(nameof(CalculatorIds))]
    public void ExpandingOneCalculatorLeavesEveryOtherCollapsed(string expandedId)
    {
        var state = new AdvancedAssumptionsSessionState();

        state.SetExpanded(expandedId, true);

        Assert.True(state.IsExpanded(expandedId));
        foreach (var otherId in AllCalculatorIds.Where(id => id != expandedId))
        {
            Assert.False(state.IsExpanded(otherId));
        }
    }

    [Fact]
    public void ExpandedCalculatorsDoNotInterfereWithEachOther()
    {
        var state = new AdvancedAssumptionsSessionState();

        state.SetExpanded("coast-fire", true);
        state.SetExpanded("debt-payoff", true);
        state.SetExpanded("coast-fire", false);

        Assert.False(state.IsExpanded("coast-fire"));
        Assert.True(state.IsExpanded("debt-payoff"));
    }

    /// <summary>
    /// Stands in for a relaunch. A new store is a new app run, and the decided behaviour is that
    /// everything is collapsed again — this is why the state is not written to SQLite beside the
    /// drafts.
    /// </summary>
    [Fact]
    public void AFreshStoreRemembersNothing()
    {
        var previousRun = new AdvancedAssumptionsSessionState();
        previousRun.SetExpanded("standard-fire", true);
        previousRun.SetExpanded("coast-fire", true);

        var newRun = new AdvancedAssumptionsSessionState();

        foreach (var id in AllCalculatorIds)
        {
            Assert.False(newRun.IsExpanded(id));
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ABlankCalculatorIdIsRejected(string calculatorId)
    {
        var state = new AdvancedAssumptionsSessionState();

        Assert.Throws<ArgumentException>(() => state.IsExpanded(calculatorId));
        Assert.Throws<ArgumentException>(() => state.SetExpanded(calculatorId, true));
    }

    [Fact]
    public void ANullCalculatorIdIsRejected()
    {
        var state = new AdvancedAssumptionsSessionState();

        Assert.Throws<ArgumentNullException>(() => state.IsExpanded(null!));
        Assert.Throws<ArgumentNullException>(() => state.SetExpanded(null!, true));
    }
}
