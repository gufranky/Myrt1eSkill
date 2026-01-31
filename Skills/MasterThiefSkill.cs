using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 顶级小偷技能 - 传送至敌方出生点
/// </summary>
public class MasterThiefSkill : PlayerSkill
{
    public override string Name => "MasterThief";
    public override string DisplayName => "🎭 顶级小偷";
    public override string Description => "传送至敌方出生点！神不知鬼不觉！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 15.0f; // 15秒冷却

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[顶级小偷] {player.PlayerName} 获得了顶级小偷技能");
        player.PrintToChat("🎭 你获得了顶级小偷技能！");
        player.PrintToChat("💡 输入 !useskill 或按键传送至敌方出生点！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        Console.WriteLine($"[顶级小偷] {player.PlayerName} 失去了顶级小偷技能");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
        {
            player?.PrintToChat("🎭 你必须存活才能使用技能！");
            return;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        Console.WriteLine($"[顶级小偷] {player.PlayerName} 尝试使用传送技能");

        // 获取敌方出生点
        var enemySpawn = GetEnemySpawnPoint(player);
        if (enemySpawn == null)
        {
            player.PrintToChat("🎭 无法找到敌方出生点！");
            return;
        }

        // 传送玩家
        TeleportPlayer(player, pawn, enemySpawn);

        // 显示效果
        player.PrintToCenter("🎭 顶级小偷！");
        player.PrintToChat("🎭 你已传送至敌方出生点！");

        Console.WriteLine($"[顶级小偷] {player.PlayerName} 成功传送至敌方出生点");
    }

    /// <summary>
    /// 获取敌方出生点位置
    /// </summary>
    private static Vector? GetEnemySpawnPoint(CCSPlayerController player)
    {
        // 根据玩家队伍选择敌方出生点
        string spawnPointName = player.Team == CsTeam.Terrorist
            ? "info_player_counterterrorist" // T阵营传送至CT出生点
            : "info_player_terrorist"; // CT阵营传送至T出生点

        var spawns = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>(spawnPointName).ToList();
        if (spawns.Count == 0)
        {
            Console.WriteLine($"[顶级小偷] 警告：找不到出生点 '{spawnPointName}'");
            return null;
        }

        // 随机选择一个敌方出生点
        var random = new Random();
        var randomSpawn = spawns[random.Next(spawns.Count)];
        return randomSpawn.AbsOrigin;
    }

    /// <summary>
    /// 传送玩家到指定位置
    /// </summary>
    private static void TeleportPlayer(CCSPlayerController player, CCSPlayerPawn pawn, Vector position)
    {
        // 传送玩家
        pawn.Teleport(position, pawn.AbsRotation, new Vector(0, 0, 0));

        // 临时设置为穿透模式，防止卡在墙里
        pawn.Collision.CollisionGroup = 1; // COLLISION_GROUP_DISSOLVING
        pawn.Collision.CollisionAttribute.CollisionGroup = 1;
        Utilities.SetStateChanged(pawn, "CCollisionProperty", "m_CollisionGroup");
        Utilities.SetStateChanged(pawn, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");

        // 下一帧恢复正常碰撞
        Server.NextFrame(() =>
        {
            if (pawn == null || !pawn.IsValid || pawn.LifeState != 2) // LIFE_ALIVE
                return;

            pawn.Collision.CollisionGroup = 2; // COLLISION_GROUP_PLAYER
            pawn.Collision.CollisionAttribute.CollisionGroup = 2;
            Utilities.SetStateChanged(pawn, "CCollisionProperty", "m_CollisionGroup");
            Utilities.SetStateChanged(pawn, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
        });
    }
}
