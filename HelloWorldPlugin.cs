using System;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using HelloWorldPlugin.Core;
using HelloWorldPlugin.Features;
using HelloWorldPlugin.Skills;

namespace HelloWorldPlugin;

/// <summary>
/// CS2 娱乐事件插件主类
/// </summary>
public class HelloWorldPlugin : BasePlugin, IPluginConfig<EventWeightsConfig>
{
    public override string ModuleName => "CS2 Entertainment Events Plugin";
    public override string ModuleVersion => "1.3.0";

    // 配置
    public EventWeightsConfig Config { get; set; } = null!;
    public EventWeightsConfig EventConfig { get; set; } = null!;

    // 管理器
    public HeavyArmorManager HeavyArmorManager { get; private set; } = null!;
    public BombPlantManager BombPlantManager { get; private set; } = null!;
    public EntertainmentEventManager EventManager { get; private set; } = null!;
    public PlayerSkillManager SkillManager { get; private set; } = null!;
    private PluginCommands _commands = null!;

    // 事件状态
    public EntertainmentEvent? CurrentEvent { get; set; }
    public EntertainmentEvent? PreviousEvent { get; set; }

    // 技能系统控制
    public bool DisableSkillsThisRound { get; set; } = false;

    // 友军伤害踢人保护
    private bool _originalAutoKickValue = false;

    public void OnConfigParsed(EventWeightsConfig config)
    {
        Config = config;
        EventConfig = config;
        Console.WriteLine("[配置] 事件权重配置已加载");
    }

    public override void Load(bool hotReload)
    {
        // 禁用友军伤害自动踢人并启用派对模式
        DisableFriendlyFireKick();
        EnablePartyMode();

        // 初始化管理器
        HeavyArmorManager = new HeavyArmorManager(this);
        BombPlantManager = new BombPlantManager();
        EventManager = new EntertainmentEventManager(this);
        SkillManager = new PlayerSkillManager(this);
        _commands = new PluginCommands(this);

        // 注册事件处理器
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
        RegisterListener<Listeners.OnPlayerButtonsChanged>(OnPlayerButtonsChanged);
        RegisterListener<Listeners.OnServerPostEntityThink>(OnServerPostEntityThink);

        // 注册命令
        RegisterCommands();

        Console.WriteLine("[娱乐事件插件] v1.3.0 已加载！");
        Console.WriteLine("[娱乐事件系统] 已初始化，共加载 " + EventManager.GetEventCount() + " 个事件");
        Console.WriteLine("[玩家技能系统] 已初始化，共加载 " + SkillManager.GetSkillCount() + " 个技能");
        Console.WriteLine("[任意下包功能] 状态: " + (BombPlantManager.AllowAnywherePlant ? "✅ 启用" : "❌ 禁用"));
        Console.WriteLine("[炸弹时间设置] 当前时间: " + BombPlantManager.BombTimer + " 秒");
        Console.WriteLine("[友军伤害保护] 已禁用自动踢人功能");
        Console.WriteLine("[派对模式] 🎉 已启用派对模式！");
    }

    #region 事件处理

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        // 恢复上一回合事件
        if (PreviousEvent != null)
        {
            Console.WriteLine("[娱乐事件] 正在恢复上回合事件: " + PreviousEvent.Name);
            PreviousEvent.OnRevert();
            PreviousEvent = null;
        }

        // 1. 处理重甲战士（第一优先级）
        HeavyArmorManager.OnRoundStart();

        // 2. 选择并应用新事件（第二优先级）
        if (EventManager.IsEnabled)
        {
            CurrentEvent = EventManager.SelectRandomEvent();
            if (CurrentEvent != null)
            {
                Console.WriteLine("[娱乐事件] 本回合事件: " + CurrentEvent.DisplayName + " - " + CurrentEvent.Description);
                CurrentEvent.OnApply();

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

        // 3. 为所有玩家应用技能（第三优先级，延迟1秒确保事件已完全应用）
        if (SkillManager.IsEnabled && !DisableSkillsThisRound)
        {
            AddTimer(1.0f, () =>
            {
                SkillManager.ApplySkillsToAllPlayers();
            });
        }
        else if (DisableSkillsThisRound)
        {
            Console.WriteLine("[技能系统] 本回合技能已被事件禁用");
        }

        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        // 保存当前事件为上一回合事件
        if (CurrentEvent != null)
        {
            PreviousEvent = CurrentEvent;
            CurrentEvent = null;
        }

        // 重置技能禁用标志
        DisableSkillsThisRound = false;

        // 移除所有玩家技能
        if (SkillManager.IsEnabled)
        {
            SkillManager.RemoveAllPlayerSkills();
        }

        // 清理重甲战士
        HeavyArmorManager.OnRoundEnd();

        return HookResult.Continue;
    }

    private HookResult OnPlayerTakeDamagePre(CCSPlayerPawn player, CTakeDamageInfo info)
    {
        // 收集所有伤害倍数修正器
        float totalMultiplier = 1.0f;

        // 处理重甲战士减伤（返回伤害倍数）
        float? heavyArmorMultiplier = HeavyArmorManager.HandleDamage(player, info);
        if (heavyArmorMultiplier.HasValue)
        {
            totalMultiplier *= heavyArmorMultiplier.Value;
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

        // 应用累积的倍数
        if (totalMultiplier != 1.0f)
        {
            float originalDamage = info.Damage;
            info.Damage *= totalMultiplier;
            Console.WriteLine($"[伤害结算] 原始: {originalDamage}, 总倍数: {totalMultiplier:F2}, 最终: {info.Damage}");
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

        return HookResult.Continue;
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

        // 处理重甲战士武器限制
        if (HeavyArmorManager.HandleWeaponSelection(player, selectedWeapon))
        {
            return HookResult.Stop;
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

        var itemName = @event.Item;

        // 处理重甲战士拾取限制
        if (HeavyArmorManager.HandleItemPickup(player, itemName))
        {
            return HookResult.Stop;
        }

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
    }

    #endregion

    #region 命令注册

    private void RegisterCommands()
    {
        // 重甲战士命令
        AddCommand("css_heavyarmor_enable", "启用重甲战士模式", _commands.CommandEnableHeavyArmor);
        AddCommand("css_heavyarmor_disable", "禁用重甲战士模式", _commands.CommandDisableHeavyArmor);
        AddCommand("css_heavyarmor_status", "查看重甲战士状态", _commands.CommandStatusHeavyArmor);

        // 娱乐事件命令
        AddCommand("css_event_enable", "启用娱乐事件系统", _commands.CommandEventEnable);
        AddCommand("css_event_disable", "禁用娱乐事件系统", _commands.CommandEventDisable);
        AddCommand("css_event_status", "查看当前事件信息", _commands.CommandEventStatus);
        AddCommand("css_event_list", "列出所有可用事件", _commands.CommandEventList);
        AddCommand("css_event_weight", "查看/设置事件权重", _commands.CommandEventWeight);
        AddCommand("css_event_weights", "查看所有事件权重", _commands.CommandEventWeights);

        // 玩家技能命令
        AddCommand("css_skill_enable", "启用玩家技能系统", _commands.CommandSkillEnable);
        AddCommand("css_skill_disable", "禁用玩家技能系统", _commands.CommandSkillDisable);
        AddCommand("css_skill_status", "查看技能系统状态", _commands.CommandSkillStatus);
        AddCommand("css_skill_list", "列出所有可用技能", _commands.CommandSkillList);
        AddCommand("css_skill_weight", "查看/设置技能权重", _commands.CommandSkillWeight);
        AddCommand("css_skill_weights", "查看所有技能权重", _commands.CommandSkillWeights);
        AddCommand("css_useskill", "使用/激活你的技能", _commands.CommandUseSkill);

        // 炸弹相关命令
        AddCommand("css_allowanywhereplant_enable", "启用任意下包功能", _commands.CommandEnableAllowAnywherePlant);
        AddCommand("css_allowanywhereplant_disable", "禁用任意下包功能", _commands.CommandDisableAllowAnywherePlant);
        AddCommand("css_allowanywhereplant_status", "查看任意下包功能状态", _commands.CommandAllowAnywherePlantStatus);
        AddCommand("css_bombtimer_set", "设置炸弹爆炸时间（秒）", _commands.CommandSetBombTimer);
        AddCommand("css_bombtimer_status", "查看炸弹爆炸时间", _commands.CommandBombTimerStatus);
    }

    #region 友军伤害保护

    /// <summary>
    /// 禁用友军伤害自动踢人功能
    /// </summary>
    private void DisableFriendlyFireKick()
    {
        try
        {
            // 获取当前的 mp_autokick 值
            var autoKickConVar = ConVar.Find("mp_autokick");
            if (autoKickConVar != null)
            {
                _originalAutoKickValue = autoKickConVar.GetPrimitiveValue<bool>();

                // 禁用自动踢人
                autoKickConVar.SetValue(false);
                Console.WriteLine($"[友军伤害保护] 已禁用 mp_autokick (原始值: {_originalAutoKickValue})");
            }
            else
            {
                Console.WriteLine("[友军伤害保护] 警告：无法找到 mp_autokick ConVar");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[友军伤害保护] 错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 启用派对模式
    /// </summary>
    private void EnablePartyMode()
    {
        try
        {
            var partyModeConVar = ConVar.Find("sv_partymode");
            if (partyModeConVar != null)
            {
                partyModeConVar.SetValue(true);
                Console.WriteLine("[派对模式] 已启用 sv_partymode");
            }
            else
            {
                Console.WriteLine("[派对模式] 警告：无法找到 sv_partymode ConVar");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[派对模式] 错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 恢复友军伤害自动踢人功能
    /// </summary>
    private void RestoreFriendlyFireKick()
    {
        try
        {
            var autoKickConVar = ConVar.Find("mp_autokick");
            if (autoKickConVar != null)
            {
                autoKickConVar.SetValue(_originalAutoKickValue);
                Console.WriteLine($"[友军伤害保护] 已恢复 mp_autokick 为 {_originalAutoKickValue}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[友军伤害保护] 错误：{ex.Message}");
        }
    }

    public override void Unload(bool hotReload)
    {
        // 恢复友军伤害自动踢人功能
        RestoreFriendlyFireKick();

        base.Unload(hotReload);
        Console.WriteLine("[娱乐事件插件] 已卸载，友军伤害保护已移除");
    }

    #endregion

    #endregion
}
