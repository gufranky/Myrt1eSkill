using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace HelloWorldPlugin;

/// <summary>
/// 低重力事件 - 玩家跳跃更高
/// </summary>
public class LowGravityEvent : EntertainmentEvent
{
    public override string Name => "LowGravity";
    public override string DisplayName => "低重力";
    public override string Description => "玩家可以跳得更高！";

    public override void OnApply()
    {
        Console.WriteLine("[低重力] 设置重力为 0.5");

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            var pawn = player.PlayerPawn.Get();
            if (pawn == null || !pawn.IsValid) continue;

            pawn.GravityScale = 0.5f;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flGravityScale");
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[低重力] 恢复重力为 1.0");

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            var pawn = player.PlayerPawn.Get();
            if (pawn == null || !pawn.IsValid) continue;

            pawn.GravityScale = 1.0f;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flGravityScale");
        }
    }
}
