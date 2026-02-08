// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on ArmoredSkill implementation

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MyrtleSkill.Skills;

/// <summary>
/// 假肢技能 - 被动技能
/// 手臂和腿部防弹 - 受到伤害时 20% 伤害（模拟四肢防弹）
/// </summary>
public class ProstheticSkill : PlayerSkill
{
    public override string Name => "Prosthetic";
    public override string DisplayName => "🦾 假肢";
    public override string Description => "手臂和腿部防弹！受到的伤害降低80%！";
    public override bool IsActive => false; // 被动技能
    public override float Cooldown => 0f; // 被动技能无冷却

    // 伤害减免倍率（20% 伤害，即减免 80%）
    private const float DAMAGE_MULTIPLIER = 0.2f;

    // 与其他生存技能互斥
    public override List<string> ExcludedSkills => new() { "Armored", "Juggernaut", "SecondChance", "Meito", "BigStomach", "HighRiskHighReward" };

    // 存储每个玩家的伤害减免状态
    private readonly HashSet<ulong> _activePlayers = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        _activePlayers.Add(player.SteamID);

        Console.WriteLine($"[假肢] {player.PlayerName} 获得了假肢技能");

        player.PrintToCenter("🦾 假肢已装备！");
        player.PrintToChat("🦾 你获得了假肢技能！");
        player.PrintToChat("💡 手臂和腿部防弹！受到的伤害降低80%！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        _activePlayers.Remove(player.SteamID);

        Console.WriteLine($"[假肢] {player.PlayerName} 失去了假肢技能");
    }

    /// <summary>
    /// 处理伤害前事件（在主文件的 OnPlayerTakeDamagePre 中调用）
    /// 返回伤害倍率
    /// </summary>
    public float? HandleDamage(CCSPlayerPawn player, CTakeDamageInfo info)
    {
        if (player == null || !player.IsValid)
            return null;

        var controller = player.Controller.Value;
        if (controller == null || !controller.IsValid || controller is not CCSPlayerController playerController)
            return null;

        // 检查玩家是否有假肢技能
        if (!_activePlayers.Contains(playerController.SteamID))
            return null;

        if (!playerController.PawnIsAlive)
            return null;

        // 应用伤害减免
        Console.WriteLine($"[假肢] {playerController.PlayerName} 受到伤害，应用倍率：{DAMAGE_MULTIPLIER}");

        return DAMAGE_MULTIPLIER;
    }
}
