using System;
using System.Collections.ObjectModel;

namespace Cc.IDE.TaskEditor;

/// <summary>
/// 工具箱 ViewModel。提供可用节点类型的分类列表，供用户拖拽到流程图画布上。
/// 节点类型涵盖流程控制（Start/End/Condition/Loop/Delay/CallTask）、
/// IO 控制（数字输出、数字输入、模拟量读写）、仪器和调试类节点。
/// </summary>
public class ToolboxViewModel : Cc.IDE.Mvvm.ViewModelBase
{
    /// <summary>工具项分类的列表。每个分类包含一组可拖拽的节点类型描述符。</summary>
    public ObservableCollection<ToolboxCategory> Categories { get; } = new();

    /// <summary>
    /// 初始化工具箱 ViewModel 的新实例。
    /// 预填充所有内置的节点类型分类和描述符。
    /// </summary>
    public ToolboxViewModel()
    {
        Categories.Add(new ToolboxCategory("流程控制", new[]
        {
            new NodeTypeDescriptor { NodeType = "Start",     DisplayName = "开始",       Category = "流程控制", IconGlyph = "Play",       Tooltip = "任务执行的入口点，每个任务应仅有一个开始节点。" },
            new NodeTypeDescriptor { NodeType = "End",       DisplayName = "结束",       Category = "流程控制", IconGlyph = "Stop",       Tooltip = "任务执行的出口点，到达此节点时任务正常结束。" },
            new NodeTypeDescriptor { NodeType = "Condition", DisplayName = "条件分支",   Category = "流程控制", IconGlyph = "Branch",     Tooltip = "根据表达式结果选择不同的执行路径。" },
            new NodeTypeDescriptor { NodeType = "Loop",      DisplayName = "循环",       Category = "流程控制", IconGlyph = "Loop",       Tooltip = "重复执行循环体内的节点，支持计数和条件循环。" },
            new NodeTypeDescriptor { NodeType = "Delay",     DisplayName = "延时",       Category = "流程控制", IconGlyph = "Clock",      Tooltip = "暂停执行指定的毫秒数后继续。" },
            new NodeTypeDescriptor { NodeType = "CallTask",  DisplayName = "调用子任务", Category = "流程控制", IconGlyph = "CallTask",   Tooltip = "调用另一个已定义的任务，支持参数映射。" },
        }));

        Categories.Add(new ToolboxCategory("IO 控制", new[]
        {
            new NodeTypeDescriptor { NodeType = "IOAction", DisplayName = "设置数字输出", Category = "IO 控制", IconGlyph = "DigitalOut", Tooltip = "设置指定数字输出通道的电平状态（高/低）。" },
            new NodeTypeDescriptor { NodeType = "IOAction", DisplayName = "等待数字输入", Category = "IO 控制", IconGlyph = "DigitalIn",  Tooltip = "等待指定数字输入通道达到期望的电平状态，支持超时。" },
            new NodeTypeDescriptor { NodeType = "IOAction", DisplayName = "写入模拟量",   Category = "IO 控制", IconGlyph = "AnalogOut",  Tooltip = "向指定模拟输出通道写入电压或电流值。" },
            new NodeTypeDescriptor { NodeType = "IOAction", DisplayName = "读取模拟量",   Category = "IO 控制", IconGlyph = "AnalogIn",   Tooltip = "从指定模拟输入通道读取电压或电流值并存入变量。" },
        }));

        Categories.Add(new ToolboxCategory("仪器", Array.Empty<NodeTypeDescriptor>()));
        Categories.Add(new ToolboxCategory("调试", new[]
        {
            new NodeTypeDescriptor { NodeType = "Comment", DisplayName = "注释", Category = "调试", IconGlyph = "Comment", Tooltip = "在流程图中添加说明注释，不影响执行。" },
        }));
    }
}

/// <summary>
/// 工具箱分类，包含一组相关的节点类型描述符。
/// </summary>
public sealed class ToolboxCategory
{
    /// <summary>分类名称（例如 "流程控制"、"IO 控制"）。</summary>
    public string CategoryName { get; }

    /// <summary>此分类下的节点类型描述符集合。</summary>
    public ObservableCollection<NodeTypeDescriptor> Items { get; } = new();

    /// <summary>
    /// 初始化 <see cref="ToolboxCategory"/> 的新实例。
    /// </summary>
    /// <param name="categoryName">分类名称。</param>
    /// <param name="items">此分类下的节点类型描述符。</param>
    public ToolboxCategory(string categoryName, IEnumerable<NodeTypeDescriptor> items)
    {
        CategoryName = categoryName;
        foreach (var item in items)
            Items.Add(item);
    }
}
