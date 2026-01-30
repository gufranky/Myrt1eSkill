using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace HelloWorldPlugin;

/// <summary>
/// 重装战士事件 - 高护甲低速度
/// </summary>
public class JuggernautEvent : EntertainmentEvent
{
    public override string Name => "Juggernaut";
    public override string DisplayName => "重装战士";
    public override string Description => "所有玩家获得200护甲，但移速降低！";

    public override void OnApply()
    {
        Console.WriteLine("[重装战士] 设置护甲200，生命500，移速0.7");

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            var pawn = player.PlayerPawn.Get();
            if (pawn == null || !pawn.IsValid) continue;

            // 设置生命值
            pawn.Health = 500;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            // 设置护甲值
            pawn.ArmorValue = 200;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");

            // 降低移速
            pawn.VelocityModifier = 0.7f;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");

            Console.WriteLine($"[重装战士] {player.PlayerName} 已成为重装战士");
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[重装战士] 恢复移速为 1.0");

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            var pawn = player.PlayerPawn.Get();
            if (pawn == null || !pawn.IsValid) continue;

            // 恢复移速
            pawn.VelocityModifier = 1.0f;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
        }
    }
}
