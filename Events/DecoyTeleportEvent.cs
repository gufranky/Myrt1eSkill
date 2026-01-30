using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using HelloWorldPlugin.ThirdParty;

namespace HelloWorldPlugin;

/// <summary>
/// 诱饵传送事件 - 玩家会传送到诱饵弹的落点
/// </summary>
public class DecoyTeleportEvent : EntertainmentEvent
{
    public override string Name => "DecoyTeleport";
    public override string DisplayName => "🎯 TP弹模式";
    public override string Description => "投掷诱饵弹后会传送到落点！每回合自动获得诱饵弹。";

    public override void OnApply()
    {
        Console.WriteLine("[TP弹模式] 事件已激活");

        // 给所有玩家诱饵弹
        GiveDecoyToAllPlayers();

        // 注册诱饵弹开始事件
        if (Plugin != null)
        {
            Plugin.RegisterEventHandler<EventDecoyStarted>(OnDecoyStarted, HookMode.Post);
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[TP弹模式] 事件已恢复");

        // 移除事件监听
        if (Plugin != null)
        {
            Plugin.DeregisterEventHandler<EventDecoyStarted>(OnDecoyStarted, HookMode.Post);
        }
    }

    /// <summary>
    /// 给所有玩家诱饵弹
    /// </summary>
    private void GiveDecoyToAllPlayers()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive) continue;

            player.GiveNamedItem("weapon_decoy");
            Console.WriteLine($"[TP弹模式] 已给予 {player.PlayerName} 诱饵弹");
        }
    }

    /// <summary>
    /// 处理诱饵弹开始触发事件
    /// </summary>
    private HookResult OnDecoyStarted(EventDecoyStarted @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        // 获取诱饵弹的位置
        var position = new CounterStrikeSharp.API.Modules.Utils.Vector(@event.X, @event.Y, @event.Z);

        // 传送玩家到诱饵弹位置
        TeleportPlayer(player, position);

        player.PrintToCenter("🎯 传送到诱饵弹位置！");
        Console.WriteLine($"[TP弹模式] {player.PlayerName} 传送到诱饵弹位置 ({@event.X}, {@event.Y}, {@event.Z})");

        // 给玩家新的诱饵弹
        Server.NextFrame(() =>
        {
            if (player.IsValid && player.PawnIsAlive)
            {
                player.GiveNamedItem("weapon_decoy");
            }
        });

        return HookResult.Continue;
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
