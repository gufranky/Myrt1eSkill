using System;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using MyrtleSkill.Core;
using MyrtleSkill.Features;
using MyrtleSkill.Skills;

namespace MyrtleSkill;

/// <summary>
/// CS2 娱乐事件插件主类
/// </summary>
public class MyrtleSkill : BasePlugin, IPluginConfig<EventWeightsConfig>
{
    public override string ModuleName => "Myrtle Skill Plugin";
    public override string ModuleVersion => "2.0.0";

    // 配置
    public EventWeightsConfig Config { get; set; } = null!;
    public EventWeightsConfig EventConfig { get; set; } = null!;

    // 管理器
    public HeavyArmorManager HeavyArmorManager { get; private set; } = null!;
    public BombPlantManager BombPlantManager { get; private set; } = null!;
    public EntertainmentEventManager EventManager { get; private set; } = null!;
    public PlayerSkillManager SkillManager { get; private set; } = null!;
    public WelfareManager WelfareManager { get; private set; } = null!;
    public BotManager BotManager { get; private set; } = null!;
    public PositionRecorder PositionRecorder { get; private set; } = null!;
    private PluginCommands _commands = null!;

    // 事件状态
    public EntertainmentEvent? CurrentEvent { get; set; }
    public EntertainmentEvent? PreviousEvent { get; set; }
    public string? ForcedEventName { get; set; } = null; // 调试功能：强制下回合的事件

    // 技能系统控制
    public bool DisableSkillsThisRound { get; set; } = false;

    // 静态实例（供技能访问）
    public static MyrtleSkill? Instance { get; private set; }

    public void OnConfigParsed(EventWeightsConfig config)
    {
        Config = config;
        EventConfig = config;
        Console.WriteLine("[配置] 事件权重配置已加载");
    }

    public override void Load(bool hotReload)
    {
        // 设置静态实例
        Instance = this;

        // 初始化娱乐服务器全局设置
        Utils.ServerSettings.InitializeAllSettings();

        // 初始化管理器
        HeavyArmorManager = new HeavyArmorManager(this);
        BombPlantManager = new BombPlantManager();
        EventManager = new EntertainmentEventManager(this);
        SkillManager = new PlayerSkillManager(this);
        WelfareManager = new WelfareManager(this);
        BotManager = new BotManager(this);
        PositionRecorder = new PositionRecorder(this);
        _commands = new PluginCommands(this);

        // 默认启用机器人管理功能
        BotManager.EnableBotControl();

        // 启动位置记录器
        PositionRecorder.Start();

        // 设置技能静态引用（用于技能内部访问插件）
        Skills.TeamWhipSkill.MyrtleSkillPlugin = this;

        // 注册事件处理器
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterEventHandler<EventRoundStart>(OnRoundStart, HookMode.Post);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd, HookMode.Post);
        RegisterListener<Listeners.OnPlayerTakeDamagePre>(OnPlayerTakeDamagePre);
        RegisterListener<Listeners.OnPlayerTakeDamagePost>(OnPlayerTakeDamagePost);
        RegisterEventHandler<EventWeaponFire>(OnWeaponFire, HookMode.Post);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Post);
        RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Post);
        RegisterEventHandler<EventWeaponhudSelection>(OnWeaponHudSelection, HookMode.Pre);
        RegisterEventHandler<EventBombAbortplant>(OnBombAbortPlant, HookMode.Pre);
        RegisterEventHandler<EventBombPlanted>(OnBombPlanted, HookMode.Post);
        RegisterEventHandler<EventItemPickup>(OnItemPickup, HookMode.Pre);
        RegisterEventHandler<EventDecoyStarted>(OnDecoyStarted, HookMode.Post);
        RegisterEventHandler<EventSmokegrenadeDetonate>(OnSmokegrenadeDetonate, HookMode.Post);
        RegisterEventHandler<EventSmokegrenadeExpired>(OnSmokegrenadeExpired, HookMode.Post);
        RegisterEventHandler<EventPlayerBlind>(OnPlayerBlind, HookMode.Post);
        RegisterEventHandler<EventPlayerJump>(OnPlayerJump, HookMode.Post);
        RegisterListener<Listeners.OnPlayerButtonsChanged>(OnPlayerButtonsChanged);
        RegisterListener<Listeners.OnServerPostEntityThink>(OnServerPostEntityThink);
        RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
        RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);

        // 注册命令
        RegisterCommands();

        Console.WriteLine("[Myrtle技能插件] v2.0.0 已加载！");
        Console.WriteLine("[娱乐事件系统] 已初始化，共加载 " + EventManager.GetEventCount() + " 个事件");
        Console.WriteLine("[玩家技能系统] 已初始化，共加载 " + SkillManager.GetSkillCount() + " 个技能");
        Console.WriteLine("[任意下包功能] 状态: " + (BombPlantManager.AllowAnywherePlant ? "✅ 启用" : "❌ 禁用"));
        Console.WriteLine("[炸弹时间设置] 当前时间: " + BombPlantManager.BombTimer + " 秒");
        Console.WriteLine("[友军伤害] ⚔️ 已启用友军伤害");
        Console.WriteLine("[坠落伤害] 🪽 已禁用坠落伤害");
        Console.WriteLine("[友军伤害保护] 已禁用自动踢人功能");
        Console.WriteLine("[派对模式] 🎉 已启用派对模式！");
    }

    #region 事件处理

    private void OnMapStart(string mapName)
    {
        // 地图切换时清理所有位置记录，防止传送到地图外
        PositionRecorder?.ClearAllHistory();
        Console.WriteLine($"[位置记录器] 地图切换到 {mapName}，已清理所有位置记录");
        return;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        // -1. 重新应用娱乐服务器全局设置（防止被其他插件或游戏机制覆盖）
        Utils.ServerSettings.InitializeAllSettings();

        // 0. 开局福利系统（最优先执行）
        WelfareManager.OnRoundStart();

        // 0.1 清除所有机器人
        BotManager.OnRoundStart();

        // 0.2 清理第二次机会使用记录
        Skills.SecondChanceSkill.OnRoundStart();

        // 0.3 清理格拉兹烟雾弹追踪
        Skills.GlazSkill.OnRoundStart();

        // 0.4 清理名刀使用记录
        Skills.MeitoSkill.OnRoundStart();

        // 1. 首先重置技能禁用标志（新回合开始）
        DisableSkillsThisRound = false;

        // 2. 恢复上一回合事件
        if (PreviousEvent != null)
        {
            Console.WriteLine("[娱乐事件] 正在恢复上回合事件: " + PreviousEvent.Name);
            PreviousEvent.OnRevert();
            PreviousEvent = null;
            Console.WriteLine("[娱乐事件] 上回合事件已恢复完毕");
        }
        else
        {
            Console.WriteLine("[娱乐事件] 没有上一回合事件需要恢复（第一回合或PreviousEvent为null）");
        }

        // 3. 选择并应用新事件（第一优先级）
        if (EventManager.IsEnabled)
        {
            // 检查是否有强制事件
            if (!string.IsNullOrEmpty(ForcedEventName))
            {
                Console.WriteLine("[娱乐事件] 检测到强制事件: " + ForcedEventName);
                CurrentEvent = EventManager.GetEvent(ForcedEventName);
                ForcedEventName = null; // 清除强制事件

                if (CurrentEvent == null)
                {
                    Console.WriteLine("[娱乐事件] 警告：找不到强制的事件 '" + ForcedEventName + "'，改用随机选择");
                    CurrentEvent = EventManager.SelectRandomEvent();
                }
                else
                {
                    Console.WriteLine("[娱乐事件] 成功获取强制事件: " + CurrentEvent.Name);
                }
            }
            else
            {
                CurrentEvent = EventManager.SelectRandomEvent();
            }

            if (CurrentEvent != null)
            {
                Console.WriteLine("[娱乐事件] 本回合事件: " + CurrentEvent.DisplayName + " - " + CurrentEvent.Description);
                CurrentEvent.OnApply();

                // 立即把新事件保存为PreviousEvent（用于下回合恢复）
                Console.WriteLine("[娱乐事件] 保存本回合事件: " + CurrentEvent.Name + " 为PreviousEvent");
                PreviousEvent = CurrentEvent;

                // 显示事件提示（包括 NoEvent）
                foreach (var p in Utilities.GetPlayers())
                {
                    if (p.IsValid)
                    {
                        p.PrintToChat("───────────────────");
                        p.PrintToChat("🎲 " + CurrentEvent.DisplayName);
                        p.PrintToChat("📝 " + CurrentEvent.Description);
                        p.PrintToChat("───────────────────");
                    }
                }
                AddTimer(3.0f, () =>
                {
                    foreach (var p in Utilities.GetPlayers())
                    {
                        if (p.IsValid)
                        {
                            p.PrintToCenter("━━━━━━━━━━━━━━━━\n " + CurrentEvent.DisplayName + "\n━━━━━━━━━━━━━━━━");
                        }
                    }
                });
            }
        }

        // 5. 为所有玩家应用技能（第三优先级，延迟1秒确保事件已完全应用）
        if (SkillManager.IsEnabled && !DisableSkillsThisRound)
        {
            Console.WriteLine("[技能系统] 准备为玩家应用技能...");
            AddTimer(1.0f, () =>
            {
                Console.WriteLine("[技能系统] 开始应用技能到所有玩家");
                SkillManager.ApplySkillsToAllPlayers();
            });
        }
        else if (DisableSkillsThisRound)
        {
            Console.WriteLine("[技能系统] 本回合技能已被事件禁用，原因: DisableSkillsThisRound=" + DisableSkillsThisRound);
            foreach (var p in Utilities.GetPlayers())
            {
                if (p.IsValid)
                {
                    p.PrintToChat("🚫 本回合技能已被禁用！");
                }
            }
        }
        else
        {
            Console.WriteLine("[技能系统] 技能系统未启用");
        }

        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        // 保存当前事件为上一回合事件
        if (CurrentEvent != null)
        {
            Console.WriteLine("[娱乐事件] 回合结束，保存当前事件: " + CurrentEvent.Name + " 为PreviousEvent");
            PreviousEvent = CurrentEvent;
            CurrentEvent = null;
        }
        else
        {
            Console.WriteLine("[娱乐事件] 回合结束，但没有当前事件需要保存");
        }

        // 重置技能禁用标志
        DisableSkillsThisRound = false;

        // 移除所有玩家技能
        if (SkillManager.IsEnabled)
        {
            SkillManager.RemoveAllPlayerSkills();
        }

        // 清理笨笨机器人记录
        Skills.DumbBotSkill.ClearDumbBots();

        // 清理透视诱饵弹记录
        Skills.DecoyXRaySkill.ClearAllDecoys();

        // 清理有毒烟雾弹记录
        Skills.ToxicSmokeSkill.ClearAllToxicSmokes();

        return HookResult.Continue;
    }

    private HookResult OnPlayerTakeDamagePre(CCSPlayerPawn player, CTakeDamageInfo info)
    {
        // 处理爆炸射击技能
        Skills.ExplosiveShotSkill.HandlePlayerDamagePre(player, info);

        // 收集所有伤害倍数修正器
        float totalMultiplier = 1.0f;

        // 处理重甲战士减伤
        var controller = player.Controller.Value;
        if (controller != null && controller.IsValid && controller is CCSPlayerController csController)
        {
            var skill = SkillManager.GetPlayerSkill(csController);
            if (skill?.Name == "HeavyArmor")
            {
                var heavyArmorSkill = (Skills.HeavyArmorSkill)skill;
                float? heavyArmorMultiplier = heavyArmorSkill?.HandleDamage(player, info);
                if (heavyArmorMultiplier.HasValue)
                {
                    totalMultiplier *= heavyArmorMultiplier.Value;
                }
            }
        }

        // 处理鞭策队友技能（在Pre阶段处理，取消伤害并治疗）
        float? teamWhipMultiplier = Skills.TeamWhipSkill.HandleDamagePre(player, info);
        if (teamWhipMultiplier.HasValue)
        {
            totalMultiplier *= teamWhipMultiplier.Value;
        }

        // 处理苦命鸳鸯配对伤害加成
        if (CurrentEvent is UnluckyCouplesEvent couplesEvent)
        {
            float? couplesMultiplier = couplesEvent.HandleDamagePre(player, info);
            if (couplesMultiplier.HasValue)
            {
                totalMultiplier *= couplesMultiplier.Value;
            }
        }

        // 处理名刀技能（致命伤害保护）- 在所有其他倍数之后处理
        float? meitoMultiplier = Skills.MeitoSkill.HandleDamagePre(player, info, totalMultiplier);
        if (meitoMultiplier.HasValue)
        {
            totalMultiplier *= meitoMultiplier.Value;
        }

        // 应用伤害倍数
        if (totalMultiplier != 1.0f)
        {
            info.Damage *= totalMultiplier;
        }

        return HookResult.Continue;
    }

    private void OnPlayerTakeDamagePost(CCSPlayerPawn player, CTakeDamageInfo info, CTakeDamageResult result)
    {
        // 处理 TeleportOnDamage 事件
        if (CurrentEvent is TeleportOnDamageEvent teleportEvent)
        {
            teleportEvent.HandlePlayerDamage(player, info, result);
        }
    }

    private HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        // 处理 JumpOnShoot 事件
        if (CurrentEvent is JumpOnShootEvent jumpEvent)
        {
            jumpEvent.HandleWeaponFire(@event);
        }

        // 处理 JumpPlusPlus 事件
        if (CurrentEvent is JumpPlusPlusEvent jumpPlusPlusEvent)
        {
            jumpPlusPlusEvent.HandleWeaponFire(@event);
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        // 处理 Vampire 事件
        if (CurrentEvent is VampireEvent vampireEvent)
        {
            vampireEvent.HandlePlayerDeath(@event);
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        // 处理 Ninja 技能（检测致命伤害）
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        // 处理 Vampire 事件
        if (CurrentEvent is VampireEvent vampireEvent)
        {
            vampireEvent.HandlePlayerHurt(@event);
        }

        // 处理 SwapOnHit 事件
        if (CurrentEvent is SwapOnHitEvent swapEvent)
        {
            swapEvent.HandlePlayerHurt(@event);
        }

        // 处理第二次机会技能
        Skills.SecondChanceSkill.HandlePlayerHurt(@event);

        // 处理敌人旋转技能
        Skills.EnemySpinSkill.HandlePlayerHurt(@event, SkillManager);

        // 处理裁军技能
        Skills.DisarmSkill.HandlePlayerHurt(@event, SkillManager);

        return HookResult.Continue;
    }

    private HookResult OnDecoyStarted(EventDecoyStarted @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        // 处理透视诱饵弹技能
        var skill = SkillManager.GetPlayerSkill(player);
        if (skill?.Name == "DecoyXRay")
        {
            var decoyXRaySkill = (Skills.DecoyXRaySkill)skill;

            // 查找诱饵弹实体
            var decoyEntities = Utilities.FindAllEntitiesByDesignerName<CDecoyGrenade>("decoy_projectile");
            if (decoyEntities.Any())
            {
                // 获取最后一个投掷的诱饵弹
                var decoy = decoyEntities.LastOrDefault(d => d.IsValid);
                if (decoy != null)
                {
                    decoyXRaySkill.OnDecoyThrown(player, decoy);
                }
            }
        }

        return HookResult.Continue;
    }

    private HookResult OnSmokegrenadeDetonate(EventSmokegrenadeDetonate @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        // 处理有毒烟雾弹技能
        var skill = SkillManager.GetPlayerSkill(player);
        if (skill?.Name == "ToxicSmoke")
        {
            var toxicSmokeSkill = (Skills.ToxicSmokeSkill)skill;
            toxicSmokeSkill.OnSmokegrenadeDetonate(@event);
        }

        // 处理格拉兹技能
        if (skill?.Name == "Glaz")
        {
            Skills.GlazSkill.OnSmokegrenadeDetonate(@event);
        }

        return HookResult.Continue;
    }

    private HookResult OnSmokegrenadeExpired(EventSmokegrenadeExpired @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        // 处理有毒烟雾弹技能
        var skill = SkillManager.GetPlayerSkill(player);
        if (skill?.Name == "ToxicSmoke")
        {
            var toxicSmokeSkill = (Skills.ToxicSmokeSkill)skill;
            toxicSmokeSkill.OnSmokegrenadeExpired(@event);
        }

        // 处理格拉兹技能
        if (skill?.Name == "Glaz")
        {
            Skills.GlazSkill.OnSmokegrenadeExpired(@event);
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerBlind(EventPlayerBlind @event, GameEventInfo info)
    {
        // 处理杀手闪电技能
        Skills.KillerFlashSkill.HandlePlayerBlind(@event, SkillManager);

        // 处理防闪光技能
        Skills.AntiFlashSkill.HandlePlayerBlind(@event, SkillManager);

        // 处理闪光跳跃技能
        Skills.FlashJumpSkill.HandlePlayerBlind(@event, SkillManager);

        return HookResult.Continue;
    }

    private HookResult OnPlayerJump(EventPlayerJump @event, GameEventInfo info)
    {
        return HookResult.Continue;
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        // 处理爆炸射击技能
        Skills.ExplosiveShotSkill.OnEntitySpawned(entity);

        // 处理有毒烟雾弹技能（修改烟雾颜色）
        // 参考 jRandomSkills 使用 OwnerEntity 而不是 Thrower
        var name = entity.DesignerName;
        if (name == "smokegrenade_projectile")
        {
            var grenade = entity.As<CBaseCSGrenadeProjectile>();
            if (grenade != null && grenade.IsValid &&
                grenade.OwnerEntity != null && grenade.OwnerEntity.IsValid &&
                grenade.OwnerEntity.Value != null && grenade.OwnerEntity.Value.IsValid)
            {
                var pawn = grenade.OwnerEntity.Value.As<CCSPlayerPawn>();
                if (pawn != null && pawn.IsValid &&
                    pawn.Controller != null && pawn.Controller.IsValid &&
                    pawn.Controller.Value != null && pawn.Controller.Value.IsValid)
                {
                    var player = pawn.Controller.Value.As<CCSPlayerController>();
                    if (player != null && player.IsValid)
                    {
                        var skill = SkillManager.GetPlayerSkill(player);
                        if (skill?.Name == "ToxicSmoke")
                        {
                            var toxicSmokeSkill = (Skills.ToxicSmokeSkill)skill;
                            toxicSmokeSkill.OnEntitySpawned(entity);
                        }
                    }
                }
            }
        }
    }

    private HookResult OnWeaponHudSelection(EventWeaponhudSelection @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        var pawn = player.PlayerPawn.Get();
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null)
            return HookResult.Continue;

        // 查找选中的武器
        CBasePlayerWeapon? selectedWeapon = null;
        foreach (var weaponHandle in weaponServices.MyWeapons)
        {
            var weapon = weaponHandle.Get();
            if (weapon != null && weapon.IsValid && weapon.Index == (int)@event.Entindex)
            {
                selectedWeapon = weapon;
                break;
            }
        }

        return HookResult.Continue;
    }

    private HookResult OnBombAbortPlant(EventBombAbortplant @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        // 处理 AnywhereBombPlant 事件
        if (CurrentEvent is AnywhereBombPlantEvent anywhereBombEvent)
        {
            if (anywhereBombEvent.HandleBombAbortPlant(player))
            {
                return HookResult.Stop;
            }
        }

        // 处理旧的任意下包功能（向后兼容）
        if (BombPlantManager.HandleBombAbortPlant(player))
        {
            return HookResult.Stop;
        }

        return HookResult.Continue;
    }

    private HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
    {
        // 处理 AnywhereBombPlant 事件的炸弹计时器
        if (CurrentEvent is AnywhereBombPlantEvent)
        {
            var plantedBombs = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4");
            if (plantedBombs.Count() > 0)
            {
                var bomb = plantedBombs.First();
                if (bomb.IsValid)
                {
                    bomb.TimerLength = 60.0f;
                    bomb.C4Blow = (float)DateTime.Now.TimeOfDay.TotalSeconds + bomb.TimerLength;

                    Console.WriteLine("[任意下包事件] 炸弹爆炸时间已修改为 " + bomb.TimerLength + " 秒");
                }
            }
        }

        return HookResult.Continue;
    }

    private HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        return HookResult.Continue;
    }

    private void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
    {
        // 处理 AnywhereBombPlant 事件
        if (CurrentEvent is AnywhereBombPlantEvent anywhereBombEvent)
        {
            anywhereBombEvent.HandlePlayerButtonsChanged(player, pressed);
        }

        // 处理旧的任意下包功能（向后兼容）
        BombPlantManager.HandlePlayerButtonsChanged(player, pressed);
    }

    private void OnServerPostEntityThink()
    {
        // 处理 AnywhereBombPlant 事件
        if (CurrentEvent is AnywhereBombPlantEvent anywhereBombEvent)
        {
            anywhereBombEvent.HandleServerPostEntityThink();
        }

        // 处理旧的任意下包功能（向后兼容）
        BombPlantManager.HandleServerPostEntityThink();

        // 处理有毒烟雾弹的持续伤害
        ProcessToxicSmokeDamage();

        // 处理短跑技能（每帧更新）
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            var skill = SkillManager.GetPlayerSkill(player);
            if (skill?.Name == "Sprint")
            {
                var sprintSkill = (Skills.SprintSkill)skill;
                sprintSkill.OnTick(player);
            }
            else if (skill?.Name == "RadarHack")
            {
                var radarHackSkill = (Skills.RadarHackSkill)skill;
                radarHackSkill.OnTick(player);
            }
            else if (skill?.Name == "QuickShot")
            {
                Skills.QuickShotSkill.OnTick(SkillManager);
            }
        }

        // 处理黑暗技能（检查持续时间）
        var darknessSkill = (Skills.DarknessSkill?)SkillManager.GetSkill("Darkness");
        darknessSkill?.OnTick();

        // 处理永动机事件
        if (CurrentEvent is KeepMovingEvent keepMovingEvent)
        {
            keepMovingEvent.OnTick();
        }
    }

    /// <summary>
    /// 检查传输时控制烟雾弹的可见性（格拉兹技能）
    /// </summary>
    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        Skills.GlazSkill.OnCheckTransmit(infoList);
    }

    /// <summary>
    /// 处理有毒烟雾弹的持续伤害
    /// </summary>
    private void ProcessToxicSmokeDamage()
    {
        // 找到所有拥有有毒烟雾弹技能的玩家
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid)
                continue;

            var skill = SkillManager.GetPlayerSkill(player);
            if (skill?.Name == "ToxicSmoke")
            {
                var toxicSmokeSkill = (Skills.ToxicSmokeSkill)skill;
                toxicSmokeSkill.OnTick();
            }
        }
    }

    #endregion

    #region 命令注册

    private void RegisterCommands()
    {
        // 开局福利命令
        AddCommand("css_welfare_enable", "启用开局福利系统", _commands.CommandWelfareEnable);
        AddCommand("css_welfare_disable", "禁用开局福利系统", _commands.CommandWelfareDisable);
        AddCommand("css_welfare_status", "查看开局福利系统状态", _commands.CommandWelfareStatus);

        // 机器人控制命令
        AddCommand("css_botcontrol_enable", "启用玩家控制机器人", _commands.CommandBotControlEnable);
        AddCommand("css_botcontrol_disable", "禁用玩家控制机器人", _commands.CommandBotControlDisable);
        AddCommand("css_botcontrol_status", "查看机器人控制状态", _commands.CommandBotControlStatus);

        // 娱乐事件命令
        AddCommand("css_event_enable", "启用娱乐事件系统", _commands.CommandEventEnable);
        AddCommand("css_event_disable", "禁用娱乐事件系统", _commands.CommandEventDisable);
        AddCommand("css_event_status", "查看当前事件信息", _commands.CommandEventStatus);
        AddCommand("css_event_list", "列出所有可用事件", _commands.CommandEventList);
        AddCommand("css_event_weight", "查看/设置事件权重", _commands.CommandEventWeight);
        AddCommand("css_event_weights", "查看所有事件权重", _commands.CommandEventWeights);
        AddCommand("css_forceevent", "强制下回合触发指定事件（调试用）", _commands.CommandForceEvent);

        // 玩家技能命令
        AddCommand("css_skill_enable", "启用玩家技能系统", _commands.CommandSkillEnable);
        AddCommand("css_skill_disable", "禁用玩家技能系统", _commands.CommandSkillDisable);
        AddCommand("css_skill_status", "查看技能系统状态", _commands.CommandSkillStatus);
        AddCommand("css_skill_list", "列出所有可用技能", _commands.CommandSkillList);
        AddCommand("css_skill_weight", "查看/设置技能权重", _commands.CommandSkillWeight);
        AddCommand("css_skill_weights", "查看所有技能权重", _commands.CommandSkillWeights);
        AddCommand("css_useskill", "使用/激活你的技能", _commands.CommandUseSkill);
        AddCommand("css_forceskill", "强制赋予玩家指定技能（调试用）", _commands.CommandForceSkill);

        // 炸弹相关命令
        AddCommand("css_allowanywhereplant_enable", "启用任意下包功能", _commands.CommandEnableAllowAnywherePlant);
        AddCommand("css_allowanywhereplant_disable", "禁用任意下包功能", _commands.CommandDisableAllowAnywherePlant);
        AddCommand("css_allowanywhereplant_status", "查看任意下包功能状态", _commands.CommandAllowAnywherePlantStatus);
        AddCommand("css_bombtimer_set", "设置炸弹爆炸时间（秒）", _commands.CommandSetBombTimer);
        AddCommand("css_bombtimer_status", "查看炸弹爆炸时间", _commands.CommandBombTimerStatus);

        // 位置记录器命令
        AddCommand("css_pos_history", "查看你的位置历史", _commands.CommandPosHistory);
        AddCommand("css_pos_clear", "清除你的位置历史", _commands.CommandPosClear);
        AddCommand("css_pos_stats", "查看位置记录器统计信息", _commands.CommandPosStats);
        AddCommand("css_pos_clear_all", "清除所有玩家的位置历史", _commands.CommandPosClearAll);
    }

    #endregion
}
