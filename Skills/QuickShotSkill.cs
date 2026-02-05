using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 速射技能 - 被动技能
/// 无后坐力，射速最大化，可以瞬间开火
/// </summary>
public class QuickShotSkill : PlayerSkill
{
    public override string Name => "QuickShot";
    public override string DisplayName => "⚡ 速射";
    public override string Description => "无后坐力！射速最大化！瞬间开火！";
    public override bool IsActive => false; // 被动技能

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[速射] {player.PlayerName} 获得了速射技能");
        player.PrintToChat("⚡ 你获得了速射技能！");
        player.PrintToChat("🔫 无后坐力！射速最大化！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[速射] {player.PlayerName} 失去了速射技能");
    }

    /// <summary>
    /// 每帧更新 - 移除后坐力并重置攻击时间
    /// </summary>
    public static void OnTick(PlayerSkillManager skillManager)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid)
                continue;

            // 检查玩家是否有速射技能（修复：检查所有技能）
            var skills = skillManager.GetPlayerSkills(player);
            if (skills.Count == 0)
                continue;

            var quickShotSkill = skills.FirstOrDefault(s => s.Name == "QuickShot");
            if (quickShotSkill == null)
                continue;

            var pawn = player.PlayerPawn?.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            var weaponServices = pawn.WeaponServices;
            if (weaponServices == null || weaponServices.ActiveWeapon == null || !weaponServices.ActiveWeapon.IsValid)
                continue;

            var weapon = weaponServices.ActiveWeapon.Value;
            if (weapon == null || !weapon.IsValid || pawn.CameraServices == null)
                continue;

            // 移除后坐力
            pawn.AimPunchTickBase = 0;
            pawn.AimPunchTickFraction = 0f;
            pawn.CameraServices.CsViewPunchAngleTick = 0;
            pawn.CameraServices.CsViewPunchAngleTickRatio = 0f;

            // 设置武器下次攻击时间为当前时间（射速最大化）
            weapon.NextPrimaryAttackTick = Server.TickCount;
            weapon.NextSecondaryAttackTick = Server.TickCount;

            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextSecondaryAttackTick");
        }
    }
}
