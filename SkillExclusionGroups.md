# MyrtleSkill 技能互斥组文档

本文档记录了 MyrtleSkill CS2 插件中技能互斥组的详细配置信息。

---

## 概述

技能互斥组用于防止某些功能冲突或效果重叠的技能同时被分配给同一个玩家。当玩家在历史记录中拥有某个互斥组内的技能时，该组内的其他技能将不会被分配给该玩家。

**状态**: 🔴 **未启用** - 互斥组属性已添加，但过滤逻辑尚未在 `PlayerSkillManager.SelectRandomSkill()` 中启用。

---

## 互斥组列表

### 1. 🚀 闪光弹组

**互斥原因**: 都使用闪光弹作为主要机制，效果会相互冲突

| 技能名称 | 内部名称 | 图标 | 类型 | 互斥技能 |
|---------|---------|------|------|---------|
| 防闪光 | AntiFlash | ✨ | 被动 | FlashJump, KillerFlash |
| 闪光跳跃 | FlashJump | ✈️ | 被动 | AntiFlash, KillerFlash |
| 杀手闪电 | KillerFlash | ⚡ | 被动 | AntiFlash, FlashJump |

**技能描述**:
- **防闪光**: 免疫闪光弹，你的闪光弹持续7秒，获得闪光弹（投掷后自动补充）
- **闪光跳跃**: 你的闪光弹会让敌人飞起来！致盲时间越长飞得越高！
- **杀手闪电**: 你的闪光弹变得致命！任何被完全致盲的人都会死亡（包括你自己！）

---

### 2. 🌫️ 烟雾弹组

**互斥原因**: 格拉兹可以透过烟雾看到东西，与有毒烟雾弹的效果冲突

| 技能名称 | 内部名称 | 图标 | 类型 | 互斥技能 |
|---------|---------|------|------|---------|
| 格拉兹 | Glaz | 🌫 | 被动 | ToxicSmoke |
| 有毒烟雾弹 | ToxicSmoke | ☠️ | 主动 | Glaz |

**技能描述**:
- **格拉兹**: 你可以透过烟雾弹看到东西！
- **有毒烟雾弹**: 开局有毒烟雾弹，持续伤害敌人！

---

### 3. 👁️ 视野组

**互斥原因**: 都是显示敌人位置的技能，效果重叠

| 技能名称 | 内部名称 | 图标 | 类型 | 互斥技能 |
|---------|---------|------|------|---------|
| 透视 | Wallhack | 👁️ | 被动 | RadarHack, DecoyXRay |
| 雷达黑客 | RadarHack | 📡 | 被动 | Wallhack, DecoyXRay |
| 透视诱饵弹 | DecoyXRay | 💣 | 主动 | Wallhack, RadarHack |

**技能描述**:
- **透视**: 你可以透过墙壁看到所有敌人的位置！
- **雷达黑客**: 雷达上可以看到敌人！知晓敌人位置！
- **透视诱饵弹**: 开局诱饵弹，爆炸显示敌人位置！

---

### 4. ⚔️ 保命组

**互斥原因**: 都是复活/免死机制，同时拥有会过强

| 技能名称 | 内部名称 | 图标 | 类型 | 互斥技能 |
|---------|---------|------|------|---------|
| 名刀 | Meito | ⚔️ | 被动 | SecondChance |
| 第二次机会 | SecondChance | 🔄 | 被动 | Meito |

**技能描述**:
- **名刀**: 致命伤害时取消伤害并获得0.5秒无敌！每回合限用一次！
- **第二次机会**: 死亡后，你会以相同的生命值复活！每回合限用一次！

---

### 5. 🏃 移动组

**互斥原因**: 移速效果叠加可能过强，或者移动机制冲突

| 技能名称 | 内部名称 | 图标 | 类型 | 互斥技能 |
|---------|---------|------|------|---------|
| 速度提升 | SpeedBoost | ⚡ | 被动 | HeavyArmor, Sprint |
| 重甲战士 | HeavyArmor | 🛡️ | 被动 | SpeedBoost, Sprint |
| 短跑 | Sprint | 💨 | 被动 | SpeedBoost, HeavyArmor |

**技能描述**:
- **速度提升**: 移动速度提升50%！
- **重甲战士**: 获得200护甲！60%伤害减免！移速80%！
- **短跑**: 进行第二次跳跃以冲刺！按住移动方向键可以冲刺到该方向！

---

## 技能互斥关系图

```
🚀 闪光弹组
├── ✨ 防闪光 ───┐
├── ✈️ 闪光跳跃 ──┼─ 互斥 ──
└── ⚡ 杀手闪电 ──┘

🌫️ 烟雾弹组
├── 🌫 格拉兹 ─────┬─ 互斥 ──
└── ☠️ 有毒烟雾弹 ─┘

👁️ 视野组
├── 👁️ 透视 ──────┐
├── 📡 雷达黑客 ───┼─ 互斥 ──
└── 💣 透视诱饵弹 ─┘

⚔️ 保命组
├── ⚔️ 名刀 ──────┬─ 互斥 ──
└── 🔄 第二次机会 ─┘

🏃 移动组
├── ⚡ 速度提升 ───┐
├── 🛡️ 重甲战士 ───┼─ 互斥 ──
└── 💨 短跑 ───────┘
```

---

## 配置文件对应

### PlayerSkill 基类属性

所有技能通过重写 `ExcludedSkills` 属性来定义互斥关系：

```csharp
/// <summary>
/// 与该技能互斥的技能名称列表。如果玩家已有这些技能，该技能不会被抽到。
/// </summary>
public virtual List<string> ExcludedSkills { get; } = new();
```

### 配置示例

```csharp
// 闪光跳跃技能配置
public class FlashJumpSkill : PlayerSkill
{
    public override string Name => "FlashJump";
    public override string DisplayName => "✈️ 闪光跳跃";
    public override bool IsActive => false;

    // 定义与该技能互斥的其他技能
    public override List<string> ExcludedSkills => new() { "AntiFlash", "KillerFlash" };
}
```

---

## 启用互斥过滤

### 当前状态

❌ **未启用** - `PlayerSkillManager.SelectRandomSkill()` 方法中**未使用** `ExcludedSkills` 属性进行过滤。

### 如何启用

在 `Skills/PlayerSkillManager.cs` 的 `SelectRandomSkill` 方法中添加以下逻辑：

```csharp
// 收集玩家历史中的所有互斥技能名称
var excludedSkillNames = new HashSet<string>();
if (playerHistory != null)
{
    foreach (var historySkillName in playerHistory)
    {
        var historySkill = GetSkill(historySkillName);
        if (historySkill != null)
        {
            // 将该历史技能的互斥技能列表添加到排除集合
            foreach (var excludedName in historySkill.ExcludedSkills)
            {
                excludedSkillNames.Add(excludedName);
            }
        }
    }
}

// 在过滤时添加互斥技能检查
var availableSkills = _skills.Values
    .Where(s => s.Weight > 0)
    .Where(s => string.IsNullOrEmpty(currentEventName) || !s.ExcludedEvents.Contains(currentEventName))
    .Where(s => playerHistory == null || !playerHistory.Contains(s.Name))
    .Where(s => !excludedSkillNames.Contains(s.Name)) // ← 添加此行
    .ToList();
```

---

## 未配置互斥的技能

以下技能**未配置**互斥关系：

- 🤖 召唤队友 (BotSummon)
- 🌑 黑暗 (Darkness)
- 🔇 失聪 (Deaf)
- ✂ 裁军 (Disarm)
- 🤖 笨笨机器人 (DumbBot)
- 🔄 敌人旋转 (EnemySpin)
- 💥 爆炸射击 (ExplosiveShot)
- 👨‍🚀 宇航员 (HighJump)
- 🎭 顶级小偷 (MasterThief)
- ⚡ 速射 (QuickShot)
- 💉 鞭策队友 (TeamWhip)
- 🌀 瞬间移动 (Teleport)

---

## 版本历史

- **V2.6.0** (2026-02-03): 创建技能互斥组系统，添加 `ExcludedSkills` 属性
  - 配置5个互斥组：闪光弹组、烟雾弹组、视野组、保命组、移动组
  - 涉及13个技能
  - 互斥过滤逻辑暂未启用

---

## 维护说明

### 添加新互斥组

1. 确定互斥的技能列表
2. 在每个技能类中添加 `ExcludedSkills` 属性
3. 列出该组内的其他技能名称
4. 更新本文档

### 修改现有互斥组

1. 修改相关技能的 `ExcludedSkills` 属性
2. 确保互斥关系对称（A排除B，B也排除A）
3. 更新本文档

### 注意事项

- ⚠️ 主动技能之间的互斥关系由手动维护
- ⚠️ 互斥关系应该**对称**：如果A排除B，那么B也应该排除A
- ⚠️ 确保使用正确的**内部名称**（`Name` 属性），而非显示名称
