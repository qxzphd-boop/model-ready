# ModelReady v0.1 中文项目总览

## 1. 一句话定义

ModelReady 是一个运行在 Rhino 8 内部的本地预检插件。建筑学生或设计师在提交、出图或交接模型前运行一次 `ModelReady`，它会按照固定的 **Studio Submission** 标准检查模型，分成失败、警告和通过，并帮助用户定位问题对象。

它不评价设计好不好，也不自动修复模型。它解决的是：提交前很累、时间很紧、检查命令分散、容易忘记检查项目，而且不同人缺少一致的提交标准。

## 2. 真实用户流程

1. 用户打开 Rhino 模型，输入 `ModelReady`。
2. 插件打开一个简洁窗口，使用内置的 `Studio Submission` 检查标准。
3. 用户确认预期单位，点击 **Run Preflight**。
4. 插件只读扫描模型，不删除、解锁、显示或修改任何对象。
5. 结果分成 `Fail`、`Warning`、`Pass`。
6. 点击某条问题可以定位普通对象；如果对象隐藏或锁定，只显示对象 GUID 和图层，不改变其状态。
7. 用户手动修复后重新运行。
8. 用户主动选择时，才导出 HTML 或 JSON 报告。

最终状态不使用主观的百分制分数：

- `NOT READY`：至少存在一个 Fail；
- `READY WITH WARNINGS`：没有 Fail，但仍有 Warning；
- `READY`：没有 Fail 和 Warning。

## 3. v0.1 冻结的七条检查

1. 模型单位是否与用户选择的预期单位一致；
2. absolute tolerance 换算成米后是否处于 `0.000001–0.001 m`；
3. RhinoCommon 是否判断几何为 invalid；
4. 是否存在位置和几何都完全相同的重复对象；
5. 对象包围盒中心是否距离世界原点超过 `1,000 m`；
6. 支持对象的包围盒对角线是否小于 `1 mm`；
7. 支持对象是否仍然位于 Rhino 的 Default 图层。

首版只扫描顶层 model-space Curve、Brep、Extrusion、Mesh 和 SubD。它包括隐藏和锁定对象，但不深入 block definition，不扫描 linked reference、标注、灯光、裁剪平面和 layout/detail 对象。

## 4. 技术栈决定

### 正式产品路线

- 语言：C#；
- 插件平台：Rhino 8.18+；
- API：RhinoCommon `8.18.25100.11001`；
- 插件运行环境：.NET 7；
- 独立核心逻辑：.NET Standard 2.0；
- 测试运行器与 CI SDK：.NET 10；
- 界面：Rhino 官方生态中的 Eto.Forms；
- JSON：System.Text.Json；
- 单元测试：xUnit；
- 自动构建：GitHub Actions；
- Rhino 内自动验证：RhinoCode CLI；
- 本地安装包：Yak；
- 版本控制：Git + GitHub。

### 为什么不以 Python 作为正式产品主线

Rhino 8 的 Python 3 可以制作脚本和插件，因此 Python 不是“做不了”。但 ModelReady 需要稳定窗口、多项服务、明确的数据类型、重复构建、错误处理和安装包。C# 与 RhinoCommon 的类型系统、官方插件模板和编译反馈更适合这个正式软件。Python 可以用于快速验证单个 Rhino API，但验证代码不能直接变成缺少结构的正式产品。

### 为什么不做网页或云服务

这个工具必须在 deadline 前离线工作，并直接读取当前 RhinoDoc。网页、账户、数据库、服务器和 Rhino.Compute 不解决首版问题，反而增加故障点，因此全部排除。

## 5. 软件结构

项目分为三部分：

1. `ModelReady.Core`：不依赖 Rhino，保存检查结果、阈值、规则和 readiness 计算；
2. `ModelReady.Rhino`：读取 RhinoDoc、处理几何比较、显示 Eto 窗口、定位对象和导出报告；
3. `ModelReady.Core.Tests`：在不启动 Rhino 的情况下测试大部分规则。

这样做的原因是，GitHub Actions 通常没有 Rhino 许可证，不能假装在线 CI 已经验证 Rhino 内行为。CI 负责核心测试和 `.rhp` 编译；本机 RhinoCode fixture 测试是发布前不可取消的第二道门。

## 6. 主要开发难点与解决办法

### Rhino 版本不匹配

难点：用较新 RhinoCommon 编译的插件可能无法在本机 Rhino 加载。

方案：把 RhinoCommon 精确固定为本机 Rhino 8.18 对应的 `8.18.25100.11001`。任何升级必须单独验证，不能自动追最新版本。

### 重复对象检查可能非常慢

难点：每个对象与所有其他对象比较会变成平方级复杂度。

方案：先按照几何类型和经过 tolerance 量化的 bounding box 分桶，只在同一小桶内调用 Rhino 的精确几何比较。10,000 个对象必须在开发机 10 秒内完成，否则不能发布 v0.1。

### 规则容易产生误报

难点：很小、离原点远或开放的几何有时是有意的。

方案：首版完全取消笼统的 open geometry 检查；有语境差异的项目只标 Warning；报告显示实际阈值；永远不自动修复。发布前使用三份真实学生模型做匿名误报评估。

### Rhino API 与后台线程

难点：把 Rhino 几何访问随意放到后台线程可能不安全；全部在界面线程运行又可能卡住 Rhino。

方案：v0.1 在 Rhino 命令线程读取对象，尽快转换为轻量 snapshot，然后使用分桶减少工作量。先做性能测试，不能为了“看起来快”而加入未经证明安全的并行几何调用。

### 隐藏和锁定对象难以定位

难点：自动显示或解锁会偷偷修改用户模型。

方案：只选择 Rhino 正常允许选择的对象；隐藏或锁定对象只返回 GUID 和图层。插件绝不调用 Show、Unlock、Delete 或 Purge。

### 单位不同导致阈值失真

难点：`1` 在毫米模型和米模型里代表完全不同的尺寸。

方案：读取 Rhino 数据后，所有距离和 tolerance 先统一换算成米，核心规则只比较米制标准值。

### GitHub CI 不能完整运行 Rhino

难点：普通 CI 可以编译，却不能证明插件在 Rhino 里能加载和执行。

方案：GitHub Actions 负责 restore、build、core tests；本机用 RhinoCode 自动启动 Rhino fixture 测试，记录 Rhino 版本、commit、fixture checksum、规则计数、耗时和报告一致性。

### 报告可能泄露隐私

难点：完整文件路径可能包含用户名、学校项目目录或客户信息。

方案：报告只保存文件 basename，不保存完整路径、Windows 用户名、账户数据、机器标识或模型几何；没有任何 telemetry。

## 7. 开发阶段

1. **工程门验证**：空插件能够编译、加载并运行 `ModelReady`；
2. **核心数据模型**：定义 severity、readiness、snapshot、profile 和 rule result；
3. **七条纯规则**：先写失败测试，再实现规则；
4. **Rhino snapshot**：读取对象、统一单位、过滤首版范围；
5. **重复对象算法**：候选分桶加精确比较；
6. **扫描编排**：保证固定规则顺序，任何异常不能被算成 Pass；
7. **Eto 界面**：运行、结果分组、状态文字和错误反馈；
8. **安全定位**：不改变隐藏和锁定状态；
9. **HTML/JSON 报告**：两种格式计数必须一致；
10. **性能与真实模型验证**：fixture、10,000 对象、三份匿名真实模型；
11. **Yak 本地安装包**：先本机安装验证，不公开发布到 Rhino Package Manager；
12. **README 与证据**：安装、规则、限制、测试结果和截图。

## 8. v0.1 明确不做

不做自动修复或删除；不评价建筑设计；不检查建筑法规；不读取 Grasshopper definition；不深入 block definition；不做 Revit、BEAM、IFC 或 BIM metadata；不做 fabrication/3D print profile；不比较两个模型版本；不做 profile 编辑器；不上传报告；不使用 AI/LLM；不做账户、协作、分析或 telemetry；不承诺 macOS；不公开发布 Yak 包。

## 9. 当前已验证状态

截至项目初始化：

- Rhino 8.18.25100.11001 已安装；
- RhinoCode 8.18.25100 和 Yak 0.14.2 可用；
- `ModelReady.rhp` Release 构建为 0 warning / 0 error；
- 核心测试 1/1 通过；
- Rhino 内 `LoadPlugIn` 返回 `Success`；
- 固定插件 ID：`a891b51d-0b9e-497e-bf69-33fcc4682014`；
- Rhino 内运行 `ModelReady` 返回 `True`；
- 七条正式规则尚未实现，仓库不会把空骨架冒充为 v0.1 完成品。
