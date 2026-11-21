namespace Refitter.Core;

/// <summary>
/// Bounds the size a YAML document reaches once its aliases are expanded. The YAML reader shares one node
/// between an anchor and all of its aliases, which is cheap, but serializing the document as JSON writes
/// the node out again for every alias. A few hundred bytes of nested aliases ("billion laughs") expand
/// to gigabytes that way.
/// </summary>
internal static class YamlAliasExpansionGuard
{
    /// <summary>
    /// The expanded size, in nodes, up to which a document is always accepted. A node is a mapping, a
    /// sequence, a key or a value. Expanding this many nodes takes well under a second. Larger documents are
    /// accepted too, as long as they are not amplified (see <see cref="MaxAmplification"/>), so the size of a
    /// legitimate specification is never limited by this value.
    /// </summary>
    internal const long MaxExpandedNodes = 100_000;

    /// <summary>
    /// How many times larger than the document itself the expanded size may be, once it is above
    /// <see cref="MaxExpandedNodes"/>. A very large document without aliases expands to about its own size and
    /// is never rejected by size alone, while a bomb expands to millions of times its size. Bounding the ratio
    /// also bounds the output to a small multiple of the input.
    /// </summary>
    internal const int MaxAmplification = 20;

    /// <summary>
    /// The expanded size, in characters, up to which a document is always accepted. The node count alone does
    /// not bound the output, because one long string referenced by many aliases is a single node that is
    /// written out once per alias. Larger documents are accepted too, as long as they are not amplified
    /// (see <see cref="MaxAmplification"/>).
    /// </summary>
    internal const long MaxExpandedSize = 16 * 1024 * 1024;

    // Approximate serialized overhead of a node besides its text: quotes, separators and indentation
    private const long NodeOverhead = 4;

    // Saturation value: large enough to be far above any budget, small enough that adding two never overflows
    private const long Saturated = long.MaxValue / 4;

    /// <summary>
    /// Throws a <see cref="ReferenceResolutionException"/> when expanding the aliases of
    /// <paramref name="root"/> would exceed <paramref name="maxExpandedNodes"/> nodes and
    /// <paramref name="maxAmplification"/> times the size of the document.
    /// </summary>
    internal static void EnsureWithinBudget(
        object? root,
        string source,
        long maxExpandedNodes = MaxExpandedNodes,
        int maxAmplification = MaxAmplification,
        long maxExpandedSize = MaxExpandedSize)
    {
        var expanded = Measure(root, out var distinct);
        if (expanded.Size > maxExpandedSize && expanded.Size / maxAmplification > distinct.Size)
        {
            throw new ReferenceResolutionException(
                $"OpenAPI document \"{source}\" expands to more than {maxExpandedSize} characters once its YAML " +
                $"aliases are resolved, more than {maxAmplification} times its own size, and is rejected, " +
                "because resolving it could exhaust memory.");
        }

        if (expanded.Nodes > maxExpandedNodes && expanded.Nodes / maxAmplification > distinct.Nodes)
        {
            throw new ReferenceResolutionException(
                $"OpenAPI document \"{source}\" expands to more than {maxExpandedNodes} nodes once its YAML " +
                $"aliases are resolved, more than {maxAmplification} times its own size, and is rejected, " +
                "because resolving it could exhaust memory.");
        }
    }

    /// <summary>
    /// Counts the nodes of <paramref name="root"/> as if every alias were expanded, saturating at a large value.
    /// </summary>
    internal static long MeasureExpandedNodes(object? root) => Measure(root, out _).Nodes;

    /// <summary>
    /// Estimates the serialized size, in characters, of <paramref name="root"/> as if every alias were expanded,
    /// saturating at a large value. Each scalar costs its length and each node a small constant.
    /// </summary>
    internal static long MeasureExpandedSize(object? root) => Measure(root, out _).Size;

    private readonly record struct Cost(long Nodes, long Size)
    {
        public static Cost operator +(Cost left, Cost right) =>
            new(Math.Min(Saturated, left.Nodes + right.Nodes), Math.Min(Saturated, left.Size + right.Size));
    }

    private static Cost Scalar(object? value) =>
        new(1, NodeOverhead + ((value as string)?.Length ?? 0));

    // Post-order walk with an explicit stack, so a deeply nested document cannot overflow the stack, and a size
    // per shared node, so the walk visits each node once however many aliases refer to it:
    // size(node) = 1 + the sum of the sizes of its children
    private static Cost Measure(object? root, out Cost distinct)
    {
        distinct = default;
        var children = GetChildren(root);
        if (children == null)
            return Scalar(root);

        // The size of the document itself: a string shared by aliases is stored once however often it is referenced
        var seenStrings = new HashSet<object>(ReferenceGuard.ReferenceComparer.Instance);
        Cost Distinct(List<object?> list)
        {
            var cost = new Cost(1 + list.Count, NodeOverhead * (1 + list.Count));
            foreach (var item in list)
            {
                if (item is string text && seenStrings.Add(text))
                    cost += new Cost(0, text.Length);
            }

            return cost;
        }

        var sizes = new Dictionary<object, Cost>(ReferenceGuard.ReferenceComparer.Instance);
        var inProgress = new HashSet<object>(ReferenceGuard.ReferenceComparer.Instance);
        var frames = new Stack<Frame>();
        frames.Push(new Frame(root!, children));
        inProgress.Add(root!);
        distinct += Distinct(children);

        Cost result = default;
        while (frames.Count > 0)
        {
            var frame = frames.Peek();
            if (frame.Next < frame.Children.Count)
            {
                var child = frame.Children[frame.Next++];

                // A shared node is looked up before its children are listed, so each reference costs O(1)
                List<object?>? grandChildren;
                if (child is null or string)
                {
                    frame.Sum += Scalar(child);
                }
                else if (sizes.TryGetValue(child, out var known))
                {
                    frame.Sum += known;
                }
                else if ((grandChildren = GetChildren(child)) == null)
                {
                    frame.Sum += Scalar(child);
                }
                else if (inProgress.Contains(child!))
                {
                    // An alias inside the node it refers to has no finite expansion
                    throw new ReferenceResolutionException(
                        "OpenAPI document contains a YAML alias that refers to its own ancestor and is rejected, " +
                        "because it has no finite expansion.");
                }
                else
                {
                    inProgress.Add(child!);
                    distinct += Distinct(grandChildren);
                    frames.Push(new Frame(child!, grandChildren));
                }

                continue;
            }

            frames.Pop();
            inProgress.Remove(frame.Node);
            var size = new Cost(1, NodeOverhead) + frame.Sum;
            sizes[frame.Node] = size;
            if (frames.Count > 0)
                frames.Peek().Sum += size;
            else
                result = size;
        }

        return result;
    }


    // Keys and values of a mapping are both nodes of the expanded document
    private static List<object?>? GetChildren(object? node)
    {
        if (node is IDictionary<object, object> mapping)
        {
            var list = new List<object?>(mapping.Count * 2);
            foreach (var entry in mapping)
            {
                list.Add(entry.Key);
                list.Add(entry.Value);
            }

            return list;
        }

        if (node is IList<object> sequence)
        {
            var list = new List<object?>(sequence.Count);
            foreach (var item in sequence)
                list.Add(item);
            return list;
        }

        return null;
    }

    private sealed class Frame(object node, List<object?> children)
    {
        public object Node { get; } = node;

        public List<object?> Children { get; } = children;

        public int Next { get; set; }

        public Cost Sum { get; set; }
    }
}
