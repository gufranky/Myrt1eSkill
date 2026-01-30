using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;

namespace HelloWorldPlugin;

/// <summary>
/// 一发AK事件 - 所有玩家的枪都只有一发子弹（弹夹），备用弹药保留
/// </summary>
public class OneShotEvent : EntertainmentEvent
{
    public override string Name => "OneShot";
    public override string DisplayName => "💥 一发AK";
    public override string Description => "所有玩家的枪都只有一发子弹（弹夹）！备用弹药保留！";

    public override void OnApply()
    {
        Console.WriteLine("[一发AK] 事件已激活");

        // 设置所有玩家的武器为1发子弹
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive) continue;

            SetAllWeaponsToOneBullet(player);
        }

        // 注册事件监听
        if (Plugin != null)
        {
            Plugin.RegisterEventHandler<EventItemEquip>(OnItemEquip, HookMode.Post);
            Plugin.RegisterEventHandler<EventItemPickup>(OnItemPickup, HookMode.Post);
            Plugin.RegisterEventHandler<EventWeaponReload>(OnWeaponReload, HookMode.Post);
            Plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
        }

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToCenter("💥 一发AK模式！\n弹夹只有1发！备用弹药保留！");
                player.PrintToChat(" 💥 一发AK模式已启用！");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[一发AK] 事件已恢复");

        // 移除事件监听
        if (Plugin != null)
        {
            Plugin.DeregisterEventHandler<EventItemEquip>(OnItemEquip, HookMode.Post);
            Plugin.DeregisterEventHandler<EventItemPickup>(OnItemPickup, HookMode.Post);
            Plugin.DeregisterEventHandler<EventWeaponReload>(OnWeaponReload, HookMode.Post);
            Plugin.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
        }

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("💥 一发AK模式已禁用");
            }
        }
    }

    /// <summary>
    /// 设置玩家所有武器为1发子弹
    /// </summary>
    private void SetAllWeaponsToOneBullet(CCSPlayerController player)
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

            // 跳过刀和C4
            var weaponType = weaponBase.VData.WeaponType;
            if (weaponType == CSWeaponType.WEAPONTYPE_KNIFE ||
                weaponType == CSWeaponType.WEAPONTYPE_C4)
                continue;

            // 只设置弹夹为1发，保留备用弹药
            // 这样玩家换弹时可以从备用弹药补充
            weaponBase.Clip1 = 1;

            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
        }

        Console.WriteLine($"[一发AK] {player.PlayerName} 的所有武器已设置为1发弹夹");
    }

    /// <summary>
    /// 监听装备武器事件
    /// </summary>
    private HookResult OnItemEquip(EventItemEquip @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            SetAllWeaponsToOneBullet(player);
        });

        return HookResult.Continue;
    }

    /// <summary>
    /// 监听拾取武器事件
    /// </summary>
    private HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            SetAllWeaponsToOneBullet(player);
        });

        return HookResult.Continue;
    }

    /// <summary>
    /// 监听换弹事件
    /// </summary>
    private HookResult OnWeaponReload(EventWeaponReload @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            SetAllWeaponsToOneBullet(player);
        });

        return HookResult.Continue;
    }

    /// <summary>
    /// 监听玩家生成事件
    /// </summary>
    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            SetAllWeaponsToOneBullet(player);
            player.PrintToCenter("💥 一发AK模式！\n弹夹只有1发！备用弹药保留！");
        });

        return HookResult.Continue;
    }
}
