// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills Aimbot skill

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

        Console.WriteLine($"[自瞄] {player.PlayerName} 失去了自瞄技能");
    }

    /// <summary>
    /// 处理伤害前事件 - 将命中部位修改为头部（适配 OnPlayerTakeDamagePre 监听器）
    /// </summary>
    public static void OnPlayerTakeDamagePre(CCSPlayerPawn victim, CTakeDamageInfo info, PlayerSkillManager skillManager)
    {
        if (info == null || info.Attacker == null || !info.Attacker.IsValid)
            return;

        var attackerEntity = info.Attacker.Value;
        if (attackerEntity == null || !attackerEntity.IsValid)
            return;

        // 检查是否是玩家实体
        if (attackerEntity is not CCSPlayerPawn attacker)
            return;

        // 检查是否是玩家
        var controller = attacker.Controller.Value;
        if (controller == null || !controller.IsValid || controller is not CCSPlayerController playerController)
            return;

        // 检查玩家是否有自瞄技能
        var skills = skillManager.GetPlayerSkills(playerController);
        if (skills.Count == 0)
            return;

        var autoAimSkill = skills.FirstOrDefault(s => s.Name == "AutoAim");
        if (autoAimSkill == null)
            return;

        if (!playerController.PawnIsAlive)
            return;

        try
        {
            // 完全复制 jRandomSkills 的内存操作
            nint hitGroupPointer = Marshal.ReadIntPtr(info.Handle, GameData.GetOffset("CTakeDamageInfo_HitGroup"));
            if (hitGroupPointer != nint.Zero)
            {
                nint hitGroupOffset = Marshal.ReadIntPtr(hitGroupPointer, 16);
                if (hitGroupOffset != nint.Zero)
                {
                    // 保存原始值
                    int oldValue = Marshal.ReadInt32(hitGroupOffset, 56);
                    _hitGroups.TryAdd(hitGroupOffset, oldValue);

                    // 设置为头部
                    Marshal.WriteInt32(hitGroupOffset, 56, (int)HitGroup_t.HITGROUP_HEAD);

                    Console.WriteLine($"[自瞄] {playerController.PlayerName} 的子弹算作爆头（原始命中部位：{oldValue}）");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[自瞄] 修改命中部位时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 恢复所有命中组的原始值（禁用技能时调用）
    /// </summary>
    public static void RestoreAllHitGroups()
    {
        foreach (var hit in _hitGroups)
        {
            Marshal.WriteInt32(hit.Key, 56, hit.Value);
        }
        _hitGroups.Clear();
        Console.WriteLine("[自瞄] 已恢复所有命中组的原始值");
    }
}
