// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills by Juzlus (Armored skill)

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MyrtleSkill.Skills;

/// <summary>
/// 装甲技能 - 被动技能
/// 获得一个随机的伤害减免倍率（0.55 - 0.8x），即减免20%-45%的伤害
/// 完全参照 jRandomSkills Armored 实现
/// </summary>
public class ArmoredSkill : PlayerSkill
{
    public override string Name => "Armored";
    public override string DisplayName => "🛡️ 装甲";
    public override string Description => "获得一个随机的伤害减免倍率（0.55 - 0.8x）！";
    public override bool IsActive => false; // 被动技能
    public override float Cooldown => 0f; // 被动技能无冷却

    // 伤害减免倍率范围
    private const float MIN_MULTIPLIER = 0.55f;
    private const float MAX_MULTIPLIER = 0.8f;

    // 存储每个玩家的随机倍率
    private readonly Dictionary<ulong, float> _playerMultipliers = new();
    private readonly Random _random = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 生成随机倍率（参照 jRandomSkills 实现）
        float newScale = (float)_random.NextDouble() * (MAX_MULTIPLIER - MIN_MULTIPLIER) + MIN_MULTIPLIER;
        newScale = (float)Math.Round(newScale, 2);

        // 保存倍率
        _playerMultipliers[player.SteamID] = newScale;

        Console.WriteLine($"[装甲] {player.PlayerName} 获得了装甲技能，伤害倍率：{newScale}");

        // 计算减免百分比
        int reductionPercent = (int)((1 - newScale) * 100);

        // 显示提示
        player.PrintToCenter($"🛡️ 伤害减免 {reductionPercent}%！");
        player.PrintToChat($"🛡️ 你获得了装甲技能！");
        player.PrintToChat($"💡 伤害倍率：{newScale}x（减免{reductionPercent}%）");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 移除倍率记录
        _playerMultipliers.Remove(player.SteamID);

        Console.WriteLine($"[装甲] {player.PlayerName} 失去了装甲技能");
    }

    /// <summary>
    /// 处理伤害事件（在主文件的 OnPlayerTakeDamagePre 中调用）
    /// 参照 jRandomSkills Armored.OnTakeDamage 实现
    /// </summary>
    public float? HandleDamage(CCSPlayerPawn player, CTakeDamageInfo info)
    {
        if (player == null || !player.IsValid)
            return null;

        var controller = player.Controller.Value;
        if (controller == null || !controller.IsValid || controller is not CCSPlayerController playerController)
            return null;

        // 检查玩家是否有装甲技能
        if (!_playerMultipliers.TryGetValue(playerController.SteamID, out var multiplier))
            return null;

        if (!playerController.PawnIsAlive)
            return null;

        // 应用伤害减免（参照 jRandomSkills 实现）
        // param2.Damage *= skillChance ?? 1f;
        Console.WriteLine($"[装甲] {playerController.PlayerName} 受到伤害，应用倍率：{multiplier}");

        return multiplier;
    }
}
