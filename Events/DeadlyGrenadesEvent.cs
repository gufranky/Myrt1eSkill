using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace HelloWorldPlugin;

/// <summary>
/// 更致命的手雷事件 - 无限高爆手雷、移除主副武器、禁用商店、手雷伤害和范围增加
/// </summary>
public class DeadlyGrenadesEvent : EntertainmentEvent
{
    public override string Name => "DeadlyGrenades";
    public override string DisplayName => "💣 更致命的手雷";
    public override string Description => "无限高爆手雷！移除主副武器！禁用商店！手雷伤害和范围增加！";

    private ConVar? _buyAllowGunsConVar;
    private ConVar? _heDamageConVar;
    private ConVar? _heRadiusConVar;
    private ConVar? _infiniteAmmoConVar;
    private bool _originalBuyAllowGuns = true;
    private float _originalHeDamage = 1.0f;
    private float _originalHeRadius = 1.0f;
    private bool _originalInfiniteAmmo = false;

    private readonly Dictionary<int, List<string>> _cachedWeapons = new();

    public override void OnApply()
    {
        Console.WriteLine("[更致命的手雷] 事件已激活");

        // 1. 禁用商店
        _buyAllowGunsConVar = ConVar.Find("mp_buy_allow_guns");
        if (_buyAllowGunsConVar != null)
        {
            _originalBuyAllowGuns = _buyAllowGunsConVar.GetPrimitiveValue<bool>();
            _buyAllowGunsConVar.SetValue(false);
            Console.WriteLine("[更致命的手雷] mp_buy_allow_guns 已设置为 false");
        }

        // 2. 增加手雷伤害和范围
        _heDamageConVar = ConVar.Find("sv_hegrenade_damage_multiplier");
        if (_heDamageConVar != null)
        {
            _originalHeDamage = _heDamageConVar.GetPrimitiveValue<float>();
            _heDamageConVar.SetValue(3.0f); // 3倍伤害
            Console.WriteLine($"[更致命的手雷] sv_hegrenade_damage_multiplier 已设置为 3.0 (原值: {_originalHeDamage})");
        }

        _heRadiusConVar = ConVar.Find("sv_hegrenade_radius_multiplier");
        if (_heRadiusConVar != null)
        {
            _originalHeRadius = _heRadiusConVar.GetPrimitiveValue<float>();
            _heRadiusConVar.SetValue(5.0f); // 5倍范围
            Console.WriteLine($"[更致命的手雷] sv_hegrenade_radius_multiplier 已设置为 5.0 (原值: {_originalHeRadius})");
        }

        // 3. 启用无限弹药
        _infiniteAmmoConVar = ConVar.Find("sv_infinite_ammo");
        if (_infiniteAmmoConVar != null)
        {
            _originalInfiniteAmmo = _infiniteAmmoConVar.GetPrimitiveValue<bool>();
            _infiniteAmmoConVar.SetValue(true);
            Console.WriteLine($"[更致命的手雷] sv_infinite_ammo 已设置为 true (原值: {_originalInfiniteAmmo})");
        }

        // 4. 移除所有玩家的主副武器并给予手雷
        RemoveWeaponsAndGiveGrenades();

        // 5. 注册事件监听
        if (Plugin != null)
        {
            Plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
            Plugin.RegisterEventHandler<EventItemPickup>(OnItemPickup, HookMode.Post);
            Plugin.RegisterEventHandler<EventItemEquip>(OnItemEquip, HookMode.Post);
        }

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToCenter("💣 更致命的手雷！\n无限高爆手雷 + 3倍伤害 + 5倍范围！");
                player.PrintToChat("💣 更致命的手雷模式已启用！");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[更致命的手雷] 事件已恢复");

        // 移除事件监听
        if (Plugin != null)
        {
            Plugin.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
            Plugin.DeregisterEventHandler<EventItemPickup>(OnItemPickup, HookMode.Post);
            Plugin.DeregisterEventHandler<EventItemEquip>(OnItemEquip, HookMode.Post);
        }

        // 恢复商店
        if (_buyAllowGunsConVar != null)
        {
            _buyAllowGunsConVar.SetValue(_originalBuyAllowGuns);
            Console.WriteLine($"[更致命的手雷] mp_buy_allow_guns 已恢复为 {_originalBuyAllowGuns}");
        }

        // 恢复手雷伤害和范围
        if (_heDamageConVar != null)
        {
            _heDamageConVar.SetValue(_originalHeDamage);
            Console.WriteLine($"[更致命的手雷] sv_hegrenade_damage_multiplier 已恢复为 {_originalHeDamage}");
        }

        if (_heRadiusConVar != null)
        {
            _heRadiusConVar.SetValue(_originalHeRadius);
            Console.WriteLine($"[更致命的手雷] sv_hegrenade_radius_multiplier 已恢复为 {_originalHeRadius}");
        }

        // 恢复无限弹药
        if (_infiniteAmmoConVar != null)
        {
            _infiniteAmmoConVar.SetValue(_originalInfiniteAmmo);
            Console.WriteLine($"[更致命的手雷] sv_infinite_ammo 已恢复为 {_originalInfiniteAmmo}");
        }

        // 返还武器
        ReturnAllWeapons();

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("💣 更致命的手雷模式已禁用");
            }
        }

        _cachedWeapons.Clear();
    }

    /// <summary>
    /// 移除主副武器并给予手雷
    /// </summary>
    private void RemoveWeaponsAndGiveGrenades()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive) continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            var weaponServices = pawn.WeaponServices;
            if (weaponServices == null) continue;

            // 保存当前武器
            List<string> cachedWeapons = new List<string>();
            List<CHandle<CBasePlayerWeapon>>? weaponHandles = weaponServices.MyWeapons.ToList();

            foreach (var weaponHandle in weaponHandles)
            {
                if (weaponHandle.IsValid && weaponHandle.Value != null)
                {
                    var weapon = weaponHandle.Get();
                    if (weapon != null && weapon.IsValid)
                    {
                        var weaponBase = weapon.As<CCSWeaponBase>();
                        if (weaponBase != null && weaponBase.VData != null)
                        {
                            var weaponType = weaponBase.VData.WeaponType;
                            // 只保存主武器和副武器
                            if (weaponType == CSWeaponType.WEAPONTYPE_PISTOL ||
                                weaponType == CSWeaponType.WEAPONTYPE_SUBMACHINEGUN ||
                                weaponType == CSWeaponType.WEAPONTYPE_RIFLE ||
                                weaponType == CSWeaponType.WEAPONTYPE_SHOTGUN ||
                                weaponType == CSWeaponType.WEAPONTYPE_SNIPER_RIFLE ||
                                weaponType == CSWeaponType.WEAPONTYPE_MACHINEGUN)
                            {
                                cachedWeapons.Add(weaponBase.DesignerName);
                            }
                        }
                    }
                }
            }

            _cachedWeapons[player.Slot] = cachedWeapons;

            // 移除所有武器
            RemoveAllWeapons(player);

            // 只给予高爆手雷
            player.GiveNamedItem("weapon_hegrenade");

            Console.WriteLine($"[更致命的手雷] {player.PlayerName} 的主副武器已移除，给予高爆手雷");
        }
    }

    /// <summary>
    /// 移除玩家所有武器
    /// </summary>
    private void RemoveAllWeapons(CCSPlayerController player)
    {
        if (player == null || !player.IsValid) return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return;

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null) return;

        // 移除所有武器
        var weaponsToRemove = new List<CBasePlayerWeapon>();
        foreach (var weaponHandle in weaponServices.MyWeapons)
        {
            if (weaponHandle.IsValid)
            {
                var weapon = weaponHandle.Get();
                if (weapon != null && weapon.IsValid)
                {
                    weaponsToRemove.Add(weapon);
                }
            }
        }

        foreach (var weapon in weaponsToRemove)
        {
            weapon.Remove();
        }
    }

    /// <summary>
    /// 返还所有玩家的原始武器
    /// </summary>
    private void ReturnAllWeapons()
    {
        foreach (var kvp in _cachedWeapons)
        {
            var player = Utilities.GetPlayerFromSlot(kvp.Key);
            if (player == null || !player.IsValid || !player.PawnIsAlive)
                continue;

            // 先移除当前武器
            RemoveAllWeapons(player);

            // 返还原始武器
            foreach (var weaponName in kvp.Value)
            {
                player.GiveNamedItem(weaponName);
            }
        }

        _cachedWeapons.Clear();
    }

    /// <summary>
    /// 玩家生成时给予手雷
    /// </summary>
    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            // 移除主副武器，给予高爆手雷
            RemoveWeaponsAndGiveGrenades();
            player.PrintToCenter("💣 更致命的手雷！\n无限高爆手雷 + 3倍伤害 + 5倍范围！");
        });

        return HookResult.Continue;
    }

    /// <summary>
    /// 阻止拾取主副武器
    /// </summary>
    private HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            // 再次移除主副武器，确保只有手雷
            RemoveNonGrenadeWeapons(player);
        });

        return HookResult.Continue;
    }

    /// <summary>
    /// 阻止装备主副武器
    /// </summary>
    private HookResult OnItemEquip(EventItemEquip @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            // 再次移除主副武器，确保只有手雷
            RemoveNonGrenadeWeapons(player);
        });

        return HookResult.Continue;
    }

    /// <summary>
    /// 移除除手雷外的所有武器
    /// </summary>
    private void RemoveNonGrenadeWeapons(CCSPlayerController player)
    {
        if (player == null || !player.IsValid) return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return;

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null) return;

        foreach (var weaponHandle in weaponServices.MyWeapons)
        {
            if (!weaponHandle.IsValid) continue;

            var weapon = weaponHandle.Get();
            if (weapon == null || !weapon.IsValid) continue;

            var weaponBase = weapon.As<CCSWeaponBase>();
            if (weaponBase == null || weaponBase.VData == null) continue;

            // 只保留高爆手雷，移除其他所有武器
            string weaponName = weaponBase.DesignerName.ToLower();
            bool isHEGrenade = weaponName.Contains("hegrenade");

            // 如果不是高爆手雷，移除它
            if (!isHEGrenade)
            {
                weapon.Remove();
                Console.WriteLine($"[更致命的手雷] 移除了 {player.PlayerName} 的 {weaponBase.DesignerName}");
            }
        }
    }
}
