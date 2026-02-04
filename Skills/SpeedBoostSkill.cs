using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MyrtleSkill.Skills;

/// <summary>
/// 速度提升技能 - 被动技能示例
/// 玩家移动速度提升
/// </summary>
public class SpeedBoostSkill : PlayerSkill
{
    public override string Name => "SpeedBoost";
    public override string DisplayName => "⚡ 速度提升";
    public override string Description => "移动速度提升50%！";
    public override bool IsActive => false; // 被动技能
    public override float Cooldown => 0f; // 被动技能无冷却

    // 与其他移动技能互斥
    public override List<string> ExcludedSkills => new() { "HeavyArmor", "Sprint" };

    private readonly Dictionary<int, float> _originalSpeed = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 保存原始速度
        _originalSpeed[player.Slot] = pawn.VelocityModifier;

        // 应用速度提升
        pawn.VelocityModifier *= 1.5f;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");

        Console.WriteLine($"[速度提升] {player.PlayerName} 的移动速度已提升");
        player.PrintToCenter("⚡ 速度提升！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[速度提升] {player.PlayerName} 的移动速度即将恢复");

        // 使用 NextFrame 确保即使在回合结束时也能正确恢复
        Server.NextFrame(() =>
        {
            if (player == null || !player.IsValid)
                return;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                return;

            // 恢复原始速度
            if (_originalSpeed.ContainsKey(player.Slot))
            {
                float originalSpeed = _originalSpeed[player.Slot];
                pawn.VelocityModifier = originalSpeed;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                _originalSpeed.Remove(player.Slot);

                Console.WriteLine($"[速度提升] {player.PlayerName} 的移动速度已恢复为 {originalSpeed}");
            }
        });
    }
}
