# MyrtleSkill 插件事件与技能文档

本文档记录了 MyrtleSkill CS2 插件中所有事件和技能的详细信息。

---

## 统计信息

- **事件总数**: 36 个
- **技能总数**: 24 个

---

## 事件列表

### 1. AnywhereBombPlant (任意下包)
- **文件名**: AnywhereBombPlantEvent.cs
- **内部名称**: AnywhereBombPlant
- **显示名称**: 任意下包
- **描述**: 可以在地图任意位置下包！
- **特殊机制**:
  - 监听玩家按键变化，检测是否按下 Use 键
  - 如果手持 C4，强制设置 `InBombZone = true`
  - 阻止下包被取消
  - 持续检查手持 C4 玩家状态

### 2. AutoBhop (自动Bhop)
- **文件名**: AutoBhopEvent.cs
- **内部名称**: AutoBhop
- **显示名称**: 🐰 自动Bhop
- **描述**: 自动连跳启用！移动速度提升！跳跃更流畅！
- **特殊机制**:
  - 设置 `sv_enablebunnyhopping = true`
  - 设置 `sv_maxspeed = 500`（默认320）
  - 设置 `sv_accelerate = 10`（默认5.5）
  - 保存原始值并在恢复时还原

### 3. Blitzkrieg (闪击行动)
- **文件名**: BlitzkriegEvent.cs
- **内部名称**: Blitzkrieg
- **显示名称**: ⚡ 闪击行动
- **描述**: 游戏速度提升至2倍，一切都在加速进行！
- **特殊机制**:
  - 设置 `host_timescale = 2.0`
  - 恢复时设置为 1.0

### 4. ChickenMode (我是小鸡)
- **文件名**: ChickenModeEvent.cs
- **内部名称**: ChickenMode
- **显示名称**: 🐔 我是小鸡
- **描述**: 所有玩家都变成了小鸡！移速1.1倍，血量50%！禁用大部分武器！
- **特殊机制**:
  - 创建鸡模型跟随玩家移动
  - 设置玩家透明度为0（完全透明）
  - 禁用阴影
  - 玩家缩放到0.2倍
  - 移速1.1倍，血量50
  - 禁用多种武器（步枪、冲锋枪、狙击、霰弹枪、机枪）
  - 使用 CheckTransmit 机制隐藏玩家实体
  - 注册 EventPlayerSpawn 和 EventItemPickup 事件

### 5. DeadlyGrenades (更致命的手雷)
- **文件名**: DeadlyGrenadesEvent.cs
- **内部名称**: DeadlyGrenades
- **显示名称**: 💣 更致命的手雷
- **描述**: 无限高爆手雷！手雷伤害和范围大幅增加！
- **特殊机制**:
  - 设置 `mp_buy_allow_guns = 0`（禁用商店）
  - 设置 `sv_hegrenade_damage_multiplier = 3.0`（3倍伤害）
  - 设置 `sv_hegrenade_radius_multiplier = 5.0`（5倍范围）
  - 设置 `sv_infinite_ammo = 1`（无限弹药）
  - 移除玩家主副武器
  - 给予3颗高爆手雷
  - 投掷手雷后自动补充

### 6. DecoyTeleport (TP弹模式)
- **文件名**: DecoyTeleportEvent.cs
- **内部名称**: DecoyTeleport
- **显示名称**: 🎯 TP弹模式
- **描述**: 投掷诱饵弹后会传送到落点！每回合自动获得诱饵弹。
- **特殊机制**:
  - 监听 EventDecoyStarted 事件
  - 传送玩家到诱饵弹落点位置
  - 临时设置碰撞组为 COLLISION_GROUP_DISSOLVING 防止卡墙
  - 自动给予新诱饵弹

### 7. Foggy (雾蒙蒙)
- **文件名**: FoggyEvent.cs
- **内部名称**: Foggy
- **显示名称**: 🌫 雾蒙蒙
- **描述**: 全员20%亮度！视野一片模糊！
- **特殊机制**:
  - 创建 PostProcessingVolume 实体
  - 设置曝光控制为 0.2（20%亮度）
  - 替换玩家 CameraServices 中的所有 PostProcessingVolumes
  - 恢复时还原原始配置

### 8. HeadshotOnly (只有爆头)
- **文件名**: HeadshotOnlyEvent.cs
- **内部名称**: HeadshotOnly
- **显示名称**: 🎯 只有爆头
- **描述**: 只有爆头才能造成伤害！
- **特殊机制**:
  - 设置 `mp_damage_headshot_only = true`
  - 保存原始值并在恢复时还原
  - 注册 EventPlayerSpawn 事件显示提示

### 9. HighSpeed (高速移动)
- **文件名**: HighSpeedEvent.cs
- **内部名称**: HighSpeed
- **显示名称**: 高速移动
- **描述**: 所有玩家移速翻倍！
- **特殊机制**:
  - 设置玩家速度倍率为 2.0 倍
  - 保存原始速度值并在恢复时还原

### 10. InfiniteBulletMode (无限子弹模式)
- **文件名**: InfiniteBulletModeEvent.cs
- **内部名称**: InfiniteBulletMode
- **显示名称**: 🔥 无限子弹模式
- **描述**: 无限备弹！自动补充！无需换弹！火力全开！
- **特殊机制**:
  - 监听 EventWeaponFire 事件
  - 射击后自动补充弹夹到最大值
  - 监听 EventPlayerSpawn 事件，生成时补充弹药
  - 延迟 0.05 秒补充

### 11. Juggernaut (重装战士)
- **文件名**: JuggernautEvent.cs
- **内部名称**: Juggernaut
- **显示名称**: 重装战士
- **描述**: 所有玩家获得500生命、200护甲，但移速降低30%！
- **特殊机制**:
  - 设置生命值为 500（绝对值）
  - 设置护甲值为 200（绝对值）
  - 设置移速为 0.7 倍
  - 保存原始速度值并在恢复时还原

### 12. JumpOnShoot (射击跳跃)
- **文件名**: JumpOnShootEvent.cs
- **内部名称**: JumpOnShoot
- **显示名称**: 射击跳跃
- **描述**: 开枪时会自动跳跃！仅在地面时触发！
- **特殊机制**:
  - 监听 EventWeaponFire 事件
  - 检查玩家是否在地面（FL_ONGROUND 标志）
  - 给予向上速度 300.0f
  - 空中不触发，防止无限连跳

### 13. JumpPlusPlus (超级跳跃)
- **文件名**: JumpPlusPlusEvent.cs
- **内部名称**: JumpPlusPlus
- **显示名称**: 🦘 超级跳跃
- **描述**: 开枪自动跳跃且无扩散！免疫落地伤害！
- **特殊机制**:
  - 监听 EventWeaponFire 事件
  - 设置 `weapon_accuracy_nospread true`（无扩散）
  - 设置 `sv_falldamage_scale = 0`（免疫落地伤害）
  - 给予向上速度 400.0f
  - 不检测是否在地面

### 14. LowGravity (低重力)
- **文件名**: LowGravityEvent.cs
- **内部名称**: LowGravity
- **显示名称**: 🌑 低重力
- **描述**: 玩家可以跳得更高！
- **特殊机制**:
  - 设置 `sv_gravity` 为原值的 0.5 倍（800→400）
  - 保存原始重力值并在恢复时还原

### 15. LowGravityPlusPlus (超低重力)
- **文件名**: LowGravityPlusPlusEvent.cs
- **内部名称**: LowGravityPlusPlus
- **显示名称**: 🌑 超低重力
- **描述**: 重力大幅降低，空中射击无扩散！
- **特殊机制**:
  - 设置 `sv_gravity` 为原值的 0.2 倍（800→160）
  - 启用 `weapon_accuracy_nospread 1`（无扩散）

### 16. MiniSize (迷你尺寸)
- **文件名**: MiniSizeEvent.cs
- **内部名称**: MiniSize
- **显示名称**: 迷你尺寸
- **描述**: 所有玩家变成迷你尺寸！
- **特殊机制**:
  - 设置玩家模型缩放为当前值的 0.5 倍
  - 保存原始缩放值
  - 使用 AcceptInput("SetScale") 应用缩放

### 17. NoEvent (正常回合)
- **文件名**: NoEventEvent.cs
- **内部名称**: NoEvent
- **显示名称**: 正常回合
- **描述**: 本回合无特殊效果，正常进行
- **特殊机制**:
  - 权重为 40（高于其他事件）
  - 空实现，用于事件系统重置

### 18. OneShot (一发AK)
- **文件名**: OneShotEvent.cs
- **内部名称**: OneShot
- **显示名称**: 💥 一发AK
- **描述**: 所有玩家的枪都只有一发子弹（弹夹）！备用弹药保留！
- **特殊机制**:
  - 监听 EventItemEquip、EventItemPickup、EventPlayerSpawn 事件
  - 修改武器 VData.MaxClip1 为 1
  - 设置当前 Clip1 为 1
  - 保存原始 MaxClip1 值
  - 恢复时还原所有武器的 MaxClip1

### 19. RainyDay (下雨天)
- **文件名**: RainyDayEvent.cs
- **内部名称**: RainyDay
- **显示名称**: 🌧️ 下雨天
- **描述**: 所有玩家隐身！随机每隔3~10秒显形2秒！
- **特殊机制**:
  - 初始所有玩家隐身
  - 随机间隔 3~10 秒显形 2 秒
  - 使用 CheckTransmit 机制控制可见性
  - 记录隐藏实体索引（玩家、武器等）
  - 注册 EventPlayerSpawn、EventPlayerDeath 事件

### 20. ScreamingRabbit (怪叫兔)
- **文件名**: ScreamingRabbitEvent.cs
- **内部名称**: ScreamingRabbit
- **显示名称**: 🐰 怪叫兔
- **描述**: 每隔15秒所有玩家发出定位音效！暴露位置！
- **特殊机制**:
  - 每 15 秒触发一次定位音效
  - 播放前 3 秒倒计时
  - 使用 EmitSound 播放音效
  - 音效列表：种弹声、C4爆炸声、治疗成功声等

### 21. SlowMotion (慢动作)
- **文件名**: SlowMotionEvent.cs
- **内部名称**: SlowMotion
- **显示名称**: 🎬 慢动作
- **描述**: 游戏速度变为0.5倍！一切都变慢了！
- **特殊机制**:
  - 设置 `host_timescale = 0.5`
  - 保存原始值并在恢复时还原

### 22. SmallAndDeadly (小而致命)
- **文件名**: SmallAndDeadlyEvent.cs
- **内部名称**: SmallAndDeadly
- **显示名称**: 小而致命
- **描述**: 玩家体型缩小至0.4倍，只有1滴血！一击必杀！
- **特殊机制**:
  - 设置玩家血量为 1 HP
  - 设置玩家模型缩放为 0.4 倍
  - 恢复时重置为 1.0

### 23. StayQuiet (保持安静)
- **文件名**: StayQuietEvent.cs
- **内部名称**: StayQuiet
- **显示名称**: 🤫 保持安静
- **描述**: 保持安静时隐身！发出声音会现身！
- **特殊机制**:
  - 监听声音事件（UserMessage ID=208）
  - 检测脚步声和其他声音
  - 发出声音时现身
  - 3 秒后重新隐身
  - 使用 CheckTransmit 机制控制可见性

### 24. Strangers (不认识的人)
- **文件名**: StrangersEvent.cs
- **内部名称**: Strangers
- **显示名称**: 👥 不认识的人
- **描述**: 所有人的模型都一样！可以对友军造成伤害！不显示小地图！
- **特殊机制**:
  - 启用友军伤害 `mp_friendlyfire = true`
  - 启用"队友是敌人"模式 `mp_teammates_are_enemies = 1`
  - 禁用小地图 `sv_radar_enable = 0`
  - 所有玩家使用统一模型
  - 随机传送所有玩家到不同位置

### 25. SuperpowerXray (超能力者)
- **文件名**: SuperpowerXrayEvent.cs
- **内部名称**: SuperpowerXray
- **显示名称**: 🦸 超能力者
- **描述**: 双方各有一名玩家获得透视能力！只有超能力者能看到敌人位置！
- **特殊机制**:
  - 随机选择 T 和 CT 各一名超能力者
  - 为所有敌人添加发光效果
  - 使用 CheckTransmit 控制可见性
  - 只有超能力者和观察者能看到发光效果

### 26. SwapOnHit (击中交换)
- **文件名**: SwapOnHitEvent.cs
- **内部名称**: SwapOnHit
- **显示名称**: 击中交换
- **描述**: 击中敌人时会交换位置！
- **特殊机制**:
  - 监听 EventPlayerHurt 事件
  - 交换攻击者和受害者的位置和角度
  - 防止自己对自己触发

### 27. TeleportOnDamage (受伤传送)
- **文件名**: TeleportOnDamageEvent.cs
- **内部名称**: TeleportOnDamage
- **显示名称**: 受伤传送
- **描述**: 受到伤害时会随机传送到场上玩家之前经过的位置！
- **特殊机制**:
  - 从位置记录器获取所有玩家的历史位置
  - 随机选择一个位置传送
  - 临时设置碰撞组防止卡墙

### 28. TopTierParty (顶级狂欢)
- **文件名**: TopTierPartyEvent.cs
- **内部名称**: TopTierParty
- **显示名称**: 🎊 顶级狂欢
- **描述**: 顶级狂欢！同时启用两个随机事件！混乱与乐趣并存！
- **特殊机制**:
  - 随机选择两个不同的事件
  - 过滤掉 NoEvent 和 TopTierParty 系列
  - 同时应用两个事件
  - 恢复时按相反顺序恢复

### 29. TopTierPartyPlusPlus (顶级狂欢++)
- **文件名**: TopTierPartyPlusPlusEvent.cs
- **内部名称**: TopTierPartyPlusPlus
- **显示名称**: 🎊🎊 顶级狂欢++
- **描述**: 终极狂欢！同时启用三个随机事件！绝对的混乱与极致的乐趣！
- **特殊机制**:
  - 随机选择三个不同的事件
  - 过滤掉 NoEvent 和 TopTierParty 系列
  - 同时应用三个事件
  - 恢复顺序：第三 → 第二 → 第一（LIFO）

### 30. UnluckyCouples (苦命鸳鸯)
- **文件名**: UnluckyCouplesEvent.cs
- **内部名称**: UnluckyCouples
- **显示名称**: 💑 苦命鸳鸯
- **描述**: 玩家两两配对！配对玩家互相可见且伤害增加！
- **特殊机制**:
  - 随机配对玩家（单数玩家被忽略）
  - 为配对玩家添加粉红色发光效果
  - 配对玩家互相伤害 2 倍
  - 使用 CheckTransmit 控制可见性

### 31. Vampire (吸血鬼)
- **文件名**: VampireEvent.cs
- **内部名称**: Vampire
- **显示名称**: 吸血鬼
- **描述**: 造成伤害时吸取等量生命值！
- **特殊机制**:
  - 监听 EventPlayerHurt 事件
  - 造成伤害时回复等量生命值
  - 击杀额外奖励 50 生命值
  - 无生命值上限

### 32. Xray (全员透视)
- **文件名**: XrayEvent.cs
- **内部名称**: Xray
- **显示名称**: 👁️ 全员透视
- **描述**: 所有玩家可以透过墙壁看到彼此！敌我位置一览无余！
- **特殊机制**:
  - 为所有玩家添加发光效果
  - T 队橙色，CT 队天蓝色
  - 所有人都能看到所有发光效果
  - 使用 CheckTransmit 控制传输

### 33. NoSkill (没有技能)
- **文件名**: NoSkillEvent.cs
- **内部名称**: NoSkill
- **显示名称**: 😌 没有技能
- **描述**: 这是更加平静的一天，所有人都没有技能！
- **特殊机制**:
  - 设置 `Plugin.DisableSkillsThisRound = true`
  - 禁用本回合的技能分配

---

## 技能列表

### 1. AntiFlash (防闪光)
- **文件名**: AntiFlashSkill.cs
- **内部名称**: AntiFlash
- **显示名称**: ✨ 防闪光
- **描述**: 免疫闪光弹！你的闪光弹持续7秒！获得3颗闪光弹（投掷后自动补充）！
- **类型**: 被动技能
- **特殊机制**:
  - 监听 EventPlayerBlind 事件
  - 免疫所有闪光弹
  - 自己的闪光弹持续 7 秒
  - 获得 3 颗闪光弹
  - 投掷后自动补充（上限3颗）

### 2. BotSummon (召唤队友)
- **文件名**: BotSummonSkill.cs
- **内部名称**: BotSummon
- **显示名称**: 🤖 召唤队友
- **描述**: 召唤机器人助阵，一回合一次！
- **类型**: 主动技能
- **冷却时间**: 9999秒（一回合一次）
- **特殊机制**:
  - 追踪每回合是否已使用
  - 增加 bot_quota
  - 随机加入机器人到玩家队伍
  - 重命名为"[召唤] 玩家名的助手"

### 3. Darkness (黑暗)
- **文件名**: DarknessSkill.cs
- **内部名称**: Darkness
- **显示名称**: 🌑 黑暗
- **描述**: 随机对一名敌人施加黑暗效果
- **类型**: 主动技能
- **冷却时间**: 30秒
- **特殊机制**:
  - 随机选择一名敌人
  - 施加 0.01 亮度（黑暗效果）
  - 持续 10 秒
  - 与雾蒙蒙事件互斥

### 4. DecoyXRay (透视诱饵弹)
- **文件名**: DecoyXRaySkill.cs
- **内部名称**: DecoyXRay
- **显示名称**: 💣 透视诱饵弹
- **描述**: 开局3个诱饵弹，爆炸显示敌人位置！
- **类型**: 主动技能
- **冷却时间**: 9999秒（一局只能用一次）
- **特殊机制**:
  - 给予 3 个诱饵弹
  - 诱饵弹落地立即爆炸
  - 爆炸范围内敌人发光
  - 持续 10 秒
  - 只有投掷者队友能看到发光效果
  - 与全员透视事件互斥

### 5. Disarm (裁军)
- **文件名**: DisarmSkill.cs
- **内部名称**: Disarm
- **显示名称**: ✂ 裁军
- **描述**: 击中敌人时有30%几率使其掉落武器！
- **类型**: 被动技能
- **特殊机制**:
  - 监听 EventPlayerHurt 事件
  - 30% 概率触发
  - 让敌人掉落当前武器
  - 刀和 C4 不掉落

### 6. DumbBot (笨笨机器人)
- **文件名**: DumbBotSkill.cs
- **内部名称**: DumbBot
- **显示名称**: 🤖 笨笨机器人
- **描述**: 召唤300血肉盾，没枪但能抗！
- **类型**: 主动技能
- **冷却时间**: 9999秒（一回合一次）
- **特殊机制**:
  - 召唤 300 血机器人
  - 移除所有武器
  - 持续监控防止捡枪
  - 追踪笨笨机器人列表

### 7. EnemySpin (敌人旋转)
- **文件名**: EnemySpinSkill.cs
- **内部名称**: EnemySpin
- **显示名称**: 🔄 敌人旋转
- **描述**: 攻击敌人时有40%几率使其旋转180度！让敌人迷失方向！
- **类型**: 被动技能
- **特殊机制**:
  - 监听 EventPlayerHurt 事件
  - 40% 概率触发
  - 旋转敌人 180 度（Y轴）
  - 传送保持位置，只改变角度

### 8. ExplosiveShot (爆炸射击)
- **文件名**: ExplosiveShotSkill.cs
- **内部名称**: ExplosiveShot
- **显示名称**: 💥 爆炸射击
- **描述**: 射击时有20%-30%几率在目标位置引发爆炸！
- **类型**: 被动技能
- **特殊机制**:
  - 监听伤害前事件
  - 每个玩家随机分配 20%-30% 概率
  - 创建 HE 手雷弹道
  - 爆炸伤害 25，半径 210
  - 使用特殊角度识别自己创建的爆炸

### 9. FlashJump (闪光跳跃)
- **文件名**: FlashJumpSkill.cs
- **内部名称**: FlashJump
- **显示名称**: ✈️ 闪光跳跃
- **描述**: 你的闪光弹会让敌人飞起来！致盲时间越长飞得越高！
- **类型**: 被动技能
- **特殊机制**:
  - 监听 EventPlayerBlind 事件
  - 根据致盲时长计算跳跃速度
  - 基础速度 200，每秒增加 200
  - 最大速度 800
  - 获得 3 颗闪光弹，投掷后自动补充

### 10. Glaz (格拉兹)
- **文件名**: GlazSkill.cs
- **内部名称**: Glaz
- **显示名称**: 🌫 格拉兹
- **描述**: 你可以透过烟雾弹看到东西！
- **类型**: 被动技能
- **特殊机制**:
  - 监听烟雾弹爆炸和过期事件
  - 追踪所有存活的烟雾弹
  - 使用 CheckTransmit 移除烟雾弹实体

### 11. HeavyArmor (重甲战士)
- **文件名**: HeavyArmorSkill.cs
- **内部名称**: HeavyArmor
- **显示名称**: 🛡️ 重甲战士
- **描述**: 获得200护甲！60%伤害减免！移速80%！
- **类型**: 被动技能
- **特殊机制**:
  - 设置护甲值为 200
  - 伤害减免 60%（返回 0.4 倍数）
  - 移速降低到 80%

### 12. HighJump (超级跳跃)
- **文件名**: HighJumpSkill.cs
- **内部名称**: HighJump
- **显示名称**: 🦘 超级跳跃
- **描述**: 跳跃高度大幅提升！
- **类型**: 被动技能
- **特殊机制**:
  - 设置重力为 0.5
  - 调用 `Utilities.SetStateChanged` 通知客户端
  - 与低重力事件互斥

### 13. KillerFlash (杀手闪电)
- **文件名**: KillerFlashSkill.cs
- **内部名称**: KillerFlash
- **显示名称**: ⚡ 杀手闪电
- **描述**: 你的闪光弹变得致命！任何被完全致盲的人都会死亡（包括你自己！）
- **类型**: 被动技能
- **特殊机制**:
  - 监听 EventPlayerBlind 事件
  - 致盲持续时间 >= 2.2 秒时触发
  - 造成 999 点伤害
  - 使用 SkillUtils.DealDamage 造成伤害

### 14. MasterThief (顶级小偷)
- **文件名**: MasterThiefSkill.cs
- **内部名称**: MasterThief
- **显示名称**: 🎭 顶级小偷
- **描述**: 传送至敌方出生点！神不知鬼不觉！
- **类型**: 主动技能
- **冷却时间**: 15秒
- **特殊机制**:
  - 根据队伍选择敌方出生点
  - 随机选择一个敌方出生点
  - 传送玩家到该位置

### 15. Meito (名刀)
- **文件名**: MeitoSkill.cs
- **内部名称**: Meito
- **显示名称**: ⚔️ 名刀
- **描述**: 致命伤害时取消伤害并获得0.5秒无敌！每回合限用一次！
- **类型**: 被动技能
- **特殊机制**:
  - 在伤害造成前处理（Pre阶段）
  - 检查伤害是否致命
  - 取消伤害并保持原始血量
  - 给予 0.5 秒无敌状态
  - 每回合只能触发一次
  - 使用 ConcurrentDictionary 追踪使用状态

### 16. QuickShot (速射)
- **文件名**: QuickShotSkill.cs
- **内部名称**: QuickShot
- **显示名称**: ⚡ 速射
- **描述**: 无后坐力！射速最大化！瞬间开火！
- **类型**: 被动技能
- **特殊机制**:
  - 每帧移除后坐力
  - 设置武器下次攻击时间为当前时间
  - 实现"瞬开"效果

### 17. RadarHack (雷达黑客)
- **文件名**: RadarHackSkill.cs
- **内部名称**: RadarHack
- **显示名称**: 📡 雷达黑客
- **描述**: 雷达上可以看到敌人！知晓敌人位置！
- **类型**: 被动技能
- **特殊机制**:
  - 每帧更新
  - 设置敌人在雷达上可见
  - 设置 C4 在雷达上可见
  - 与全员透视事件互斥

### 18. SecondChance (第二次机会)
- **文件名**: SecondChanceSkill.cs
- **内部名称**: SecondChance
- **显示名称**: 🔄 第二次机会
- **描述**: 死亡后，你会以相同的生命值复活！每回合限用一次！
- **类型**: 被动技能
- **特殊机制**:
  - 监听 EventPlayerHurt 事件
  - 检查血量 <= 0 且未使用过
  - 复活时设置血量为 50
  - 保留护甲值
  - 传送到随机出生点
  - 每回合只能使用一次

### 19. SpeedBoost (速度提升)
- **文件名**: SpeedBoostSkill.cs
- **内部名称**: SpeedBoost
- **显示名称**: ⚡ 速度提升
- **描述**: 移动速度提升50%！
- **类型**: 被动技能
- **特殊机制**:
  - 设置速度倍率为 1.5 倍
  - 保存原始速度值并在恢复时还原

### 20. Sprint (短跑)
- **文件名**: SprintSkill.cs
- **内部名称**: Sprint
- **显示名称**: 💨 短跑
- **描述**: 进行第二次跳跃以冲刺！按住移动方向键可以冲刺到该方向！
- **类型**: 被动技能
- **特殊机制**:
  - 监听跳跃按键
  - 第二次跳跃时触发冲刺
  - 根据按下的方向键决定冲刺方向
  - 向上速度 150，水平速度 600

### 21. TeamWhip (鞭策队友)
- **文件名**: TeamWhipSkill.cs
- **内部名称**: TeamWhip
- **显示名称**: 💉 鞭策队友
- **描述**: 射击队友将伤害转化为治疗量！不会造成友军伤害！
- **类型**: 被动技能
- **特殊机制**:
  - 在伤害造成前处理（Pre阶段）
  - 检查是否为队友
  - 取消伤害并治疗
  - 治疗倍数 100%（伤害量完全转化为治疗量）

### 22. Teleport (瞬间移动)
- **文件名**: TeleportSkill.cs
- **内部名称**: Teleport
- **显示名称**: 🌀 瞬间移动
- **描述**: 传送到玩家历史位置！
- **类型**: 主动技能
- **冷却时间**: 15秒
- **特殊机制**:
  - 从位置记录器获取所有玩家的历史位置
  - 随机选择一个位置传送
  - 临时设置碰撞组防止卡墙

### 23. ToxicSmoke (有毒烟雾弹)
- **文件名**: ToxicSmokeSkill.cs
- **内部名称**: ToxicSmoke
- **显示名称**: ☠️ 有毒烟雾弹
- **描述**: 开局3个有毒烟雾弹，持续伤害敌人！
- **类型**: 主动技能
- **冷却时间**: 9999秒（一局只能用一次）
- **特殊机制**:
  - 给予 3 个烟雾弹
  - 修改烟雾颜色为紫色（255, 0, 255）
  - 每 17 tick（约0.27秒）造成 5 点伤害
  - 烟雾半径 180
  - 持续追踪有毒烟雾位置

### 24. DarknessSkill (黑暗)
- **同 DarknessSkill.cs**

---

## 附录

### 技术要点

#### 客户端-服务器同步
- **关键方法**: `Utilities.SetStateChanged(entity, "ClassName", "PropertyName")`
- **用途**: 通知客户端实体属性已更改
- **常见场景**:
  - 修改重力后: `Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_flGravityScale")`
  - 修改速度后: `Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_flVelocityModifier")`
  - 修改透明度后: `Utilities.SetStateChanged(entity, "CBaseModelEntity", "m_clrRender")`

#### CheckTransmit 机制
- **用途**: 控制实体对特定玩家的可见性
- **实现**: 通过移除 `info.TransmitEntities` 中的实体索引
- **应用场景**: 隐身、发光效果控制等

#### 发光效果实现
- **创建两个实体**: modelRelay（不可见）和 modelGlow（发光）
- **设置颜色**: 根据队伍设置不同颜色
- **跟随实体**: 使用 `AcceptInput("FollowEntity")`
- **控制可见性**: 通过 CheckTransmit 控制谁能看到

#### ConVar 常用命令
- `sv_gravity`: 重力（默认 800）
- `sv_infinite_ammo`: 无限弹药（0/1）
- `mp_friendlyfire`: 友军伤害（0/1）
- `mp_buy_allow_guns`: 允许购买枪械（0/1）
- `host_timescale`: 游戏速度（默认 1.0）
- `sv_cheats`: 作弊模式（0/1）
- `bot_quota`: 机器人数量
- `bot_difficulty`: 机器人难度（0-3）
- `mp_teammates_are_enemies`: 队友是敌人模式（0/1）
- `sv_radar_enable`: 雷达启用（0/1）
- `weapon_accuracy_nospread`: 无扩散（true/false）

#### 状态保存原则
- **关键**: 在应用效果前保存原始状态
- **错误示例**: 先修改缩放，再保存原始值（保存的已是修改后的值）
- **正确示例**: 先保存当前缩放值，再应用修改

---

**文档生成时间**: 2025-02-03
**插件版本**: 基于 MyrtleSkill CS2 插件
