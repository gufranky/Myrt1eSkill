using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using MyrtleSkill.Features;
using MyrtleSkill.Utils;
using System.Collections.Generic;

namespace MyrtleSkill;

/// <summary>
/// 受伤传送事件 - 受到伤害时随机传送到玩家记录过的位置
/// </summary>
public class TeleportOnDamageEvent : EntertainmentEvent
{
    public override string Name => "TeleportOnDamage";
    public override string DisplayName => "受伤传送";
    public override string Description => "受到伤害时会随机传送到场上玩家之前经过的位置！";

    public override void OnApply()
    {
        Console.WriteLine("[受伤传送] 事件已激活");
    }

    /// <summary>
    /// 处理玩家受伤事件（在主文件的 OnPlayerHurt 中调用）
    /// </summary>
    public void HandlePlayerHurt(EventPlayerHurt @event)
    {
        var controller = @event.Userid;
        if (controller == null || !controller.IsValid)
            return;

        if (!controller.PawnIsAlive)
            return;

        var pawn = controller.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        Console.WriteLine($"[受伤传送-DEBUG] {controller.PlayerName} 受到伤害，检查是否传送");

        // 获取插件实例
        var plugin = MyrtleSkill.Instance;
        if (plugin == null || plugin.PositionRecorder == null)
        {
            Console.WriteLine($"[受伤传送] 警告：位置记录器未启动！");
            return;
        }

        // 收集所有玩家的位置历史
        var allPositions = new List<(PositionEntry Entry, string PlayerName)>();

        foreach (var p in Utilities.GetPlayers())
        {
            if (!p.IsValid)
                continue;

            var history = plugin.PositionRecorder.GetPlayerHistory(p);
            if (history != null && history.Positions.Count > 0)
            {
                foreach (var pos in history.Positions)
                {
                    allPositions.Add((pos, history.PlayerName));
                }
            }
        }

        if (allPositions.Count == 0)
        {
            Console.WriteLine($"[受伤传送] 警告：没有找到任何位置记录！");
            return;
        }

        // 从所有位置中随机选择一个（带碰撞检测重试）
        var random = new Random();
        var teleportPosition = default(CounterStrikeSharp.API.Modules.Utils.Vector);
        var ownerName = "";
        var selectedPosition = default(Features.PositionEntry);
        bool foundSafePosition = false;
        int maxAttempts = Math.Min(10, allPositions.Count);

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // 随机选择一个位置
            int randomIndex = random.Next(allPositions.Count);
            (selectedPosition, ownerName) = allPositions[randomIndex];

            // 创建位置向量
            teleportPosition = new CounterStrikeSharp.API.Modules.Utils.Vector(
                selectedPosition.Position.X,
                selectedPosition.Position.Y,
                selectedPosition.Position.Z
            );

            // 检查位置是否安全
            if (SkillUtils.IsPositionSafe(teleportPosition, controller))
            {
                foundSafePosition = true;
                break;
            }

            Console.WriteLine($"[受伤传送] 尝试 {attempt + 1}/{maxAttempts}: 位置不安全，重新选择");
        }

        if (!foundSafePosition)
        {
            Console.WriteLine($"[受伤传送] {controller.PlayerName} 无法找到安全传送位置");
            controller.PrintToChat("⚠️ 无法找到安全传送位置！");
            return;
        }

        // 执行传送（使用之前定义的 pawn）
        pawn.Teleport(teleportPosition, pawn.AbsRotation, new CounterStrikeSharp.API.Modules.Utils.Vector(0, 0, 0));

        // 计算时间差
        float timeAgo = Server.CurrentTime - selectedPosition.Timestamp;

        // 显示提示
        controller.PrintToCenter($"💫 你被传送了！");
        controller.PrintToChat($"📍 位置来自: {ownerName} | {timeAgo:F0}秒前");

        Console.WriteLine($"[受伤传送] {controller.PlayerName} 被传送到 {ownerName} {timeAgo:F0} 秒前的位置");
    }
}
