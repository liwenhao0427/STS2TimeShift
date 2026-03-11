# AGENTS.md - DevDeckTools 协作规范

本文件面向自动化编码代理，目标是让代理在 DevDeckTools 中稳定迭代“开发者菜单”功能。

## 0. 作用域与硬性约束

- 仅允许修改 `src/Mods/DevDeckTools/`。
- `src/Core/`、`src/gdscript/`、`src/GameInfo/` 仅可读，不可改。
- 不要删除用户已有改动，不使用 `git reset --hard`、`git checkout --`。
- 与用户沟通、日志与注释使用中文；代码标识符使用英文。
- 每次会话有代码改动时，完成后提交一次 commit（除非用户明确禁止提交）。

## 1. 项目目标

- 目标 Mod：通过快捷键打开开发者菜单，提升调试效率。
- 核心能力（规划中）：
  - 添加任意卡牌（按 ID）
  - 删除卡组中指定卡牌
  - 添加/移除任意遗物
  - 快速应用调试预设

## 2. 代码入口与结构

- 初始化入口：`Scripts/Entry.cs`
- 注入点补丁：`Scripts/Patch/NGamePatch.cs`
- 菜单控制器：`Scripts/DevMenuController.cs`

推荐目录：

- `Scripts/Commands/`：封装“加卡/删卡/加遗物”等操作
- `Scripts/Ui/`：菜单窗口、输入框、列表、按钮
- `Scripts/Patch/`：必要的 Harmony 补丁

## 3. Build / Lint / Test

在目录 `src/Mods/DevDeckTools` 下执行。

### 3.1 构建

- `dotnet restore DevDeckTools.csproj`
- `dotnet build DevDeckTools.csproj -c Debug`
- `dotnet build DevDeckTools.csproj -c Release`

### 3.2 格式与静态质量

- 检查格式：`dotnet format DevDeckTools.csproj --verify-no-changes`
- 自动格式化：`dotnet format DevDeckTools.csproj`

说明：当前未配置独立 lint，使用 `dotnet format + 编译警告` 作为基础门禁。

### 3.3 测试

- 当前仓库无测试项目。
- 若后续新增测试工程：
  - 全量：`dotnet test <TestProject.csproj> -c Debug`
  - 单测：`dotnet test <TestProject.csproj> --filter "FullyQualifiedName~Namespace.Class.Method"`
  - 单类：`dotnet test <TestProject.csproj> --filter "FullyQualifiedName~Namespace.Class"`

## 4. 日志与验证

- 日志文件：`C:\Users\temp\AppData\Roaming\SlayTheSpire2\logs\godot.log`
- 日志前缀统一：`[DevDeckTools]`
- 优先 `Log.Info`（`Log.Debug` 默认不可见）

最小验证用例：

- 主菜单或运行中按 `F8`：菜单能打开/关闭。
- 菜单打开后不应破坏正常输入链（不误触发战斗关键输入）。
- 菜单关闭后应完全恢复输入行为。

## 5. C# 风格约定

### 5.1 using 与命名空间

- using 顺序：系统库 → 第三方库 → 项目内命名空间。
- 删除未使用 using。
- 使用文件作用域命名空间。

### 5.2 命名

- 类型/方法/属性：`PascalCase`
- 局部变量/参数/私有字段：`camelCase`
- 私有字段前缀 `_`
- 常量：`SCREAMING_SNAKE_CASE`

### 5.3 类型与可空

- 启用 `<Nullable>enable</Nullable>`，显式处理 `null`。
- 优先显式类型，避免无意义 `var`。
- 访问可空对象前先守卫。

### 5.4 格式

- 4 空格缩进。
- 大括号沿用现有 C# 风格（K&R）。
- 复杂逻辑禁止一行 `if`。

### 5.5 错误处理

- 输入事件、UI 引用、运行态对象都要前置判空。
- 对“执行失败”要给出可追踪日志，不吞异常。
- 对幂等操作（重复点击、重复打开菜单）要容错。

## 6. Harmony 补丁原则

- 默认优先 `Postfix` 做附加逻辑。
- 若要拦截危险路径再用 `Prefix`。
- 高频方法中避免重逻辑和反射热路径。
- 补丁命名统一：`PrefixXxx` / `PostfixXxx`。

## 7. 开发者菜单业务规则

- 菜单只改变调试能力，不应破坏正常游戏主流程。
- “加卡/删卡/加遗物”必须作用于当前 run/player 的真实状态对象。
- UI 输入聚焦时应防止和游戏主输入冲突。
- 若菜单不可用（非运行态），按钮应禁用并给提示。

## 8. 提交前检查清单

- `dotnet build DevDeckTools.csproj -c Debug` 通过。
- `dotnet format DevDeckTools.csproj --verify-no-changes` 通过。
- 日志中关键步骤有 `[DevDeckTools]` 输出。
- 新增功能至少有一次手动验证路径记录。

## 9. Cursor / Copilot 规则兼容

- 已检查：未发现 `.cursor/rules/`、`.cursorrules`。
- 已检查：未发现 `.github/copilot-instructions.md`。
- 若后续新增规则文件，需同步更新本 AGENTS.md。

## 10. 推荐工作流

- 先读入口：`Scripts/Entry.cs`、`Scripts/Patch/NGamePatch.cs`。
- 再做最小增量改动，避免一次性重写。
- 本地先 build，再 format verify。
- 最后检查日志与手动路径，完成后提交。
