# AGENTS.md - TimeShift Mod 协作规范

本文件给自动化编码代理使用，目标是让代理在本仓库中稳定改动、可验证、可回滚。

## 0. 作用域与硬性约束

- 仅允许修改 `src/Mods/TimeShift/`。
- `src/Core/`、`src/gdscript/`、`src/GameInfo/` 视为只读参考源码。
- 不要删除用户已有改动，不要用 `git reset --hard`、`git checkout --`。
- 语言：与用户沟通、日志说明、注释均用中文；标识符保持英文。
- 提交策略：每次会话完成代码修改后，立即提交一次 commit（除非用户明确要求不提交）。

## 1. 项目背景（必须理解）

- 目标功能：按住 `Shift` 预览卡牌“另一面”。
- 基础卡：显示升级版；升级卡：显示基础版。
- 当前关键风险：预览态使用克隆 `CardModel`，若直接点击可能进入战斗打牌流程并触发 ID 数据库异常。
- 处理原则：**任何点击/打牌动作发生前，必须先恢复到非预览原卡**。

## 2. 代码位置

- 入口：`Scripts/Entry.cs`
- 主要补丁：`Scripts/Patch/NGridCardHolderPatch.cs`
- 当前文件中同时包含：
  - `NGridCardHolderPatch`
  - `NHandCardHolderPatch`
  - 预览逻辑节点 `TimeShiftGridPatch` / `TimeShiftHandPatch`

## 3. Build / Lint / Test 命令

在目录 `src/Mods/TimeShift` 下执行。

### 3.1 恢复与构建

- 依赖恢复：`dotnet restore TimeShift.csproj`
- Debug 构建：`dotnet build TimeShift.csproj -c Debug`
- Release 构建：`dotnet build TimeShift.csproj -c Release`

### 3.2 代码格式与静态检查

- 检查格式（不改文件）：`dotnet format TimeShift.csproj --verify-no-changes`
- 自动格式化：`dotnet format TimeShift.csproj`

说明：仓库未配置独立 lint 工具（如 StyleCop/roslynator），以 `dotnet format` + 编译警告作为基础质量门禁。

### 3.3 测试

- 当前仓库 **没有测试项目**，`dotnet test` 会因缺少测试 csproj 无法覆盖有效用例。
- 若后续新增测试项目，默认命令：
  - 全量：`dotnet test <TestProject.csproj> -c Debug`
  - 单个测试（重点）：
    - `dotnet test <TestProject.csproj> --filter "FullyQualifiedName~Namespace.ClassName.MethodName"`
  - 单个测试类：
    - `dotnet test <TestProject.csproj> --filter "FullyQualifiedName~Namespace.ClassName"`

## 4. 运行与日志验证

- 构建后 DLL 会通过 `TimeShift.csproj` 的 `Copy Mod` 目标复制到游戏 `mods` 目录。
- 日志路径：`C:\Users\temp\AppData\Roaming\SlayTheSpire2\logs\godot.log`
- 日志级别：优先 `Log.Info`（`Log.Debug` 默认不可见）。
- 日志前缀统一：`[TimeShift]`

建议验证用例：

- 奖励界面：按住/松开 `Shift`，观察预览切换与恢复。
- 战斗手牌：按住 `Shift` 后直接点击、拖拽、松开，确认不会再出现 combat ID database 报错。
- 已升级卡牌：按住 `Shift` 时应显示基础版。

## 5. C# 代码风格

### 5.1 导入与命名空间

- `using` 分组：系统库 → 第三方库 → 项目内命名空间。
- 删除未使用 `using`。
- 文件作用域命名空间：`namespace TimeShift.Scripts.Patch;`

### 5.2 命名规则

- 类型/方法/属性：`PascalCase`
- 局部变量/参数/私有字段：`camelCase`
- 私有字段使用前缀 `_`（如 `_baseCard`）
- 常量：`SCREAMING_SNAKE_CASE`（如果项目中已有）

### 5.3 类型与可空

- 保持显式类型，避免无意义 `var`。
- 已启用 `<Nullable>enable</Nullable>`，必须处理 `null` 分支。
- 对外可空引用使用 `?`，访问前做短路或守卫。

### 5.4 格式化

- 使用 4 空格缩进。
- 大括号与现有 C# 风格保持一致（K&R）。
- 单行 `if` 仅用于非常简单返回；复杂逻辑使用完整块。

### 5.5 错误处理与防御式编程

- 补丁逻辑先做前置判空：`Holder == null`、`CardNode == null`、`_baseCard == null`。
- 对输入事件处理要幂等：重复触发恢复逻辑不得导致状态错乱。
- 不吞异常；必要时记录 `Log.Info` 说明上下文。

## 6. Harmony 补丁规范

- 优先 `Postfix` 做附加行为，`Prefix` 做“前置修正/保护”。
- 仅在必要时 patch 高频方法，避免过多反射与重逻辑。
- 若涉及点击/打牌流程，优先在 `TryPlayCard` 前恢复原卡，避免克隆卡进入战斗动作。
- 补丁方法命名建议：`PrefixXxx` / `PostfixXxx`，清晰表达时机。

## 7. TimeShift 业务规则（关键）

- 预览状态不应改变真实战斗数据，仅改变展示模型。
- 一旦发生点击（任意鼠标键）或准备打牌，必须立即恢复 `_baseCard`。
- 恢复后应重置状态位（如 `_isShowingPreview`、`_wasShiftPressed`）。
- `_ExitTree` 时应兜底恢复，防止节点销毁时遗留预览模型。

## 8. 提交改动前自检清单

- 能编译通过：`dotnet build TimeShift.csproj -c Debug`
- 格式通过：`dotnet format TimeShift.csproj --verify-no-changes`
- 手工验证至少覆盖：奖励界面 + 战斗手牌 + 已升级卡牌
- 日志中不再出现：
  - `could not be found in combat ID database`

## 9. Cursor / Copilot 规则兼容

- 已检查：未发现 `.cursor/rules/`、`.cursorrules`。
- 已检查：未发现 `.github/copilot-instructions.md`。
- 因此本文件即当前仓库内代理执行的主规则。

## 10. 已知报错样例（供定位）

- 典型异常：`System.InvalidOperationException: Card ... could not be found in combat ID database!`
- 触发链路：`TryManualPlay -> PlayCardAction -> NetCombatCardDb.GetCardId`
- 结论：点击时仍持有预览克隆卡进入战斗播放流程。
- 修复策略：输入点击与 `TryPlayCard` 前双保险恢复原卡。

## 11. 推荐工作流（给代理）

- 第一步先阅读：`Scripts/Patch/NGridCardHolderPatch.cs` 与 `Scripts/Entry.cs`。
- 第二步再修改：优先小步改动，避免一次性重写整文件。
- 第三步本地验证：先 `dotnet build`，再 `dotnet format --verify-no-changes`。
- 第四步日志核对：检查 `godot.log` 中 `[TimeShift]` 输出是否与预期一致。
- 第五步提交代码：本会话有改动时，执行 `git add -A && git commit -m "<message>"`。

## 12. 常见误区与规避

- 不要把预览克隆卡写回真实战斗状态。
- 不要只在 `Shift` 松开时恢复，必须覆盖“点击触发打牌”路径。
- 不要假设 `_Input` 一定先于打牌逻辑执行，需在 `TryPlayCard` 前再做保护。
- 不要依赖 `Log.Debug` 进行关键问题排查，默认看不到。

## 13. 额外说明

- 若新增测试工程，请同步更新本文件的测试命令示例。
- 若未来添加 `.cursor/rules/`、`.cursorrules` 或 `.github/copilot-instructions.md`，需在本文件明确摘录关键约束。
- 本文件是 `src/Mods/TimeShift` 目录下代理执行任务的第一参考文档。

## 14. PCK 导出与复制（部署）

- 手动导出：在 Godot 中使用“导出 PCK/ZIP”，产出 `TimeShift.pck`。
- 本项目内置复制脚本：`copy_pck_to_game.ps1`。
- 默认来源：`src/Mods/TimeShift/TimeShift.pck`
- 默认目标：`E:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\TimeShift\TimeShift.pck`
- 执行示例（PowerShell）：`powershell -ExecutionPolicy Bypass -File .\copy_pck_to_game.ps1`
- 说明：脚本会自动创建目标目录并覆盖同名文件。

## 15. Release 发布约定

- 发版时必须同时上传两个产物：`TimeShift.pck` 与 `TimeShift.dll`。
- 推荐先执行：`dotnet build TimeShift.csproj -c Release`，再创建 Release。
- `TimeShift.dll` 来源目录：`src/Mods/TimeShift/build/Release/TimeShift.dll`。
- `TimeShift.pck` 来源目录：`src/Mods/TimeShift/TimeShift.pck`。
- 版本号策略：每次发布前必须将版本号递增（至少同步更新 `mod_manifest.json` 与 `TimeShift.json` 的 `version` 字段）。

## 16. JSON 发布规范（新）

- 发版时除 `TimeShift.pck` 与 `TimeShift.dll` 外，必须同时发布 JSON 配置文件：`TimeShift.json`。
- JSON 文件命名规则：`<ModId>.json`，TimeShift 固定使用 `TimeShift.json`（不使用 `.dll.json` 后缀）。
- JSON 内容字段固定为：
  - `id`（字符串）
  - `name`（字符串）
  - `author`（字符串）
  - `description`（字符串）
  - `version`（字符串）
  - `has_pck`（布尔）
  - `has_dll`（布尔）
  - `dependencies`（字符串数组）
  - `affects_gameplay`（布尔）
- `TimeShift` 当前建议模板（按需更新 `version`）：

```json
{
  "id": "TimeShift",
  "name": "TimeShift",
  "author": "磁石战士Ω",
  "description": "按住 Shift 键预览卡牌的升级版/基础版",
  "version": "v0.1",
  "has_pck": true,
  "has_dll": true,
  "dependencies": [],
  "affects_gameplay": true
}
```

- 不重新打包快速发布（仅更新配置）：
  1) 确认 `TimeShift.json` 内容正确并提交。
  2) 直接创建 Release 并上传现有产物：`TimeShift.pck`、`build/Release/TimeShift.dll`、`TimeShift.json`。
  3) 同步复制到游戏目录：`E:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\TimeShift\`。
