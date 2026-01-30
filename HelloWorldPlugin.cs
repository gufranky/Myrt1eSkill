using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json.Serialization;

namespace HelloWorldPlugin;

public class EventWeightsConfig : BasePluginConfig
{
    [JsonPropertyName("EventWeights")]
    public Dictionary<string, int> EventWeights { get; set; } = new Dictionary<string, int>
    {
        ["NoEvent"] = 40,
        ["LowGravity"] = 10,
        ["LowGravityPlusPlus"] = 10,
        ["HighSpeed"] = 10,
        ["Vampire"] = 10,
        ["TeleportOnDamage"] = 10,
        ["JumpOnShoot"] = 10,
        ["JumpPlusPlus"] = 10,
        ["AnywhereBombPlant"] = 10,
        ["MiniSize"] = 10,
        ["Juggernaut"] = 10,
        ["InfiniteAmmo"] = 10,
        ["SwapOnHit"] = 10,
        ["SmallAndDeadly"] = 10
    };

    [JsonPropertyName("Notes")]
    public string Notes { get; set; } = "权重越高，事件被选中的概率越大。设置为0可禁用某个事件。";
}

public class HelloWorldPlugin : BasePlugin, IPluginConfig<EventWeightsConfig>
{
    public EventWeightsConfig Config { get; set; } = null!;

    public void OnConfigParsed(EventWeightsConfig config)
    {
        Config = config;
        EventConfig = config;
        Console.WriteLine("[配置] 事件权重配置已加载");
    }

    public EventWeightsConfig EventConfig { get; set; } = null!;

    public override string ModuleName => "Heavy Armor Lucky Player Plugin";
    public override string ModuleVersion => "1.2.0";

    private CCSPlayerController? _currentHeavyArmorPlayer;
    private readonly Random _random = new();
    private bool _pluginEnabled = true;
    public bool _allowAnywherePlant = false;
    public float _bombTimer = 40.0f;

    private EntertainmentEvent? _currentEvent;
    private EntertainmentEvent? _previousEvent;
    private EntertainmentEventManager _eventManager = null!;

    public Random RandomGenerator => _random;

    public override void Load(bool hotReload)
    {
        _eventManager = new EntertainmentEventManager(this);

        RegisterEventHandler<EventRoundStart>(OnRoundStart, HookMode.Post);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd, HookMode.Post);
        RegisterListener<Listeners.OnPlayerTakeDamagePre>(OnPlayerTakeDamagePre);
        RegisterListener<Listeners.OnPlayerTakeDamagePost>(OnPlayerTakeDamagePostGlobal);
        RegisterEventHandler<EventWeaponFire>(OnWeaponFire, HookMode.Post);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Post);
        RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Post);
        RegisterEventHandler<EventWeaponhudSelection>(OnWeaponHudSelection, HookMode.Pre);
        RegisterEventHandler<EventBombAbortplant>(OnBombAbortPlant, HookMode.Pre);
        RegisterEventHandler<EventBombPlanted>(OnBombPlanted, HookMode.Post);
        RegisterEventHandler<EventItemPickup>(OnItemPickup, HookMode.Pre);
        RegisterListener<Listeners.OnPlayerButtonsChanged>(OnPlayerButtonsChanged);
        RegisterListener<Listeners.OnServerPostEntityThink>(OnServerPostEntityThink);

        AddCommand("css_heavyarmor_enable", "启用重甲战士模式", CommandEnableHeavyArmor);
        AddCommand("css_heavyarmor_disable", "禁用重甲战士模式", CommandDisableHeavyArmor);
        AddCommand("css_heavyarmor_status", "查看重甲战士状态", CommandStatusHeavyArmor);
        AddCommand("css_allowanywhereplant_enable", "启用任意下包功能", CommandEnableAllowAnywherePlant);
        AddCommand("css_allowanywhereplant_disable", "禁用任意下包功能", CommandDisableAllowAnywherePlant);
        AddCommand("css_allowanywhereplant_status", "查看任意下包功能状态", CommandAllowAnywherePlantStatus);
        AddCommand("css_bombtimer_set", "设置炸弹爆炸时间（秒）", CommandSetBombTimer);
        AddCommand("css_bombtimer_status", "查看炸弹爆炸时间", CommandBombTimerStatus);
        AddCommand("css_event_enable", "启用娱乐事件系统", CommandEventEnable);
        AddCommand("css_event_disable", "禁用娱乐事件系统", CommandEventDisable);
        AddCommand("css_event_status", "查看当前事件信息", CommandEventStatus);
        AddCommand("css_event_list", "列出所有可用事件", CommandEventList);
        AddCommand("css_event_weight", "查看/设置事件权重", CommandEventWeight);
        AddCommand("css_event_weights", "查看所有事件权重", CommandEventWeights);

        Console.WriteLine("[重甲幸运玩家插件] v1.2.0 已加载！");
        Console.WriteLine("[娱乐事件系统] 已初始化，共加载 " + _eventManager.GetEventCount() + " 个事件");
        Console.WriteLine("[任意下包功能] 状态: " + (_allowAnywherePlant ? "✅ 启用" : "❌ 禁用"));
        Console.WriteLine("[炸弹时间设置] 当前时间: " + _bombTimer + " 秒");
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (_previousEvent != null)
        {
            Console.WriteLine("[娱乐事件] 正在恢复上回合事件: " + _previousEvent.Name);
            _previousEvent.OnRevert();
            _previousEvent = null;
        }

        if (_eventManager.IsEnabled)
        {
            _currentEvent = _eventManager.SelectRandomEvent();
            if (_currentEvent != null)
            {
                Console.WriteLine("[娱乐事件] 本回合事件: " + _currentEvent.DisplayName + " - " + _currentEvent.Description);
                _currentEvent.OnApply();

                // 所有事件都显示提示（包括 NoEvent）
                foreach (var p in Utilities.GetPlayers())
                {
                    if (p.IsValid)
                    {
                        p.PrintToChat("───────────────────");
                        p.PrintToChat("🎲 " + _currentEvent.DisplayName);
                        p.PrintToChat("📝 " + _currentEvent.Description);
                        p.PrintToChat("───────────────────");
                    }
                }
                AddTimer(3.0f, () =>
                {
                    foreach (var p in Utilities.GetPlayers())
                    {
                        if (p.IsValid)
                        {
                            p.PrintToCenter("━━━━━━━━━━━━━━━━\n " + _currentEvent.DisplayName + "\n━━━━━━━━━━━━━━━━");
                        }
                    }
                });
            }
        }

        if (!_pluginEnabled)
        {
            Console.WriteLine("[重甲幸运玩家插件] 插件已禁用，跳过本回合");
            return HookResult.Continue;
        }

        var players = Utilities.GetPlayers();
        if (players.Count == 0) return HookResult.Continue;

        if (_currentHeavyArmorPlayer != null && _currentHeavyArmorPlayer.IsValid)
        {
            var oldPawn = _currentHeavyArmorPlayer.PlayerPawn.Get();
            if (oldPawn != null && oldPawn.IsValid)
            {
                SetPlayerSpeed(oldPawn, 1.0f);
            }
        }

        var luckyPlayer = SelectRandomPlayer();
        if (luckyPlayer != null)
        {
            _currentHeavyArmorPlayer = luckyPlayer;

            luckyPlayer.GiveNamedItem("item_assaultsuit");

            var pawn = luckyPlayer.PlayerPawn.Get();
            if (pawn != null && pawn.IsValid)
            {
                pawn.ArmorValue = 200;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
                SetPlayerSpeed(pawn, 0.6f);
                RestrictToSecondaryWeapons(luckyPlayer);

                luckyPlayer.PrintToChat(" 🛡️ 你被选中为重甲战士！");
                luckyPlayer.PrintToChat(" ⚡ 护甲值: 200 | 速度: 60% | 伤害抗性: +60% | 武器限制: 仅副武器");
                luckyPlayer.PrintToCenter(" 🛡️ 重甲战士模式已激活！");
            }

            StartWeaponCheckTimer();

            foreach (var p in Utilities.GetPlayers())
            {
                if (p.IsValid)
                {
                    p.PrintToChat("🎲 幸运玩家：" + luckyPlayer.PlayerName + " 获得了重甲战士效果！");
                }
            }
        }

        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        if (_currentEvent != null)
        {
            _previousEvent = _currentEvent;
            _currentEvent = null;
        }

        if (_currentHeavyArmorPlayer != null && _currentHeavyArmorPlayer.IsValid)
        {
            var pawn = _currentHeavyArmorPlayer.PlayerPawn.Get();
            if (pawn != null && pawn.IsValid)
            {
                SetPlayerSpeed(pawn, 1.0f);
            }
            _currentHeavyArmorPlayer = null;
        }

        StopWeaponCheckTimer();

        return HookResult.Continue;
    }

    private HookResult OnPlayerTakeDamagePre(CCSPlayerPawn player, CTakeDamageInfo info)
    {
        // 处理 SmallAndDeadly 事件（伤害翻倍）
        if (_currentEvent is SmallAndDeadlyEvent smallAndDeadlyEvent)
        {
            smallAndDeadlyEvent.HandleDamage(info);
        }

        // 处理重甲战士减伤
        var controller = player.Controller.Value;
        if (controller == null || !controller.IsValid)
            return HookResult.Continue;

        if (controller != _currentHeavyArmorPlayer)
            return HookResult.Continue;

        const float damageReduction = 0.6f;
        float originalDamage = info.Damage;
        float newDamage = originalDamage * (1.0f - damageReduction);
        info.Damage = newDamage;

        Console.WriteLine("[减伤] 玩家: " + controller.PlayerName + " | 原始伤害: " + originalDamage + " | 减免后: " + newDamage + " | 减免: " + (originalDamage - newDamage));

        return HookResult.Continue;
    }

    private void SetPlayerSpeed(CCSPlayerPawn pawn, float multiplier)
    {
        pawn.VelocityModifier = multiplier;

        var movementServices = pawn.MovementServices;
        if (movementServices != null)
        {
            movementServices.Maxspeed = multiplier * 240.0f;
        }

        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");

        Console.WriteLine("[速度设置] VelocityModifier=" + pawn.VelocityModifier + ", Maxspeed=" + (movementServices?.Maxspeed ?? 0));
    }

    private void RestrictToSecondaryWeapons(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Get();
        if (pawn == null || !pawn.IsValid)
            return;

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null)
            return;

        foreach (var weaponHandle in weaponServices.MyWeapons)
        {
            var weapon = weaponHandle.Get();
            if (weapon != null && weapon.IsValid)
            {
                var weaponBase = weapon.As<CCSWeaponBase>();
                if (weaponBase != null && weaponBase.VData != null)
                {
                    var weaponType = weaponBase.VData.WeaponType;
                    if (weaponType != CSWeaponType.WEAPONTYPE_PISTOL &&
                        weaponType != CSWeaponType.WEAPONTYPE_KNIFE &&
                        weaponType != CSWeaponType.WEAPONTYPE_C4)
                    {
                        weapon.Remove();
                        Console.WriteLine("[重甲战士] 已移除玩家 " + player.PlayerName + " 的武器: (类型: " + weaponType + ")");
                    }
                }
            }
        }

        EnsurePlayerHasSecondaryWeapon(player);
        ForceSecondaryWeapon(player);
    }

    private void ForceSecondaryWeapon(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Get();
        if (pawn == null || !pawn.IsValid)
            return;

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null)
            return;

        CBasePlayerWeapon? secondaryWeapon = null;
        foreach (var weaponHandle in weaponServices.MyWeapons)
        {
            var weapon = weaponHandle.Get();
            if (weapon != null && weapon.IsValid)
            {
                var weaponBase = weapon.As<CCSWeaponBase>();
                if (weaponBase != null && weaponBase.VData != null &&
                    weaponBase.VData.WeaponType == CSWeaponType.WEAPONTYPE_PISTOL)
                {
                    secondaryWeapon = weapon;
                    break;
                }
            }
        }

        if (secondaryWeapon == null)
        {
            foreach (var weaponHandle in weaponServices.MyWeapons)
            {
                var weapon = weaponHandle.Get();
                if (weapon != null && weapon.IsValid)
                {
                    var weaponBase = weapon.As<CCSWeaponBase>();
                    if (weaponBase != null && weaponBase.VData != null &&
                        weaponBase.VData.WeaponType == CSWeaponType.WEAPONTYPE_KNIFE)
                    {
                        secondaryWeapon = weapon;
                        break;
                    }
                }
            }
        }

        if (secondaryWeapon != null && secondaryWeapon.IsValid)
        {
            var activeWeapon = weaponServices.ActiveWeapon.Get();
            if (activeWeapon == null || !activeWeapon.IsValid || activeWeapon.Index != secondaryWeapon.Index)
            {
                player.ExecuteClientCommand("slot2");
                Console.WriteLine("[重甲战士] 已强制玩家 " + player.PlayerName + " 使用副武器");
            }
        }
    }

    private void EnsurePlayerHasSecondaryWeapon(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Get();
        if (pawn == null || !pawn.IsValid)
            return;

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null)
            return;

        bool hasSecondaryWeapon = false;
        foreach (var weaponHandle in weaponServices.MyWeapons)
        {
            var weapon = weaponHandle.Get();
            if (weapon != null && weapon.IsValid)
            {
                var weaponBase = weapon.As<CCSWeaponBase>();
                if (weaponBase != null && weaponBase.VData != null &&
                    weaponBase.VData.WeaponType == CSWeaponType.WEAPONTYPE_PISTOL)
                {
                    hasSecondaryWeapon = true;
                    break;
                }
            }
        }

        if (!hasSecondaryWeapon)
        {
            string[] secondaryWeapons = { "weapon_p2000", "weapon_glock", "weapon_usp_silencer", "weapon_p250" };
            string randomWeapon = secondaryWeapons[_random.Next(secondaryWeapons.Length)];
            player.GiveNamedItem(randomWeapon);
            Console.WriteLine("[重甲战士] 已给予玩家 " + player.PlayerName + " 副武器: " + randomWeapon);
        }
    }

    private HookResult OnWeaponHudSelection(EventWeaponhudSelection @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (player != _currentHeavyArmorPlayer)
            return HookResult.Continue;

        var pawn = player.PlayerPawn.Get();
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null)
            return HookResult.Continue;

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

        if (selectedWeapon != null && selectedWeapon.IsValid)
        {
            var weaponBase = selectedWeapon.As<CCSWeaponBase>();
            if (weaponBase != null && weaponBase.VData != null)
            {
                var weaponType = weaponBase.VData.WeaponType;
                if (weaponType != CSWeaponType.WEAPONTYPE_PISTOL &&
                    weaponType != CSWeaponType.WEAPONTYPE_KNIFE &&
                    weaponType != CSWeaponType.WEAPONTYPE_C4)
                {
                    player.PrintToChat(" 🚫 重甲战士只能使用副武器！");
                    Console.WriteLine("[重甲战士] 阻止玩家 " + player.PlayerName + " 使用非副武器 (类型: " + weaponType + ")");
                    ForceSecondaryWeapon(player);
                    return HookResult.Stop;
                }
            }
        }

        return HookResult.Continue;
    }

    private void OnPlayerStateChanged()
    {
        if (_currentHeavyArmorPlayer == null || !_currentHeavyArmorPlayer.IsValid)
            return;

        var pawn = _currentHeavyArmorPlayer.PlayerPawn.Get();
        if (pawn == null || !pawn.IsValid)
            return;

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null)
            return;

        var activeWeapon = weaponServices.ActiveWeapon.Get();
        if (activeWeapon != null && activeWeapon.IsValid)
        {
            var weaponBase = activeWeapon.As<CCSWeaponBase>();
            if (weaponBase != null && weaponBase.VData != null)
            {
                var weaponType = weaponBase.VData.WeaponType;
                if (weaponType != CSWeaponType.WEAPONTYPE_PISTOL &&
                    weaponType != CSWeaponType.WEAPONTYPE_KNIFE &&
                    weaponType != CSWeaponType.WEAPONTYPE_C4)
                {
                    ForceSecondaryWeapon(_currentHeavyArmorPlayer);
                }
            }
        }
    }

    private CounterStrikeSharp.API.Modules.Timers.Timer? _weaponCheckTimer;

    private void StartWeaponCheckTimer()
    {
        if (_weaponCheckTimer != null)
            return;

        _weaponCheckTimer = AddTimer(0.5f, () =>
        {
            OnPlayerStateChanged();
        }, TimerFlags.REPEAT);
    }

    private void StopWeaponCheckTimer()
    {
        if (_weaponCheckTimer != null)
        {
            _weaponCheckTimer.Kill();
            _weaponCheckTimer = null;
        }
    }

    private CCSPlayerController? SelectRandomPlayer()
    {
        var players = Utilities.GetPlayers();
        if (players.Count == 0)
            return null;

        var validPlayers = players.Where(p => p.IsValid && p.PlayerPawn.IsValid).ToList();
        if (validPlayers.Count == 0)
            return null;

        return validPlayers[_random.Next(validPlayers.Count)];
    }

    private void CommandEnableHeavyArmor(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (_pluginEnabled)
        {
            commandInfo.ReplyToCommand("重甲战士模式已经是启用状态！");
            return;
        }

        _pluginEnabled = true;
        string message = "✅ 重甲战士模式已启用！下一回合将随机选择重甲战士。";

        if (player == null)
        {
            Console.WriteLine("[重甲幸运玩家插件] " + message);
            commandInfo.ReplyToCommand(message);
        }
        else
        {
            player.PrintToChat("[重甲战士] " + message);
            Console.WriteLine("[重甲幸运玩家插件] " + player.PlayerName + " 启用了重甲战士模式");
        }

        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsValid && p != player)
            {
                p.PrintToChat("🎮 重甲战士模式已启用！");
            }
        }
    }

    private void CommandDisableHeavyArmor(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!_pluginEnabled)
        {
            commandInfo.ReplyToCommand("重甲战士模式已经是禁用状态！");
            return;
        }

        _pluginEnabled = false;

        if (_currentHeavyArmorPlayer != null && _currentHeavyArmorPlayer.IsValid)
        {
            var pawn = _currentHeavyArmorPlayer.PlayerPawn.Get();
            if (pawn != null && pawn.IsValid)
            {
                SetPlayerSpeed(pawn, 1.0f);
            }
            _currentHeavyArmorPlayer = null;
        }

        string message = "❌ 重甲战士模式已禁用！";

        if (player == null)
        {
            Console.WriteLine("[重甲幸运玩家插件] " + message);
            commandInfo.ReplyToCommand(message);
        }
        else
        {
            player.PrintToChat("[重甲战士] " + message);
            Console.WriteLine("[重甲幸运玩家插件] " + player.PlayerName + " 禁用了重甲战士模式");
        }

        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsValid && p != player)
            {
                p.PrintToChat("🎮 重甲战士模式已禁用！");
            }
        }
    }

    private void CommandStatusHeavyArmor(CCSPlayerController? player, CommandInfo commandInfo)
    {
        string status = _pluginEnabled ? "✅ 启用" : "❌ 禁用";
        string currentWarrior = _currentHeavyArmorPlayer != null && _currentHeavyArmorPlayer.IsValid
            ? "🛡️ 当前重甲战士: " + _currentHeavyArmorPlayer.PlayerName
            : "🛡️ 当前无重甲战士";

        if (player == null)
        {
            commandInfo.ReplyToCommand("=== 重甲战士插件状态 ===");
            commandInfo.ReplyToCommand("状态: " + status);
            commandInfo.ReplyToCommand(currentWarrior);
        }
        else
        {
            player.PrintToChat("=== 重甲战士插件状态 ===");
            player.PrintToChat("状态: " + status);
            player.PrintToChat(currentWarrior);
        }
    }

    private void CommandEventEnable(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (_eventManager.IsEnabled)
        {
            commandInfo.ReplyToCommand("娱乐事件系统已经是启用状态！");
            return;
        }

        _eventManager.IsEnabled = true;
        string message = "🎲 娱乐事件系统已启用！下回合将开始随机事件。";

        if (player == null)
        {
            Console.WriteLine("[娱乐事件] " + message);
            commandInfo.ReplyToCommand(message);
        }
        else
        {
            player.PrintToChat("[娱乐事件] " + message);
            Console.WriteLine("[娱乐事件] " + player.PlayerName + " 启用了娱乐事件系统");
        }

        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsValid && p != player)
            {
                p.PrintToChat("🎲 娱乐事件系统已启用！");
            }
        }
    }

    private void CommandEventDisable(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!_eventManager.IsEnabled)
        {
            commandInfo.ReplyToCommand("娱乐事件系统已经是禁用状态！");
            return;
        }

        _eventManager.IsEnabled = false;

        if (_currentEvent != null)
        {
            _currentEvent.OnRevert();
            _currentEvent = null;
        }

        string message = "🚫 娱乐事件系统已禁用！";

        if (player == null)
        {
            Console.WriteLine("[娱乐事件] " + message);
            commandInfo.ReplyToCommand(message);
        }
        else
        {
            player.PrintToChat("[娱乐事件] " + message);
            Console.WriteLine("[娱乐事件] " + player.PlayerName + " 禁用了娱乐事件系统");
        }

        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsValid && p != player)
            {
                p.PrintToChat("🎲 娱乐事件系统已禁用！");
            }
        }
    }

    private void CommandEventStatus(CCSPlayerController? player, CommandInfo commandInfo)
    {
        string status = _eventManager.IsEnabled ? "✅ 启用" : "❌ 禁用";
        string current = _currentEvent != null
            ? "🎲 当前事件: " + _currentEvent.Name
            : "🎲 当前无事件";
        string previous = _previousEvent != null
            ? "📜 上上回合事件: " + _previousEvent.Name
            : "📜 上回合无事件";

        if (player == null)
        {
            commandInfo.ReplyToCommand("=== 娱乐事件系统状态 ===");
            commandInfo.ReplyToCommand("系统状态: " + status);
            commandInfo.ReplyToCommand(current);
            commandInfo.ReplyToCommand(previous);
        }
        else
        {
            player.PrintToChat("=== 娱乐事件系统状态 ===");
            player.PrintToChat("系统状态: " + status);
            player.PrintToChat(current);
            player.PrintToChat(previous);
        }
    }

    private void CommandEventList(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var events = _eventManager.GetAllEventNames();
        if (player == null)
        {
            commandInfo.ReplyToCommand("=== 可用事件列表 (" + events.Count + "个) ===");
            foreach (var eventName in events)
            {
                commandInfo.ReplyToCommand("  • " + eventName);
            }
        }
        else
        {
            player.PrintToChat("=== 可用事件列表 (" + events.Count + "个) ===");
            foreach (var eventName in events)
            {
                player.PrintToChat("  • " + eventName);
            }
        }
    }

    private void CommandEventWeights(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var weights = _eventManager.GetAllEventWeights();
        if (player == null)
        {
            commandInfo.ReplyToCommand("=== 事件权重列表 ===");
            foreach (var kvp in weights)
            {
                commandInfo.ReplyToCommand("  " + kvp.Key + ": " + kvp.Value);
            }
        }
        else
        {
            player.PrintToChat("=== 事件权重列表 ===");
            foreach (var kvp in weights)
            {
                player.PrintToChat("  " + kvp.Key + ": " + kvp.Value);
            }
        }
    }

    private void CommandEventWeight(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (commandInfo.ArgCount < 1)
        {
            string message = "用法: css_event_weight <事件英文名称> [权重值]";
            if (player == null)
                commandInfo.ReplyToCommand(message);
            else
                player.PrintToChat(message);
            return;
        }

        string eventName = commandInfo.GetArg(1);

        if (commandInfo.ArgCount == 1)
        {
            int weight = _eventManager.GetEventWeight(eventName);
            if (weight >= 0)
            {
                string message = "事件 '" + eventName + "' 的权重: " + weight;
                if (player == null)
                    commandInfo.ReplyToCommand(message);
                else
                    player.PrintToChat(message);
            }
            else
            {
                string message = "未找到事件: " + eventName;
                if (player == null)
                    commandInfo.ReplyToCommand(message);
                else
                    player.PrintToChat(message);
            }
            return;
        }

        if (!int.TryParse(commandInfo.GetArg(2), out int newWeight))
        {
            string message = "权重值必须是整数！";
            if (player == null)
                commandInfo.ReplyToCommand(message);
            else
                player.PrintToChat(message);
            return;
        }

        if (newWeight < 0)
        {
            string message = "权重值不能小于0！";
            if (player == null)
                commandInfo.ReplyToCommand(message);
            else
                player.PrintToChat(message);
            return;
        }

        bool success = _eventManager.SetEventWeight(eventName, newWeight);
        string resultMessage;
        if (success)
        {
            resultMessage = "✅ 事件 '" + eventName + "' 的权重已设置为 " + newWeight;
            if (newWeight == 0)
            {
                resultMessage += " (事件已禁用)";
            }
        }
        else
        {
            resultMessage = "❌ 未找到事件: " + eventName;
        }

        if (player == null)
            commandInfo.ReplyToCommand(resultMessage);
        else
            player.PrintToChat(resultMessage);
    }

    private void OnPlayerTakeDamagePostGlobal(CCSPlayerPawn player, CTakeDamageInfo info, CTakeDamageResult result)
    {
        if (_currentEvent is TeleportOnDamageEvent teleportEvent)
        {
            teleportEvent.HandlePlayerDamage(player, info, result);
        }
    }

    private HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        if (_currentEvent is JumpOnShootEvent jumpEvent)
        {
            jumpEvent.HandleWeaponFire(@event);
        }

        if (_currentEvent is JumpPlusPlusEvent jumpPlusPlusEvent)
        {
            jumpPlusPlusEvent.HandleWeaponFire(@event);
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (_currentEvent is VampireEvent vampireEvent)
        {
            vampireEvent.HandlePlayerDeath(@event);
        }

        return HookResult.Continue;
    }

    private void CommandEnableAllowAnywherePlant(CCSPlayerController? player, CommandInfo commandInfo)
    {
        _allowAnywherePlant = true;
        string message = "✅ 任意下包功能已启用！";
        if (player == null)
        {
            Console.WriteLine(message);
            commandInfo.ReplyToCommand(message);
        }
        else
        {
            player.PrintToChat(message);
        }
    }

    private void CommandDisableAllowAnywherePlant(CCSPlayerController? player, CommandInfo commandInfo)
    {
        _allowAnywherePlant = false;
        string message = "❌ 任意下包功能已禁用！";
        if (player == null)
        {
            Console.WriteLine(message);
            commandInfo.ReplyToCommand(message);
        }
        else
        {
            player.PrintToChat(message);
        }
    }

    private void CommandAllowAnywherePlantStatus(CCSPlayerController? player, CommandInfo commandInfo)
    {
        string status = _allowAnywherePlant ? "✅ 启用" : "❌ 禁用";
        string message = "任意下包功能状态: " + status;
        if (player == null)
        {
            Console.WriteLine(message);
            commandInfo.ReplyToCommand(message);
        }
        else
        {
            player.PrintToChat(message);
        }
    }

    private void CommandSetBombTimer(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (commandInfo.ArgCount < 1)
        {
            commandInfo.ReplyToCommand("用法: css_bombtimer_set <时间（秒）>");
            return;
        }

        if (!float.TryParse(commandInfo.GetArg(1), out float time))
        {
            commandInfo.ReplyToCommand("请输入有效的数字！");
            return;
        }

        if (time < 5 || time > 300)
        {
            commandInfo.ReplyToCommand("时间范围必须在 5 到 300 秒之间！");
            return;
        }

        _bombTimer = time;
        string message = "✅ 炸弹爆炸时间已设置为 " + _bombTimer + " 秒";
        if (player == null)
        {
            Console.WriteLine(message);
            commandInfo.ReplyToCommand(message);
        }
        else
        {
            player.PrintToChat(message);
        }
    }

    private void CommandBombTimerStatus(CCSPlayerController? player, CommandInfo commandInfo)
    {
        string message = "炸弹爆炸时间: " + _bombTimer + " 秒";
        if (player == null)
        {
            Console.WriteLine(message);
            commandInfo.ReplyToCommand(message);
        }
        else
        {
            player.PrintToChat(message);
        }
    }

    private HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
    {
        if (_currentEvent is AnywhereBombPlantEvent)
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

        if (player != _currentHeavyArmorPlayer)
            return HookResult.Continue;

        var itemName = @event.Item;

        if (IsPrimaryWeapon(itemName))
        {
            player.PrintToChat(" 🚫 重甲战士无法拾取主武器！");
            Console.WriteLine("[重甲战士] 阻止玩家 " + player.PlayerName + " 拾取主武器: " + itemName);

            ClearPrimaryWeapons(player);

            return HookResult.Stop;
        }

        return HookResult.Continue;
    }

    private bool IsPrimaryWeapon(string itemName)
    {
        string[] primaryWeapons =
        {
            "weapon_ak47", "weapon_m4a1", "weapon_m4a1_silencer", "weapon_aug", "weapon_sg556",
            "weapon_famas", "weapon_galilar", "weapon_awp", "weapon_ssg08",
            "weapon_g3sg1", "weapon_scar20", "weapon_m249",
            "weapon_mac10", "weapon_mp5sd", "weapon_mp7", "weapon_mp9", "weapon_p90",
            "weapon_ump45", "weapon_bizon", "weapon_mp5sd",
            "weapon_mag7", "weapon_nova", "weapon_sawedoff", "weapon_xm1014",
            "weapon_ssg08", "weapon_awp", "weapon_g3sg1", "weapon_scar20",
            "weapon_negev", "weapon_m249"
        };

        return primaryWeapons.Contains(itemName.ToLower());
    }

    private void ClearPrimaryWeapons(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Get();
        if (pawn == null || !pawn.IsValid)
            return;

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null)
            return;

        foreach (var weaponHandle in weaponServices.MyWeapons)
        {
            var weapon = weaponHandle.Get();
            if (weapon != null && weapon.IsValid)
            {
                var weaponBase = weapon.As<CCSWeaponBase>();
                if (weaponBase != null && weaponBase.VData != null)
                {
                    var weaponType = weaponBase.VData.WeaponType;
                    if (weaponType != CSWeaponType.WEAPONTYPE_PISTOL &&
                        weaponType != CSWeaponType.WEAPONTYPE_KNIFE &&
                        weaponType != CSWeaponType.WEAPONTYPE_C4 &&
                        weaponType != CSWeaponType.WEAPONTYPE_GRENADE &&
                        weaponType != CSWeaponType.WEAPONTYPE_TASER)
                    {
                        weapon.Remove();
                        Console.WriteLine("[重甲战士] 已移除玩家 " + player.PlayerName + " 的主武器: (类型: " + weaponType + ")");
                    }
                }
            }
        }
    }

    private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        if (_currentEvent is VampireEvent vampireEvent)
        {
            vampireEvent.HandlePlayerHurt(@event);
        }

        if (_currentEvent is SwapOnHitEvent swapEvent)
        {
            swapEvent.HandlePlayerHurt(@event);
        }

        return HookResult.Continue;
    }

    private void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
    {
        // 处理 AnywhereBombPlant 事件
        if (_currentEvent is AnywhereBombPlantEvent anywhereBombEvent)
        {
            anywhereBombEvent.HandlePlayerButtonsChanged(player, pressed);
        }

        // 处理旧的任意下包功能（向后兼容）
        if (!_allowAnywherePlant)
            return;

        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Get();
        if (pawn == null || !pawn.IsValid)
            return;

        if ((pressed & PlayerButtons.Use) != 0)
        {
            var weaponServices = pawn.WeaponServices;
            if (weaponServices == null)
                return;

            var activeWeapon = weaponServices.ActiveWeapon.Get();
            if (activeWeapon == null || !activeWeapon.IsValid)
                return;

            var weaponBase = activeWeapon.As<CCSWeaponBase>();
            if (weaponBase == null || weaponBase.VData == null)
                return;

            if (weaponBase.VData.WeaponType == CSWeaponType.WEAPONTYPE_C4)
            {
                pawn.InBombZone = true;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_bInBombZone");

                Console.WriteLine("[任意下包] 玩家 " + player.PlayerName + " 按下Use键，已临时设置InBombZone为true");
            }
        }
    }

    private HookResult OnBombAbortPlant(EventBombAbortplant @event, GameEventInfo info)
    {
        // 处理 AnywhereBombPlant 事件
        if (_currentEvent is AnywhereBombPlantEvent anywhereBombEvent)
        {
            var player = @event.Userid;
            if (player != null && player.IsValid)
            {
                if (anywhereBombEvent.HandleBombAbortPlant(player))
                {
                    return HookResult.Stop;
                }
            }
        }

        // 处理旧的任意下包功能（向后兼容）
        if (!_allowAnywherePlant)
            return HookResult.Continue;

        var player2 = @event.Userid;
        if (player2 == null || !player2.IsValid)
            return HookResult.Continue;

        var pawn = player2.PlayerPawn.Get();
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        if (!pawn.InBombZone)
        {
            pawn.InBombZone = true;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_bInBombZone");

            Console.WriteLine("[任意下包] 阻止玩家 " + player2.PlayerName + " 的下包被取消");

            return HookResult.Stop;
        }

        return HookResult.Continue;
    }

    private void OnServerPostEntityThink()
    {
        // 处理 AnywhereBombPlant 事件
        if (_currentEvent is AnywhereBombPlantEvent anywhereBombEvent)
        {
            anywhereBombEvent.HandleServerPostEntityThink();
        }

        // 处理旧的任意下包功能（向后兼容）
        if (_allowAnywherePlant)
        {
            var players = Utilities.GetPlayers();
            foreach (var player in players)
            {
                if (player == null || !player.IsValid)
                    continue;

                var pawn = player.PlayerPawn.Get();
                if (pawn == null || !pawn.IsValid)
                    continue;

                var weaponServices = pawn.WeaponServices;
                if (weaponServices == null)
                    continue;

                var activeWeapon = weaponServices.ActiveWeapon.Get();
                if (activeWeapon == null || !activeWeapon.IsValid)
                    continue;

                var weaponBase = activeWeapon.As<CCSWeaponBase>();
                if (weaponBase == null || weaponBase.VData == null)
                    continue;

                if (weaponBase.VData.WeaponType == CSWeaponType.WEAPONTYPE_C4)
                {
                    if (!pawn.InBombZone)
                    {
                        pawn.InBombZone = true;
                        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_bInBombZone");
                    }
                }
            }
        }
    }
}
