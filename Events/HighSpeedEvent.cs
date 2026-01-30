using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace HelloWorldPlugin;

/// <summary>
/// 高速移动事件 - 玩家移速翻倍
/// </summary>
public class HighSpeedEvent : EntertainmentEvent
{
    public override string Name => "HighSpeed";
    public override string DisplayName => "高速移动";
    public override string Description => "所有玩家移速翻倍！";

    public override void OnApply()
    {
        Console.WriteLine("[高速移动] 设置移速为 2.0");

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            var pawn = player.PlayerPawn.Get();
            if (pawn == null || !pawn.IsValid) continue;

            pawn.VelocityModifier = 2.0f;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[高速移动] 恢复移速为 1.0");

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            var pawn = player.PlayerPawn.Get();
            if (pawn == null || !pawn.IsValid) continue;

            pawn.VelocityModifier = 1.0f;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
        }
    }
}
