<p align="center">
  <a href="#english">🇺🇸 English</a> &nbsp;|&nbsp;
  <a href="#chinese">🇨🇳 中文</a>
</p>

---

<h1 align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt=".NET 8" />
  <img src="https://img.shields.io/badge/platform-Windows-0078D6?style=flat-square&logo=windows&logoColor=white" alt="Windows" />
  <img src="https://img.shields.io/badge/WPF-UI-5C2D91?style=flat-square&logo=windows&logoColor=white" alt="WPF" />
  <img src="https://img.shields.io/badge/status-alpha-orange?style=flat-square" alt="Alpha" />
  <img src="https://img.shields.io/badge/license-MIT-green?style=flat-square" alt="MIT" />
</h1>

<h1 align="center">Cc.IDE</h1>
<h3 align="center">PLC Test & Measurement Development Environment</h3>
<h3 align="center">PLC 测控开发环境</h3>

<p align="center">
  <b>A desktop IDE for industrial automation — visually design test workflows<br/>
  for PLC + Instruments + I/O, with drag-and-drop flowcharts and runtime execution.</b>
</p>

<p align="center">
  <b>面向自动化产线的桌面 IDE — 图形化编排 PLC + 仪器 + IO 测试流程，<br/>拖拽式流程图 + 运行时引擎驱动执行。</b>
</p>

---

<span id="english"></span>

# 🇺🇸 English

## 📖 Overview

**Cc.IDE** is a desktop-based development environment for building, editing, and running test & measurement procedures in industrial automation. It combines a **visual flowchart editor** with a **modular runtime engine** that orchestrates PLC communication, instrument control, and I/O operations.

> ⚠️ **Status:** Alpha / Work-in-progress. The core architecture and key data paths are functional, but the UI still has gaps and several modules remain stubbed out.

### ✨ Key Features

| Category | What's Included |
|----------|----------------|
| 🎨 **Visual Editor** | Drag-and-drop flowchart canvas (pan, zoom, multi-select, toolbox) |
| ⚙️ **Runtime Engine** | State machine-based task runner with 3-phase execution (PreIO → Instrument → PostIO) |
| 🔌 **PLC** | Modbus TCP protocol (8 function codes, MBAP framing, address parsing) |
| 🔬 **Instruments** | Plugin driver architecture — auto-discovery, capability introspection, dynamic property panels |
| 📡 **Communication** | Abstracted transport layer (`ICommunicationTransport` → Serial / TCP) |
| 🧮 **Expressions** | C-style expression evaluator (`$voltage > 3.0 && $status == 'OK'`) — 23 tests passing |
| 📊 **Reports** | JSON / HTML / CSV report generation, SQLite test result storage |
| 🖥️ **Player** | Separate operator-facing runtime UI for the production floor |
| 🔧 **CLI Tools** | Command-line smoke tests for CI/validation |

---

## 🚀 Quick Start

### Prerequisites

- **[.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)**
- Windows 10 / 11

### Build & Run the IDE

```bash
cd "C:\path\to\PLC IDE"
dotnet build Cc.IDE.slnx
dotnet run --project src/Cc.IDE.App
```

The IDE loads a **demo flowchart** on startup — 9 nodes + 8 connections. Drag nodes, right-click to wire them, or pull new nodes from the toolbox.

### CLI Tools (Smoke Tests)

```bash
dotnet run --project src/Cc.IDE.CliTools                      # Show help
dotnet run --project src/Cc.IDE.CliTools -- smoke             # Serialization round-trip
dotnet run --project src/Cc.IDE.CliTools -- debug-eval        # Expression evaluator tests
dotnet run --project src/Cc.IDE.CliTools -- runtime-test      # Runtime integration tests
```

### Operator Panel (Player)

```bash
dotnet run --project src/Cc.IDE.Player
```

---

## 📁 Project Structure

The solution (`Cc.IDE.slnx`) contains **17 projects** organized by layer:

```
src/
├── Cc.IDE.ProjectSystem/     ← Data models, JSON serialization, file I/O (zero deps)
├── Cc.IDE.Mvvm/              ← MVVM base classes (ObservableObject, RelayCommand, etc.)
├── Cc.IDE.Communication/     ← Communication interfaces (ICommunicationTransport, IIOService)
├── Cc.IDE.PLC/               ← PLC layer: Modbus TCP protocol, PLCIOService, point parsing
├── Cc.IDE.CAN/               ← CAN bus: frame models, ICANService (PCAN stub)
├── Cc.IDE.DriverSdk/         ← Instrument driver SDK (IInstrumentDriver + manager)
├── Cc.IDE.Runtime/           ← Runtime engine: TaskRunner, state machine, expression eval
├── Cc.IDE.TaskEditor/        ← Flowchart editor: FlowCanvas, toolbox, property panel
├── Cc.IDE.App/               ← IDE shell: MainWindow, DI composition
├── Cc.IDE.Player/            ← Operator panel (separate process)
├── Cc.IDE.CliTools/          ← CLI utilities
├── Cc.IDE.JsonTools/         ← JSON helpers
├── Cc.IDE.CsvTools/          ← CSV helpers
├── Cc.IDE.InstrumentEditor/  ← Instrument editor (stub)
├── Cc.IDE.IOMappingEditor/   ← I/O mapping editor (stub)
└── Drivers/
    ├── Agilent34401A.Driver/ ← 34401A DMM driver (SCPI)
    └── RigolDP832.Driver/    ← DP832 power supply driver
```

### 🔗 Dependency Flow

```
App → TaskEditor + Runtime + ...
TaskEditor → ProjectSystem + Mvvm
Runtime → ProjectSystem + Communication + DriverSdk + PLC + CAN
Communication → (zero dependencies)
ProjectSystem → (zero dependencies)
Drivers → DriverSdk + Communication
```

> ⚡ **ProjectSystem** and **Communication** are pure interface/model layers — they must not reference any other project.

---

## 🏗️ Architecture

### Data Model

All models live under `ProjectSystem/Models/` (~20 classes):

| Model | Purpose |
|-------|---------|
| `SolutionDefinition` / `ProjectDefinition` | Solution & project containers |
| `TaskDefinition` | Core unit — a flowchart = Nodes + Links |
| `FlowNodeDefinition` | Node in a flowchart (8 types) |
| `InstrumentCallDefinition` / `IOActionDefinition` | Instrument calls & I/O actions |
| `InstrumentDefinition` / `IOMappingDefinition` | Instrument instances & I/O mapping |

File format: JSON (`System.Text.Json`), extensions like `.yourtask`, `.yourinst`.

### Runtime Engine

The `TaskRunner` traverses flowchart nodes:

```
Start → ... → TestStep → Condition → ... → End
                   │           │
              PreIO       True / False
              Instrument
              PostIO
```

- **3-phase execution:** PreIO → InstrumentCalls → PostIO
- PostIO **always** executes (fail-safe I/O restoration)
- `ExpressionEvaluator` for condition nodes (C-style syntax)
- `CallTask` nodes support sub-task invocation with call-stack limit (max 10 deep)

### Instrument Drivers

Each instrument is a standalone DLL implementing `IInstrumentDriver`. Drop it in the `Drivers/` directory — the IDE auto-discovers and loads it on startup. Drivers self-describe their capabilities via `GetCapabilities()`, enabling the IDE to generate property panels dynamically (no hardcoded instrument UIs).

### Communication Layer

`ICommunicationTransport` → Serial / TCP implementations. The PLC module includes a custom Modbus TCP implementation with 8 function codes, MBAP frame construction/parsing, and exception code handling.

---

## ✅ What Works vs. What's Stubbed

### ✔️ Functional

- All data models & JSON serialization
- Modbus TCP protocol (full implementation)
- Modbus point code parsing (`D100` → HoldingRegister @ 100)
- Expression evaluator (23/23 tests passing)
- TaskRunner node traversal & state machine
- I/O execution service (real Modbus reads/writes via PLCIOService)
- Flowchart canvas: drag, move, multi-select, zoom, pan
- Toolbox drag-and-drop
- Right-click context menu for wiring
- Property panel (edit title / description / enabled)
- Player operator panel (MVVM-bound, runs tasks via IRuntimeHost)
- SQLite test record storage
- JSON / HTML / CSV report generation
- CLI smoke tests

### 🚧 Stubbed / Incomplete

- `PCANInterface` — CAN hardware not connected (connection flag only)
- `InstrumentEditor` / `IOMappingEditor` — views are placeholder TextBlocks
- Instrument driver `ExecuteAsync` — logic written, not tested against real hardware
- Expression evaluator: use **single quotes** for string literals (single-quote convention for JSON compatibility)
- 6 Coordinators — only `TaskRunCoordinator` is functional
- No unit test project (only CLI smoke & debug-eval)
- No installer package

---

## ❓ Troubleshooting Guide

| Problem | Look At |
|---------|---------|
| Model definitions unclear | `ProjectSystem/Models/` + architecture doc §5 |
| Adding a new node type | `FlowNodeDefinition.cs` → `TaskRunner.cs` switch dispatch → `FlowNodeView.xaml.cs` styling |
| Adding an instrument driver | Reference `Agilent34401ADriver.cs`, implement `IInstrumentDriver`, place in `Drivers/` |
| Adding a PLC protocol | Implement `IPlcProtocol`, see `ModbusTcpProtocol.cs` |
| Flowchart editor issues | `FlowCanvas.xaml.cs` — mainly mouse event handlers |
| Runtime behavior unexpected | Start from `TaskRunner.cs` → `ExecuteAsync` |

---

<p align="center">
  <a href="#chinese">🇨🇳 跳转到中文</a> &nbsp;|&nbsp;
  <a href="#">⬆ Back to top</a>
</p>

---

<span id="chinese"></span>

# 🇨🇳 中文

## 📖 概述

**Cc.IDE** 是一个面向工业自动化领域的桌面端测控开发环境。它将**可视化流程图编辑器**与**模块化运行时引擎**相结合，用于编排 PLC 通讯、仪器控制和 IO 操作。

> ⚠️ **状态：** Alpha / 半成品。核心架构和主要数据链路已跑通，但 UI 仍有不少缺项，部分模块还是骨架。

### ✨ 核心功能

| 类别 | 内容 |
|------|------|
| 🎨 **可视化编辑器** | 拖拽式流程图画布（平移、缩放、框选、工具箱拖放） |
| ⚙️ **运行时引擎** | 基于状态机的 TaskRunner，三段式执行（PreIO → Instrument → PostIO） |
| 🔌 **PLC 通讯** | Modbus TCP 协议（8 种功能码、MBAP 帧构造/解析、点位代码解析） |
| 🔬 **仪器驱动** | 插件式驱动架构 — 自动发现、能力自描述、动态属性面板生成 |
| 📡 **通讯层** | 抽象传输层（`ICommunicationTransport` → Serial / TCP 实现） |
| 🧮 **表达式引擎** | C 风格表达式求值器（`$voltage > 3.0 && $status == 'OK'`）— 23 个测试全部通过 |
| 📊 **报告生成** | JSON / HTML / CSV 报告，SQLite 测试结果存储 |
| 🖥️ **操作员界面** | 独立的产线操作员 Panel（Player 进程） |
| 🔧 **CLI 工具** | 命令行冒烟测试，可用于 CI/验证 |

---

## 🚀 快速开始

### 环境要求

- **[.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)**
- Windows 10 / 11

### 构建并启动 IDE

```bash
cd "C:\path\to\PLC IDE"
dotnet build Cc.IDE.slnx
dotnet run --project src/Cc.IDE.App
```

IDE 启动后自动加载一个**演示流程图**——9 个节点 + 8 条连线。可以拖拽节点、右键连线、从工具箱拖入新节点。

### CLI 工具（冒烟测试）

```bash
dotnet run --project src/Cc.IDE.CliTools                      # 查看帮助
dotnet run --project src/Cc.IDE.CliTools -- smoke             # 序列化往返测试
dotnet run --project src/Cc.IDE.CliTools -- debug-eval        # 表达式求值测试
dotnet run --project src/Cc.IDE.CliTools -- runtime-test      # 运行时集成测试
```

### 操作员界面（Player）

```bash
dotnet run --project src/Cc.IDE.Player
```

---

## 📁 项目结构

解决方案 `Cc.IDE.slnx` 包含 **17 个项目**，按分层组织：

```
src/
├── Cc.IDE.ProjectSystem/     ← 数据模型、JSON 序列化、文件读写（零依赖）
├── Cc.IDE.Mvvm/              ← MVVM 基类（ObservableObject、RelayCommand 等）
├── Cc.IDE.Communication/     ← 通讯接口（ICommunicationTransport、IIOService）
├── Cc.IDE.PLC/               ← PLC 层：Modbus TCP 协议、PLCIOService、点位解析
├── Cc.IDE.CAN/               ← CAN 总线：帧模型、ICANService（PCAN 占位）
├── Cc.IDE.DriverSdk/         ← 仪器驱动 SDK（IInstrumentDriver + 管理器）
├── Cc.IDE.Runtime/           ← 运行时引擎：TaskRunner、状态机、表达式求值
├── Cc.IDE.TaskEditor/        ← 流程图编辑器：FlowCanvas、工具箱、属性面板
├── Cc.IDE.App/               ← IDE 主壳层：MainWindow、DI 组装
├── Cc.IDE.Player/            ← 操作员界面（独立进程）
├── Cc.IDE.CliTools/          ← 命令行工具
├── Cc.IDE.JsonTools/         ← JSON 辅助
├── Cc.IDE.CsvTools/          ← CSV 辅助
├── Cc.IDE.InstrumentEditor/  ← 仪器编辑器（占位）
├── Cc.IDE.IOMappingEditor/   ← IO 映射编辑器（占位）
└── Drivers/
    ├── Agilent34401A.Driver/ ← 34401A 万用表驱动（SCPI）
    └── RigolDP832.Driver/    ← DP832 电源驱动
```

### 🔗 依赖方向

```
App → TaskEditor + Runtime + ...
TaskEditor → ProjectSystem + Mvvm
Runtime → ProjectSystem + Communication + DriverSdk + PLC + CAN
Communication → 零依赖
ProjectSystem → 零依赖
Drivers → DriverSdk + Communication
```

> ⚡ **ProjectSystem** 和 **Communication** 是纯接口/模型层，不能引用任何其他项目。

---

## 🏗️ 架构要点

### 数据模型

全部在 `ProjectSystem/Models/` 下，约 20 个类：

| 模型 | 用途 |
|------|------|
| `SolutionDefinition` / `ProjectDefinition` | 解决方案和工程 |
| `TaskDefinition` | 核心——流程图 = Nodes + Links |
| `FlowNodeDefinition` | 流程图节点（8 种类型） |
| `InstrumentCallDefinition` / `IOActionDefinition` | 仪器调用和 IO 动作 |
| `InstrumentDefinition` / `IOMappingDefinition` | 仪器实例和 IO 映射配置 |

文件格式：JSON（`System.Text.Json`），扩展名 `.yourtask`、`.yourinst`。

### 运行时引擎

`TaskRunner` 按流程图遍历节点：

```
Start → ... → TestStep → Condition → ... → End
                   │           │
              PreIO       True / False
              Instrument
              PostIO
```

- **三段式执行：** PreIO → InstrumentCalls → PostIO
- PostIO **始终执行**（故障安全恢复 IO）
- 条件节点使用 `ExpressionEvaluator`，C 风格语法
- `CallTask` 节点支持子任务调用，调用栈深度保护（最多 10 层）

### 仪器驱动

每种仪器一个独立 DLL，实现 `IInstrumentDriver` 接口，放入 `Drivers/` 目录即可。IDE 启动时自动扫描加载。驱动通过 `GetCapabilities()` 自描述能力，IDE 据此动态生成属性面板——无需为任何仪器型号硬编码 UI。

### 通讯层

`ICommunicationTransport` → Serial / TCP 实现。PLC 模块包含自研 Modbus TCP 实现，支持 8 种功能码、MBAP 帧构造/解析和异常码处理。

---

## ✅ 已完成 vs 占位

### ✔️ 可用的

- 全部数据模型和 JSON 序列化
- Modbus TCP 协议（完整实现）
- Modbus 点位代码解析（`D100` → HoldingRegister @ 100）
- 表达式求值器（23/23 测试通过）
- TaskRunner 节点遍历和状态机
- IO 执行服务（通过 PLCIOService 走真实 Modbus 读写）
- 流程图画布：拖拽、移动、框选、缩放、平移
- 工具箱拖放
- 右键菜单连线
- 属性面板（编辑标题/描述/启用）
- Player 操作员界面（MVVM 绑定，走 IRuntimeHost 运行任务）
- SQLite 测试记录存储
- JSON / HTML / CSV 报告生成
- CLI 冒烟测试

### 🚧 占位的

- `PCANInterface` — CAN 硬件未接，仅维护连接状态标志
- `InstrumentEditor` / `IOMappingEditor` — 视图仍是 TextBlock 占位
- 仪器驱动 `ExecuteAsync` — 逻辑已写，未接真实硬件测试
- 表达式求值中字符串用**单引号**，不要用双引号（兼容 JSON 解析）
- 6 个 Coordinator 仅 `TaskRunCoordinator` 真正工作
- 无单元测试项目（仅有 CLI 冒烟和 debug-eval）
- 无安装包

---

## 

## 🔧 开发约定

- 所有公共 API 写**中文 XML 注释**（`<summary>` / `<param>` / `<returns>`）
- **目标框架：** 类库项目 → `net8.0`，WPF 项目 → `net8.0-windows`
- **命名空间前缀：** `Cc.IDE`
- **DI 容器：** `Microsoft.Extensions.DependencyInjection`
- **MVVM 基类**集中在 `Cc.IDE.Mvvm`，不要重复造轮子

---

## ❓ 问题排查

| 问题 | 去哪里看 |
|------|----------|
| 模型定义不清楚 | `ProjectSystem/Models/` + 架构文档 §5 |
| 想加新节点类型 | `FlowNodeDefinition.cs` → `TaskRunner.cs` switch 分发 → `FlowNodeView.xaml.cs` 样式 |
| 想加新仪器驱动 | 参考 `Agilent34401ADriver.cs`，实现 `IInstrumentDriver`，放入 Drivers 目录 |
| 想加新 PLC 协议 | 实现 `IPlcProtocol`，参考 `ModbusTcpProtocol.cs` |
| 流程图编辑器不工作 | `FlowCanvas.xaml.cs`，主要是鼠标事件处理 |
| 运行时行为异常 | 从 `TaskRunner.cs` → `ExecuteAsync` 开始跟踪 |

---

<p align="center">
  <a href="#english">🇺🇸 Switch to English</a> &nbsp;|&nbsp;
  <a href="#">⬆ 回到顶部</a>
</p>
