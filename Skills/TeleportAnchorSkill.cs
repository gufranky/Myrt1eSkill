// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills by Juzlus (Jackal particle + Teleport)

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;

namespace MyrtleSkill.Skills;

/// <summary>
/// 传送锚点技能 - 主动技能
/// 第一次使用创建移动锚点，第二次使用传送到锚点位置
/// </summary>
public class TeleportAnchorSkill : PlayerSkill
{
    public override string Name => "TeleportAnchor";
    public override string DisplayName => "⚓ 传送锚点";
    public override string Description => "第一次使用创建移动锚点，第二次使用传送到锚点！持续10秒！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 0.0f; // 由我们手动管理30秒冷却

    // 粒子效果路径（使用类似 Jackal 的轨迹效果）
    private const string PARTICLE_NAME = "particles/ui/hud/ui_map_def_utility_trail.vpcf";

    // 锚点持续时间（秒）
    private const float ANCHOR_LIFETIME = 10.0f;

    // 移动速度（单位/秒）
    private const float MOVE_SPEED = 150.0f;

    // 跟踪每个玩家的锚点状态
    private readonly ConcurrentDictionary<ulong, AnchorState> _playerAnchors = new();

    // 跟踪每个玩家的上次使用时间（用于手动管理冷却）
    private readonly ConcurrentDictionary<ulong, float> _lastUseTime = new();

    // 锚点状态类
    private class AnchorState
    {
        public CParticleSystem? Particle { get; set; }
        public Vector? MoveDirection { get; set; }
        public bool HasAnchor { get; set; }
        public float CreateTime { get; set; }
    }

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[传送锚点] {player.PlayerName} 获得了传送锚点技能");
        player.PrintToChat("⚓ 你获得了传送锚点技能！");
        player.PrintToChat("💡 第一次使用创建锚点，第二次使用传送到锚点！");
        player.PrintToChat($"⏱️ 冷却时间：{Cooldown}秒，锚点持续{ANCHOR_LIFETIME}秒");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        // 移除锚点
        RemoveAnchor(player);

        _playerAnchors.TryRemove(player.SteamID, out _);
        _lastUseTime.TryRemove(player.SteamID, out _);

        Console.WriteLine($"[传送锚点] {player.PlayerName} 失去了传送锚点技能");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        // 检查冷却时间
        if (_lastUseTime.TryGetValue(player.SteamID, out var lastTime))
        {
            float elapsedTime = Server.CurrentTime - lastTime;
            if (elapsedTime < Cooldown)
            {
                float remainingTime = Cooldown - elapsedTime;
                player.PrintToCenter($"⏱️ 冷却中！剩余 {remainingTime:F0} 秒");
                player.PrintToChat($"⚓ 技能冷却中！还需等待 {remainingTime:F0} 秒");
                return;
            }
        }

        Console.WriteLine($"[传送锚点] {player.PlayerName} 使用了传送锚点技能");

        // 获取或创建锚点状态
        var state = _playerAnchors.GetOrAdd(player.SteamID, new AnchorState
        {
            HasAnchor = false,
            CreateTime = 0
        });

        if (!state.HasAnchor)
        {
            // 第一次使用：创建锚点（不触发冷却）
            CreateAnchor(player, state);
            player.PrintToChat("⚓ 锚点已创建！再次使用传送到锚点！");
        }
        else
        {
            // 第二次使用：传送到锚点（触发冷却）
            TeleportToAnchor(player, state);

            // 更新冷却时间
            _lastUseTime[player.SteamID] = Server.CurrentTime;
        }
    }

    /// <summary>
    /// 创建传送锚点
    /// </summary>
    private void CreateAnchor(CCSPlayerController player, AnchorState state)
    {
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null || playerPawn.AbsRotation == null)
            return;

        // 创建粒子系统
        CParticleSystem particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system")!;
        if (particle == null || !particle.IsValid)
            return;

        // 设置粒子效果
        particle.EffectName = PARTICLE_NAME;
        particle.StartActive = true;

        // 初始位置：玩家位置
        Vector startPos = new Vector(playerPawn.AbsOrigin.X, playerPawn.AbsOrigin.Y, playerPawn.AbsOrigin.Z);
        particle.Teleport(startPos);
        particle.DispatchSpawn();

        // 计算移动方向（玩家朝向的水平方向）
        Vector forward = GetForwardVector(playerPawn.AbsRotation);
        Vector moveDirection = new Vector(forward.X, forward.Y, 0); // 不包含垂直分量

        // 归一化方向
        float length = (float)Math.Sqrt(moveDirection.X * moveDirection.X + moveDirection.Y * moveDirection.Y);
        if (length > 0.001f)
        {
            moveDirection.X /= length;
            moveDirection.Y /= length;
        }

        // 保存状态
        state.Particle = particle;
        state.MoveDirection = moveDirection;
        state.HasAnchor = true;
        state.CreateTime = Server.CurrentTime;

        Console.WriteLine($"[传送锚点] {player.PlayerName} 创建了锚点，方向: ({moveDirection.X}, {moveDirection.Y}, 0)");

        // 注册 OnTick 监听（如果有锚点）
        if (_playerAnchors.Any(kvp => kvp.Value.HasAnchor) && Plugin != null)
        {
            Plugin.RegisterListener<Listeners.OnTick>(OnTick);
        }

        // 注册 CheckTransmit（让粒子透视可见）
        Plugin?.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);

        // 设置自动销毁定时器
        Plugin?.AddTimer(ANCHOR_LIFETIME, () =>
        {
            if (particle != null && particle.IsValid)
            {
                particle.AcceptInput("Kill");
                if (_playerAnchors.TryGetValue(player.SteamID, out var s))
                {
                    s.HasAnchor = false;
                    s.Particle = null;
                }
                Console.WriteLine($"[传送锚点] {player.PlayerName} 的锚点已过期销毁");
            }
        });

        player.PrintToCenter($"⚓ 锚点已创建！持续 {ANCHOR_LIFETIME} 秒！");
    }

    /// <summary>
    /// 传送到锚点位置
    /// </summary>
    private void TeleportToAnchor(CCSPlayerController player, AnchorState state)
    {
        if (state.Particle == null || !state.Particle.IsValid)
        {
            player.PrintToChat("⚓ 锚点已消失！");
            state.HasAnchor = false;
            return;
        }

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null)
            return;

        // 获取锚点当前位置
        var anchorPos = state.Particle.AbsOrigin;
        if (anchorPos == null)
            return;

        // 保存玩家当前位置的朝向
        var playerAngle = new QAngle(playerPawn.AbsRotation.X, playerPawn.AbsRotation.Y, playerPawn.AbsRotation.Z);

        // 传送玩家到锚点位置（保持玩家朝向）
        Vector targetPos = new Vector(anchorPos.X, anchorPos.Y, anchorPos.Z);
        playerPawn.Teleport(targetPos, playerAngle, new Vector(0, 0, 0));

        // 销毁锚点
        state.Particle.AcceptInput("Kill");
        state.HasAnchor = false;
        state.Particle = null;

        Console.WriteLine($"[传送锚点] {player.PlayerName} 传送到锚点位置");

        player.PrintToCenter("✨ 已传送到锚点！");
        player.PrintToChat("⚓ 传送完成！");
    }

    /// <summary>
    /// 移除玩家的锚点
    /// </summary>
    private void RemoveAnchor(CCSPlayerController player)
    {
        if (_playerAnchors.TryGetValue(player.SteamID, out var state))
        {
            if (state.Particle != null && state.Particle.IsValid)
            {
                state.Particle.AcceptInput("Kill");
            }
            state.HasAnchor = false;
            state.Particle = null;
        }
    }

    /// <summary>
    /// 每帧更新 - 移动锚点
    /// </summary>
    public void OnTick()
    {
        float currentTime = Server.CurrentTime;

        foreach (var kvp in _playerAnchors)
        {
            var state = kvp.Value;
            if (!state.HasAnchor || state.Particle == null || !state.Particle.IsValid)
                continue;

            // 检查是否过期
            if (currentTime >= state.CreateTime + ANCHOR_LIFETIME)
            {
                state.Particle.AcceptInput("Kill");
                state.HasAnchor = false;
                state.Particle = null;
                continue;
            }

            // 移动锚点（使用速度）
            float speedPerTick = MOVE_SPEED / 64.0f; // 假设 64 tick/s
            var particle = state.Particle;

            if (particle.AbsOrigin == null || particle.AbsVelocity == null || state.MoveDirection == null)
                continue;

            // 设置速度
            var moveDir = state.MoveDirection ?? new Vector(1, 0, 0);
            particle.AbsVelocity.X = moveDir.X * speedPerTick;
            particle.AbsVelocity.Y = moveDir.Y * speedPerTick;
            particle.AbsVelocity.Z = 0; // 不增加垂直速度

            Utilities.SetStateChanged(particle, "CBaseEntity", "m_vecAbsVelocity");
        }

        // 如果没有活跃的锚点，移除监听
        if (!_playerAnchors.Any(kvp => kvp.Value.HasAnchor))
        {
            Plugin?.RemoveListener<Listeners.OnTick>(OnTick);
            Plugin?.RemoveListener<Listeners.CheckTransmit>(OnCheckTransmit);
        }
    }

    /// <summary>
    /// 控制粒子可见性（只有锚点创建者能看到）
    /// 参考 Jackal 技能的 OnCheckTransmit 实现
    /// </summary>
    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        foreach (var (info, receiver) in infoList)
        {
            if (receiver == null || !receiver.IsValid)
                continue;

            foreach (var kvp in _playerAnchors)
            {
                var steamID = kvp.Key;  // 锚点创建者的 SteamID
                var state = kvp.Value;
                if (!state.HasAnchor || state.Particle == null || !state.Particle.IsValid)
                    continue;

                var particle = state.Particle;

                // 获取粒子实体
                var entity = Utilities.GetEntityFromIndex<CBaseEntity>((int)particle.Index);
                if (entity == null || !entity.IsValid)
                    continue;

                // 只有锚点创建者能看到
                if (receiver.SteamID != steamID)
                {
                    // 不是创建者，从传输列表移除
                    info.TransmitEntities.Remove(entity.Index);
                }
                // 创建者可以看到（不移除 = 显示）
            }
        }
    }

    /// <summary>
    /// 计算前方向量
    /// </summary>
    private static Vector GetForwardVector(QAngle angles)
    {
        float radiansY = angles.Y * (float)Math.PI / 180.0f;

        return new Vector(
            (float)Math.Cos(radiansY),
            (float)Math.Sin(radiansY),
            0
        );
    }
}
