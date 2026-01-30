using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace HelloWorldPlugin;

/// <summary>
/// 超低重力事件 - 重力0.25 + 空中无扩散
/// </summary>
public class LowGravityPlusPlusEvent : EntertainmentEvent
{
    public override string Name => "LowGravityPlusPlus";
    public override string DisplayName => "超低重力";
    public override string Description => "重力大幅降低，空中射击无扩散！";

    public override void OnApply()
    {
        Console.WriteLine("[超低重力] 设置重力为 0.25，启用无扩散");

        // 设置重力
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            var pawn = player.PlayerPawn.Get();
            if (pawn == null || !pawn.IsValid) continue;

            pawn.GravityScale = 0.25f;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flGravityScale");
        }

        // 启用无扩散
        Server.ExecuteCommand("weapon_accuracy_nospread true");
    }

    public override void OnRevert()
    {
        Console.WriteLine("[超低重力] 恢复重力为 1.0，禁用无扩散");

        // 恢复重力
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            var pawn = player.PlayerPawn.Get();
            if (pawn == null || !pawn.IsValid) continue;

            pawn.GravityScale = 1.0f;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flGravityScale");
        }

        // 禁用无扩散
        Server.ExecuteCommand("weapon_accuracy_nospread false");
    }
}
