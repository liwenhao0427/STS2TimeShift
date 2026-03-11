# DevDeckTools

DevDeckTools 是一个用于《杀戮尖塔2》开发调试的辅助 Mod。

当前目标：提供一个快捷键打开的开发者菜单，用于快速调试卡组与构筑流程。

## 规划功能

- 快捷键打开/关闭作弊菜单（当前默认 `F8`）
- 按卡牌 ID 添加任意卡牌
- 删除卡组中指定卡牌
- 添加/移除任意遗物
- 预设按钮（常用测试组合）

## 当前状态

- 已完成基础项目结构
- 已完成 `NGame` 注入与 `F8` 菜单开关
- 已接入“按 ID 加卡/删卡/加遗物/移除遗物”核心命令
- 已接入模糊搜索候选、回车执行、运行态禁用提示
- 已接入一键“快速预设”

## 构建

在 `src/Mods/DevDeckTools` 目录执行：

```powershell
dotnet restore DevDeckTools.csproj
dotnet build DevDeckTools.csproj -c Debug
```

## 格式检查

```powershell
dotnet format DevDeckTools.csproj --verify-no-changes
```

## 日志

- 日志前缀：`[DevDeckTools]`
- 日志路径：`C:\Users\temp\AppData\Roaming\SlayTheSpire2\logs\godot.log`

## 整合包下载

- 夸克网盘链接（含 Mod 管理器与 `Mods` 目录）：
  - `https://pan.quark.cn/s/b89dbac25ba4`
- 分享口令：`/~88063M0Ul6~:/`
- 使用方式：
  - 可直接下载整合包，使用其中的 Mod 管理器。
  - 也可只下载其中的 `Mods` 目录，手动复制到游戏根目录（`Slay the Spire 2`）下覆盖/合并。

## 文件结构

```text
DevDeckTools/
├── Scripts/
│   ├── Entry.cs
│   ├── DevMenuController.cs
│   ├── Commands/
│   │   └── DevDeckCommandService.cs
│   └── Patch/
│       └── NGamePatch.cs
├── AGENTS.md
├── DevDeckTools.csproj
├── mod_manifest.json
└── README.md
```
