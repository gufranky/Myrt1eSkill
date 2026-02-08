// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;

namespace MyrtleSkill.Skills;

/// <summary>
/// 测距仪技能 - 被动技能
/// 显示到最近敌人的距离，如果敌人在5m之内则透视显示位置
/// </summary>
public class RangeFinderSkill : PlayerSkill
{
    public override string Name => "RangeFinder";
    public override string DisplayName => "📏 测距仪";
    public override string Description => "显示到最近敌人的距离！5米内敌人会被透视标记！";
    public override bool IsActive => false; // 被动技能

    // 透视距离阈值（5米 = 500游戏单位）
    private const float XRAY_DISTANCE_THRESHOLD = 500.0f;

    // 追踪拥有该技能的玩家
    private readonly HashSet<ulong> _activePlayers = new();

    // 追踪发光效果的敌人
    private readonly Dictionary<int, (int relayIndex, int glowIndex, ulong ownerId)> _glowingEnemies = new();

    // 每个玩家最近敌人的距离
    private readonly Dictionary<ulong, float> _nearestEnemyDistance = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        _activePlayers.Add(player.SteamID);
        _nearestEnemyDistance[player.SteamID] = float.MaxValue;

        Console.WriteLine($"[测距仪] {player.PlayerName} 获得了测距仪技能");

        player.PrintToChat("📏 你获得了测距仪技能！");
        player.PrintToChat("💡 屏幕显示到最近敌人的距离！");
        player.PrintToChat("💡 5米内的敌人会被透视标记！");

        // 注册监听器
        if (Plugin != null)
        {
            Plugin.RegisterListener<Listeners.OnTick>(OnTick);
            Plugin.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
        }
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        _activePlayers.Remove(player.SteamID);
        _nearestEnemyDistance.Remove(player.SteamID);

        // 移除该玩家造成的发光效果
        RemoveGlowEffectsByOwner(player.SteamID);

        // 如果没有玩家使用测距仪技能，移除监听器
        if (_activePlayers.Count == 0 && Plugin != null)
        {
            Plugin.RemoveListener<Listeners.OnTick>(OnTick);
            Plugin.RemoveListener<Listeners.CheckTransmit>(OnCheckTransmit);
        }

        Console.WriteLine($"[测距仪] {player.PlayerName} 失去了测距仪技能");
    }

    /// <summary>
    /// 每帧更新 - 检测最近敌人并更新发光效果
    /// </summary>
    private void OnTick()
    {
        // 每10帧更新一次（避免过于频繁）
        if (Server.TickCount % 10 != 0)
            return;

        foreach (var steamId in _activePlayers)
        {
            var player = Utilities.GetPlayers().FirstOrDefault(p => p != null && p.IsValid && p.SteamID == steamId);
            if (player == null || !player.PawnIsAlive)
                continue;

            // 找到最近的敌人
            var nearestEnemy = FindNearestEnemy(player, out float distance);

            if (nearestEnemy != null)
            {
                _nearestEnemyDistance[steamId] = distance;

                // 如果距离在5米内，应用发光效果
                if (distance <= XRAY_DISTANCE_THRESHOLD)
                {
                    ApplyGlowToEnemy(nearestEnemy, player);
                }
                else
                {
                    // 移除该敌人的发光效果
                    RemoveGlowFromEnemy(nearestEnemy.Slot);
                }
            }
            else
            {
                _nearestEnemyDistance[steamId] = float.MaxValue;
            }
        }
    }

    /// <summary>
    /// 找到最近的敌人
    /// </summary>
    private CCSPlayerController? FindNearestEnemy(CCSPlayerController player, out float nearestDistance)
    {
        nearestDistance = float.MaxValue;
        CCSPlayerController? nearestEnemy = null;

        if (player == null || !player.IsValid)
            return null;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null)
            return null;

        var playerPos = playerPawn.AbsOrigin;
        var playerTeam = player.Team;

        foreach (var other in Utilities.GetPlayers())
        {
            if (other == null || !other.IsValid || !other.PawnIsAlive)
                continue;

            // 跳过同队玩家
            if (other.Team == playerTeam)
                continue;

            var otherPawn = other.PlayerPawn.Value;
            if (otherPawn == null || !otherPawn.IsValid || otherPawn.AbsOrigin == null)
                continue;

            var otherPos = otherPawn.AbsOrigin;

            // 计算距离
            float distance = (float)Math.Sqrt(
                Math.Pow(playerPos.X - otherPos.X, 2) +
                Math.Pow(playerPos.Y - otherPos.Y, 2) +
                Math.Pow(playerPos.Z - otherPos.Z, 2)
            );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = other;
            }
        }

        return nearestEnemy;
    }

    /// <summary>
    /// 为敌人添加发光效果
    /// </summary>
    private void ApplyGlowToEnemy(CCSPlayerController enemy, CCSPlayerController owner)
    {
        if (enemy == null || !enemy.IsValid)
            return;

        var pawn = enemy.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 检查是否已经有发光效果
        if (_glowingEnemies.ContainsKey(enemy.Slot))
            return;

        try
        {
            // 使用CreateGlowEffect添加发光
            bool success = ApplyEntityGlowEffect(pawn, enemy.Team, out var relayIndex, out var glowIndex);
            if (success)
            {
                _glowingEnemies[enemy.Slot] = (relayIndex, glowIndex, owner.SteamID);
                Console.WriteLine($"[测距仪] 为 {enemy.PlayerName} 添加发光效果（由 {owner.PlayerName} 触发）");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[测距仪] 添加发光效果时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 移除敌人的发光效果
    /// </summary>
    private void RemoveGlowFromEnemy(int enemySlot)
    {
        if (!_glowingEnemies.ContainsKey(enemySlot))
            return;

        var (relayIndex, glowIndex, _) = _glowingEnemies[enemySlot];

        var relay = Utilities.GetEntityFromIndex<CDynamicProp>(relayIndex);
        var glow = Utilities.GetEntityFromIndex<CDynamicProp>(glowIndex);

        if (relay != null && relay.IsValid)
        {
            relay.AcceptInput("Kill");
        }

        if (glow != null && glow.IsValid)
        {
            glow.AcceptInput("Kill");
        }

        _glowingEnemies.Remove(enemySlot);
    }

    /// <summary>
    /// 移除指定拥有者造成的所有发光效果
    /// </summary>
    private void RemoveGlowEffectsByOwner(ulong ownerId)
    {
        var toRemove = new List<int>();

        foreach (var kvp in _glowingEnemies)
        {
            var (slot, relayIndex, glowIndex, effectOwnerId) = (kvp.Key, kvp.Value.relayIndex, kvp.Value.glowIndex, kvp.Value.ownerId);

            if (effectOwnerId == ownerId)
            {
                var relay = Utilities.GetEntityFromIndex<CDynamicProp>(relayIndex);
                var glow = Utilities.GetEntityFromIndex<CDynamicProp>(glowIndex);

                if (relay != null && relay.IsValid)
                {
                    relay.AcceptInput("Kill");
                }

                if (glow != null && glow.IsValid)
                {
                    glow.AcceptInput("Kill");
                }

                toRemove.Add(slot);
            }
        }

        foreach (var slot in toRemove)
        {
            _glowingEnemies.Remove(slot);
        }
    }

    /// <summary>
    /// 检查传输时控制发光效果的可见性
    /// 只有拥有测距仪技能的玩家能看到发光效果
    /// </summary>
    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (_glowingEnemies.Count == 0)
            return;

        foreach (var (info, receiver) in infoList)
        {
            if (receiver == null || !receiver.IsValid)
                continue;

            // 检查接收者是否有测距仪技能
            bool hasSkill = _activePlayers.Contains(receiver.SteamID);

            // 如果玩家正在观察其他人，检查被观察者是否有测距仪技能
            if (!hasSkill)
            {
                var targetHandle = receiver.Pawn.Value?.ObserverServices?.ObserverTarget.Value?.Handle ?? nint.Zero;
                if (targetHandle != nint.Zero)
                {
                    var target = Utilities.GetPlayers().FirstOrDefault(p => p?.Pawn?.Value?.Handle == targetHandle);
                    if (target != null)
                    {
                        hasSkill = _activePlayers.Contains(target.SteamID);
                    }
                }
            }

            foreach (var slot in _glowingEnemies.Keys)
            {
                var (relayIndex, glowIndex, ownerId) = _glowingEnemies[slot];

                var relay = Utilities.GetEntityFromIndex<CDynamicProp>(relayIndex);
                var glow = Utilities.GetEntityFromIndex<CDynamicProp>(glowIndex);

                // 只有拥有测距仪技能的玩家能看到发光效果
                if (hasSkill && _activePlayers.Contains(ownerId))
                {
                    if (relay != null && relay.IsValid)
                    {
                        info.TransmitEntities.Add(relay.Index);
                    }

                    if (glow != null && glow.IsValid)
                    {
                        info.TransmitEntities.Add(glow.Index);
                    }
                }
                else
                {
                    if (relay != null && relay.IsValid)
                    {
                        info.TransmitEntities.Remove(relay.Index);
                    }

                    if (glow != null && glow.IsValid)
                    {
                        info.TransmitEntities.Remove(glow.Index);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 应用实体发光效果（参考 DecoyXRaySkill）
    /// </summary>
    private bool ApplyEntityGlowEffect(CBaseEntity entity, CsTeam team, out int relayIndex, out int glowIndex)
    {
        relayIndex = -1;
        glowIndex = -1;

        if (entity == null || !entity.IsValid)
            return false;

        var sceneNode = entity.CBodyComponent?.SceneNode;
        if (sceneNode == null)
            return false;

        var skeletonInstance = sceneNode.GetSkeletonInstance();
        if (skeletonInstance == null)
            return false;

        var modelName = skeletonInstance.ModelState.ModelName;
        if (string.IsNullOrEmpty(modelName))
            return false;

        var modelRelay = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        var modelGlow = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");

        if (modelRelay == null || !modelRelay.IsValid || modelGlow == null || !modelGlow.IsValid)
            return false;

        // 设置modelRelay
        modelRelay.Spawnflags = 256u;
        modelRelay.RenderMode = RenderMode_t.kRenderNone;

        if (modelRelay.CBodyComponent != null && modelRelay.CBodyComponent.SceneNode != null)
        {
            var owner = modelRelay.CBodyComponent.SceneNode.Owner;
            if (owner != null && owner.Entity != null)
            {
                owner.Entity.Flags &= ~(uint)(1 << 2);
            }
        }

        modelRelay.SetModel(modelName);
        modelRelay.DispatchSpawn();
        modelRelay.AcceptInput("FollowEntity", entity, modelRelay, "!activator");

        // 设置modelGlow
        if (modelGlow.CBodyComponent != null && modelGlow.CBodyComponent.SceneNode != null)
        {
            var owner = modelGlow.CBodyComponent.SceneNode.Owner;
            if (owner != null && owner.Entity != null)
            {
                owner.Entity.Flags &= ~(uint)(1 << 2);
            }
        }

        modelGlow.SetModel(modelName);
        modelGlow.DispatchSpawn();
        modelGlow.AcceptInput("FollowEntity", modelRelay, modelGlow, "!activator");

        // 根据队伍设置发光颜色
        switch (team)
        {
            case CsTeam.Terrorist:
                modelGlow.Glow.GlowColorOverride = Color.FromArgb(255, 165, 0); // 橙色
                break;
            case CsTeam.CounterTerrorist:
                modelGlow.Glow.GlowColorOverride = Color.FromArgb(135, 206, 235); // 天蓝色
                break;
            default:
                modelGlow.Glow.GlowColorOverride = Color.FromArgb(255, 255, 255); // 白色
                break;
        }

        modelGlow.Spawnflags = 256u;
        modelGlow.RenderMode = RenderMode_t.kRenderTransAlpha;
        modelGlow.Glow.GlowRange = 5000;
        modelGlow.Glow.GlowTeam = -1;
        modelGlow.Glow.GlowType = 3;
        modelGlow.Glow.GlowRangeMin = 20;

        relayIndex = (int)modelRelay.Index;
        glowIndex = (int)modelGlow.Index;

        return true;
    }

    /// <summary>
    /// 获取玩家到最近敌人的距离（用于HUD显示）
    /// </summary>
    public float? GetNearestEnemyDistance(ulong playerSteamId)
    {
        if (_nearestEnemyDistance.TryGetValue(playerSteamId, out var distance))
        {
            return distance;
        }
        return null;
    }
}
