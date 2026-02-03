# Myrtle Skill Plugin 文档

## 简介

Myrtle Skill Plugin 是一个 Counter-Strike 2 (CS2) 娱乐插件，为服务器添加随机事件和玩家技能系统，增加游戏的趣味性和多样性。

## 版本信息

- **插件名称**: Myrtle Skill Plugin
- **当前版本**: v2.2.0
- **CS2Sharp API 版本**: 1.0.362
- **事件数量**: 39 个
- **技能数量**: 25 个

## 功能概述

### 1. 娱乐事件系统

每回合随机选择一个全局事件，影响所有玩家。

**当前事件数量**: 36 个（包括 NoEvent 和 NoSkill）

### 2. 玩家技能系统

每个玩家在回合开始时随机获得一个个人技能，可以是主动或被动效果。

**当前技能数量**: 24 个

### 3. 开局福利系统

每回合随机为玩家分配初始福利（武器、装备等）。

### 4. 机器人控制系统

玩家死后可以控制机器人继续战斗。

### 5. 执行顺序

系统按以下顺序初始化：

1. **开局福利** - 为玩家分配初始福利
2. **娱乐事件** - 随机选择全局事件
3. **玩家技能** - 为每个玩家分配个人技能（延迟1秒确保事件已完全应用）

---

## 娱乐事件列表

### 事件快速参考表

| 内部名称             | 显示名称        | 描述                                                             |
| -------------------- | --------------- | ---------------------------------------------------------------- |
| AnywhereBombPlant    | 任意下包        | 可以在地图任意位置下包！                                         |
| AutoBhop             | 🐰 自动Bhop     | 自动连跳启用！移动速度提升！跳跃更流畅！                         |
| Blitzkrieg           | ⚡ 闪击行动     | 游戏速度提升至2倍，一切都在加速进行！                            |
| ChickenMode          | 🐔 我是小鸡     | 所有玩家都变成了小鸡！移速1.1倍，血量50%！                       |
| DeadlyGrenades       | 💣 更致命的手雷 | 无限高爆手雷！移除主副武器！禁用商店！手雷伤害和范围增加！       |
| DecoyTeleport        | 🎯 TP弹模式     | 投掷诱饵弹后会传送到落点！每回合自动获得诱饵弹！                 |
| HeadshotOnly         | 🎯 只有爆头     | 只有爆头才能造成伤害！                                           |
| HighSpeed            | 高速移动        | 所有玩家移速翻倍！                                               |
| InfiniteAmmo         | 🔥 无限子弹模式 | 无限备弹！自动补充！无需换弹！火力全开！                         |
| Juggernaut           | 🛡️ 重装战士     | 所有玩家获得500生命、200护甲，但移速降低30%！                    |
| JumpOnShoot          | 射击跳跃        | 开枪时会自动跳跃！仅在地面时触发！                               |
| JumpPlusPlus         | 🦘 超级跳跃      | 开枪自动跳跃且无扩散！免疫落地伤害！                             |
| KeepMoving           | 🏃 永动机       | 所有玩家必须持续按住 W 键！没按住的话每 0.75 秒扣 10 滴血！       |
| LowGravity           | 🌑 低重力       | 玩家可以跳得更高！                                               |
| LowGravityPlusPlus   | 🌑 超低重力     | 重力大幅降低，空中射击无扩散！                                   |
| MiniSize             | 迷你尺寸        | 所有玩家变成迷你尺寸！                                           |
| NoEvent              | 正常回合        | 本回合无特殊效果，正常进行                                       |
| NoSkill              | 😌 没有技能     | 这是更加平静的一天，所有人都没有技能！                           |
| OneShot              | 💥 一发AK       | 所有玩家的枪都只有一发子弹（弹夹）！备用弹药保留！               |
| RainyDay             | 🌧️ 下雨天     | 所有玩家隐身！随机每隔3~10秒显形2秒！                            |
| ScreamingRabbit      | 🐰 怪叫兔      | 每隔15秒所有玩家发出定位音效！暴露位置！                         |
| SlowMotion           | 🎬 慢动作       | 游戏速度变为0.5倍！一切都变慢了！                                |
| SmallAndDeadly       | 小而致命        | 玩家体型缩小至0.4倍，只有1滴血！一击必杀！                       |
| StayQuiet            | 🤫 保持安静     | 保持安静时隐身！发出声音会现身！                                 |
| Strangers            | 👥 不认识的人   | 所有人模型都一样！可以对友军造成伤害！不显示小地图！随机出生点！ |
| SuperpowerXray       | 🦸 超能力者     | 双方各有一名玩家获得透视能力！只有超能力者能看到敌人位置！       |
| SwapOnHit            | 击中交换        | 击中敌人时会交换位置！                                           |
| SoccerMode           | ⚽ 足球模式     | 没收所有物品！禁用商店！T家生成足球！球进CT区给T发AK！           |
| SuperKnockback       | 💪 超强推背   | 造成伤害时强力击退敌人！把你打飞！                               |
| SuperRecoil          | 💥 超强反冲   | 开枪时会有超强后坐力！把自己弹飞！                               |
| Bankruptcy           | 💸 全员破产     | 所有人都破产了！金币只有800！                                   |
| Deaf                 | 🔇 失聪        | 随机敌人听不到所有声音！                                       |
| TeleportOnDamage     | 受伤传送        | 受到伤害时会随机传送到地图上的其他位置！                         |
| TopTierParty         | 🎊 顶级狂欢     | 顶级狂欢！同时启用两个随机事件！混乱与乐趣并存！                 |
| TopTierPartyPlusPlus | 🎊🎊 顶级狂欢++ | 终极狂欢！同时启用三个随机事件！绝对的混乱与极致的乐趣！         |
| UnluckyCouples       | 💑 苦命鸳鸯     | 玩家两两配对！配对玩家互相可见且伤害增加！单数玩家将被忽略！     |
| Vampire              | 吸血鬼          | 造成伤害时吸取等量生命值！                                       |
| Xray                 | 👁️ 全员透视   | 所有玩家可以透过墙壁看到彼此！敌我位置一览无余！                 |

### 事件权重配置

默认权重分布（可配置）：

- **NoEvent**: 100
- **NoSkill**: 100
- **KeepMoving**: 10
- **SoccerMode**: 10
- **SuperRecoil**: 10
- **TopTierParty**: 5
- **TopTierPartyPlusPlus**: 2
- **其他事件**: 10

**概率计算**：

- 总权重 ≈ 607
- NoEvent 概率 ≈ 16.5%
- NoSkill 概率 ≈ 16.5%
- KeepMoving 概率 ≈ 1.6%
- SoccerMode 概率 ≈ 1.6%
- SuperRecoil 概率 ≈ 1.6%
- TopTierParty 概率 ≈ 0.8%
- TopTierPartyPlusPlus 概率 ≈ 0.3%
- 其他每个事件概率 ≈ 1.6%

---

## 玩家技能列表

### 技能快速参考表

| 内部名称       | 显示名称          | 描述                                   | 类型 | 冷却时间 |
| -------------- | ----------------- | -------------------------------------- | ---- | -------- |
| AntiFlash      | ✨ 防闪光         | 免疫闪光弹！你的闪光弹持续7秒！获得3颗闪光弹！              | 被动 | 无       |
| BotSummon      | 🤖 召唤队友       | 召唤机器人助阵，一回合一次！                       | 主动 | 9999秒   |
| Darkness       | 🌑 黑暗           | 随机对一名敌人施加黑暗效果，持续10秒！               | 主动 | 30秒     |
| Deaf           | 🔇 失聪           | 随机让一名敌人听不到所有声音！持续10秒！               | 主动 | 30秒     |
| DecoyXRay      | 💣 透视诱饵弹     | 开局3个诱饵弹，爆炸显示敌人位置！                   | 主动 | 9999秒   |
| Disarm         | ✂️ 裁军           | 攻击敌人有40%几率使其掉落武器！                      | 被动 | 无       |
| DumbBot        | 🤖 笨笨机器人     | 召唤300血肉盾，没枪但能抗！                         | 主动 | 9999秒   |
| EnemySpin      | 🔄 敌人旋转       | 攻击敌人有40%几率使其旋转180度！                    | 被动 | 无       |
| ExplosiveShot  | 💥 爆炸射击       | 射击时有20%-30%几率在目标位置引发爆炸！               | 被动 | 无       |
| FlashJump      | ✈️ 闪光跳跃       | 被闪时会自动向前方冲刺逃生！获得3颗闪光弹！             | 被动 | 无       |
| Glaz           | 🌫 格拉兹         | 可以透过烟雾看到敌人！获得3颗烟雾弹！                 | 被动 | 无       |
| HeavyArmor     | 🛡️ 重甲战士       | 获得200护甲！60%伤害减免！移速80%！                | 被动 | 无       |
| HighJump       | 🦘 宇航员          | 重力降低至70%，跳跃更高！                          | 被动 | 无       |
| KillerFlash    | ⚡ 杀手闪电       | 被闪时立即对范围内敌人造成高额伤害！获得3颗闪光弹！         | 被动 | 无       |
| MasterThief    | 🎭 顶级小偷       | 传送至敌方出生点！神不知鬼不觉！                     | 主动 | 15秒     |
| Meito          | ⚔️ 名刀           | 每10秒一次致命伤害保护，保留1滴血！一回合限用一次！      | 被动 | 无       |
| QuickShot      | ⚡ 速射           | 无后坐力！射速最大化！瞬间开火！                     | 被动 | 无       |
| RadarHack      | 📡 雷达黑客       | 雷达上可以看到敌人！知晓敌人位置！                    | 被动 | 无       |
| SecondChance   | 🔄 第二次机会     | 死亡后以50血量复活！每回合限用一次！                  | 被动 | 无       |
| SpeedBoost     | ⚡ 速度提升       | 移动速度提升50%！                            | 被动 | 无       |
| Sprint         | 💨 短跑           | 进行第二次跳跃以冲刺！按住移动方向键可以冲刺到该方向！      | 被动 | 无       |
| TeamWhip       | 💉 鞭策队友       | 射击队友将伤害转化为治疗量！不会造成友军伤害！            | 被动 | 无       |
| Teleport       | 🌀 瞬间移动       | 传送到玩家历史位置！                             | 主动 | 15秒     |
| ToxicSmoke     | ☠️ 有毒烟雾弹     | 开局3个有毒烟雾弹，持续伤害敌人！                     | 主动 | 9999秒   |
| Wallhack       | 👁️ 透视技能       | 可以透过墙壁看到敌人！                             | 被动 | 无       |

### 技能类型说明

- **被动技能**: 自动生效，无需玩家操作
- **主动技能**: 需要玩家通过命令或按键激活，有冷却时间

### 技能权重配置

默认权重（可配置）：

- **所有技能**: 10（除非特别指定）

### 事件互斥系统

某些技能可以指定与哪些事件互斥。在特定事件激活时，互斥的技能不会被抽到。

**互斥示例**：

- `HighJump`（宇航员）与 `LowGravity`、`LowGravityPlusPlus`、`JumpPlusPlus` 事件互斥
- 原因：这些效果都会影响跳跃/重力，避免效果重叠或冲突

---

## 控制台命令

### 娱乐事件系统

| 命令                                 | 说明                             |
| ------------------------------------ | -------------------------------- |
| `css_event_enable`                 | 启用娱乐事件系统                 |
| `css_event_disable`                | 禁用娱乐事件系统                 |
| `css_event_status`                 | 查看当前事件信息                 |
| `css_event_list`                   | 列出所有可用事件                 |
| `css_event_weight <事件名> [权重]` | 查看/设置事件权重                |
| `css_event_weights`                | 查看所有事件权重                 |
| `css_forceevent <事件名>`          | 强制下回合触发指定事件（调试用） |

### 玩家技能系统

| 命令                                 | 说明                           |
| ------------------------------------ | ------------------------------ |
| `css_skill_enable`                 | 启用玩家技能系统               |
| `css_skill_disable`                | 禁用玩家技能系统               |
| `css_skill_status`                 | 查看技能系统状态和当前技能     |
| `css_skill_list`                   | 列出所有可用技能               |
| `css_skill_weight <技能名> [权重]` | 查看/设置技能权重              |
| `css_skill_weights`                | 查看所有技能权重               |
| `css_useskill`                     | 使用/激活你的技能（主动技能）  |
| `css_forceskill <技能名> [玩家名]` | 强制赋予玩家指定技能（调试用） |

### 开局福利系统

| 命令                    | 说明                 |
| ----------------------- | -------------------- |
| `css_welfare_enable`  | 启用开局福利系统     |
| `css_welfare_disable` | 禁用开局福利系统     |
| `css_welfare_status`  | 查看开局福利系统状态 |

### 机器人控制系统

| 命令                       | 说明               |
| -------------------------- | ------------------ |
| `css_botcontrol_enable`  | 启用玩家控制机器人 |
| `css_botcontrol_disable` | 禁用玩家控制机器人 |
| `css_botcontrol_status`  | 查看机器人控制状态 |

### 位置记录器系统

| 命令                  | 说明                         |
| --------------------- | ---------------------------- |
| `css_pos_history`   | 查看你的位置历史（最近10条） |
| `css_pos_clear`     | 清除你的位置历史             |
| `css_pos_stats`     | 查看位置记录器统计信息       |
| `css_pos_clear_all` | 清除所有玩家的位置历史       |

### 炸弹相关功能

| 命令                               | 说明                   |
| ---------------------------------- | ---------------------- |
| `css_allowanywhereplant_enable`  | 启用任意下包功能       |
| `css_allowanywhereplant_disable` | 禁用任意下包功能       |
| `css_allowanywhereplant_status`  | 查看任意下包功能状态   |
| `css_bombtimer_set <秒数>`       | 设置炸弹爆炸时间（秒） |
| `css_bombtimer_status`           | 查看炸弹爆炸时间       |

---

## 配置文件

插件使用 JSON 格式的配置文件管理权重。

**配置文件位置**: `/addons/counterstrikesharp/addons/MyrtleSkill/config.json`

**配置结构**:

```json
{
  "EventWeights": {
    "NoEvent": 100,
    "LowGravity": 10,
    // ... 其他事件权重
  },
  "SkillWeights": {
    "Teleport": 10,
    "SpeedBoost": 10,
    "HighJump": 10
  },
  "Notes": "权重越高，事件/技能被选中的概率越大。设置为0可禁用某个事件/技能。"
}
```

### 修改权重

1. 编辑配置文件
2. 修改对应事件/技能的权重值
3. 重启服务器或重载插件

**权重为0** = 禁用该事件/技能

---

## 使用方法

### 1. 安装插件

1. 将编译好的 `MyrtleSkill.dll` 文件复制到：

   ```
   /addons/counterstrikesharp/addons/MyrtleSkill/
   ```
2. 确保 `config.json` 文件也在同一目录

### 2. 启用功能

在服务器控制台输入：

**启用娱乐事件系统**：

```
css_event_enable
```

**启用玩家技能系统**：

```
css_skill_enable
```

**启用开局福利系统**：

```
css_welfare_enable
```

**启用机器人控制**：

```
css_botcontrol_enable
```

### 3. 玩家使用技能

**获得技能**：

- 回合开始时自动获得随机技能
- 系统会在聊天框显示技能名称和描述

**使用主动技能**：

- 在聊天框输入：`!useskill` 或 `css_useskill`
- 等待冷却时间结束
- 技能效果立即生效

**查看状态**：

```
css_skill_status
```

---

## 技术细节

### 代码架构

```
MyrtleSkill/
├── MyrtleSkill.cs              # 主插件类
├── MyrtleSkill.csproj         # 项目配置文件
├── Core/
│   ├── PluginCommands.cs       # 所有控制台命令
│   └── PluginConfig.cs        # 配置文件处理
├── Events/
│   ├── EntertainmentEvent.cs    # 事件基类
│   ├── EntertainmentEventManager.cs # 事件管理器
│   └── [32个事件文件]         # 各个具体事件
├── Skills/
│   ├── PlayerSkill.cs         # 技能基类
│   ├── PlayerSkillManager.cs  # 技能管理器
│   └── [25个技能文件]         # 各个具体技能
├── Features/
│   ├── WelfareManager.cs      # 开局福利管理器
│   ├── BotManager.cs          # 机器人控制管理器
│   ├── BombPlantManager.cs    # 炸弹功能管理器
│   └── PositionRecorder.cs    # 位置记录器
└── ThirdParty/
    └── NavMesh.cs             # 导航网格系统
```

### 命名空间

- **MyrtleSkill** - 主命名空间
- **MyrtleSkill.Core** - 核心功能
- **MyrtleSkill.Skills** - 技能系统
- **MyrtleSkill.Features** - 功能模块

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

A: 在 `config.json` 中将对应事件/技能的权重设置为 0。

### Q: 技能冷却时间在哪里配置？

A: 目前冷却时间是硬编码在技能类中（`Cooldown` 属性）。未来版本将支持配置文件中自定义。

### Q: 为什么我的技能没有被分配？

A: 检查以下情况：

1. 技能系统是否已启用（`css_skill_enable`）
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
  - 新增 `css_useskill` 命令
- ✅ 新增 32 个娱乐事件

  - AutoBhop, Blitzkrieg, ChickenMode, DeadlyGrenades
  - DecoyTeleport, HeadshotOnly, OneShot, RainyDay
  - ScreamingRabbit, SlowMotion, StayQuiet
  - Strangers, SuperpowerXray, TopTierParty
  - TopTierPartyPlusPlus, UnluckyCouples, Xray
  - AnywhereBombPlant, InfiniteAmmo, Juggernaut
  - 以及更多...
- ✅ 新增 25 个玩家技能

  - Teleport（主动技能 - 瞬间移动）
  - SpeedBoost（被动技能 - 速度提升）
  - HighJump（被动技能，带事件互斥）
  - Glaz、AntiFlash、FlashJump、SecondChance
  - SummonAllies、SlayerLightning、Disarm
  - 以及更多...

#### 核心改进

- ⚡ 优化执行顺序：开局福利 → 事件 → 技能
- 🔧 实现事件互斥系统，避免效果冲突
- 📊 重构伤害倍数计算，支持多个修正器叠加
- ⚙️ 提高"平静回合"概率（NoEvent/NoSkill 权重提升至 100）
- 🎁 添加开局福利系统，随机分配初始装备
- 🤖 实现机器人控制，玩家死后可继续战斗
- 📍 添加位置记录器，支持传送技能

#### 技术细节

- 新增 `Skills/` 目录和完整技能框架
- 实现 `PlayerSkill` 和 `PlayerSkillManager` 类
- 添加技能管理命令（enable/disable/status/list/weight）
- 实现冷却追踪和 UI 提示系统
- 添加位置记录和历史查询功能

---

## 许可证

本项目采用 **GNU General Public License v3.0 (GPLv3)** 开源。

© 2026 MyrtleSkill Plugin Contributors

**许可证信息**：

- ✅ 允许商业使用
- ✅ 允许修改
- ✅ 允许分发
- ✅ 允许私人使用
- ⚠️ **Copyleft 条款**：衍生作品必须使用相同的许可证
- ⚠️ **必须披露源代码**：分发时必须提供源代码
- ⚠️ **必须包含原始许可证**：复制和分发时必须包含 GPLv3 许可证

**主要特点**：

- 这是一个强 copyleft 许可证，要求所有修改和衍生作品也使用 GPLv3
- 分发二进制形式时，必须同时提供源代码
- 提供源代码的方式包括：与软件一起分发、或提供下载链接（至少保留 3 年）
- 任何修改都必须明确标注，并注明修改日期

**免责声明**：

本插件按"原样"提供，不提供任何形式的明示或暗示保证。作者不对任何索赔或损害负责。

**使用说明**：

- 本插件仅供娱乐和教育用途
- 请在测试服务器上充分测试后再部署到生产环境
- 某些功能可能影响游戏平衡性，请根据实际情况调整配置
- 任何基于本代码的修改和衍生作品都必须同样使用 GPLv3 许可证

**完整许可证文本**：

请查看项目根目录下的 `LICENSE` 文件，或访问：
https://www.gnu.org/licenses/gpl-3.0.txt

---

## 致谢与来源

### 原始项目

本项目参考并使用了以下开源项目的代码和实现思路：

**[jRandomSkills](https://github.com/Juzlus/jRandomSkills)** by Juzlus

- 许可证：GNU General Public License v3.0
- GitHub: https://github.com/Juzlus/jRandomSkills
- 项目描述：CS2 随机技能插件，为游戏添加混乱和乐趣

**特别感谢以下功能的参考实现**：

- **爆炸射击技能** (`ExplosiveShotSkill.cs`)
  - 参考了 jRandomSkills 的爆炸子弹实现
  - 使用了相同的 HEGrenadeProjectile_CreateFunc 签名
  - 采用了特殊角度标识和参数传递机制

### 修改说明

本项目基于 jRandomSkills 的实现进行了以下修改和扩展：

1. **架构重构**：
   - 采用面向对象的事件和技能基类设计
   - 实现了完整的插件化架构

2. **功能扩展**：
   - 添加了永动机事件（KeepMovingEvent）
   - 实现了独立的事件和技能管理系统
   - 新增了多个原创技能和事件

3. **代码优化**：
   - 改进了代码组织和可维护性
   - 添加了详细的中文注释和文档
   - 优化了部分功能的实现逻辑

4. **本地化**：
   - 完全中文化的界面和文档
   - 针对中文用户进行了优化

**修改日期**：2026年2月

**修改者**：MyrtleSkill Plugin Contributors

---

## 支持与贡献

### 如何贡献

欢迎提交 Issue 和 Pull Request！

1. Fork 本项目
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启 Pull Request

### 联系方式

如有问题、建议或 Bug 报告，请通过以下方式联系：

- **提交 Issue**: [GitHub Issues](待添加)
- **讨论区**: [GitHub Discussions](待添加)
- **项目位置**: E:\cs-plugin\HelloWorldPlugin

---

**祝游戏愉快！** 🎮
