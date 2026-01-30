using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MyrtleSkill;

/// <summary>
/// 小而致命事件 - 玩家缩小至0.4倍，只有1滴血
/// </summary>
public class SmallAndDeadlyEvent : EntertainmentEvent
{
    public override string Name => "SmallAndDeadly";
    public override string DisplayName => "小而致命";
    public override string Description => "玩家体型缩小至0.4倍，只有1滴血！一击必杀！";

    private const float SmallScale = 0.4f;

    public override void OnApply()
    {
        Console.WriteLine("[小而致命] 设置玩家尺寸为 0.4，血量为 1 HP");

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive) continue;

            var pawn = player.PlayerPawn.Get();
            if (pawn == null || !pawn.IsValid) continue;

            // 设置玩家血量为 1 HP
            pawn.Health = 1;
            pawn.MaxHealth = 1;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");

            // 设置玩家模型缩放
            var sceneNode = pawn.CBodyComponent?.SceneNode;
            if (sceneNode != null)
            {
                sceneNode.Scale = SmallScale;
                pawn.AcceptInput("SetScale", null, null, SmallScale.ToString());
                Server.NextFrame(() =>
                {
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");
                });
            }

            Console.WriteLine($"[小而致命] {player.PlayerName} 已变小且只有1滴血");
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[小而致命] 恢复玩家尺寸为 1.0");

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

            Console.WriteLine($"[小而致命] {player.PlayerName} 已恢复");
        }
    }
}
