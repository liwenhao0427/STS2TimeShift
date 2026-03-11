# TimeShift Mod 开发总结

## 需求概述
在卡牌 UI 界面中，按住 **Shift** 键时预览卡牌的"另一面"：
- 基础版卡牌 → 显示升级版
- 升级版卡牌 → 显示基础版

## 当前进度
- ✅ 奖励选择界面预览功能
- ✅ 战斗中手牌预览功能
- ✅ 按住 Shift 点击卡牌时自动恢复原始卡牌（避免报错）

## 已知问题/待修复
1. ~~奖励界面点击报错~~ - 已修复
2. ~~战斗预览报错~~ - 已修复
3. 需要验证已升级卡牌的 Shift 预览是否正确显示

## 技术实现

### 核心类
- `NGridCardHolderPatch` - 处理网格卡牌Holder（奖励、商店、牌组等）
- `NHandCardHolderPatch` - 处理战斗手牌

### 关键逻辑
1. **初始化**：在 `_Ready` 时保存原始卡牌 (`_baseCard`) 和生成升级版本 (`_upgradedCard`)
2. **Shift 检测**：在 `_Process` 中检测 Shift 键状态变化
3. **预览逻辑**：
   - `_baseCard.IsUpgraded == true`（原始已升级）→ 显示基础版
   - `_baseCard.IsUpgraded == false`（原始未升级）→ 显示升级版
4. **点击恢复**：在 `_Input` 中检测鼠标点击，点击时恢复原始卡牌

### 遇到的问题
1. **奖励选择报错**：`MutableClone()` 生成的卡牌未加入游戏状态
   - 原因：预览时替换的 Model 不在游戏中
   - 解决：点击时先恢复原始卡牌再处理点击

2. **战斗预览报错**：`could not be found in combat ID database`
   - 原因：同奖励界面，预览的克隆卡牌不在战斗数据库中
   - 解决：同上，点击时恢复原始卡牌

## 调试日志
- 日志前缀：`[TimeShift]`
- 日志目录：`C:\Users\temp\AppData\Roaming\SlayTheSpire2\logs\godot.log`
- 使用 `Log.Info` 输出（Log.Debug 默认不输出）

## 项目结构
```
Mods/TimeShift/
├── mod_manifest.json
├── TimeShift.csproj
├── project.godot
└── Scripts/
    ├── Entry.cs
    └── Patch/
        └── NGridCardHolderPatch.cs
```

## 下一步
1. 测试已升级卡牌的 Shift 预览是否正确
2. 验证战斗中使用效果
3. 完善日志输出
