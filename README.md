# TimeShift

TimeShift 是一个《杀戮尖塔2》Mod：按住 `Shift` 预览卡牌“另一面”。

## 功能

- 基础卡按住 `Shift` 显示升级版预览。
- 已升级卡按住 `Shift` 显示基础版预览。
- 支持场景：
  - 卡组/奖励等网格卡牌（`NGridCardHolder`）
  - 战斗手牌（`NHandCardHolder`）
  - 商店卡牌（`NMerchantCard`）
- 点击保护：任意非滚轮鼠标点击会先恢复原卡，避免预览克隆卡进入真实流程导致异常。
- 滚轮不触发恢复，方便在按住 `Shift` 时滚动浏览。

## 目录结构

```text
Mods/TimeShift/
├── Scripts/
│   ├── Entry.cs
│   └── Patch/
│       └── NGridCardHolderPatch.cs
├── AGENTS.md
├── TimeShift.csproj
├── mod_manifest.json
└── README.md
```

## 构建

在 `src/Mods/TimeShift` 目录执行：

```powershell
dotnet restore TimeShift.csproj
dotnet build TimeShift.csproj -c Debug
```

构建后 DLL 会根据 `TimeShift.csproj` 自动复制到游戏 `mods` 目录。

## 代码格式检查

```powershell
dotnet format TimeShift.csproj --verify-no-changes
```

## 测试说明

当前仓库没有测试工程。若后续新增测试项目，可使用：

```powershell
dotnet test <TestProject.csproj> -c Debug
dotnet test <TestProject.csproj> --filter "FullyQualifiedName~Namespace.Class.Method"
```

## 运行验证建议

- 奖励界面：按住/松开 `Shift`，确认预览切换正确。
- 战斗手牌：按住 `Shift` 后直接点击或拖拽，确认不报 combat ID database 错误。
- 商店界面：按住 `Shift` 预览卡牌，点击购买前应恢复原卡。
- 卡组浏览：滚轮滚动时保持预览，不应出现显示与点击对象错位。

## 日志

- 日志文件：`C:\Users\temp\AppData\Roaming\SlayTheSpire2\logs\godot.log`
- 关键前缀：`[TimeShift]`
- 建议使用 `Log.Info`（`Log.Debug` 默认不可见）

## 整合包下载

- 夸克网盘链接（含 Mod 管理器与 `Mods` 目录）：
  - `https://pan.quark.cn/s/b89dbac25ba4`
- 分享口令：`/~88063M0Ul6~:/`
- 使用方式：
  - 可直接下载整合包，使用其中的 Mod 管理器。
  - 也可只下载其中的 `Mods` 目录，手动复制到游戏根目录（`Slay the Spire 2`）下覆盖/合并。

## 已知注意事项

- 预览使用克隆卡仅用于展示，不能进入真实战斗/奖励结算路径。
- 如出现异常，请优先检查日志中是否存在“点击前恢复原卡”相关输出。
