using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace HelloWorldPlugin;

/// <summary>
/// 迷你尺寸事件 - 玩家模型缩小
/// </summary>
public class MiniSizeEvent : EntertainmentEvent
{
    public override string Name => "MiniSize";
    public override string DisplayName => "迷你尺寸";
    public override string Description => "所有玩家变成迷你尺寸！";

    private const float MiniScale = 0.5f;

    public override void OnApply()
    {
        Console.WriteLine("[迷你尺寸] 设置玩家尺寸为 0.5");

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive) continue;

            var pawn = player.PlayerPawn.Get();
            if (pawn == null || !pawn.IsValid) continue;

            // 设置玩家模型缩放
            var sceneNode = pawn.CBodyComponent?.SceneNode;
            if (sceneNode != null)
            {
                sceneNode.Scale = MiniScale;
                pawn.AcceptInput("SetScale", null, null, MiniScale.ToString());
                Server.NextFrame(() =>
                {
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");
                });
            }

            Console.WriteLine($"[迷你尺寸] {player.PlayerName} 已缩小");
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[迷你尺寸] 恢复玩家尺寸为 1.0");

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            var pawn = player.PlayerPawn.Get();
            if (pawn == null || !pawn.IsValid) continue;

            // 恢复玩家模型缩放
            var sceneNode = pawn.CBodyComponent?.SceneNode;
            if (sceneNode != null)
            {
                sceneNode.Scale = 1.0f;
                pawn.AcceptInput("SetScale", null, null, "1.0");
                Server.NextFrame(() =>
                {
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");
                });
            }

            Console.WriteLine($"[迷你尺寸] {player.PlayerName} 已恢复");
        }
    }
}
