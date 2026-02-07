// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;
using System.Drawing;

namespace MyrtleSkill.Skills;

/// <summary>
/// 木头人技能 - 让对方玩家保持不动，否则被透视
/// </summary>
public class WoodManSkill : PlayerSkill
{
    public override string Name => "WoodMan";
    public override string DisplayName => "🪵 木头人";
    public override string Description => "输入 !useskill 激活！对方玩家有3秒倒数准备时间，之后3秒内移动将被透视3秒！每局可使用2次！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 0.0f; // 0秒冷却

    // 每局可使用次数
    private const int MAX_USES_PER_ROUND = 2;

    // 倒数时间（秒）
    private const float COUNTDOWN_TIME = 3.0f;

    // 检测移动时间（秒）
    private const float DETECTION_TIME = 3.0f;

    // 透视持续时间（秒）
    private const float GLOW_DURATION = 3.0f;

    // 跟踪每回合已使用次数
    private static readonly ConcurrentDictionary<string, int> _usageCount = new();

    // 跟踪被检测的玩家及其初始位置
    private readonly ConcurrentDictionary<int, WoodManPlayerInfo> _detectedPlayers = new();

    // 跟踪发光效果的敌人
    private readonly Dictionary<int, (int relayIndex, int glowIndex)> _glowingEnemies = new();

    // 玩家信息
    private class WoodManPlayerInfo
    {
        public CCSPlayerController? Player { get; set; }
        public Vector InitialPosition { get; set; } = new Vector(0, 0, 0);
        public float DetectionStartTime { get; set; }
        public bool IsMoving { get; set; }
    }

    public override void OnApply(CCSPlayerController player)
    {
        var key = player.SteamID.ToString();
        _usageCount[key] = 0;

        Console.WriteLine($"[木头人] {player.PlayerName} 获得了木头人技能");

        player.PrintToChat("🪵 你获得了木头人技能！");
        player.PrintToChat("💡 输入 !useskill 激活！");
        player.PrintToChat("⏱️ 对方玩家有3秒倒数，之后3秒内移动将被透视！");
        player.PrintToChat("⏰ 每局可使用2次，无冷却！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        var key = player.SteamID.ToString();
        _usageCount.TryRemove(key, out _);

        Console.WriteLine($"[木头人] {player.PlayerName} 失去了木头人技能");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        var key = player.SteamID.ToString();

        // 获取当前使用次数
        int currentCount = _usageCount.TryGetValue(key, out var count) ? count : 0;

        // 检查是否超过使用次数限制
        if (currentCount >= MAX_USES_PER_ROUND)
        {
            player.PrintToCenter($"❌ 本回合已使用{MAX_USES_PER_ROUND}次木头人技能！");
            player.PrintToChat($"❌ 本回合已使用{MAX_USES_PER_ROUND}次木头人技能！");
            return;
        }

        Console.WriteLine($"[木头人] {player.PlayerName} 使用了木头人技能（第{currentCount + 1}次）");

        // 增加使用次数
        _usageCount[key] = currentCount + 1;

        // 获取敌方队伍
        var enemyTeam = player.Team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;

        // 给所有敌方玩家显示倒数
        foreach (var enemy in Utilities.GetPlayers())
        {
            if (enemy == null || !enemy.IsValid || !enemy.PawnIsAlive)
                continue;

            if (enemy.Team == enemyTeam)
            {
                // 显示倒数提示
                ShowCountdown(enemy, COUNTDOWN_TIME);
            }
        }

        player.PrintToCenter($"🪵 木头人已激活！剩余次数：{MAX_USES_PER_ROUND - currentCount - 1}");
        player.PrintToChat($"🪵 木头人已激活！{COUNTDOWN_TIME}秒后开始检测移动！");

        // 显示全局提示
        Server.PrintToChatAll($"🪵 {player.PlayerName} 使用了木头人技能！{COUNTDOWN_TIME}秒后检测移动！");

        // 倒数结束后开始检测
        Plugin?.AddTimer(COUNTDOWN_TIME, () =>
        {
            StartDetection(player, enemyTeam);
        });
    }

    /// <summary>
    /// 显示倒数
    /// </summary>
    private void ShowCountdown(CCSPlayerController player, float duration)
    {
        for (int i = (int)duration; i > 0; i--)
        {
            Plugin?.AddTimer(duration - i, () =>
            {
                if (player != null && player.IsValid && player.PawnIsAlive)
                {
                    player.PrintToCenter($"🪵 倒数: {i}秒");
                    player.PrintToChat($"🪵 {i}秒后将开始检测移动！");
                }
            });
        }
    }

    /// <summary>
    /// 开始检测移动
    /// </summary>
    private void StartDetection(CCSPlayerController observer, CsTeam enemyTeam)
    {
        if (observer == null || !observer.IsValid)
            return;

        Console.WriteLine($"[木头人] 开始检测敌方队伍移动");

        // 清空之前的检测记录
        _detectedPlayers.Clear();

        // 为每个敌方玩家记录初始位置
        foreach (var enemy in Utilities.GetPlayers())
        {
            if (enemy == null || !enemy.IsValid || !enemy.PawnIsAlive)
                continue;

            if (enemy.Team == enemyTeam)
            {
                var pawn = enemy.PlayerPawn.Value;
                if (pawn != null && pawn.IsValid && pawn.AbsOrigin != null)
                {
                    _detectedPlayers.TryAdd(enemy.Slot, new WoodManPlayerInfo
                    {
                        Player = enemy,
                        InitialPosition = new Vector(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z),
                        DetectionStartTime = Server.CurrentTime,
                        IsMoving = false
                    });
                }
            }
        }

        // 显示开始提示
        foreach (var kvp in _detectedPlayers)
        {
            var player = kvp.Value.Player;
            if (player != null && player.IsValid)
            {
                player.PrintToCenter("🪵 保持不动！");
                player.PrintToChat("🪵 木头人技能生效！3秒内移动将被透视！");
            }
        }

        Server.PrintToChatAll($"🪵 木头人开始检测移动！{DETECTION_TIME}秒内移动将被透视！");

        // 注册 OnTick 监听
        if (Plugin != null)
        {
            Plugin.RegisterListener<Listeners.OnTick>(OnDetectionTick);
        }

        // 检测时间结束后移除监听并清理
        Plugin?.AddTimer(DETECTION_TIME, () =>
        {
            Plugin?.RemoveListener<Listeners.OnTick>(OnDetectionTick);
            RemoveGlowEffects();

            // 显示结算信息
            var movingPlayers = _detectedPlayers.Values.Where(p => p.IsMoving).ToList();
            if (movingPlayers.Count > 0)
            {
                string playerNames = string.Join(", ", movingPlayers.Select(p => p.Player?.PlayerName ?? "Unknown"));
                Server.PrintToChatAll($"🪵 移动的玩家: {playerNames}（透视{GLOW_DURATION}秒）");
            }
            else
            {
                Server.PrintToChatAll($"🪵 所有人都保持不动！");
            }

            _detectedPlayers.Clear();
        });
    }

    /// <summary>
    /// 每帧检测移动
    /// </summary>
    private void OnDetectionTick()
    {
        float currentTime = Server.CurrentTime;

        foreach (var kvp in _detectedPlayers)
        {
            var slot = kvp.Key;
            var info = kvp.Value;

            if (info.Player == null || !info.Player.IsValid || !info.Player.PawnIsAlive)
                continue;

            var pawn = info.Player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
                continue;

            // 检测是否移动
            bool isMoving = IsPlayerMoving(info.InitialPosition, pawn.AbsOrigin);

            // 如果从静止变为移动，施加透视
            if (isMoving && !info.IsMoving)
            {
                info.IsMoving = true;

                // 施加透视效果
                ApplyGlowToEnemy(info.Player);

                // 提示玩家
                info.Player.PrintToCenter("🪵 你移动了！被透视3秒！");
                info.Player.PrintToChat("🪵 你移动了！被透视3秒！");

                Console.WriteLine($"[木头人] {info.Player.PlayerName} 移动了，施加透视");
            }
        }
    }

    /// <summary>
    /// 检测玩家是否移动
    /// </summary>
    private bool IsPlayerMoving(Vector initialPos, Vector currentPos)
    {
        // 计算位置变化
        float deltaX = Math.Abs(currentPos.X - initialPos.X);
        float deltaY = Math.Abs(currentPos.Y - initialPos.Y);
        float deltaZ = Math.Abs(currentPos.Z - initialPos.Z);

        // 移动阈值（5单位）
        const float MOVE_THRESHOLD = 5.0f;

        return (deltaX + deltaY + deltaZ) > MOVE_THRESHOLD;
    }

    /// <summary>
    /// 对敌人施加透视发光效果
    /// 参考 DecoyXRaySkill 的实现
    /// </summary>
    private void ApplyGlowToEnemy(CCSPlayerController enemy)
    {
        if (enemy == null || !enemy.IsValid)
            return;

        var pawn = enemy.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        try
        {
            bool success = ApplyEntityGlowEffect(pawn, enemy.Team, out var relayIndex, out var glowIndex);
            if (success)
            {
                _glowingEnemies[enemy.Slot] = (relayIndex, glowIndex);
                Console.WriteLine($"[木头人] 为 {enemy.PlayerName} 添加透视发光效果");

                // 注册 CheckTransmit 监听器
                if (Plugin != null && _glowingEnemies.Count == 1)
                {
                    Plugin.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[木头人] 添加发光效果时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 应用实体发光效果（复制自 DecoyXRaySkill）
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

        // 设置 modelRelay
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

        // 设置 modelGlow
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

        // 设置颜色（根据队伍）- 使用GlowColorOverride而不是Render
        Color glowColor = team == CsTeam.Terrorist ? Color.FromArgb(255, 165, 0) : Color.FromArgb(135, 206, 235);
        modelGlow.Glow.GlowColorOverride = glowColor;
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
    /// 移除所有发光效果
    /// </summary>
    private void RemoveGlowEffects()
    {
        foreach (var (slot, (relayIndex, glowIndex)) in _glowingEnemies)
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
        }

        _glowingEnemies.Clear();
        Console.WriteLine("[木头人] 已移除所有发光效果");

        // 移除 CheckTransmit 监听器
        Plugin?.RemoveListener<Listeners.CheckTransmit>(OnCheckTransmit);
    }

    /// <summary>
    /// 检查传输时控制发光效果的可见性
    /// </summary>
    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (_glowingEnemies.Count == 0)
            return;

        foreach (var (info, receiver) in infoList)
        {
            if (receiver == null || !receiver.IsValid)
                continue;

            // 所有玩家都能看到发光效果
            foreach (var slot in _glowingEnemies.Keys)
            {
                var (relayIndex, glowIndex) = _glowingEnemies[slot];

                var relay = Utilities.GetEntityFromIndex<CDynamicProp>(relayIndex);
                var glow = Utilities.GetEntityFromIndex<CDynamicProp>(glowIndex);

                if (relay != null && relay.IsValid)
                {
                    info.TransmitEntities.Add(relay.Index);
                }

                if (glow != null && glow.IsValid)
                {
                    info.TransmitEntities.Add(glow.Index);
                }
            }
        }
    }

    /// <summary>
    /// 回合开始时清理使用记录
    /// </summary>
    public static void OnRoundStart()
    {
        _usageCount.Clear();
        Console.WriteLine("[木头人] 新回合开始，清空使用记录");
    }
}
