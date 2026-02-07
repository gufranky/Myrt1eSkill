// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills by Juzlus (Infinite Ammo skill)

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 无限弹药技能 - 被动技能
/// 你的所有武器都将获得无限弹药
/// 完全复制自 jRandomSkills Infinite Ammo
/// </summary>
public class InfiniteAmmoSkill : PlayerSkill
{
    public override string Name => "InfiniteAmmo";
    public override string DisplayName => "∞ 无限弹药";
    public override string Description => "你的所有武器都将获得无限弹药！";
    public override bool IsActive => false; // 被动技能

    // ✅ 跟踪拥有无限弹药技能的玩家（使用槽位而非SteamID，更可靠）
    private static readonly HashSet<int> _enabledPlayers = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 添加到跟踪列表
        _enabledPlayers.Add(player.Slot);

        // 应用无限弹药
        ApplyInfiniteAmmo(player);

        // 注册 OnTick 监听器（第一次）
        if (_enabledPlayers.Count == 1)
        {
            Plugin?.RegisterListener<Listeners.OnTick>(OnTick);
        }

        Console.WriteLine($"[无限弹药] {player.PlayerName} 获得了无限弹药技能");
        player.PrintToChat("∞ 你获得了无限弹药技能！");
        player.PrintToChat("💡 你的所有武器都将获得无限弹药！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 从跟踪列表移除
        _enabledPlayers.Remove(player.Slot);

        // 如果没有玩家使用技能，移除 OnTick 监听器
        if (_enabledPlayers.Count == 0)
        {
            Plugin?.RemoveListener<Listeners.OnTick>(OnTick);
        }

        Console.WriteLine($"[无限弹药] {player.PlayerName} 失去了无限弹药技能");
    }

    /// <summary>
    /// 每帧更新 - 持续设置弹夹为100
    /// </summary>
    private void OnTick()
    {
        // 为所有拥有无限弹药技能的玩家设置弹夹
        foreach (var slot in _enabledPlayers)
        {
            var player = Utilities.GetPlayerFromSlot(slot);
            if (player != null && player.IsValid && player.PawnIsAlive)
            {
                ApplyInfiniteAmmo(player);
            }
        }
    }

    /// <summary>
    /// 处理武器开火事件（在主文件的 OnWeaponFire 中调用）
    /// 完全复制自 jRandomSkills InfiniteAmmo.WeaponFire
    /// </summary>
    public void OnWeaponFire(EventWeaponFire @event)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return;

        // 检查玩家是否有无限弹药技能
        var skills = Plugin?.SkillManager.GetPlayerSkills(player);
        if (skills == null || skills.Count == 0)
            return;

        var infiniteAmmoSkill = skills.FirstOrDefault(s => s.Name == "InfiniteAmmo");
        if (infiniteAmmoSkill == null)
            return;

        // 应用无限弹药
        ApplyInfiniteAmmo(player);
    }

    /// <summary>
    /// 处理投掷手雷事件（在主文件的 OnGrenadeThrown 中调用）
    /// 完全复制自 jRandomSkills InfiniteAmmo.GrenadeThrown
    /// </summary>
    public void OnGrenadeThrown(EventGrenadeThrown @event)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return;

        // 检查玩家是否有无限弹药技能
        var skills = Plugin?.SkillManager.GetPlayerSkills(player);
        if (skills == null || skills.Count == 0)
            return;

        var infiniteAmmoSkill = skills.FirstOrDefault(s => s.Name == "InfiniteAmmo");
        if (infiniteAmmoSkill == null)
            return;

        // 补充投掷的武器
        player.GiveNamedItem($"weapon_{@event.Weapon}");

        Console.WriteLine($"[无限弹药] {player.PlayerName} 投掷 {@event.Weapon}，已补充");
    }

    /// <summary>
    /// 处理武器换弹事件（在主文件的 OnWeaponReload 中调用）
    /// 注意：由于OnTick会持续设置弹夹，这个方法现在主要是为了日志记录
    /// </summary>
    public void OnWeaponReload(EventWeaponReload @event)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return;

        // 检查玩家是否有无限弹药技能
        if (!_enabledPlayers.Contains(player.Slot))
            return;

        // OnTick 会持续设置弹夹，这里只记录日志
        Console.WriteLine($"[无限弹药] {player.PlayerName} 换弹");
    }

    /// <summary>
    /// 应用无限弹药效果
    /// 完全复制自 jRandomSkills InfiniteAmmo.ApplyInfiniteAmmo
    /// </summary>
    private void ApplyInfiniteAmmo(CCSPlayerController player)
    {
        var activeWeaponHandle = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon;
        if (activeWeaponHandle == null || activeWeaponHandle.Value == null)
            return;

        // 将弹夹设置为100（无限弹药）
        activeWeaponHandle.Value.Clip1 = 100;

        // 通知状态改变
        Utilities.SetStateChanged(activeWeaponHandle.Value, "CBasePlayerWeapon", "m_iClip1");

        Console.WriteLine($"[无限弹药] {player.PlayerName} 的武器弹药已填满");
    }
}
