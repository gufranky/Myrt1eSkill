using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using MyrtleSkill.Features;
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
    /// 处理玩家受伤后事件（在主文件的 OnPlayerTakeDamagePost 中调用）
    /// </summary>
    public void HandlePlayerDamage(CCSPlayerPawn player, CTakeDamageInfo info, CTakeDamageResult result)
    {
        if (player == null || !player.IsValid)
            return;

        var controller = player.Controller.Value as CCSPlayerController;
        if (controller == null || !controller.IsValid || !controller.PawnIsAlive)
            return;

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

        // 从所有位置中随机选择一个
        var random = new Random();
        int randomIndex = random.Next(allPositions.Count);
        var (selectedPosition, ownerName) = allPositions[randomIndex];

        // 创建位置向量
        var teleportPosition = new CounterStrikeSharp.API.Modules.Utils.Vector(
            selectedPosition.Position.X,
            selectedPosition.Position.Y,
            selectedPosition.Position.Z
        );

        // 传送玩家
        TeleportPlayer(controller, teleportPosition);

        // 计算时间差
        float timeAgo = Server.CurrentTime - selectedPosition.Timestamp;

        // 显示提示
        controller.PrintToCenter($"💫 你被传送了！");
        controller.PrintToChat($"📍 位置来自: {ownerName} | {timeAgo:F0}秒前");

        Console.WriteLine($"[受伤传送] {controller.PlayerName} 被传送到 {ownerName} {timeAgo:F0} 秒前的位置");
    }

    /// <summary>
    /// 传送玩家到指定位置，并处理碰撞组防止卡墙
    /// </summary>
    private void TeleportPlayer(CCSPlayerController player, CounterStrikeSharp.API.Modules.Utils.Vector position)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 执行传送
        pawn.Teleport(position, pawn.AbsRotation, new CounterStrikeSharp.API.Modules.Utils.Vector(0, 0, 0));

        // 临时设置为穿透模式，防止卡在墙里或其他玩家身上
        pawn.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
        pawn.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
        Utilities.SetStateChanged(pawn, "CCollisionProperty", "m_CollisionGroup");
        Utilities.SetStateChanged(pawn, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");

        // 下一帧恢复正常碰撞
        Server.NextFrame(() =>
        {
            if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                return;

            pawn.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
            pawn.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
            Utilities.SetStateChanged(pawn, "CCollisionProperty", "m_CollisionGroup");
            Utilities.SetStateChanged(pawn, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
        });
    }
}
