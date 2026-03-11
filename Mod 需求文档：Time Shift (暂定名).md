# Time Shift Mod 需求文档

## 1. 核心概述

**功能目标**：允许玩家在浏览卡牌界面时，通过按下 **Shift** 键实时预览当前悬停卡牌的"另一面"。

**设计模式**：原位替换 (In-place Swap)

---

## 2. 交互逻辑

### 触发机制
- **按下 Shift**：当鼠标悬停在任意卡牌上时，显示其"另一面"
- **松开 Shift**：卡牌恢复至原始状态

### 转换逻辑
- 基础版 (Normal) → 显示升级版 (Upgraded)
- 升级版 (Upgraded) → 显示基础版 (Normal)
- 不可升级卡牌 → 保持原样

### 重要：点击处理
- **按住 Shift 点击卡牌**：自动恢复原始卡牌后再处理点击
- 原因：预览使用的是克隆卡牌，未加入游戏状态，直接点击会导致报错：
  - 奖励界面：`must be added to a RunState before adding it to your deck`
  - 战斗：`could not be found in combat ID database`

---

## 3. 适用场景

Mod 应在以下所有包含卡牌实例的 UI 面板中生效：

1. **Master Deck View**：查看自己当前的牌组
2. **Compendium**：卡牌图鉴
3. **Reward Screens**：战斗后的三选一界面
4. **Shop Screen**：商店购买界面
5. **Card Select Screen**：如吸血鬼事件、铁匠铺选择升级等弹出界面
6. **战斗中手牌**：预览手牌升级效果

---

## 4. 技术实现

### 核心类
- `NGridCardHolderPatch` - 处理网格卡牌Holder（奖励、商店、牌组等）
- `NHandCardHolderPatch` - 处理战斗中的手牌

### 实现方式
1. 通过 Harmony Patch 在 `NGridCardHolder._Ready` 和 `NHandCardHolder._Ready` 后添加子节点
2. 子节点负责监听 Shift 键状态并切换卡牌显示

### 关键方法
- `_Ready()` - 初始化时保存原始卡牌 (`_baseCard`) 和生成升级版本 (`_upgradedCard`)
- `_Process()` - 检测 Shift 键状态变化，切换预览
- `_Input()` - 检测鼠标点击，点击时恢复原始卡牌

### 核心逻辑
```csharp
// 初始化
_baseCard = Holder.CardNode.Model;
_upgradedCard = (CardModel)_baseCard.MutableClone();
_upgradedCard.UpgradeInternal();

// 预览逻辑
if (_baseCard.IsUpgraded)  // 原始已升级
    显示基础版
else                         // 原始未升级
    显示升级版

// 点击时恢复
if (点击 && 正在预览)
    恢复原始卡牌
```

---

## 5. 性能优化

- **预实例化**：在 `_Ready` 时生成升级版本，避免运行时克隆
- **缓存机制**：`_baseCard` 和 `_upgradedCard` 保存在内存中

---

## 6. 调试

- 日志前缀：`[TimeShift]`
- 日志位置：`%APPDATA%\SlayTheSpire2\logs\godot.log`
- 重要：使用 `Log.Info` 输出（`Log.Debug` 默认不输出）

---

## 7. 已知问题

1. ~~奖励界面点击报错~~ - 已修复
2. ~~战斗预览报错~~ - 已修复
3. 需要验证已升级卡牌的 Shift 预览是否正确显示

---

## 8. 待完成

- [ ] 验证已升级卡牌的 Shift 预览
- [ ] 添加视觉反馈（预览时的光效或文字）
- [ ] 测试更多界面
