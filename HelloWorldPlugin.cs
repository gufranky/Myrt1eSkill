using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using HelloWorldPlugin.Core;
using HelloWorldPlugin.Features;

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
    private PluginCommands _commands = null!;

    // 事件状态
    public EntertainmentEvent? CurrentEvent { get; set; }
    public EntertainmentEvent? PreviousEvent { get; set; }

    public void OnConfigParsed(EventWeightsConfig config)
    {
        Config = config;
        EventConfig = config;
        Console.WriteLine("[配置] 事件权重配置已加载");
    }

    public override void Load(bool hotReload)
    {
        // 初始化管理器
        HeavyArmorManager = new HeavyArmorManager(this);
        BombPlantManager = new BombPlantManager();
        EventManager = new EntertainmentEventManager(this);
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
        Console.WriteLine("[任意下包功能] 状态: " + (BombPlantManager.AllowAnywherePlant ? "✅ 启用" : "❌ 禁用"));
        Console.WriteLine("[炸弹时间设置] 当前时间: " + BombPlantManager.BombTimer + " 秒");
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

        // 选择并应用新事件
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

        // 处理重甲战士
        HeavyArmorManager.OnRoundStart();

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

        // 清理重甲战士
        HeavyArmorManager.OnRoundEnd();

        return HookResult.Continue;
    }

    private HookResult OnPlayerTakeDamagePre(CCSPlayerPawn player, CTakeDamageInfo info)
    {
        // 处理 SmallAndDeadly 事件（伤害翻倍）
        if (CurrentEvent is SmallAndDeadlyEvent smallAndDeadlyEvent)
        {
            smallAndDeadlyEvent.HandleDamage(info);
        }

        // 处理重甲战士减伤
        HeavyArmorManager.HandleDamage(player, info);

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

        // 炸弹相关命令
        AddCommand("css_allowanywhereplant_enable", "启用任意下包功能", _commands.CommandEnableAllowAnywherePlant);
        AddCommand("css_allowanywhereplant_disable", "禁用任意下包功能", _commands.CommandDisableAllowAnywherePlant);
        AddCommand("css_allowanywhereplant_status", "查看任意下包功能状态", _commands.CommandAllowAnywherePlantStatus);
        AddCommand("css_bombtimer_set", "设置炸弹爆炸时间（秒）", _commands.CommandSetBombTimer);
        AddCommand("css_bombtimer_status", "查看炸弹爆炸时间", _commands.CommandBombTimerStatus);
    }

    #endregion
}
