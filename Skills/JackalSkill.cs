// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills by Juzlus (Jackal skill)

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;

namespace MyrtleSkill.Skills;

/// <summary>
/// 豺狼/追踪技能 - 被动技能
/// 显示所有敌人最近10秒的移动轨迹
/// </summary>
public class JackalSkill : PlayerSkill
{
    public override string Name => "Jackal";
    public override string DisplayName => "🦊 豺狼";
    public override string Description => "所有敌人身后留下轨迹，显示他们最近10秒的移动路径！";
    public override bool IsActive => false; // 被动技能

    // 粒子效果路径（与 jRandomSkills 一致）
    private const string PARTICLE_NAME = "particles/ui/hud/ui_map_def_utility_trail.vpcf";

    // 轨迹持续时间（秒）
    private const float TRAIL_DURATION = 10.0f;

    // 位置记录间隔（秒）
    private const float RECORD_INTERVAL = 0.5f;

    // 跟踪每个玩家的位置历史
    private readonly ConcurrentDictionary<ulong, PlayerPositionHistory> _playerPositions = new();

    // 位置历史记录
    private class PlayerPositionHistory
    {
        public ConcurrentBag<PositionRecord> Positions { get; set; } = new();
    }

    // 位置记录
    private class PositionRecord
    {
        public Vector Position { get; set; }
        public float Time { get; set; }
        public CParticleSystem Particle { get; set; }
    }

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[豺狼] {player.PlayerName} 获得了豺狼技能");
        player.PrintToChat("🦊 你获得了豺狼技能！");
        player.PrintToChat("💡 所有敌人身后会留下轨迹，显示他们最近10秒的移动路径！");

        // 注册 OnTick 监听（无条件注册，确保开始记录位置）
        if (Plugin != null)
        {
            Plugin.RegisterListener<Listeners.OnTick>(OnTick);
        }

        // 注册 CheckTransmit 监听
        Plugin?.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
    }

    public override void OnRevert(CCSPlayerController player)
    {
        // 清理该玩家的位置历史
        RemovePlayerHistory(player.SteamID);

        // 如果没有玩家使用豺狼技能，移除监听
        if (_playerPositions.Count == 0 && Plugin != null)
        {
            Plugin.RemoveListener<Listeners.OnTick>(OnTick);
            Plugin.RemoveListener<Listeners.CheckTransmit>(OnCheckTransmit);
        }

        Console.WriteLine($"[豺狼] {player.PlayerName} 失去了豺狼技能");
    }

    /// <summary>
    /// 每帧更新 - 记录敌人位置并更新轨迹
    /// </summary>
    public void OnTick()
    {
        float currentTime = Server.CurrentTime;

        // 每0.5秒记录一次位置（避免记录过于频繁）
        if (Server.TickCount % 32 != 0) // 64 tick/s * 0.5s = 32 ticks
            return;

        // 获取所有有豺狼技能的玩家
        var playersWithJackal = new List<CCSPlayerController>();
        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || !player.IsValid)
                continue;

            var skills = Plugin?.SkillManager.GetPlayerSkills(player);
            bool hasJackal = skills?.Any(s => s.Name == "Jackal") ?? false;
            if (hasJackal)
            {
                playersWithJackal.Add(player);
            }
        }

        // 如果没有玩家有豺狼技能，返回
        if (playersWithJackal.Count == 0)
            return;

        // 记录所有敌人的位置
        foreach (var enemy in Utilities.GetPlayers())
        {
            if (enemy == null || !enemy.IsValid || !enemy.PawnIsAlive)
                continue;

            var enemyPawn = enemy.PlayerPawn.Value;
            if (enemyPawn == null || !enemyPawn.IsValid || enemyPawn.AbsOrigin == null)
                continue;

            // 记录位置
            RecordEnemyPosition(enemy, currentTime);
        }

        // 清理过期的位置记录
        CleanupOldPositions(currentTime);
    }

    /// <summary>
    /// 记录敌人位置
    /// </summary>
    private void RecordEnemyPosition(CCSPlayerController enemy, float currentTime)
    {
        var enemyPawn = enemy.PlayerPawn.Value;
        if (enemyPawn == null || !enemyPawn.IsValid || enemyPawn.AbsOrigin == null)
            return;

        // 获取或创建位置历史
        var history = _playerPositions.GetOrAdd(enemy.SteamID, new PlayerPositionHistory());

        // 创建位置记录
        var record = new PositionRecord
        {
            Position = new Vector(enemyPawn.AbsOrigin.X, enemyPawn.AbsOrigin.Y, enemyPawn.AbsOrigin.Z),
            Time = currentTime,
            Particle = null
        };

        // 创建粒子效果
        CParticleSystem particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system")!;
        if (particle != null && particle.IsValid)
        {
            particle.EffectName = PARTICLE_NAME;
            particle.StartActive = true;
            particle.Teleport(record.Position);
            particle.DispatchSpawn();
            record.Particle = particle;
        }

        // 添加到历史记录
        history.Positions.Add(record);

        Console.WriteLine($"[豺狼] 记录 {enemy.PlayerName} 的位置");
    }

    /// <summary>
    /// 清理过期的位置记录
    /// </summary>
    private void CleanupOldPositions(float currentTime)
    {
        foreach (var kvp in _playerPositions)
        {
            var steamID = kvp.Key;
            var history = kvp.Value;

            // 获取过期的记录
            var expiredRecords = history.Positions.Where(p => currentTime - p.Time > TRAIL_DURATION).ToList();

            foreach (var record in expiredRecords)
            {
                // 销毁粒子
                if (record.Particle != null && record.Particle.IsValid)
                {
                    record.Particle.AcceptInput("Kill");
                }

                // ConcurrentBag不支持移除操作，需要重新创建
                var remainingRecords = history.Positions.Where(p => p != record);
                history.Positions = new ConcurrentBag<PositionRecord>(remainingRecords);
            }
        }
    }

    /// <summary>
    /// 清理玩家的位置历史
    /// </summary>
    private void RemovePlayerHistory(ulong steamID)
    {
        if (_playerPositions.TryGetValue(steamID, out var history))
        {
            foreach (var record in history.Positions)
            {
                if (record.Particle != null && record.Particle.IsValid)
                {
                    record.Particle.AcceptInput("Kill");
                }
            }

            _playerPositions.TryRemove(steamID, out _);
        }
    }

    /// <summary>
    /// 控制轨迹可见性
    /// 只有拥有豺狼技能的玩家能看到轨迹
    /// </summary>
    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        foreach (var (info, player) in infoList)
        {
            if (player == null || !player.IsValid)
                continue;

            // 检查玩家是否有豺狼技能
            var skills = Plugin?.SkillManager.GetPlayerSkills(player);
            bool hasSkill = skills?.Any(s => s.Name == "Jackal") ?? false;

            // 如果玩家正在观察其他人，检查被观察者是否有豺狼技能
            if (!hasSkill)
            {
                var targetHandle = player.Pawn.Value?.ObserverServices?.ObserverTarget.Value?.Handle ?? nint.Zero;
                if (targetHandle != nint.Zero)
                {
                    var target = Utilities.GetPlayers().FirstOrDefault(p => p?.Pawn?.Value?.Handle == targetHandle);
                    if (target != null)
                    {
                        var targetSkills = Plugin?.SkillManager.GetPlayerSkills(target);
                        hasSkill = targetSkills?.Any(s => s.Name == "Jackal") ?? false;
                    }
                }
            }

            // 控制每个轨迹粒子的可见性
            foreach (var kvp in _playerPositions)
            {
                var history = kvp.Value;

                foreach (var record in history.Positions)
                {
                    if (record.Particle == null || !record.Particle.IsValid)
                        continue;

                    var entity = Utilities.GetEntityFromIndex<CBaseEntity>((int)record.Particle.Index);
                    if (entity == null || !entity.IsValid)
                        continue;

                    // 如果玩家没有豺狼技能，则隐藏轨迹
                    if (!hasSkill)
                    {
                        info.TransmitEntities.Remove(entity.Index);
                    }
                    // 有技能的玩家可以看到
                }
            }
        }
    }
}
