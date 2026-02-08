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
/// 显示所有敌人身后留下轨迹
/// </summary>
public class JackalSkill : PlayerSkill
{
    public override string Name => "Jackal";
    public override string DisplayName => "🦊 豺狼";
    public override string Description => "所有敌人身后留下轨迹，显示他们最近10秒的移动路径！";
    public override bool IsActive => false; // 被动技能

    // 粒子效果路径（与 jRandomSkills 一致）
    private const string PARTICLE_NAME = "particles/ui/hud/ui_map_def_utility_trail.vpcf";

    // 轨迹持续时间（秒）- 显示10秒的移动路径
    private const float TRAIL_LIFETIME = 10.0f;

    // 创建新轨迹的间隔（秒）
    private const float TRAIL_CREATE_INTERVAL = 2.5f;

    // 跟踪每个玩家的当前轨迹粒子
    private static readonly ConcurrentDictionary<CCSPlayerController, CParticleSystem?> _playerTrails = new();

    // 跟踪拥有该技能的玩家
    private static readonly HashSet<ulong> _activePlayers = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        _activePlayers.Add(player.SteamID);

        Console.WriteLine($"[豺狼] {player.PlayerName} 获得了豺狼技能");

        player.PrintToChat("🦊 你获得了豺狼技能！");
        player.PrintToChat("💡 所有敌人身后会留下轨迹！");

        // 如果是第一个玩家，开始为所有敌人创建轨迹
        if (_activePlayers.Count == 1)
        {
            // 注册 CheckTransmit 监听（控制轨迹可见性）
            Plugin?.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);

            // 为所有敌人创建轨迹
            foreach (var enemy in Utilities.GetPlayers())
            {
                if (enemy == null || !enemy.IsValid)
                    continue;

                // 只为敌人创建轨迹
                if (enemy.Team == player.Team)
                    continue;

                if (!enemy.PawnIsAlive || enemy.IsBot || enemy.IsHLTV)
                    continue;

                if (enemy.Team is not CsTeam.CounterTerrorist and not CsTeam.Terrorist)
                    continue;

                // 开始创建循环轨迹
                StartPlayerTrail(enemy);
            }
        }
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        _activePlayers.Remove(player.SteamID);

        Console.WriteLine($"[豺狼] {player.PlayerName} 失去了豺狼技能");

        // 如果没有玩家使用豺狼技能，清理所有轨迹
        if (_activePlayers.Count == 0)
        {
            CleanupAllTrails();
            Plugin?.RemoveListener<Listeners.CheckTransmit>(OnCheckTransmit);
        }
    }

    /// <summary>
    /// 开始为玩家创建循环轨迹
    /// </summary>
    private void StartPlayerTrail(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 检查是否已经有轨迹在运行
        if (_playerTrails.ContainsKey(player))
            return;

        // 立即创建第一个轨迹
        CreatePlayerTrail(player);

        // 循环创建轨迹（每2.5秒创建一个新轨迹点）
        Plugin?.AddTimer(TRAIL_CREATE_INTERVAL, () =>
        {
            // 检查玩家是否还有豺狼技能激活
            if (_activePlayers.Count == 0)
                return;

            // 检查玩家是否还有效
            if (player == null || !player.IsValid || !player.PawnIsAlive)
            {
                // 清理该玩家的轨迹
                if (_playerTrails.TryRemove(player, out var particle))
                {
                    if (particle != null && particle.IsValid)
                    {
                        particle.AcceptInput("Kill");
                    }
                }
                return;
            }

            // 创建新的轨迹并继续循环
            CreatePlayerTrail(player);
            StartPlayerTrail(player);
        });
    }

    /// <summary>
    /// 为玩家创建单个轨迹粒子
    /// </summary>
    private void CreatePlayerTrail(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
            return;

        if (pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            return;

        // 创建粒子系统
        CParticleSystem particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system")!;
        if (particle == null || !particle.IsValid)
            return;

        particle.EffectName = PARTICLE_NAME;
        particle.StartActive = true;

        // 传送到玩家位置
        particle.Teleport(pawn.AbsOrigin);
        particle.DispatchSpawn();

        // 关键：绑定到玩家 Pawn，让粒子跟随玩家移动
        particle.AcceptInput("SetParent", pawn, particle, "!activator");

        // 启动粒子
        particle.AcceptInput("Start");

        // 保存粒子引用
        _playerTrails.AddOrUpdate(player, particle, (k, v) =>
        {
            // 销毁旧的粒子
            if (v != null && v.IsValid)
            {
                v.AcceptInput("Kill");
            }
            return particle;
        });

        Console.WriteLine($"[豺狼] 为 {player.PlayerName} 创建轨迹粒子");

        // 设置自动销毁（2.5秒后）
        Plugin?.AddTimer(TRAIL_LIFETIME, () =>
        {
            if (particle != null && particle.IsValid)
            {
                particle.AcceptInput("Kill");
            }
        });
    }

    /// <summary>
    /// 清理所有轨迹
    /// </summary>
    private void CleanupAllTrails()
    {
        foreach (var kvp in _playerTrails)
        {
            var particle = kvp.Value;
            if (particle != null && particle.IsValid)
            {
                particle.AcceptInput("Kill");
            }
        }

        _playerTrails.Clear();
    }

    /// <summary>
    /// 回合开始时清理所有轨迹
    /// </summary>
    public static void OnRoundStart()
    {
        foreach (var kvp in _playerTrails)
        {
            var particle = kvp.Value;
            if (particle != null && particle.IsValid)
            {
                particle.AcceptInput("Kill");
            }
        }

        _playerTrails.Clear();
        _activePlayers.Clear();
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
            bool hasJackalSkill = _activePlayers.Contains(player.SteamID);

            // 如果玩家正在观察其他人，检查被观察者是否有豺狼技能
            if (!hasJackalSkill)
            {
                var targetHandle = player.Pawn.Value?.ObserverServices?.ObserverTarget.Value?.Handle ?? nint.Zero;
                if (targetHandle != nint.Zero)
                {
                    var target = Utilities.GetPlayers().FirstOrDefault(p => p?.Pawn?.Value?.Handle == targetHandle);
                    if (target != null)
                    {
                        hasJackalSkill = _activePlayers.Contains(target.SteamID);
                    }
                }
            }

            // 控制每个轨迹粒子的可见性
            foreach (var kvp in _playerTrails)
            {
                var trailOwner = kvp.Key;
                var particle = kvp.Value;

                if (particle == null || !particle.IsValid)
                    continue;

                var entity = Utilities.GetEntityFromIndex<CBaseEntity>((int)particle.Index);
                if (entity == null || !entity.IsValid)
                    continue;

                // 隐藏条件：
                // 1. 玩家没有豺狼技能
                // 2. 或者轨迹所有者和玩家是同一队伍（不应该看到队友的轨迹）
                if (!hasJackalSkill || trailOwner.Team == player.Team)
                {
                    info.TransmitEntities.Remove(entity.Index);
                }
            }
        }
    }
}
