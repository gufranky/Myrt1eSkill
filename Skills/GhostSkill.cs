// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills by Juzlus (Ghost skill)

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;

namespace MyrtleSkill.Skills;

/// <summary>
/// 鬼技能 - 被动技能
/// 你完全隐形，但是受到伤害或造成伤害就会永久显形
/// </summary>
public class GhostSkill : PlayerSkill
{
    public override string Name => "Ghost";
    public override string DisplayName => "👻 鬼";
    public override string Description => "你完全隐形！受到伤害或造成伤害就会永久显形！可以使用任意武器！";
    public override bool IsActive => false; // 被动技能

    // 血液粒子效果
    private const string BLOOD_PARTICLE = "particles/blood_impact/blood_impact_high.vpcf";

    // 跟踪隐形的玩家
    private static readonly ConcurrentDictionary<ulong, GhostState> _ghostStates = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[鬼] {player.PlayerName} 获得了鬼技能");

        // 添加到隐形列表
        _ghostStates.TryAdd(player.SteamID, new GhostState
        {
            Player = player,
            IsInvisible = true
        });

        player.PrintToChat("👻 你获得了鬼技能！");
        player.PrintToChat("💡 你完全隐形！");
        player.PrintToChat("⚠️ 受到伤害或造成伤害就会永久显形！");
        player.PrintToChat("🔫 可以使用任意武器！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[鬼] {player.PlayerName} 失去了鬼技能");

        // 移除状态
        _ghostStates.TryRemove(player.SteamID, out _);
    }

    /// <summary>
    /// 处理玩家受伤事件（永久显形）
    /// </summary>
    public static void HandlePlayerHurt(EventPlayerHurt @event)
    {
        var attacker = @event.Attacker;
        if (attacker == null || !attacker.IsValid)
            return;

        // 检查攻击者是否有鬼技能
        if (!_ghostStates.ContainsKey(attacker.SteamID))
            return;

        // 造成伤害，永久显形
        RevealGhost(attacker, "💥 你造成了伤害，显形了！");
    }

    /// <summary>
    /// 处理玩家被伤害事件（永久显形）
    /// </summary>
    public static void HandlePlayerDamaged(CCSPlayerController victim)
    {
        if (victim == null || !victim.IsValid)
            return;

        // 检查受害者是否有鬼技能
        if (!_ghostStates.ContainsKey(victim.SteamID))
            return;

        // 受到伤害，永久显形
        RevealGhost(victim, "💥 你受到了伤害，显形了！");

        // 显示血液粒子效果
        SpawnBloodParticle(victim);
    }

    /// <summary>
    /// 让鬼显形
    /// </summary>
    private static void RevealGhost(CCSPlayerController player, string message)
    {
        if (!_ghostStates.TryGetValue(player.SteamID, out var state))
            return;

        if (!state.IsInvisible)
            return;

        // 标记为显形
        state.IsInvisible = false;

        // 提示玩家
        player.PrintToChat(message);
        player.PrintToCenter("👻 你已经显形了！");

        Console.WriteLine($"[鬼] {player.PlayerName} 显形了");
    }

    /// <summary>
    /// 显示血液粒子效果
    /// </summary>
    private static void SpawnBloodParticle(CCSPlayerController player)
    {
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null)
            return;

        var particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
        if (particle == null || !particle.IsValid)
            return;

        particle.EffectName = BLOOD_PARTICLE;
        particle.StartActive = true;

        Vector pos = new(playerPawn.AbsOrigin.X, playerPawn.AbsOrigin.Y, playerPawn.AbsOrigin.Z + 50);
        particle.Teleport(pos);
        particle.DispatchSpawn();

        particle.AcceptInput("Start");

        // 2秒后移除粒子
        Server.NextFrame(() =>
        {
            if (particle.IsValid)
            {
                particle.AcceptInput("Kill");
            }
        });
    }

    /// <summary>
    /// 检查玩家是否隐形（用于 CheckTransmit）
    /// </summary>
    public static bool IsInvisible(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return false;

        if (!_ghostStates.TryGetValue(player.SteamID, out var state))
            return false;

        return state.IsInvisible;
    }

    /// <summary>
    /// 处理 CheckTransmit（隐藏玩家）
    /// </summary>
    public static void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (infoList == null)
            return;

        foreach (var (info, receiver) in infoList)
        {
            if (receiver == null || !receiver.IsValid)
                continue;

            // 检查所有隐形的玩家
            foreach (var state in _ghostStates.Values)
            {
                if (!state.IsInvisible)
                    continue;

                var ghostPlayer = state.Player;
                if (ghostPlayer == null || !ghostPlayer.IsValid)
                    continue;

                // 不对自己隐藏
                if (receiver.SteamID == ghostPlayer.SteamID)
                    continue;

                var ghostPawn = ghostPlayer.PlayerPawn.Value;
                if (ghostPawn == null || !ghostPawn.IsValid)
                    continue;

                // 隐藏玩家实体
                var entity = Utilities.GetEntityFromIndex<CBaseEntity>((int)ghostPawn.Index);
                if (entity != null && entity.IsValid)
                {
                    info.TransmitEntities.Remove(entity.Index);
                }

                // 隐藏 C4（如果持有）
                var bombIndex = GetBombIndex(ghostPlayer);
                if (bombIndex.HasValue)
                {
                    var bombEntity = Utilities.GetEntityFromIndex<CBaseEntity>((int)bombIndex.Value);
                    if (bombEntity != null && bombEntity.IsValid)
                    {
                        info.TransmitEntities.Remove(bombEntity.Index);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 获取玩家持有的 C4 索引
    /// </summary>
    private static uint? GetBombIndex(CCSPlayerController player)
    {
        var bombEntities = Utilities.FindAllEntitiesByDesignerName<CC4>("weapon_c4");
        if (bombEntities == null || !bombEntities.Any())
            return null;

        var bomb = bombEntities.FirstOrDefault();
        if (bomb == null || !bomb.IsValid)
            return null;

        if (bomb.OwnerEntity.Index != player.Index)
            return null;

        return bomb.Index;
    }

    /// <summary>
    /// 清理所有鬼状态（回合结束时调用）
    /// </summary>
    public static void ClearAllGhosts()
    {
        _ghostStates.Clear();
        Console.WriteLine("[鬼] 已清理所有鬼状态");
    }

    /// <summary>
    /// 每帧更新（清理死亡的玩家）
    /// </summary>
    public static void OnTick()
    {
        var toRemove = new List<ulong>();

        foreach (var kvp in _ghostStates)
        {
            var player = kvp.Value.Player;
            if (player == null || !player.IsValid || !player.PawnIsAlive)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var steamId in toRemove)
        {
            _ghostStates.TryRemove(steamId, out _);
        }
    }

    /// <summary>
    /// 鬼状态
    /// </summary>
    private class GhostState
    {
        public required CCSPlayerController Player { get; set; }
        public bool IsInvisible { get; set; }
    }
}
