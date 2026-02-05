using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MyrtleSkill.Skills;

/// <summary>
/// 重甲战士技能 - 被动技能
/// 获得200护甲，60%伤害减免，移速降低到80%
/// </summary>
public class HeavyArmorSkill : PlayerSkill
{
    public override string Name => "HeavyArmor";
    public override string DisplayName => "🛡️ 重甲战士";
    public override string Description => "获得200护甲！60%伤害减免！移速80%！";
    public override bool IsActive => false; // 被动技能

    // 与其他移动技能互斥
    public override List<string> ExcludedSkills => new() { "SpeedBoost", "Sprint" };

    // 护甲值
    private const int ARMOR_VALUE = 200;

    // 伤害减免（60%）
    private const float DAMAGE_REDUCTION = 0.6f;

    // 移速倍数（80%）
    private const float SPEED_MULTIPLIER = 0.8f;

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[重甲战士] {player.PlayerName} 获得了重甲战士技能");

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 设置护甲
        pawn.ArmorValue = ARMOR_VALUE;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");

        // 设置移速
        pawn.VelocityModifier = SPEED_MULTIPLIER;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");

        player.PrintToChat("🛡️ 你获得了重甲战士技能！");
        player.PrintToChat($"🛡️ 护甲值: {ARMOR_VALUE}！");
        player.PrintToChat($"💥 伤害减免: {DAMAGE_REDUCTION * 100}%！");
        player.PrintToChat($"🏃 移速: {SPEED_MULTIPLIER * 100}%！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[重甲战士] {player.PlayerName} 失去了重甲战士技能");

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 恢复移速
        pawn.VelocityModifier = 1.0f;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
    }

    /// <summary>
    /// 处理玩家受到伤害
    /// </summary>
    public float? HandleDamage(CCSPlayerPawn player, CTakeDamageInfo info)
    {
        var controller = player.Controller.Value;
        if (controller == null || !controller.IsValid)
            return null;

        // 检查玩家是否有重甲战士技能
        if (controller is not CCSPlayerController csController)
            return null;

        var skills = Plugin?.SkillManager.GetPlayerSkills(csController);
        if (skills == null || skills.Count == 0)
            return null;

        var skill = skills.FirstOrDefault(s => s.Name == "HeavyArmor");
        if (skill == null)
            return null;

        // 应用伤害减免
        float multiplier = 1.0f - DAMAGE_REDUCTION; // 0.4倍伤害

        Console.WriteLine($"[重甲战士] {csController.PlayerName} 受到伤害，应用减免: {multiplier * 100}%");

        return multiplier;
    }
}
