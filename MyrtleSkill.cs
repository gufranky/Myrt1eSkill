// MyrtleSkill.cs
// Copyright (C) 2026 MyrtleSkill Plugin Contributors
//
// This file is part of MyrtleSkill Plugin
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.UserMessages;
using MyrtleSkill.Core;
using MyrtleSkill.Events;
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

    // HUD 系统控制
    private Dictionary<ulong, DateTime> _playerHudExpired = new();
    private const float HUD_DISPLAY_DURATION = 20.0f; // HUD 显示时长（秒）

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

        // ⚠️ 不在 Load 阶段初始化服务器设置，等待 OnMapStart

        // 初始化管理器
        BombPlantManager = new BombPlantManager();
        EventManager = new EntertainmentEventManager(this);
        SkillManager = new PlayerSkillManager(this);
        WelfareManager = new WelfareManager(this);
        BotManager = new BotManager(this);
        PositionRecorder = new PositionRecorder(this);
        _commands = new PluginCommands(this);

        // 默认启用机器人管理功能
        BotManager.EnableBotControl();

        // 设置技能静态引用（用于技能内部访问插件）
        Skills.TeamWhipSkill.MyrtleSkillPlugin = this;

        // 注册事件处理器
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterEventHandler<EventRoundStart>(OnRoundStart, HookMode.Post);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd, HookMode.Post);
        RegisterListener<Listeners.OnPlayerTakeDamagePre>(OnPlayerTakeDamagePre);
        RegisterListener<Listeners.OnPlayerTakeDamagePost>(OnPlayerTakeDamagePost);
        RegisterEventHandler<EventWeaponFire>(OnWeaponFire, HookMode.Post);
        RegisterEventHandler<EventWeaponReload>(OnWeaponReload, HookMode.Post);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Post);
        RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Post);
        RegisterEventHandler<EventWeaponhudSelection>(OnWeaponHudSelection, HookMode.Pre);
        RegisterEventHandler<EventBombAbortplant>(OnBombAbortPlant, HookMode.Pre);
        RegisterEventHandler<EventBombPlanted>(OnBombPlanted, HookMode.Post);
        RegisterEventHandler<EventItemPickup>(OnItemPickup, HookMode.Pre);
        RegisterEventHandler<EventItemEquip>(OnItemEquip, HookMode.Pre);
        RegisterEventHandler<EventDecoyStarted>(OnDecoyStarted, HookMode.Post);
        RegisterEventHandler<EventGrenadeThrown>(OnGrenadeThrown, HookMode.Post);
        RegisterEventHandler<EventDecoyDetonate>(OnDecoyDetonate, HookMode.Post);
        RegisterEventHandler<EventSmokegrenadeDetonate>(OnSmokegrenadeDetonate, HookMode.Post);
        RegisterEventHandler<EventSmokegrenadeExpired>(OnSmokegrenadeExpired, HookMode.Post);
        RegisterEventHandler<EventFlashbangDetonate>(OnFlashbangDetonate, HookMode.Post);
        RegisterEventHandler<EventPlayerBlind>(OnPlayerBlind, HookMode.Post);
        RegisterEventHandler<EventPlayerJump>(OnPlayerJump, HookMode.Post);
        RegisterListener<Listeners.OnPlayerButtonsChanged>(OnPlayerButtonsChanged);
        RegisterListener<Listeners.OnServerPostEntityThink>(OnServerPostEntityThink);
        RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
        RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
        RegisterListener<Listeners.OnTick>(OnTick);  // 添加 OnTick 监听器

        // 注册实体伤害Hook（用于全息图等技能）
        CounterStrikeSharp.API.Modules.Memory.VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnEntityTakeDamage, HookMode.Pre);

        // 注册用户消息Hook（用于沉默技能等）
        HookUserMessage(208, OnPlayerMakeSound);

        // 注册命令
        RegisterCommands();

        Console.WriteLine("[Myrtle技能插件] v2.0.0 已加载！");
        Console.WriteLine("[娱乐事件系统] 已初始化，共加载 " + EventManager.GetEventCount() + " 个事件");
        Console.WriteLine("[玩家技能系统] 已初始化，共加载 " + SkillManager.GetSkillCount() + " 个技能");
        Console.WriteLine("[服务器设置] ⏳ 等待地图加载后初始化服务器设置...");
    }

    #region 事件处理

    private void OnMapStart(string mapName)
    {
        // ✅ 在地图加载后初始化服务器设置（此时 ConVar 已可用）
        Utils.ServerSettings.InitializeAllSettings();

        // 预加载堡垒之夜技能的模型
        Skills.FortniteSkill.PrecacheModel();

        // 预加载第三只眼技能的模型
        Skills.ThirdEyeSkill.PrecacheModel();

        // 启动位置记录器（此时全局变量已初始化，可以安全调用）
        PositionRecorder?.Start();

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

        // 0.15 清除上回合残留的强制技能列表（防止影响本回合）
        if (SkillManager.HasForcedSkills())
        {
            Console.WriteLine("[技能系统] 清除上回合残留的强制技能列表");
            SkillManager.ClearForcedSkills();
        }

        // 0.16 移除所有玩家的技能（确保清理上回合的技能）
        if (SkillManager.IsEnabled)
        {
            Console.WriteLine("[技能系统] 清理所有玩家的上回合技能");
            SkillManager.RemoveAllPlayerSkills();
        }

        // 0.17 清理击飞咯和推手技能的状态（确保跨回合清理）
        var blastOffSkill = (Skills.BlastOffSkill?)SkillManager.GetSkill("BlastOff");
        blastOffSkill?.ClearAllChances();

        var pushSkill = (Skills.PushSkill?)SkillManager.GetSkill("Push");
        pushSkill?.ClearAllChances();

        // 0.2 清理第二次机会使用记录
        Skills.SecondChanceSkill.OnRoundStart();

        // 0.25 清理凤凰使用记录并重新生成复活几率
        Skills.PhoenixSkill.OnRoundStart();

        // 0.26 清理木头人使用记录
        Skills.WoodManSkill.OnRoundStart();

        // 0.27 ZRY技能
        Skills.ZRYSkill.OnRoundStart();

        // 0.28 清理圣手榴弹计数器
        Skills.HolyHandGrenadeSkill.OnRoundStart();

        // 0.3 清理格拉兹烟雾弹追踪
        Skills.GlazSkill.OnRoundStart();

        // 0.4 清理名刀使用记录
        Skills.MeitoSkill.OnRoundStart();

        // 0.5 清理全息图
        Skills.HologramSkill.ClearAllHolograms();

        // 0.51 清理第三只眼相机
        var thirdEyeSkill = (Skills.ThirdEyeSkill?)SkillManager.GetSkill("ThirdEye");
        thirdEyeSkill?.ClearAllCameras();

        // 0.52 清理堡垒之夜路障
        Skills.FortniteSkill.ClearAllBarricades();

        // 0.55 清理冷冻诱饵
        Skills.FrozenDecoySkill.OnRoundStart();

        // 0.56 清理残局使者状态
        Skills.LastStandSkill.ClearAllLastStand();

        // 0.57 清理故障效果
        Skills.GlitchSkill.ClearAllGlitches();

        // 0.6 清理鬼状态
        Skills.GhostSkill.ClearAllGhosts();

        // 0.7 清理杀人无敌记录
        Skills.KillInvincibilitySkill.OnRoundStart();

        // 0.75 清理检查扫描使用次数
        Skills.FreeCameraSkill.OnRoundStart();

        // 0.76 清理豺狼轨迹
        Skills.JackalSkill.OnRoundStart();

        // 1. 恢复上一回合事件（不要在这里重置DisableSkillsThisRound标志）
        // DisableSkillsThisRound = false;  // 移除这行，让事件有机会设置它

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

            Console.WriteLine($"[娱乐事件调试] 选择事件结果: {(CurrentEvent != null ? CurrentEvent.Name : "NULL")}");

            if (CurrentEvent != null)
            {
                Console.WriteLine("[娱乐事件] 本回合事件: " + CurrentEvent.DisplayName + " - " + CurrentEvent.Description);
                CurrentEvent.OnApply();

                // 立即把新事件保存为PreviousEvent（用于下回合恢复）
                Console.WriteLine("[娱乐事件] 保存本回合事件: " + CurrentEvent.Name + " 为PreviousEvent");
                PreviousEvent = CurrentEvent;

                // 显示事件提示（聊天框）
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

                // 移除旧的 PrintToCenter，统一在技能应用后显示 HUD
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

                // 技能应用完成后，显示 HUD（延迟2秒确保所有技能都已应用）
                AddTimer(2.0f, () =>
                {
                    Console.WriteLine("[HUD] 准备显示回合开始 HUD");
                    ShowRoundStartHUD();
                });
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

            // 仍然显示 HUD（显示无技能）
            AddTimer(1.0f, () =>
            {
                ShowRoundStartHUD();
            });
        }
        else
        {
            Console.WriteLine("[技能系统] 技能系统未启用");

            // 仍然显示 HUD（显示无技能）
            AddTimer(1.0f, () =>
            {
                ShowRoundStartHUD();
            });
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
            Console.WriteLine($"[娱乐事件调试] 回合结束：CurrentEvent 已设为 null，PreviousEvent = {(PreviousEvent != null ? PreviousEvent.Name : "NULL")}");
        }
        else
        {
            Console.WriteLine("[娱乐事件] 回合结束，但没有当前事件需要保存");
            Console.WriteLine($"[娱乐事件调试] 回合结束：CurrentEvent 已经是 null，PreviousEvent = {(PreviousEvent != null ? PreviousEvent.Name : "NULL")}");
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

        // 清理治疗烟雾弹记录
        Skills.HealingSmokeSkill.ClearAllHealingSmokes();

        // 清理高风险，高回报技能的奖励记录
        Skills.HighRiskHighRewardSkill.ClearRewardedPlayers();

        // 清理 HUD 过期时间字典
        _playerHudExpired.Clear();
        Console.WriteLine("[HUD] 已清理所有玩家的 HUD 过期时间");

        return HookResult.Continue;
    }

    private HookResult OnPlayerTakeDamagePre(CCSPlayerPawn player, CTakeDamageInfo info)
    {
        // 处理爆炸射击技能
        Skills.ExplosiveShotSkill.OnTakeDamagePre(player, info);

        // 处理自瞄技能（将命中部位修改为头部）
        Skills.AutoAimSkill.OnPlayerTakeDamagePre(player, info, SkillManager);

        // 收集所有伤害倍数修正器
        float totalMultiplier = 1.0f;

        // 处理装甲技能（随机伤害减免）
        var controller = player.Controller.Value;
        if (controller != null && controller.IsValid && controller is CCSPlayerController csController)
        {
            var skills = SkillManager.GetPlayerSkills(csController);

            // 处理装甲技能
            var armoredSkill = skills.FirstOrDefault(s => s.Name == "Armored");
            if (armoredSkill != null)
            {
                var armored = (Skills.ArmoredSkill)armoredSkill;
                float? armoredMultiplier = armored?.HandleDamage(player, info);
                if (armoredMultiplier.HasValue)
                {
                    totalMultiplier *= armoredMultiplier.Value;
                }
            }

            // 处理假肢技能（四肢防弹）
            var prostheticSkill = skills.FirstOrDefault(s => s.Name == "Prosthetic");
            if (prostheticSkill != null)
            {
                var prosthetic = (Skills.ProstheticSkill)prostheticSkill;
                float? prostheticMultiplier = prosthetic?.HandleDamage(player, info);
                if (prostheticMultiplier.HasValue)
                {
                    totalMultiplier *= prostheticMultiplier.Value;
                }
            }
        }

        // 处理鞭策队友技能（在Pre阶段处理，取消伤害并治疗）
        Skills.TeamWhipSkill.HandleDamagePre(player, info);

        // 处理苦命鸳鸯配对伤害加成（包括子事件）
        var couplesEvents = FindEventsOfType<UnluckyCouplesEvent>();
        foreach (var couplesEvent in couplesEvents)
        {
            float? couplesMultiplier = couplesEvent.HandleDamagePre(player, info);
            if (couplesMultiplier.HasValue)
            {
                totalMultiplier *= couplesMultiplier.Value;
            }
        }

        // 处理反向爆头事件（包括子事件）
        var inverseEvents = FindEventsOfType<InverseHeadshotEvent>();
        foreach (var inverseEvent in inverseEvents)
        {
            float? inverseMultiplier = InverseHeadshotEvent.HandleDamagePre(player, info);
            if (inverseMultiplier.HasValue)
            {
                totalMultiplier *= inverseMultiplier.Value;
            }
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
        // 这个监听器可能不工作，改用 OnPlayerHurt
        // 保留此方法以防万一
    }

    private HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        // 处理 JumpOnShoot 事件（包括子事件）
        var jumpEvents = FindEventsOfType<JumpOnShootEvent>();
        foreach (var jumpEvent in jumpEvents)
        {
            jumpEvent.HandleWeaponFire(@event);
        }

        // 处理 JumpPlusPlus 事件（包括子事件）
        var jumpPlusPlusEvents = FindEventsOfType<JumpPlusPlusEvent>();
        foreach (var jumpPlusPlusEvent in jumpPlusPlusEvents)
        {
            jumpPlusPlusEvent.HandleWeaponFire(@event);
        }

        // 处理无限弹药技能
        var player = @event.Userid;
        if (player != null && player.IsValid)
        {
            var skills = SkillManager.GetPlayerSkills(player);
            var infiniteAmmoSkill = skills.FirstOrDefault(s => s.Name == "InfiniteAmmo");
            if (infiniteAmmoSkill != null)
            {
                var infiniteAmmo = (Skills.InfiniteAmmoSkill)infiniteAmmoSkill;
                infiniteAmmo.OnWeaponFire(@event);
            }
        }

        return HookResult.Continue;
    }

    private HookResult OnWeaponReload(EventWeaponReload @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        // 处理无限弹药技能
        var skills = SkillManager.GetPlayerSkills(player);
        var infiniteAmmoSkill = skills.FirstOrDefault(s => s.Name == "InfiniteAmmo");
        if (infiniteAmmoSkill != null)
        {
            var infiniteAmmo = (Skills.InfiniteAmmoSkill)infiniteAmmoSkill;
            infiniteAmmo.OnWeaponReload(@event);
        }

        return HookResult.Continue;
    }

    private HookResult OnGrenadeThrown(EventGrenadeThrown @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        // ✅ 移除无限弹药技能的投掷物补充（无限弹药只影响枪械，不影响投掷物）
        var skills = SkillManager.GetPlayerSkills(player);

        // 处理圣手榴弹技能（补充手雷）
        var holyHandGrenadeSkill = skills.FirstOrDefault(s => s.Name == "HolyHandGrenade");
        if (holyHandGrenadeSkill != null)
        {
            var holyHandGrenade = (Skills.HolyHandGrenadeSkill)holyHandGrenadeSkill;
            holyHandGrenade.OnGrenadeThrown(@event);
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        // 处理 Vampire 事件（包括子事件）
        var vampireEvents = FindEventsOfType<VampireEvent>();
        foreach (var vampireEvent in vampireEvents)
        {
            vampireEvent.HandlePlayerDeath(@event);
        }

        // 处理 KeepMoving 事件（包括子事件）
        var keepMovingEvents = FindEventsOfType<KeepMovingEvent>();
        foreach (var keepMovingEvent in keepMovingEvents)
        {
            keepMovingEvent.HandlePlayerDeath(@event);
        }

        // 处理名刀技能
        var victim = @event.Userid;
        if (victim != null && victim.IsValid)
        {
            var skills = SkillManager.GetPlayerSkills(victim);
            var meitoSkill = skills.FirstOrDefault(s => s.Name == "Meito");
            if (meitoSkill != null)
            {
                Skills.MeitoSkill.HandlePlayerDeath(@event);
            }

            // 处理穆罕默德技能（死后爆炸）
            var muhammadSkill = skills.FirstOrDefault(s => s.Name == "Muhammad");
            if (muhammadSkill != null)
            {
                Skills.MuhammadSkill.HandlePlayerDeath(@event);
            }
        }

        // 处理杀人无敌技能（击杀者获得无敌）
        Skills.KillInvincibilitySkill.HandlePlayerDeath(@event);

        // 处理高风险，高回报技能（击杀者血量增加到500）
        var attacker = @event.Attacker;
        if (attacker != null && attacker.IsValid)
        {
            var attackerSkills = SkillManager.GetPlayerSkills(attacker);
            var highRiskSkill = attackerSkills.FirstOrDefault(s => s.Name == "HighRiskHighReward");
            if (highRiskSkill != null)
            {
                var highRisk = (Skills.HighRiskHighRewardSkill)highRiskSkill;
                highRisk.OnPlayerDeath(@event);
            }
        }

        // 处理残局使者技能（检查是否只剩一人）
        var lastStandSkill = (Skills.LastStandSkill?)SkillManager.GetSkill("LastStand");
        lastStandSkill?.OnPlayerDeath(@event);

        // 处理故障技能（移除死亡玩家的故障效果）
        Skills.GlitchSkill.OnPlayerDeath(@event.Userid);
        Skills.GlitchSkill.OnPlayerDeath(@event.Attacker);

        return HookResult.Continue;
    }

    private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        // 处理 Ninja 技能（检测致命伤害）
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        // 处理名刀技能（致命伤害保护）
        var skills = SkillManager.GetPlayerSkills(player);
        var meitoSkill = skills.FirstOrDefault(s => s.Name == "Meito");
        if (meitoSkill != null)
        {
            Skills.MeitoSkill.HandlePlayerHurt(@event);
        }

        // 处理 Vampire 事件（包括子事件）
        var vampireEvents = FindEventsOfType<VampireEvent>();
        foreach (var vampireEvent in vampireEvents)
        {
            vampireEvent.HandlePlayerHurt(@event);
        }

        // 处理 SwapOnHit 事件（包括子事件）
        var swapEvents = FindEventsOfType<SwapOnHitEvent>();
        foreach (var swapEvent in swapEvents)
        {
            swapEvent.HandlePlayerHurt(@event);
        }

        // 处理受伤传送事件（包括子事件）
        var teleportEvents = FindEventsOfType<TeleportOnDamageEvent>();
        foreach (var teleportEvent in teleportEvents)
        {
            teleportEvent.HandlePlayerHurt(@event);
        }

        // 处理第二次机会技能
        Skills.SecondChanceSkill.HandlePlayerHurt(@event);

        // 处理凤凰技能
        Skills.PhoenixSkill.HandlePlayerHurt(@event);

        // 处理敌人旋转技能
        Skills.EnemySpinSkill.HandlePlayerHurt(@event, SkillManager);

        // 处理裁军技能
        Skills.DisarmSkill.HandlePlayerHurt(@event, SkillManager);

        // 处理全息图技能（玩家受伤时销毁全息图）
        Skills.HologramSkill.HandlePlayerHurt(player);

        // 处理鬼技能（玩家受伤或造成伤害显形）
        Skills.GhostSkill.HandlePlayerHurt(@event);
        Skills.GhostSkill.HandlePlayerDamaged(player);

        // 处理杀人无敌技能（无敌期间保护）
        Skills.KillInvincibilitySkill.HandlePlayerHurt(@event);

        // 处理推手技能（击退敌人）
        var pushSkill = (Skills.PushSkill?)SkillManager.GetSkill("Push");
        pushSkill?.HandlePlayerHurt(@event);

        // 处理击飞咯技能（让敌人起飞）
        var blastOffSkill = (Skills.BlastOffSkill?)SkillManager.GetSkill("BlastOff");
        blastOffSkill?.HandlePlayerHurt(@event);

        // 处理破产之枪事件（伤害改为扣钱，包括子事件）
        var bankruptcyEvents = FindEventsOfType<BankruptcyWeaponEvent>();
        foreach (var bankruptcyWeapon in bankruptcyEvents)
        {
            bankruptcyWeapon.HandlePlayerHurt(@event);
        }

        // 处理剑圣技能（格挡射击）
        var bladeMasterSkill = skills.FirstOrDefault(s => s.Name == "BladeMaster");
        if (bladeMasterSkill != null)
        {
            var bladeMaster = (Skills.BladeMasterSkill)bladeMasterSkill;
            bladeMaster.HandlePlayerHurt(@event, SkillManager);
        }

        return HookResult.Continue;
    }

    private HookResult OnDecoyStarted(EventDecoyStarted @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        // 处理透视诱饵弹技能
        var skills = SkillManager.GetPlayerSkills(player);
        var decoyXRaySkill = skills.FirstOrDefault(s => s.Name == "DecoyXRay");
        if (decoyXRaySkill != null)
        {
            var decoyXRay = (Skills.DecoyXRaySkill)decoyXRaySkill;

            // 查找诱饵弹实体
            var decoyEntities = Utilities.FindAllEntitiesByDesignerName<CDecoyGrenade>("decoy_projectile");
            if (decoyEntities.Any())
            {
                // 获取最后一个投掷的诱饵弹
                var decoy = decoyEntities.LastOrDefault(d => d.IsValid);
                if (decoy != null)
                {
                    decoyXRay.OnDecoyThrown(player, decoy);
                }
            }
        }

        // 处理冷冻诱饵技能
        var frozenDecoySkill = skills.FirstOrDefault(s => s.Name == "FrozenDecoy");
        if (frozenDecoySkill != null)
        {
            var frozenDecoy = (Skills.FrozenDecoySkill)frozenDecoySkill;
            frozenDecoy.OnDecoyStarted(@event);
        }

        // 处理ZRY技能
        var zrySkill = skills.FirstOrDefault(s => s.Name == "ZRY");
        if (zrySkill != null)
        {
            var zry = (Skills.ZRYSkill)zrySkill;

            // 查找诱饵弹实体
            var decoyEntities = Utilities.FindAllEntitiesByDesignerName<CDecoyGrenade>("decoy_projectile");
            if (decoyEntities.Any())
            {
                // 获取最后一个投掷的诱饵弹
                var decoy = decoyEntities.LastOrDefault(d => d.IsValid);
                if (decoy != null)
                {
                    zry.OnDecoyThrown(player, decoy);
                }
            }
        }

        return HookResult.Continue;
    }

    private HookResult OnDecoyDetonate(EventDecoyDetonate @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        var skills = SkillManager.GetPlayerSkills(player);

        // 处理冷冻诱饵技能
        var frozenDecoySkill = skills.FirstOrDefault(s => s.Name == "FrozenDecoy");
        if (frozenDecoySkill != null)
        {
            var frozenDecoy = (Skills.FrozenDecoySkill)frozenDecoySkill;
            frozenDecoy.OnDecoyDetonate(@event);
        }

        return HookResult.Continue;
    }

    private HookResult OnSmokegrenadeDetonate(EventSmokegrenadeDetonate @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        var skills = SkillManager.GetPlayerSkills(player);

        // 处理有毒烟雾弹技能
        var toxicSmokeSkill = skills.FirstOrDefault(s => s.Name == "ToxicSmoke");
        if (toxicSmokeSkill != null)
        {
            var toxicSmoke = (Skills.ToxicSmokeSkill)toxicSmokeSkill;
            toxicSmoke.OnSmokegrenadeDetonate(@event);
        }

        // 处理治疗烟雾弹技能
        var healingSmokeSkill = skills.FirstOrDefault(s => s.Name == "HealingSmoke");
        if (healingSmokeSkill != null)
        {
            var healingSmoke = (Skills.HealingSmokeSkill)healingSmokeSkill;
            healingSmoke.OnSmokegrenadeDetonate(@event);
        }

        // 处理格拉兹技能
        var glazSkill = skills.FirstOrDefault(s => s.Name == "Glaz");
        if (glazSkill != null)
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

        var skills = SkillManager.GetPlayerSkills(player);

        // 处理有毒烟雾弹技能
        var toxicSmokeSkill = skills.FirstOrDefault(s => s.Name == "ToxicSmoke");
        if (toxicSmokeSkill != null)
        {
            var toxicSmoke = (Skills.ToxicSmokeSkill)toxicSmokeSkill;
            toxicSmoke.OnSmokegrenadeExpired(@event);
        }

        // 处理治疗烟雾弹技能
        var healingSmokeSkill = skills.FirstOrDefault(s => s.Name == "HealingSmoke");
        if (healingSmokeSkill != null)
        {
            var healingSmoke = (Skills.HealingSmokeSkill)healingSmokeSkill;
            healingSmoke.OnSmokegrenadeExpired(@event);
        }

        // 处理格拉兹技能
        var glazSkill = skills.FirstOrDefault(s => s.Name == "Glaz");
        if (glazSkill != null)
        {
            Skills.GlazSkill.OnSmokegrenadeExpired(@event);
        }

        return HookResult.Continue;
    }

    private HookResult OnFlashbangDetonate(EventFlashbangDetonate @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        var skills = SkillManager.GetPlayerSkills(player);

        // 处理防闪光技能
        var antiFlashSkill = skills.FirstOrDefault(s => s.Name == "AntiFlash");
        if (antiFlashSkill != null)
        {
            var antiFlash = (Skills.AntiFlashSkill)antiFlashSkill;
            antiFlash.OnFlashbangDetonate(@event);
        }

        // 处理闪光跳跃技能
        var flashJumpSkill = skills.FirstOrDefault(s => s.Name == "FlashJump");
        if (flashJumpSkill != null)
        {
            var flashJump = (Skills.FlashJumpSkill)flashJumpSkill;
            flashJump.OnFlashbangDetonate(@event);
        }

        // 处理超级闪光技能
        var superFlashSkill = skills.FirstOrDefault(s => s.Name == "SuperFlash");
        if (superFlashSkill != null)
        {
            var superFlash = (Skills.SuperFlashSkill)superFlashSkill;
            superFlash.OnFlashbangDetonate(@event);
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

        // 处理穆罕默德技能（修改HE手雷属性）
        Skills.MuhammadSkill.OnEntitySpawned(entity);

        // 处理圣手榴弹技能（增强HE手雷伤害和范围）
        // 优化：先找到投掷者，只调用投掷者的圣手榴弹技能
        var entityName = entity.DesignerName;
        if (entityName == "hegrenade_projectile")
        {
            var hegrenade = entity.As<CHEGrenadeProjectile>();
            if (hegrenade != null && hegrenade.IsValid)
            {
                var playerPawn = hegrenade.Thrower.Value;
                if (playerPawn != null && playerPawn.IsValid)
                {
                    var thrower = Utilities.GetPlayers().FirstOrDefault(p => p.PlayerPawn?.Value?.Index == playerPawn.Index);
                    if (thrower != null && thrower.IsValid)
                    {
                        var skills = SkillManager.GetPlayerSkills(thrower);
                        if (skills != null && skills.Count > 0)
                        {
                            var holyHandGrenadeSkill = skills.FirstOrDefault(s => s.Name == "HolyHandGrenade");
                            if (holyHandGrenadeSkill != null)
                            {
                                var holyHandGrenade = (Skills.HolyHandGrenadeSkill)holyHandGrenadeSkill;
                                holyHandGrenade.OnEntitySpawned(entity);
                            }
                        }
                    }
                }
            }
        }

        // 处理有毒烟雾弹技能（修改烟雾颜色）
        // 处理治疗烟雾弹技能（修改烟雾颜色）
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
                        var skills = SkillManager.GetPlayerSkills(player);
                        var toxicSmokeSkill = skills.FirstOrDefault(s => s.Name == "ToxicSmoke");
                        if (toxicSmokeSkill != null)
                        {
                            var toxicSmoke = (Skills.ToxicSmokeSkill)toxicSmokeSkill;
                            toxicSmoke.OnEntitySpawned(entity);
                        }

                        var healingSmokeSkill = skills.FirstOrDefault(s => s.Name == "HealingSmoke");
                        if (healingSmokeSkill != null)
                        {
                            var healingSmoke = (Skills.HealingSmokeSkill)healingSmokeSkill;
                            healingSmoke.OnEntitySpawned(entity);
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

        // 处理旧的任意下包功能（向后兼容）
        if (BombPlantManager.HandleBombAbortPlant(player))
        {
            return HookResult.Stop;
        }

        return HookResult.Continue;
    }

    private HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
    {
        Console.WriteLine($"[任意下包调试] OnBombPlanted 触发！CurrentEvent: {(CurrentEvent != null ? CurrentEvent.Name : "NULL")}");

        // 处理 AnywhereBombPlant 事件（包括子事件）
        ProcessAnywhereBombPlantEvent(CurrentEvent, @event);

        return HookResult.Continue;
    }

    /// <summary>
    /// 处理任意下包事件（包括子事件检查）
    /// </summary>
    private void ProcessAnywhereBombPlantEvent(EntertainmentEvent? checkEvent, EventBombPlanted bombEvent)
    {
        if (checkEvent == null)
        {
            Console.WriteLine("[任意下包调试] 事件为 null，跳过处理");
            return;
        }

        // 检查是否是任意下包事件
        if (checkEvent is AnywhereBombPlantEvent anywhereBombEvent)
        {
            Console.WriteLine("[任意下包调试] 找到 AnywhereBombPlantEvent，调用 HandleBombPlanted");
            anywhereBombEvent.HandleBombPlanted(bombEvent);
            return;
        }

        // 检查子事件（处理双重狂欢等组合事件）
        var subEvents = checkEvent.GetSubEvents();
        if (subEvents.Count > 0)
        {
            Console.WriteLine($"[任意下包调试] 检查子事件，共 {subEvents.Count} 个");
            foreach (var subEvent in subEvents)
            {
                ProcessAnywhereBombPlantEvent(subEvent, bombEvent);
            }
        }
    }

    /// <summary>
    /// 处理任意下包事件Tick（包括子事件检查）
    /// </summary>
    private void ProcessAnywhereBombPlantTick(EntertainmentEvent? checkEvent)
    {
        if (checkEvent == null)
            return;

        // 检查是否是任意下包事件
        if (checkEvent is AnywhereBombPlantEvent anywhereBombEvent)
        {
            // 每60帧输出一次调试日志（避免日志过多）
            if (Server.TickCount % 60 == 0)
            {
                Console.WriteLine("[任意下包调试] OnServerPostEntityThink: 找到 AnywhereBombPlantEvent");
            }
            anywhereBombEvent.HandleServerPostEntityThink();
            return;
        }

        // 检查子事件（处理双重狂欢等组合事件）
        var subEvents = checkEvent.GetSubEvents();
        if (subEvents.Count > 0)
        {
            foreach (var subEvent in subEvents)
            {
                ProcessAnywhereBombPlantTick(subEvent);
            }
        }
    }

    /// <summary>
    /// 查找指定类型的所有事件（包括子事件）
    /// </summary>
    private List<T> FindEventsOfType<T>() where T : EntertainmentEvent
    {
        var result = new List<T>();

        if (CurrentEvent == null)
            return result;

        // 检查当前事件
        if (CurrentEvent is T currentTyped)
        {
            result.Add(currentTyped);
        }

        // 递归检查子事件
        FindSubEventsOfType(CurrentEvent, result);

        return result;
    }

    /// <summary>
    /// 递归查找子事件中指定类型的事件
    /// </summary>
    private void FindSubEventsOfType<T>(EntertainmentEvent? checkEvent, List<T> result) where T : EntertainmentEvent
    {
        if (checkEvent == null)
            return;

        var subEvents = checkEvent.GetSubEvents();
        if (subEvents.Count > 0)
        {
            foreach (var subEvent in subEvents)
            {
                if (subEvent is T typedEvent)
                {
                    result.Add(typedEvent);
                }
                // 递归检查更深层的子事件
                FindSubEventsOfType(subEvent, result);
            }
        }
    }

    private HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        // 处理猎鹰之眼技能（摄像头模式下禁用武器）
        var falconEyeSkill = (Skills.FalconEyeSkill?)SkillManager.GetSkill("FalconEye");
        falconEyeSkill?.OnItemPickup(@event);

        return HookResult.Continue;
    }

    private HookResult OnItemEquip(EventItemEquip @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        return HookResult.Continue;
    }

    private void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
    {
        // 处理旧的任意下包功能（向后兼容）
        BombPlantManager.HandlePlayerButtonsChanged(player, pressed);
    }

    private void OnServerPostEntityThink()
    {
        // 处理 AnywhereBombPlant 事件（包括子事件）
        ProcessAnywhereBombPlantTick(CurrentEvent);

        // 处理旧的任意下包功能（向后兼容）
        BombPlantManager.HandleServerPostEntityThink();

        // 处理有毒烟雾弹的持续伤害
        ProcessToxicSmokeDamage();

        // 处理短跑技能（每帧更新）
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            var skills = SkillManager.GetPlayerSkills(player);
            var sprintSkill = skills.FirstOrDefault(s => s.Name == "Sprint");
            if (sprintSkill != null)
            {
                var sprint = (Skills.SprintSkill)sprintSkill;
                sprint.OnTick(player);
            }

            var radarHackSkill = skills.FirstOrDefault(s => s.Name == "RadarHack");
            if (radarHackSkill != null)
            {
                var radarHack = (Skills.RadarHackSkill)radarHackSkill;
                radarHack.OnTick(player);
            }

            var quickShotSkill = skills.FirstOrDefault(s => s.Name == "QuickShot");
            if (quickShotSkill != null)
            {
                Skills.QuickShotSkill.OnTick(SkillManager);
            }

            // 处理剑圣技能（移动速度修正）
            var bladeMasterSkill = skills.FirstOrDefault(s => s.Name == "BladeMaster");
            if (bladeMasterSkill != null)
            {
                var bladeMaster = (Skills.BladeMasterSkill)bladeMasterSkill;
                bladeMaster.OnTick(player);
            }
        }

        // 处理黑暗技能（检查持续时间）
        var darknessSkill = (Skills.DarknessSkill?)SkillManager.GetSkill("Darkness");
        darknessSkill?.OnTick();

        // 处理超级闪光技能（检查持续时间）
        var superFlashSkill = (Skills.SuperFlashSkill?)SkillManager.GetSkill("SuperFlash");
        superFlashSkill?.OnTick();

        // 处理永动机事件（包括子事件）
        var keepMovingEvents = FindEventsOfType<KeepMovingEvent>();
        foreach (var keepMovingEvent in keepMovingEvents)
        {
            keepMovingEvent.OnTick();
        }

        // 处理击中交换事件（清理交换冷却，包括子事件）
        var swapOnHitEvents = FindEventsOfType<SwapOnHitEvent>();
        foreach (var swapOnHitEvent in swapOnHitEvents)
        {
            swapOnHitEvent.OnTick();
        }

        // 处理信号屏蔽事件（持续清除雷达显示，包括子事件）
        var signalJamEvents = FindEventsOfType<SignalJamEvent>();
        foreach (var signalJamEvent in signalJamEvents)
        {
            signalJamEvent.OnTick();
        }

        // 处理鬼技能（清理死亡的玩家）
        Skills.GhostSkill.OnTick();

        // 处理杀人无敌技能（清理过期的无敌状态）
        Skills.KillInvincibilitySkill.OnTick();
    }

    /// <summary>
    /// 每帧更新 - 持续刷新 HUD 显示
    /// </summary>
    private void OnTick()
    {
        var currentTime = DateTime.Now;

        // 持续刷新 HUD 显示
        if (_playerHudExpired.Count > 0 && CurrentEvent != null)
        {
            var expiredPlayers = new List<ulong>();

            foreach (var (steamId, expireTime) in _playerHudExpired)
            {
                // 检查是否过期
                if (currentTime >= expireTime)
                {
                    expiredPlayers.Add(steamId);
                    continue;
                }

                // 找到玩家并刷新 HUD
                var player = Utilities.GetPlayers().FirstOrDefault(p => p.SteamID == steamId);
                if (player != null && player.IsValid)
                {
                    var skills = SkillManager.GetPlayerSkills(player);
                    string htmlContent = BuildRoundStartHtml(CurrentEvent, skills);
                    player.PrintToCenterHtml(htmlContent);
                }
            }

            // 移除过期的玩家
            foreach (var steamId in expiredPlayers)
            {
                _playerHudExpired.Remove(steamId);
            }

            // 如果所有玩家都过期了，记录日志
            if (expiredPlayers.Count > 0)
            {
                Console.WriteLine($"[HUD] 已移除 {expiredPlayers.Count} 个玩家的 HUD 显示");
            }
        }

        // 处理冷冻诱饵技能（冻结附近的玩家）
        var frozenDecoySkill = (Skills.FrozenDecoySkill?)SkillManager.GetSkill("FrozenDecoy");
        frozenDecoySkill?.OnTick();

        // 处理猎鹰之眼技能（更新摄像头位置）
        var falconEyeSkill = (Skills.FalconEyeSkill?)SkillManager.GetSkill("FalconEye");
        falconEyeSkill?.OnTick();

        // 处理传送锚点技能（移动锚点粒子）
        var teleportAnchorSkill = (Skills.TeleportAnchorSkill?)SkillManager.GetSkill("TeleportAnchor");
        teleportAnchorSkill?.OnTick();

        // 处理精神骇入技能（检查目标是否存活）
        var mindHackSkill = (Skills.MindHackSkill?)SkillManager.GetSkill("MindHack");
        mindHackSkill?.OnTick();

        // 处理测距仪技能（显示到最近敌人的距离）
        var rangeFinderSkill = (Skills.RangeFinderSkill?)SkillManager.GetSkill("RangeFinder");
        if (rangeFinderSkill != null)
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (player == null || !player.IsValid || !player.PawnIsAlive)
                    continue;

                var skills = SkillManager.GetPlayerSkills(player);
                bool hasRangeFinder = skills?.Any(s => s.Name == "RangeFinder") ?? false;

                if (hasRangeFinder)
                {
                    var distance = rangeFinderSkill.GetNearestEnemyDistance(player.SteamID);
                    if (distance.HasValue && distance.Value < float.MaxValue)
                    {
                        // 转换为米（100游戏单位 = 1米）
                        float distanceInMeters = distance.Value / 100.0f;

                        // 根据距离显示不同的颜色和提示
                        string color = distanceInMeters <= 5.0f ? "#ff0000" : // 红色（5米内）
                                      distanceInMeters <= 10.0f ? "#ffaa00" : // 橙色（10米内）
                                      "#00ff00"; // 绿色（10米外）

                        string message = distanceInMeters <= 5.0f ?
                            $"📏 最近敌人: <font color='{color}'>{distanceInMeters:F1}m</font> ⚠️ 透视标记！" :
                            $"📏 最近敌人: <font color='{color}'>{distanceInMeters:F1}m</font>";

                        player.PrintToCenterHtml(message);
                    }
                    else
                    {
                        player.PrintToCenterHtml("📏 扫描中...");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 处理实体受到伤害（用于全息图等技能）
    /// </summary>
    private HookResult OnEntityTakeDamage(DynamicHook hook)
    {
        // 获取伤害参数
        var entity = hook.GetParam<CEntityInstance>(0);
        var info = hook.GetParam<CTakeDamageInfo>(1);

        if (entity == null || info == null)
            return HookResult.Continue;

        // 处理全息图克隆体受到伤害
        if (entity.Entity?.Name?.StartsWith("HologramClone_") == true)
        {
            Skills.HologramSkill.HandleCloneDamage(entity, info);
        }

        // 处理复制品受到伤害
        if (entity.Entity?.Name?.StartsWith("Replica_") == true)
        {
            var replicatorSkill = (Skills.ReplicatorSkill?)SkillManager.GetSkill("Replicator");
            replicatorSkill?.OnEntityTakeDamage(hook);
        }

        // 处理探索者受到伤害
        if (entity.Entity?.Name?.StartsWith("Explorer_") == true)
        {
            var explorerSkill = (Skills.ExplorerSkill?)SkillManager.GetSkill("Explorer");
            explorerSkill?.OnEntityTakeDamage(hook);
        }

        // 处理堡垒之夜路障受到伤害（使用 jRandomSkills 的命名）
        if (entity.Entity?.Name?.StartsWith("FortniteWall") == true)
        {
            Skills.FortniteSkill.HandleBarricadeDamage(entity, info);
        }

        return HookResult.Continue;
    }

    /// <summary>
    /// 处理玩家发出声音事件（用于沉默技能和聋技能）
    /// 拦截脚步声和跳跃声，处理失聪玩家
    /// </summary>
    private HookResult OnPlayerMakeSound(UserMessage um)
    {
        // 先处理聋事件（移除所有失聪玩家）
        DeafEvent.OnPlayerMakeSound(um);

        // 再处理聋技能（移除失聪玩家）
        var deafSkill = (Skills.DeafSkill?)SkillManager.GetSkill("Deaf");
        deafSkill?.HandlePlayerMakeSound(um);

        // 最后处理沉默技能（检查是否有沉默技能玩家）
        Skills.SilentSkill.PlayerMakeSound(um);

        return HookResult.Continue;
    }

    /// <summary>
    /// 检查传输时控制烟雾弹的可见性（格拉兹技能）
    /// </summary>
    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        // 处理格拉兹技能（烟雾弹可见性）
        Skills.GlazSkill.OnCheckTransmit(infoList);

        // 处理鬼技能（隐形）
        Skills.GhostSkill.OnCheckTransmit(infoList);
    }

    /// <summary>
    /// 处理有毒烟雾弹的持续伤害
    /// 处理治疗烟雾弹的持续治疗
    /// </summary>
    private void ProcessToxicSmokeDamage()
    {
        // 找到所有拥有有毒烟雾弹技能的玩家
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid)
                continue;

            var skills = SkillManager.GetPlayerSkills(player);
            var toxicSmokeSkill = skills.FirstOrDefault(s => s.Name == "ToxicSmoke");
            if (toxicSmokeSkill != null)
            {
                var toxicSmoke = (Skills.ToxicSmokeSkill)toxicSmokeSkill;
                toxicSmoke.OnTick();
            }

            var healingSmokeSkill = skills.FirstOrDefault(s => s.Name == "HealingSmoke");
            if (healingSmokeSkill != null)
            {
                var healingSmoke = (Skills.HealingSmokeSkill)healingSmokeSkill;
                healingSmoke.OnTick();
            }
        }
    }

    #endregion

    #region HUD 显示

    /// <summary>
    /// 显示回合开始 HUD（事件 + 技能）
    /// </summary>
    private void ShowRoundStartHUD()
    {
        if (CurrentEvent == null)
            return;

        var currentTime = DateTime.Now;

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid)
                continue;

            // 获取玩家的技能列表
            var skills = SkillManager.GetPlayerSkills(player);

            // 构建 HTML 内容
            string htmlContent = BuildRoundStartHtml(CurrentEvent, skills);

            // 显示 HUD
            player.PrintToCenterHtml(htmlContent);

            // 记录 HUD 过期时间
            _playerHudExpired[player.SteamID] = currentTime.AddSeconds(HUD_DISPLAY_DURATION);
        }

        Console.WriteLine($"[HUD] 已显示回合开始 HUD，显示时长: {HUD_DISPLAY_DURATION} 秒");
    }

    /// <summary>
    /// 清除玩家的 HUD（当玩家使用技能时调用）
    /// </summary>
    public void ClearPlayerHUD(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 从 HUD 过期字典中移除该玩家
        if (_playerHudExpired.ContainsKey(player.SteamID))
        {
            _playerHudExpired.Remove(player.SteamID);
            Console.WriteLine($"[HUD] {player.PlayerName} 使用技能，已清除 HUD 显示");
        }
    }

    /// <summary>
    /// 构建回合开始的 HTML 内容
    /// </summary>
    private string BuildRoundStartHtml(EntertainmentEvent eventData, List<PlayerSkill> skills)
    {
        // 第一行：当前事件
        string eventLine = $"<font class='fontWeight-Bold fontSize-ml' color='#FFFF00'>🎲 当前事件: {eventData.DisplayName}</font><br>";

        // 第二行：事件效果（或子事件列表）
        string eventDetailLine;
        var subEvents = eventData.GetSubEvents();
        if (subEvents.Count > 0)
        {
            // 顶级狂欢事件：显示子事件列表
            string subEventsList = string.Join(", ", subEvents.Select(e => e.DisplayName));
            eventDetailLine = $"<font class='fontSize-sm' color='#FFFFFF'>{subEventsList}</font><br>";
        }
        else
        {
            // 普通事件：显示描述
            eventDetailLine = $"<font class='fontSize-sm' color='#CCCCCC'>📝 事件效果: {eventData.Description}</font><br>";
        }

        // 第三行：当前技能
        string skillLine;
        if (skills.Count == 0)
        {
            skillLine = $"<font class='fontWeight-Bold fontSize-ml' color='#FFFF00'>🎁 当前技能: 无</font><br>";
        }
        else if (skills.Count == 1)
        {
            skillLine = $"<font class='fontWeight-Bold fontSize-ml' color='#FFFF00'>🎁 当前技能: {skills[0].DisplayName}</font><br>";
        }
        else
        {
            // 多个技能：显示技能列表
            string skillsList = string.Join(", ", skills.Select(s => s.DisplayName));
            skillLine = $"<font class='fontWeight-Bold fontSize-ml' color='#FFFF00'>🎁 当前技能: {skillsList}</font><br>";
        }

        // 第四行：技能效果（或技能列表）
        string skillDetailLine;
        if (skills.Count == 0)
        {
            skillDetailLine = "<font class='fontSize-sm' color='#CCCCCC'>本回合没有技能</font><br>";
        }
        else if (skills.Count == 1)
        {
            // 单个技能：显示描述
            skillDetailLine = $"<font class='fontSize-sm' color='#CCCCCC'>📝 技能效果: {skills[0].Description}</font><br>";
        }
        else
        {
            // 多个技能：显示所有技能的描述
            var skillDescriptions = skills.Select(s => $"• {s.DisplayName}: {s.Description}");
            string allDescriptions = string.Join("<br>", skillDescriptions);
            skillDetailLine = $"<font class='fontSize-xs' color='#CCCCCC'>{allDescriptions}</font><br>";
        }

        // 合并所有内容，并添加带边框和内边距的容器
        string content = eventLine + eventDetailLine + "<br>" + skillLine + skillDetailLine;
        return $"<div style='background-color: rgba(0, 0, 0, 0.7); border: 3px solid #FFFF00; border-radius: 8px; padding: 20px 40px; margin: 10px;'>{content}</div>";
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
