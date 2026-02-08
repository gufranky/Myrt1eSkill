using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MyrtleSkill.Skills;

/// <summary>
/// 大胃袋技能 - 被动技能
/// 获得技能时随机增加100~250点生命值（可超过100）
/// </summary>
public class BigStomachSkill : PlayerSkill
{
    public override string Name => "BigStomach";
    public override string DisplayName => "🍖 大胃袋";
    public override string Description => "获得技能时随机增加100~250点生命值！可超过血量上限！";
    public override bool IsActive => false; // 被动技能
    public override float Cooldown => 0f; // 被动技能无冷却

    // 血量增加范围
    private const int MIN_HEALTH_BONUS = 100;
    private const int MAX_HEALTH_BONUS = 250;

    // 与其他生存技能互斥
    public override List<string> ExcludedSkills => new() { "Juggernaut" };

    private readonly Random _random = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 计算随机血量增加
        int healthBonus = _random.Next(MIN_HEALTH_BONUS, MAX_HEALTH_BONUS + 1);

        // 获取当前血量
        int currentHealth = pawn.Health;

        // 增加血量（允许超过100）
        int newHealth = currentHealth + healthBonus;

        // 设置新血量
        pawn.Health = newHealth;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        Console.WriteLine($"[大胃袋] {player.PlayerName} 的血量增加了 {healthBonus} 点：{currentHealth} → {newHealth}");

        // 显示提示
        player.PrintToCenter($"🍖 +{healthBonus} HP！");
        player.PrintToChat($"🍖 大胃袋！血量增加了 {healthBonus} 点！");
        player.PrintToChat($"💡 当前血量：{newHealth}");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 恢复血量到100
        pawn.Health = 100;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        Console.WriteLine($"[大胃袋] {player.PlayerName} 失去了大胃袋技能，血量已恢复到100");
    }
}
