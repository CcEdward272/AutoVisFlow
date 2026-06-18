namespace Cc.IDE.TaskEditor;

/// <summary>
/// 工具箱中可拖拽的节点类型描述符。
/// 驱动工具箱菜单的生成，并为每种节点类型提供元数据，
/// 包括分类、显示名称、图标和工具提示等 UI 呈现所需的信息。
/// </summary>
public sealed class NodeTypeDescriptor
{
    /// <summary>节点类型的内部标识符，对应 <see cref="FlowNodeViewModel.NodeType"/>（例如 "Start"、"Loop"、"IOAction"）。</summary>
    public string NodeType { get; set; } = string.Empty;

    /// <summary>节点类型在工具箱和菜单中显示的中文名称。</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>节点类型所属的分类名称（例如 "流程控制"、"IO 控制"、"仪器"、"调试"）。</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>可选的图标字形标识符，用于在工具箱中渲染图标。</summary>
    public string? IconGlyph { get; set; }

    /// <summary>可选的工具提示文本，当用户悬停在工具箱项上时显示。</summary>
    public string? Tooltip { get; set; }
}
