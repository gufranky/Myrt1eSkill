using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 传送技能 - 传送到玩家历史位置
/// 从所有玩家的历史位置中随机选择一个进行传送
/// </summary>
public class TeleportSkill : PlayerSkill
{
    public override string Name => "Teleport";
    public override string DisplayName => "🌀 瞬间移动";
    public override string Description => "传送到玩家历史位置！";
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

        // 获取位置记录器
        var plugin = MyrtleSkill.Instance;
        if (plugin?.PositionRecorder == null)
        {
            player.PrintToChat("💫 位置记录器未启用！");
            return;
        }

        // 收集所有玩家的历史位置
        var allPositions = new List<(Features.PositionEntry, string)>();
        foreach (var p in Utilities.GetPlayers())
        {
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
            player.PrintToChat("💫 没有可用的传送位置！");
            return;
        }

        // 随机选择一个位置
        var random = new Random();
        int randomIndex = random.Next(allPositions.Count);
        var (selectedPosition, ownerName) = allPositions[randomIndex];

        // 计算时间差
        float timeAgo = Server.CurrentTime - selectedPosition.Timestamp;
        string timeDesc = timeAgo < 60
            ? $"{(int)timeAgo}秒前"
            : timeAgo < 3600
                ? $"{(int)(timeAgo / 60)}分钟前"
                : $"{(int)(timeAgo / 3600)}小时前";

        var targetPosition = new CounterStrikeSharp.API.Modules.Utils.Vector(
            selectedPosition.Position.X,
            selectedPosition.Position.Y,
            selectedPosition.Position.Z
        );

        Console.WriteLine($"[瞬间移动] {player.PlayerName} 传送到 {ownerName} 的位置 ({timeDesc})");

        // 传送玩家
        TeleportPlayer(player, pawn, targetPosition);

        // 显示效果
        player.PrintToCenter("🌀 瞬间移动！");
        player.PrintToChat($"🌀 已传送到 {ownerName} {timeDesc} 的位置！");

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
}
