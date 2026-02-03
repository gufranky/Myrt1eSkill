using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace MyrtleSkill.Skills;

/// <summary>
/// 宇航员技能 - 被动技能
/// 获得更低的重力，跳跃更高
/// </summary>
public class HighJumpSkill : PlayerSkill
{
    public override string Name => "HighJump";
    public override string DisplayName => "👨‍🚀 宇航员";
    public override string Description => "重力降低至70%，跳跃更高！";
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
        _originalGravity[player.Slot] = pawn.ActualGravityScale;

        // 降低重力以实现高跳效果（70%重力）
        // 参考 jRandomSkills Astronaut 技能，使用 ActualGravityScale
        pawn.ActualGravityScale = 0.7f;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_flActualGravityScale");

        Console.WriteLine($"[宇航员] {player.PlayerName} 获得了宇航员能力 (重力: 0.7f)");
        player.PrintToChat("👨‍🚀 宇航员模式！重力降低至70%！");
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
            // 参考 jRandomSkills Astronaut 技能，使用 ActualGravityScale
            pawn.ActualGravityScale = _originalGravity[player.Slot];
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_flActualGravityScale");
            _originalGravity.Remove(player.Slot);
        }

        Console.WriteLine($"[宇航员] {player.PlayerName} 失去了宇航员能力");
    }
}
