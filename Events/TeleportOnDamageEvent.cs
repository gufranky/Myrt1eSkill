using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using HelloWorldPlugin.ThirdParty;

namespace HelloWorldPlugin;

/// <summary>
/// 受伤传送事件 - 受到伤害时随机传送到地图上的可行走位置
/// </summary>
public class TeleportOnDamageEvent : EntertainmentEvent
{
    public override string Name => "TeleportOnDamage";
    public override string DisplayName => "受伤传送";
    public override string Description => "受到伤害时会随机传送到地图上的其他位置！";

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

        // 使用 NavMesh 获取随机可行走位置
        Vector? randomPosition = NavMesh.GetRandomPosition();
        if (randomPosition == null)
        {
            Console.WriteLine($"[受伤传送] 警告：无法为 {controller.PlayerName} 找到随机位置！");
            return;
        }

        // 传送玩家并处理碰撞
        TeleportPlayer(controller, randomPosition);

        controller.PrintToCenter("💫 你被传送了！");
        Console.WriteLine($"[受伤传送] {controller.PlayerName} 被传送到随机位置");
    }

    /// <summary>
    /// 传送玩家到指定位置，并处理碰撞组防止卡墙
    /// </summary>
    private void TeleportPlayer(CCSPlayerController player, Vector position)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 执行传送
        pawn.Teleport(position, pawn.AbsRotation, new Vector(0, 0, 0));

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
