using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;

namespace MyrtleSkill;

/// <summary>
/// 一发AK事件 - 所有玩家的枪都只有一发子弹（弹夹），备用弹药保留
/// </summary>
public class OneShotEvent : EntertainmentEvent
{
    public override string Name => "OneShot";
    public override string DisplayName => "💥 一发AK";
    public override string Description => "所有玩家的枪都只有一发子弹（弹夹）！备用弹药保留！";

    // 保存每个武器类型的原始MaxClip1（全局共享，按武器类型）
    private readonly Dictionary<string, int> _cachedMaxClip1 = new();

    // 标志：事件是否激活（用于防止监听器在事件结束后继续工作）
    private bool _isActive = false;

    public override void OnApply()
    {
        Console.WriteLine("[一发AK] 事件已激活");

        // 设置激活标志
        _isActive = true;

        // 设置所有玩家的武器为1发子弹，并保存原始弹夹数量
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

        // 首先取消激活标志，阻止监听器继续工作
        _isActive = false;

        // 移除事件监听
        if (Plugin != null)
        {
            Plugin.DeregisterEventHandler<EventItemEquip>(OnItemEquip, HookMode.Post);
            Plugin.DeregisterEventHandler<EventItemPickup>(OnItemPickup, HookMode.Post);
            Plugin.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
        }

        // 恢复所有武器的MaxClip1
        RestoreAllWeaponMaxClip1();

        // 清空缓存（重要！）
        _cachedMaxClip1.Clear();
        Console.WriteLine("[一发AK] 缓存已清空");

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
    /// 恢复所有武器的MaxClip1
    /// </summary>
    /// <summary>
    /// 恢复所有武器的MaxClip1
    /// </summary>
    private void RestoreAllWeaponMaxClip1()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive)
                continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            var weaponServices = pawn.WeaponServices;
            if (weaponServices == null)
                continue;

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

                var weaponType = weaponBase.VData.WeaponType;
                if (weaponType == CSWeaponType.WEAPONTYPE_KNIFE ||
                    weaponType == CSWeaponType.WEAPONTYPE_C4)
                    continue;

                string weaponName = weaponBase.DesignerName;
                if (_cachedMaxClip1.TryGetValue(weaponName, out int originalMaxClip1))
                {
                    // 使用 NextFrame 确保武器实体仍然有效
                    Server.NextFrame(() =>
                    {
                        if (weaponBase.IsValid && weaponBase.VData != null)
                        {
                            // 恢复 MaxClip1
                            weaponBase.VData.MaxClip1 = originalMaxClip1;
                            
                            // 强制通知客户端
                            Utilities.SetStateChanged(weaponBase, "CBasePlayerWeapon", "m_iClip1");
                            
                            Console.WriteLine($"[一发AK] {player.PlayerName} 的武器 {weaponName} MaxClip1已恢复为 {originalMaxClip1}");
                        }
                    });
                }
            }
        }
    }

    /// <summary>
    /// 设置玩家所有武器为1发子弹，并修改MaxClip1
    /// </summary>
    /// <summary>
    /// 设置玩家所有武器为1发子弹，并修改MaxClip1
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

            // 跳过刀、C4和投掷物
            var weaponType = weaponBase.VData.WeaponType;
            if (weaponType == CSWeaponType.WEAPONTYPE_KNIFE ||
                weaponType == CSWeaponType.WEAPONTYPE_C4 ||
                weaponType == CSWeaponType.WEAPONTYPE_GRENADE)
                continue;

            string weaponName = weaponBase.DesignerName;

            // 保存原始MaxClip1（如果还没保存过）
            if (!_cachedMaxClip1.ContainsKey(weaponName))
            {
                _cachedMaxClip1[weaponName] = weaponBase.VData.MaxClip1;
                Console.WriteLine($"[一发AK] 保存 {weaponName} 的原始MaxClip1: {_cachedMaxClip1[weaponName]}");
            }

            // 修改MaxClip1为1（关键！这样换弹后也只能装1发）
            Server.NextFrame(() =>
            {
                if (weaponBase.IsValid && weaponBase.VData != null)
                {
                    weaponBase.VData.MaxClip1 = 1;
                }
            });

            // 设置当前Clip1为1
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
        // 如果事件不激活，不处理
        if (!_isActive) return HookResult.Continue;

        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            // 再次检查事件是否仍然激活
            if (_isActive)
            {
                SetAllWeaponsToOneBullet(player);
            }
        });

        return HookResult.Continue;
    }

    /// <summary>
    /// 监听拾取武器事件
    /// </summary>
    private HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
    {
        // 如果事件不激活，不处理
        if (!_isActive) return HookResult.Continue;

        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            // 再次检查事件是否仍然激活
            if (_isActive)
            {
                SetAllWeaponsToOneBullet(player);
            }
        });

        return HookResult.Continue;
    }

    /// <summary>
    /// 监听玩家生成事件
    /// </summary>
    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        // 如果事件不激活，不处理
        if (!_isActive) return HookResult.Continue;

        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            // 再次检查事件是否仍然激活
            if (_isActive)
            {
                SetAllWeaponsToOneBullet(player);
                player.PrintToCenter("💥 一发AK模式！\n弹夹只有1发！备用弹药保留！");
            }
        });

        return HookResult.Continue;
    }
}
