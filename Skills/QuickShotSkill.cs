// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills QuickShot

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 速射技能 - 被动技能
/// 无后坐力，射速最大化
/// 完全复制自 jRandomSkills QuickShot
/// </summary>
public class QuickShotSkill : PlayerSkill
{
    public override string Name => "QuickShot";
    public override string DisplayName => "⚡ 速射";
    public override string Description => "无后坐力！射速最大化！瞬间开火！";
    public override bool IsActive => false; // 被动技能

    // 与专注技能互斥（两者都修改武器状态）
    public override List<string> ExcludedSkills => new() { "Focus" };

    // 跟踪拥有该技能的玩家
    private readonly HashSet<int> _enabledPlayers = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        _enabledPlayers.Add(player.Slot);

        Console.WriteLine($"[速射] {player.PlayerName} 获得了速射技能");

        player.PrintToChat("⚡ 你获得了速射技能！");
        player.PrintToChat("🔫 无后坐力！射速最大化！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        _enabledPlayers.Remove(player.Slot);

        Console.WriteLine($"[速射] {player.PlayerName} 失去了速射技能");
    }

    /// <summary>
    /// 每帧更新 - 射速最大化（完全复制 jRandomSkills QuickShot.OnTick）
    /// </summary>
    public void OnTick()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid)
                continue;

            // 检查玩家是否有速射技能
            if (!_enabledPlayers.Contains(player.Slot))
                continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            var weaponServices = pawn.WeaponServices;
            if (weaponServices == null || weaponServices.ActiveWeapon == null || !weaponServices.ActiveWeapon.IsValid)
                continue;

            var weapon = weaponServices.ActiveWeapon.Value;
            if (weapon == null || !weapon.IsValid)
                continue;

            if (pawn.CameraServices == null)
                continue;

            // 重置后坐力视角偏移（复制自 jRandomSkills）
            pawn.AimPunchTickBase = 0;
            pawn.AimPunchTickFraction = 0f;
            pawn.CameraServices.CsViewPunchAngleTick = 0;
            pawn.CameraServices.CsViewPunchAngleTickRatio = 0f;

            // 设置武器下次攻击时间为当前时间（复制自 jRandomSkills）
            weapon.NextPrimaryAttackTick = Server.TickCount;
            weapon.NextSecondaryAttackTick = Server.TickCount;

            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextSecondaryAttackTick");
        }
    }
}
