using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace HelloWorldPlugin.Skills;

/// <summary>
/// 高跳技能 - 被动技能
/// 跳跃高度提升，但与低重力事件互斥
/// </summary>
public class HighJumpSkill : PlayerSkill
{
    public override string Name => "HighJump";
    public override string DisplayName => "🦘 超级跳跃";
    public override string Description => "跳跃高度大幅提升！";
    public override bool IsActive => false; // 被动技能
    public override float Cooldown => 0f;

    // 与低重力相关事件互斥（因为效果重叠）
    public override List<string> ExcludedEvents => new()
    {
        "LowGravity",
        "LowGravityPlusPlus",
        "JumpPlusPlus"
    };

    private readonly Dictionary<int, float> _originalGravity = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 保存原始重力
        _originalGravity[player.Slot] = pawn.GravityScale;

        // 降低重力以实现高跳效果
        pawn.GravityScale = 0.5f;

        Console.WriteLine($"[超级跳跃] {player.PlayerName} 获得了超级跳跃能力");
        player.PrintToCenter("🦘 超级跳跃！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 恢复原始重力
        if (_originalGravity.ContainsKey(player.Slot))
        {
            pawn.GravityScale = _originalGravity[player.Slot];
            _originalGravity.Remove(player.Slot);
        }

        Console.WriteLine($"[超级跳跃] {player.PlayerName} 失去了超级跳跃能力");
    }
}
