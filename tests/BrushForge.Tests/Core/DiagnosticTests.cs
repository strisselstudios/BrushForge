using BrushForge.Core.Diagnostics;

namespace BrushForge.Tests.Core;

public sealed class DiagnosticTests
{
    [Fact]
    public void DiagnosticBagTracksWarningsAndErrors()
    {
        DiagnosticBag bag = new();

        bag.Add(
            "BF1000",
            DiagnosticSeverity.Information,
            "Generation started.");

        bag.Add(
            "BF2000",
            DiagnosticSeverity.Warning,
            "Brush count is approaching the configured budget.");

        bag.Add(
            "BF3000",
            DiagnosticSeverity.Error,
            "A brush failed convexity validation.");

        Assert.Equal(3, bag.Count);
        Assert.True(bag.HasWarnings);
        Assert.True(bag.HasErrors);
    }

    [Fact]
    public void SnapshotIsIndependentFromLaterChanges()
    {
        DiagnosticBag bag = new();

        bag.Add(
            "BF1000",
            DiagnosticSeverity.Information,
            "First diagnostic.");

        IReadOnlyList<Diagnostic> snapshot = bag.Snapshot();

        bag.Add(
            "BF1001",
            DiagnosticSeverity.Information,
            "Second diagnostic.");

        Assert.Single(snapshot);
        Assert.Equal(2, bag.Count);
    }

    [Fact]
    public void DiagnosticRequiresCodeAndMessage()
    {
        Assert.Throws<ArgumentException>(
            () => new Diagnostic(
                "",
                DiagnosticSeverity.Error,
                "Failure."));

        Assert.Throws<ArgumentException>(
            () => new Diagnostic(
                "BF3000",
                DiagnosticSeverity.Error,
                ""));
    }
}
