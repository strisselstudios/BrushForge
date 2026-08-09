using System.Collections.ObjectModel;

namespace BrushForge.Generation.Foliage.Branches;

/// <summary>
/// Immutable, topologically ordered branch skeleton. Parent branches always
/// precede their children and every branch path is stable and unique.
/// </summary>
public sealed class TreeBranchSkeleton
{
    private readonly ReadOnlyCollection<PlannedTreeBranch> _branches;

    public TreeBranchSkeleton(
        IEnumerable<PlannedTreeBranch> branches)
    {
        ArgumentNullException.ThrowIfNull(branches);

        PlannedTreeBranch[] branchArray =
            branches.ToArray();

        if (branchArray.Length == 0) {
            throw new ArgumentException(
                "A branch skeleton requires at least one branch.",
                nameof(branches));
        }

        HashSet<string> knownPaths =
            new(StringComparer.Ordinal);
        Dictionary<string, int> knownDepths =
            new(StringComparer.Ordinal);

        foreach (PlannedTreeBranch branch in branchArray) {
            ArgumentNullException.ThrowIfNull(branch);

            if (!knownPaths.Add(branch.Path)) {
                throw new ArgumentException(
                    "Branch skeleton paths must be unique.",
                    nameof(branches));
            }

            if (branch.ParentPath is null) {
                if (branch.Depth != 0) {
                    throw new ArgumentException(
                        "Only depth-zero primary branches may omit a parent path.",
                        nameof(branches));
                }
            }
            else {
                if (
                    !knownDepths.TryGetValue(
                        branch.ParentPath,
                        out int parentDepth)
                ) {
                    throw new ArgumentException(
                        "A branch parent must precede its child in the skeleton.",
                        nameof(branches));
                }

                if (branch.Depth != parentDepth + 1) {
                    throw new ArgumentException(
                        "A child branch depth must be exactly one greater than its parent depth.",
                        nameof(branches));
                }
            }

            knownDepths.Add(
                branch.Path,
                branch.Depth);
        }

        _branches =
            Array.AsReadOnly(branchArray);
        MaximumDepth =
            branchArray.Max(
                branch =>
                    branch.Depth);
    }

    public IReadOnlyList<PlannedTreeBranch> Branches =>
        _branches;

    public int Count =>
        _branches.Count;

    public int MaximumDepth { get; }
}
