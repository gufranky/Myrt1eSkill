using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace HelloWorldPlugin;

/// <summary>
/// 小而致命事件 - 玩家缩小但伤害翻倍
/// </summary>
public class SmallAndDeadlyEvent : EntertainmentEvent
{
    public override string Name => "SmallAndDeadly";
    public override string DisplayName => "小而致命";
    public override string Description => "玩家变小但伤害翻倍！";

    private const float SmallScale = 0.7f;
    private const float DamageMultiplier = 2.0f;

    public override void OnApply()
    {
        Console.WriteLine("[小而致命] 设置玩家尺寸为 0.7，伤害翻倍");

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive) continue;

            var pawn = player.PlayerPawn.Get();
            if (pawn == null || !pawn.IsValid) continue;

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

            Console.WriteLine($"[小而致命] {player.PlayerName} 已变小且致命");
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

    /// <summary>
    /// 处理伤害（需要在主文件的 OnPlayerTakeDamagePre 中调用）
    /// </summary>
    public void HandleDamage(CTakeDamageInfo info)
    {
        // 伤害翻倍
        info.Damage *= DamageMultiplier;
    }
}
