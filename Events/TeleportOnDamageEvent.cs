using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace HelloWorldPlugin;

/// <summary>
/// 受伤传送事件 - 受到伤害时随机传送
/// </summary>
public class TeleportOnDamageEvent : EntertainmentEvent
{
    public override string Name => "TeleportOnDamage";
    public override string DisplayName => "受伤传送";
    public override string Description => "受到伤害时会随机传送！";

    private readonly Random _random = new();

    public override void OnApply()
    {
        Console.WriteLine("[受伤传送] 事件已激活");
    }

    /// <summary>
    /// 处理玩家受伤后事件（在主文件的 OnPlayerTakeDamagePost 中调用）
    /// </summary>
    public void HandlePlayerDamage(CCSPlayerPawn player, CTakeDamageInfo info, CTakeDamageResult result)
    {
        if (player == null || !player.IsValid)
            return;

        var controller = player.Controller.Value as CCSPlayerController;
        if (controller == null || !controller.IsValid || !controller.PawnIsAlive)
            return;

        // 获取所有存活的玩家位置
        var alivePlayers = Utilities.GetPlayers()
            .Where(p => p.IsValid && p.PawnIsAlive && p != controller)
            .ToList();

        if (alivePlayers.Count == 0)
            return;

        // 随机选择一个玩家的位置
        var targetPlayer = alivePlayers[_random.Next(alivePlayers.Count)];
        var targetPawn = targetPlayer.PlayerPawn.Get();
        if (targetPawn == null || !targetPawn.IsValid)
            return;

        // 传送到目标位置附近
        var targetPos = targetPawn.AbsOrigin;
        if (targetPos != null)
        {
            // 在目标位置附近随机偏移
            float offsetX = (_random.Next(-200, 200));
            float offsetY = (_random.Next(-200, 200));

            player.Teleport(
                new Vector(targetPos.X + offsetX, targetPos.Y + offsetY, targetPos.Z + 10),
                player.AbsRotation,
                player.AbsVelocity
            );

            controller.PrintToCenter("💫 你被传送了！");
            Console.WriteLine($"[受伤传送] {controller.PlayerName} 被传送到 {targetPlayer.PlayerName} 附近");
        }
    }
}
