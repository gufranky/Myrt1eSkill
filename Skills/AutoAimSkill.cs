// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills Aimbot skill by Juzlus

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using System.Runtime.InteropServices;

namespace MyrtleSkill.Skills;

/// <summary>
/// 自瞄技能 - 被动技能
/// 每一颗击中的子弹都算作爆头
/// 完全复制自 jRandomSkills Aimbot
/// </summary>
public class AutoAimSkill : PlayerSkill
{
    public override string Name => "AutoAim";
    public override string DisplayName => "🎯 自瞄";
    public override string Description => "每一颗击中的子弹都算作爆头！";
    public override bool IsActive => false; // 被动技能

    // 跟踪命中组的原始值（用于恢复）
    // 使用 nint 作为键，而不是原来的实现
    private static readonly Dictionary<nint, int> _hitGroups = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[自瞄] {player.PlayerName} 获得了自瞄技能");

        player.PrintToChat("🎯 你获得了自瞄技能！");
        player.PrintToChat("💡 每一颗击中的子弹都算作爆头！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 清理该玩家的所有命中组记录
        // 注意：jRandomSkills 的实现是在技能切换时恢复所有值
        // 这里我们不做清理，让 DisableAllHitGroups 来统一处理
        Console.WriteLine($"[自瞄] {player.PlayerName} 失去了自瞄技能");
    }

    /// <summary>
    /// 处理伤害前事件 - 将命中部位修改为头部
    /// 完全复制自 jRandomSkills Aimbot.OnTakeDamage
    /// </summary>
    public static void OnPlayerTakeDamagePre(CCSPlayerPawn victim, CTakeDamageInfo info, PlayerSkillManager skillManager)
    {
        if (info == null || info.Attacker == null || !info.Attacker.IsValid)
            return;

        var attackerPawnHandle = info.Attacker.Value;
        if (attackerPawnHandle == null || !attackerPawnHandle.IsValid)
            return;

        // 需要转换为 CCSPlayerPawn
        var attackerPawn = attackerPawnHandle.As<CCSPlayerPawn>();
        if (attackerPawn == null || !attackerPawn.IsValid)
            return;

        var victimPawn = victim;
        if (victimPawn == null || !victimPawn.IsValid)
            return;

        // 检查是否是玩家实体
        if (attackerPawn.DesignerName != "player" || victimPawn.DesignerName != "player")
            return;

        var attackerController = attackerPawn.Controller.Value;
        if (attackerController == null || !attackerController.IsValid || attackerController is not CCSPlayerController attacker)
            return;

        var victimController = victimPawn.Controller.Value;
        if (victimController == null || !victimController.IsValid || victimController is not CCSPlayerController victimPlayer)
            return;

        // 检查玩家是否有自瞄技能
        var skills = skillManager.GetPlayerSkills(attacker);
        if (skills.Count == 0)
            return;

        var autoAimSkill = skills.FirstOrDefault(s => s.Name == "AutoAim");
        if (autoAimSkill == null)
            return;

        if (!attacker.PawnIsAlive)
            return;

        // 完全复制 jRandomSkills 的内存操作
        nint hitGroupPointer = Marshal.ReadIntPtr(info.Handle, GameData.GetOffset("CTakeDamageInfo_HitGroup"));
        if (hitGroupPointer != nint.Zero)
        {
            nint hitGroupOffset = Marshal.ReadIntPtr(hitGroupPointer, 16);
            if (hitGroupOffset != nint.Zero)
            {
                // jRandomSkills: 只在有技能时修改
                if (autoAimSkill != null)
                {
                    // 保存原始值并设置为头部（读取两次，像 jRandomSkills 一样）
                    _hitGroups.TryAdd(hitGroupOffset, Marshal.ReadInt32(hitGroupOffset, 56));
                    Marshal.WriteInt32(hitGroupOffset, 56, (int)HitGroup_t.HITGROUP_HEAD);

                    Console.WriteLine($"[自瞄] {attacker.PlayerName} 的子弹算作爆头");
                }
                else if (_hitGroups.TryGetValue(hitGroupOffset, out var hitGroup))
                {
                    // 恢复原始值
                    Marshal.WriteInt32(hitGroupOffset, 56, hitGroup);
                }
            }
        }
    }

    /// <summary>
    /// 禁用技能时恢复所有命中组的原始值
    /// 完全复制自 jRandomSkills Aimbot.DisableSkill
    /// </summary>
    public static void DisableAllHitGroups()
    {
        foreach (var hit in _hitGroups)
        {
            Marshal.WriteInt32(hit.Key, 56, hit.Value);
        }
        _hitGroups.Clear();
        Console.WriteLine("[自瞄] 已恢复所有命中组的原始值");
    }
}
