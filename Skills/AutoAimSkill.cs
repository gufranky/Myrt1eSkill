// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;

namespace MyrtleSkill.Skills;

/// <summary>
/// 自瞄技能 - 被动技能
/// 每一颗击中的子弹都算作爆头
/// </summary>
public class AutoAimSkill : PlayerSkill
{
    public override string Name => "AutoAim";
    public override string DisplayName => "🎯 自瞄";
    public override string Description => "每一颗击中的子弹都算作爆头！";
    public override bool IsActive => false; // 被动技能

    // 跟踪拥有该技能的玩家
    private readonly HashSet<ulong> _activePlayers = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        _activePlayers.Add(player.SteamID);

        Console.WriteLine($"[自瞄] {player.PlayerName} 获得了自瞄技能");

        player.PrintToChat("🎯 你获得了自瞄技能！");
        player.PrintToChat("💡 每一颗击中的子弹都算作爆头！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        _activePlayers.Remove(player.SteamID);

        Console.WriteLine($"[自瞄] {player.PlayerName} 失去了自瞄技能");
    }

    /// <summary>
    /// 处理伤害前事件 - 将命中部位修改为头部
    /// </summary>
    public static void OnPlayerTakeDamagePre(CCSPlayerPawn victim, CTakeDamageInfo info, PlayerSkillManager skillManager)
    {
        if (info == null)
            return;

        // 获取攻击者
        var attackerHandle = info.Attacker;
        if (attackerHandle == null || !attackerHandle.IsValid)
            return;

        var attackerEntity = attackerHandle.Value;
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

        // 将命中部位修改为头部
        Schema.SetSchemaValue<int>(info.Handle, "CTakeDamageInfo", "m_nHitgroup", (int)HitGroup_t.HITGROUP_HEAD);

        Console.WriteLine($"[自瞄] {playerController.PlayerName} 的子弹算作爆头");
    }
}
