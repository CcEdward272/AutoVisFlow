namespace Cc.IDE.ProjectSystem.Models;

/// <summary>
/// Describes a looping construct in a flow graph.
/// Supports for-loops, while-loops, and for-each iterations
/// with an optional iteration cap to prevent runaway loops.
/// </summary>
public sealed class LoopDefinition
{
    /// <summary>
    /// The type of loop: ForLoop (counter-based), WhileLoop (condition-based),
    /// or ForEach (collection iteration).
    /// </summary>
    public LoopType Type { get; set; } = LoopType.ForLoop;

    /// <summary>
    /// For ForLoop: the loop-expression (e.g., "i < 10; i++").
    /// For WhileLoop: the boolean condition (e.g., "flag != 1").
    /// For ForEach: the collection and item variables (e.g., "item in collection").
    /// </summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// The maximum number of iterations allowed before the loop is
    /// forcibly terminated. Used to detect and prevent infinite loops.
    /// Defaults to 1000.
    /// </summary>
    public int MaxIterations { get; set; } = 1000;
}
