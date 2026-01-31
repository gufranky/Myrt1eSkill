using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using MyrtleSkill.ThirdParty;

namespace MyrtleSkill.Skills;

/// <summary>
/// 传送技能 - 主动技能示例
/// 玩家可以传送到随机位置
/// </summary>
public class TeleportSkill : PlayerSkill
{
    public override string Name => "Teleport";
    public override string DisplayName => "🌀 瞬间移动";
    public override string Description => "传送到地图上的随机位置！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 15.0f; // 15秒冷却

    public override void OnApply(CCSPlayerController player)
    {
        // 主动技能在获得时不需要做什么，等待玩家激活
        Console.WriteLine($"[瞬间移动] {player.PlayerName} 获得了瞬间移动技能");
        player.PrintToChat("🌀 你获得了瞬间移动技能！输入 !useskill 或按键激活！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        // 移除技能时的清理工作
        Console.WriteLine($"[瞬间移动] {player.PlayerName} 失去了瞬间移动技能");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        Console.WriteLine($"[瞬间移动] {player.PlayerName} 尝试使用传送技能");

        // 方法1: 尝试使用 NavMesh 获取随机位置
        Vector? randomPosition = NavMesh.GetRandomPosition(maxAttempts: 50);

        if (randomPosition != null)
        {
            Console.WriteLine($"[瞬间移动] 使用 NavMesh 找到位置: {randomPosition.X}, {randomPosition.Y}, {randomPosition.Z}");
        }
        else
        {
            Console.WriteLine($"[瞬间移动] NavMesh 未找到位置，使用备用方案");

            // 方法2: 使用简单的坐标偏移作为备用
            randomPosition = GetRandomPositionByOffset(pawn.AbsOrigin);

            if (randomPosition == null)
            {
                player.PrintToChat("💫 无法找到传送位置！");
                return;
            }

            Console.WriteLine($"[瞬间移动] 使用偏移方案找到位置: {randomPosition.X}, {randomPosition.Y}, {randomPosition.Z}");
        }

        // 传送玩家
        TeleportPlayer(player, pawn, randomPosition);

        // 显示效果
        player.PrintToCenter("🌀 瞬间移动！");
        player.PrintToChat($"🌀 已传送到随机位置！");

        Console.WriteLine($"[瞬间移动] {player.PlayerName} 成功使用传送技能");
    }

    /// <summary>
    /// 传送玩家到指定位置
    /// </summary>
    private void TeleportPlayer(CCSPlayerController player, CCSPlayerPawn pawn, Vector position)
    {
        // 传送玩家
        pawn.Teleport(position, pawn.AbsRotation, new Vector(0, 0, 0));

        // 临时设置为穿透模式，防止卡在墙里
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

    /// <summary>
    /// 备用方案：通过坐标偏移获取随机位置
    /// </summary>
    private Vector? GetRandomPositionByOffset(Vector? currentPosition)
    {
        if (currentPosition == null)
            return null;

        var random = new Random();

        // 在当前位置周围随机偏移 200-800 单位
        float offsetX = (random.NextSingle() * 2 - 1) * 600; // -600 到 +600
        float offsetY = (random.NextSingle() * 2 - 1) * 600;
        float offsetZ = 0; // 保持相同高度，或者可以稍微上下浮动

        Vector newPosition = new Vector(
            currentPosition.X + offsetX,
            currentPosition.Y + offsetY,
            currentPosition.Z + offsetZ
        );

        // 确保不会传送到地图外太远的地方（简单的边界检查）
        if (newPosition.X < -4000 || newPosition.X > 4000 ||
            newPosition.Y < -4000 || newPosition.Y > 4000 ||
            newPosition.Z < -500 || newPosition.Z > 2000)
        {
            Console.WriteLine($"[瞬间移动] 偏移位置超出合理范围，尝试中心位置");
            // 返回地图大概中心位置
            return new Vector(0, 0, 0);
        }

        return newPosition;
    }
}
