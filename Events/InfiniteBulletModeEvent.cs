using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;

namespace MyrtleSkill;

/// <summary>
/// 无限子弹模式事件 - 无限备弹、自动补充、无需换弹
/// 不使用 sv_cheats，通过监听射击事件来补充弹药
/// </summary>
public class InfiniteBulletModeEvent : EntertainmentEvent
{
    public override string Name => "InfiniteBulletMode";
    public override string DisplayName => "🔥 无限子弹模式";
    public override string Description => "无限备弹！自动补充！无需换弹！火力全开！";

    private bool _isActive = false;

    public override void OnApply()
    {
        Console.WriteLine("[无限子弹模式] 事件已激活");

        // 设置激活标志
        _isActive = true;

        // 为所有玩家补充弹药
        RefillAllAmmo();

        // 注册事件监听
        if (Plugin != null)
        {
            Plugin.RegisterEventHandler<EventWeaponFire>(OnWeaponFire, HookMode.Post);
            Plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
        }

        // 显示提示（保留聊天框提示，移除屏幕中间提示，统一由HUD显示）
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("🔥 无限子弹模式已启用！");
                player.PrintToChat("💡 射击自动补充弹药，无需换弹！");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[无限子弹模式] 事件已恢复");

        // 首先取消激活标志
        _isActive = false;

        // 移除事件监听
        if (Plugin != null)
        {
            Plugin.DeregisterEventHandler<EventWeaponFire>(OnWeaponFire, HookMode.Post);
            Plugin.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
        }

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("🔥 无限子弹模式已禁用");
            }
        }
    }

    /// <summary>
    /// 为所有玩家补充弹药
    /// </summary>
    private void RefillAllAmmo()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive)
                continue;

            RefillPlayerAmmo(player);
        }
    }

    /// <summary>
    /// 为单个玩家补充所有武器弹药
    /// </summary>
    private void RefillPlayerAmmo(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null)
            return;

        // 补充所有武器的弹药
        foreach (var weaponHandle in weaponServices.MyWeapons)
        {
            if (!weaponHandle.IsValid)
                continue;

            var weapon = weaponHandle.Get();
            if (weapon == null || !weapon.IsValid)
                continue;

            var weaponBase = weapon.As<CCSWeaponBase>();
            if (weaponBase == null || weaponBase.VData == null)
                continue;

            // 获取武器定义
            var weaponData = weaponBase.VData;

            // 直接设置弹夹为最大值
            if (weaponBase.Clip1 >= 0 && weaponData.MaxClip1 > 0)
            {
                weaponBase.Clip1 = weaponData.MaxClip1;
            }

            if (weaponBase.Clip2 >= 0 && weaponData.MaxClip2 > 0)
            {
                weaponBase.Clip2 = weaponData.MaxClip2;
            }

            Console.WriteLine($"[无限子弹模式] {player.PlayerName} 的 {weaponData.Name} 弹药已补充");
        }
    }

    /// <summary>
    /// 处理武器射击事件 - 自动补充弹药
    /// </summary>
    private HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        // 如果事件不激活，不处理
        if (!_isActive)
            return HookResult.Continue;

        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null)
            return HookResult.Continue;

        var activeWeapon = weaponServices.ActiveWeapon.Value;
        if (activeWeapon == null || !activeWeapon.IsValid)
            return HookResult.Continue;

        var weaponBase = activeWeapon.As<CCSWeaponBase>();
        if (weaponBase == null || weaponBase.VData == null)
            return HookResult.Continue;

        // 延迟补充弹药（等待射击完成）
        Plugin?.AddTimer(0.05f, () =>
        {
            if (_isActive && player.IsValid && player.PawnIsAlive)
            {
                // 补充弹夹到最大值
                if (weaponBase.Clip1 >= 0 && weaponBase.VData.MaxClip1 > 0)
                {
                    weaponBase.Clip1 = weaponBase.VData.MaxClip1;
                }

                if (weaponBase.Clip2 >= 0 && weaponBase.VData.MaxClip2 > 0)
                {
                    weaponBase.Clip2 = weaponBase.VData.MaxClip2;
                }
            }
        });

        return HookResult.Continue;
    }

    /// <summary>
    /// 玩家生成时补充弹药
    /// </summary>
    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        // 如果事件不激活，不处理
        if (!_isActive)
            return HookResult.Continue;

        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        // 延迟处理，等待玩家完全生成
        Plugin?.AddTimer(0.5f, () =>
        {
            if (_isActive && player.IsValid && player.PawnIsAlive)
            {
                RefillPlayerAmmo(player);
                player.PrintToCenter("🔥 无限子弹模式！");
                Console.WriteLine($"[无限子弹模式] {player.PlayerName} 生成，已补充弹药");
            }
        });

        return HookResult.Continue;
    }
}
