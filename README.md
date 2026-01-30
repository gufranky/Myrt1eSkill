# Myrtle Skill Plugin 文档

## 简介

Myrtle Skill Plugin 是一个 Counter-Strike 2 (CS2) 娱乐插件，为服务器添加随机事件和玩家技能系统，增加游戏的趣味性和多样性。

## 版本信息

- **插件名称**: Myrtle Skill Plugin
- **当前版本**: v2.0.0
- **CS2Sharp API 版本**: 1.0.362

## 功能概述

### 1. 娱乐事件系统

每回合随机选择一个全局事件，影响所有玩家。

### 2. 玩家技能系统

每个玩家在回合开始时随机获得一个个人技能，可以是主动或被动效果。

### 3. 重甲战士系统

随机选择一名玩家获得重甲战士能力。

### 4. 执行顺序

系统按以下顺序初始化：
1. **重甲战士** - 随机选择重甲战士
2. **娱乐事件** - 随机选择全局事件
3. **玩家技能** - 为每个玩家分配个人技能（延迟1秒确保事件已完全应用）

---

## 娱乐事件列表

| 内部名称 | 显示名称 | 描述 |
|---------|---------|------|
| AnywhereBombPlant | 任意下包 | 可以在地图任意位置下包！ |
| AutoBhop | 🐰 自动Bhop | 自动连跳启用！移动速度提升！跳跃更流畅！ |
| Blitzkrieg | ⚡ 闪击行动 | 游戏速度提升至2倍，一切都在加速进行！ |
| ChickenMode | 🐔 我是小鸡 | 所有玩家都变成了小鸡！移速1.1倍，血量50%！ |
| DeadlyGrenades | 💣 更致命的手雷 | 无限高爆手雷！移除主副武器！禁用商店！手雷伤害和范围增加！ |
| DecoyTeleport | 🎯 TP弹模式 | 投掷诱饵弹后会传送到落点！每回合自动获得诱饵弹！ |
| HeadshotOnly | 🎯 只有爆头 | 只有爆头才能造成伤害！ |
| HighSpeed | 高速移动 | 所有玩家移速翻倍！ |
| InfiniteAmmo | 无限弹药 | 弹药永不耗尽！ |
| Juggernaut | 重装战士 | 所有玩家获得500生命、200护甲，但移速降低30%！ |
| JumpOnShoot | 射击跳跃 | 开枪时会自动跳跃！仅在地面时触发！ |
| JumpPlusPlus | 超级跳跃 | 开枪自动跳跃且无扩散！免疫落地伤害！ |
| LowGravity | 低重力 | 玩家可以跳得更高！ |
| LowGravityPlusPlus | 超低重力 | 重力大幅降低，空中射击无扩散！ |
| MiniSize | 迷你尺寸 | 所有玩家变成迷你尺寸！ |
| NoEvent | 正常回合 | 本回合无特殊效果，正常进行 |
| NoSkill | 😌 没有技能 | 这是更加平静的一天，所有人都没有技能！ |
| OneShot | 💥 一发AK | 所有玩家的枪都只有一发子弹（弹夹）！备用弹药保留！ |
| RainyDay | 🌧️ 下雨天 | 所有玩家隐身！随机每隔3~10秒显形2秒！ |
| ScreamingRabbit | 🐰 尖叫兔 | 每隔15秒所有玩家发出定位音效！暴露位置！ |
| SlowMotion | 🎬 慢动作 | 游戏速度变为0.5倍！一切都变慢了！ |
| SmallAndDeadly | 小而致命 | 玩家体型缩小至0.4倍，只有1滴血！一击必杀！ |
| StayQuiet | 🤫 保持安静 | 保持安静时隐身！发出声音会现身！ |
| Strangers | 👥 不认识的人 | 所有人模型都一样！可以对友军造成伤害！不显示小地图！随机出生点！ |
| SuperpowerXray | 🦸 超能力者 | 双方各有一名玩家获得透视能力！只有超能力者能看到敌人位置！ |
| SwapOnHit | 击中交换 | 击中敌人时会交换位置！ |
| TeleportOnDamage | 受伤传送 | 受到伤害时会随机传送到地图上的其他位置！ |
| TopTierParty | 🎊 顶级狂欢 | 顶级狂欢！同时启用两个随机事件！混乱与乐趣并存！ |
| TopTierPartyPlusPlus | 🎊🎊 极顶狂欢++ | 终极狂欢！同时启用三个随机事件！绝对的混乱与极致的乐趣！ |
| UnluckyCouples | 💑 苦命鸳鸯 | 玩家两两配对！配对玩家互相可见且伤害增加！单数玩家将被忽略！ |
| Vampire | 吸血鬼 | 造成伤害时吸取等量生命值！ |
| Xray | 👁️ 全员透视 | 所有玩家可以透过墙壁看到彼此！敌我位置一览无余！ |

### 事件权重配置

默认权重分布（可配置）：
- **NoEvent**: 100
- **NoSkill**: 100
- **其他事件**: 10

**概率计算**：
- 总权重 = 470
- NoEvent 概率 = 21.3%
- NoSkill 概率 = 21.3%
- 其他每个事件概率 = 2.1%

---

## 玩家技能列表

| 内部名称 | 显示名称 | 描述 | 类型 | 冷却时间 | 互斥事件 |
|---------|---------|------|------|---------|----------|
| HighJump | 🦘 超级跳跃 | 跳跃高度大幅提升！ | 被动 | 无 | LowGravity, LowGravityPlusPlus, JumpPlusPlus |
| SpeedBoost | ⚡ 速度提升 | 移动速度提升50%！ | 被动 | 无 | 无 |
| Teleport | 🌀 瞬间移动 | 传送到地图上的随机位置！ | 主动 | 15.0秒 | 无 |

### 技能类型说明

- **被动技能**: 自动生效，无需玩家操作
- **主动技能**: 需要玩家通过命令或按键激活，有冷却时间

### 技能权重配置

默认权重（可配置）：
- **Teleport**: 10
- **SpeedBoost**: 10
- **HighJump**: 10

### 事件互斥系统

技能可以指定与哪些事件互斥。在特定事件激活时，互斥的技能不会被抽到。

**示例**：
- `HighJump` 技能与 `LowGravity`、`LowGravityPlusPlus`、`JumpPlusPlus` 事件互斥
- 原因：这些效果都会影响跳跃/重力，避免效果重叠或冲突

---

## 控制台命令

### 娱乐事件系统

| 命令 | 说明 |
|-------|------|
| `css_event_enable` | 启用娱乐事件系统 |
| `css_event_disable` | 禁用娱乐事件系统 |
| `css_event_status` | 查看当前事件信息 |
| `css_event_list` | 列出所有可用事件 |
| `css_event_weight <事件名> [权重]` | 查看/设置事件权重 |
| `css_event_weights` | 查看所有事件权重 |

### 玩家技能系统

| 命令 | 说明 |
|-------|------|
| `css_skill_enable` | 启用玩家技能系统 |
| `css_skill_disable` | 禁用玩家技能系统 |
| `css_skill_status` | 查看技能系统状态和当前技能 |
| `css_skill_list` | 列出所有可用技能 |
| `css_skill_weight <技能名> [权重]` | 查看/设置技能权重 |
| `css_skill_weights` | 查看所有技能权重 |
| `css_useskill` | 使用/激活你的技能（主动技能） |

### 重甲战士系统

| 命令 | 说明 |
|-------|------|
| `css_heavyarmor_enable` | 启用重甲战士模式 |
| `css_heavyarmor_disable` | 禁用重甲战士模式 |
| `css_heavyarmor_status` | 查看重甲战士状态 |

### 炸弹相关功能

| 命令 | 说明 |
|-------|------|
| `css_allowanywhereplant_enable` | 启用任意下包功能 |
| `css_allowanywhereplant_disable` | 禁用任意下包功能 |
| `css_allowanywhereplant_status` | 查看任意下包功能状态 |
| `css_bombtimer_set <秒数>` | 设置炸弹爆炸时间（秒） |
| `css_bombtimer_status` | 查看炸弹爆炸时间 |

---

## 配置文件

插件使用 JSON 格式的配置文件管理权重。

**配置文件位置**: `/addons/counterstrikesharp/addons/MyrtleSkill/config.json`

**配置结构**:
\`\`\`json
{
  "EventWeights": {
    "NoEvent": 100,
    "LowGravity": 10,
    // ... 其他事件权重
  },
  "SkillWeights": {
{
    "Teleport": 10,
    "SpeedBoost": 10,
    "HighJump": 10
  },
  "Notes": "权重越高，事件/技能被选中的概率越大。设置为0可禁用某个事件/技能。"
}
\`\`\`

### 修改权重

1. 编辑配置文件
2. 修改对应事件/技能的权重值
3. 重启服务器或重载插件

**权重为0** = 禁用该事件/技能

---

## 使用方法

### 1. 安装插件

1. 将编译好的 `MyrtleSkill.dll` 文件复制到：
   \`\`\`
   /addons/counterstrikesharp/addons/MyrtleSkill/
   \`\`\`

2. 确保 `config.json` 文件也在同一目录

### 2. 启用功能

在服务器控制台输入：

**启用娱乐事件系统**：
\`\`\`
css_event_enable
\`\`\`

**启用玩家技能系统**：
\`\`\`
css_skill_enable
\`\`\`

**启用重甲战士系统**：
\`\`\`
css_heavyarmor_enable
\`\`\`

### 3. 玩家使用技能

**获得技能**：
-`回合开始时自动获得随机技能
- 系统会在聊天框显示技能名称和描述

**使用主动技能**：
- 在聊天框输入：`\`!useskill\`\`` 或 `\`css_useskill\`\`
- 等待冷却时间结束
- 技能效果立即生效

**查看状态**：
\`\`\`
css_skill_status
\`\`\`

---

## 技术细节

### 代码架构

\`\`\`
MyrtleSkill/
├── MyrtleSkill.cs              # 主插件类
├── MyrtleSkill.csproj         # 项目配置文件
├── Core/
│   ├── PluginCommands.cs       # 所有控制台命令
│   └── PluginConfig.cs        # 配置文件处理
├── Events/
│   ├── EntertainmentEvent.cs    # 事件基类
│   ├── EntertainmentEventManager.cs # 事件管理器
│   └── [30个事件文件]         # 各个具体事件
├── Skills/
│   ├── PlayerSkill.cs         # 技能基类
│   ├── PlayerSkillManager.cs  # 技能管理器
│   └── [3个技能文件]            # 各个具体技能
├── Features/
│   ├── HeavyArmorManager.cs   # 重甲战士管理器
│   └── BombPlantManager.cs    # 炸弹功能管理器
└── ThirdParty/
    └── NavMesh.cs             # 导航网格系统
\`\`\`

### 命名空间

- `\`MyrtleSkill\`\` - 主命名空间
- `\`MyrtleSkill.Core\`\` - 核心功能
- `\`MyrtleSkill.Skills\`\` - 技能系统
- `\`MyrtleSkill.Features\`\` - 功能模块

### 依赖项

- **.NET 8.0**
- **CounterStrikeSharp.API v1.0.362**

### API 版本兼容性

- 支持 Counter-Strike 2 最新版本
- 使用 CS2Sharp 提供的标准 API
- 无需额外依赖

---

## 常见问题

### Q: 如何禁用某个事件或技能？

A: 在 `\`config.json\`\` 中将对应事件/技能的权重设置为 0。

### Q: 技能冷却时间在哪里配置？

A: 目前冷却时间是硬编码在技能类中（`\`Cooldown\`\` 属性）。未来版本将支持配置文件中自定义。

### Q: 为什么我的技能没有被分配？

A: 检查以下情况：
1. 技能系统是否已启用（`\`css_skill_enable\`\`）
2. 技能权重是否大于 0
3. 当前激活的事件是否与技能互斥

### Q: 可以同时运行事件和技能吗？

A: 可以！系统设计为独立运行，可以：
- 只有事件，没有技能
- 只有技能，没有事件
- 事件 + 技能 同时运行

### Q: 如何查看调试信息？

A: 查看服务器控制台输出，所有重要操作都有日志记录。

---

## 更新日志

### v2.0.0 (2026-01-30)

#### 新增功能

- ✅ 添加完整的玩家技能系统
  - 支持主动和被动技能
  - 实现技能冷却机制
  - 添加事件互斥系统
  - 新增 `\`css_useskill\`\` 命令

- ✅ 新增 18 个娱乐事件
  - AutoBhop, Blitzkrieg, ChickenMode, DeadlyGrenades
  - DecoyTeleport, HeadshotOnly, OneShot, RainyDay
  - ScreamingRabbit, SlowMotion, StayQuiet
  - Strangers, SuperpowerXray, TopTierParty
  - TopTierPartyPlusPlus, UnluckyCouples, Xray

- ✅ 新增 3 个示例技能
  - Teleport（主动技能 - 瞬间移动）
  - SpeedBoost（被动技能 - 速度提升）
  - HighJump（被动技能，带事件互斥）

#### 核心改进

- ⚡ 优化执行顺序：重甲战士 → 事件 → 技能
- 🔧 实现事件互斥系统，避免效果冲突
- 📊 重构伤害倍数计算，支持多个修正器叠加
- ⚙️ 提高"平静回合"概率（NoEvent/NoSkill 权重提升至 100）

#### 技术细节

- 新增 `\`Skills/\`\` 目录和完整技能框架
- 实现 `\`PlayerSkill\`\` 和 `\`PlayerSkillManager\`\` 类
- 添加技能管理命令（enable/disable/status/list/weight）
- 实现冷却追踪和 UI 提示系统

---

## 许可证

本插件遵循 Counter-Strike 2 的插件开发规范。

**注意事项**：
- 本插件仅供娱乐和教育用途
- 请在官方服务器上测试后再部署到生产环境
- 某些功能可能影响游戏平衡性，请谨慎使用

---

## 支持与

如有问题、建议或 Bug 报告，请联系开发者。

**项目位置**: E:\cs-plugin\HelloWorldPlugin
**Git 仓库**: [待添加]

---

**祝游戏愉快！** 🎮
