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
    /// 获取敌方出生点位置（优先选择没有玩家的出生点）
    /// </summary>
    private static Vector? GetEnemySpawnPoint(CCSPlayerController player)
    {
        // 根据玩家队伍选择敌方出生点
        string spawnPointName = player.Team == CsTeam.Terrorist
            ? "info_player_counterterrorist" // T阵营传送至CT出生点
            : "info_player_terrorist"; // CT阵营传送至T阵营出生点

        var spawns = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>(spawnPointName).ToList();
        if (spawns.Count == 0)
        {
            Console.WriteLine($"[顶级小偷] 警告：找不到出生点 '{spawnPointName}'");
            return null;
        }

        // 查找没有玩家的出生点（半径150单位内没有其他玩家）
        var safeSpawns = new List<(SpawnPoint spawn, Vector position)>();
        const float SAFE_DISTANCE = 150.0f;

        foreach (var spawn in spawns)
        {
            if (spawn == null || !spawn.IsValid || spawn.AbsOrigin == null)
                continue;

            var spawnPos = spawn.AbsOrigin;
            bool isSafe = true;

            // 检查附近是否有其他玩家
            foreach (var p in Utilities.GetPlayers())
            {
                if (p == null || !p.IsValid || !p.PawnIsAlive)
                    continue;

                // 排除自己
                if (p.SteamID == player.SteamID)
                    continue;

                var pawn = p.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
                    continue;

                // 计算距离
                float distance = GetDistance(spawnPos, pawn.AbsOrigin);
                if (distance < SAFE_DISTANCE)
                {
                    isSafe = false;
                    break;
                }
            }

            if (isSafe)
            {
                safeSpawns.Add((spawn, spawnPos));
            }
        }

        // 如果有安全的出生点，随机选择一个
        if (safeSpawns.Count > 0)
        {
            var random = new Random();
            var selected = safeSpawns[random.Next(safeSpawns.Count)];
            Console.WriteLine($"[顶级小偷] 找到 {safeSpawns.Count} 个安全出生点，选择其中一个");
            return selected.position;
        }

        // 如果所有出生点都不安全，选择随机一个（保持原有行为）
        Console.WriteLine($"[顶级小偷] 警告：所有出生点附近都有玩家，使用随机位置");
        var randomSpawn = spawns[new Random().Next(spawns.Count)];
        return randomSpawn.AbsOrigin;
    }

    /// <summary>
    /// 计算两点之间的距离
    /// </summary>
    private static float GetDistance(Vector pos1, Vector pos2)
    {
        return (float)Math.Sqrt(
            Math.Pow(pos1.X - pos2.X, 2) +
            Math.Pow(pos1.Y - pos2.Y, 2) +
            Math.Pow(pos1.Z - pos2.Z, 2)
        );
    }

    /// <summary>
    /// 传送玩家到指定位置
    /// </summary>
    private static void TeleportPlayer(CCSPlayerController player, CCSPlayerPawn pawn, Vector position)
    {
        // 传送玩家
        pawn.Teleport(position, pawn.AbsRotation, new Vector(0, 0, 0));
    }
}
