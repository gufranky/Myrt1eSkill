using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MyrtleSkill;

/// <summary>
/// 低重力事件 - 玩家跳跃更高
/// </summary>
public class LowGravityEvent : EntertainmentEvent
{
    public override string Name => "LowGravity";
    public override string DisplayName => "低重力";
    public override string Description => "玩家可以跳得更高！";

    private const float GravityMultiplier = 0.5f;
    private readonly Dictionary<int, float> _originalGravity = new();

    public override void OnApply()
    {
        Console.WriteLine("[低重力] 设置重力为当前值的 " + GravityMultiplier + " 倍");

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            // 保存原始重力值
            _originalGravity[player.Slot] = pawn.GravityScale;

            // 应用重力倍数
            pawn.GravityScale *= GravityMultiplier;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flGravityScale");
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[低重力] 恢复重力为原始值");

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            // 恢复原始重力值
            if (_originalGravity.ContainsKey(player.Slot))
            {
                pawn.GravityScale = _originalGravity[player.Slot];
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flGravityScale");
            }
        }

        _originalGravity.Clear();
    }
}
