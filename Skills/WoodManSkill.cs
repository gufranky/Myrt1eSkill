// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills Woodman skill

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;
using System.Drawing;

namespace MyrtleSkill.Skills;

/// <summary>
/// 木头人技能 - 主动技能
/// 让对方玩家保持不动，否则被透视
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

    // 移动检测阈值（移动超过此距离被视为移动）
    private const float MOVEMENT_THRESHOLD = 10.0f;

    // 跟踪每局使用次数（静态，允许在回合开始时重置）
    private static readonly ConcurrentDictionary<ulong, int> _usageCount = new();

    // 跟踪当前检测的玩家信息
    private readonly ConcurrentDictionary<int, WoodManPlayerInfo> _detectedPlayers = new();

    // 跟踪被透视的玩家（用于清理）
    private readonly Dictionary<int, (int relayIndex, int glowIndex)> _glowingPlayers = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 初始化使用次数
        _usageCount.TryAdd(player.SteamID, 0);

        Console.WriteLine($"[木头人] {player.PlayerName} 获得了木头人技能");

        player.PrintToChat("🪵 你获得了木头人技能！");
        player.PrintToChat("💡 输入 !useskill 或按键激活！");
        player.PrintToChat($"🎯 每局可使用{MAX_USES_PER_ROUND}次！");
        player.PrintToChat("⚠️ 对方玩家3秒倒数准备时间，之后3秒内移动将被透视！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        _usageCount.TryRemove(player.SteamID, out _);

        Console.WriteLine($"[木头人] {player.PlayerName} 失去了木头人技能");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        // 检查使用次数
        if (!_usageCount.TryGetValue(player.SteamID, out int count) || count >= MAX_USES_PER_ROUND)
        {
            player.PrintToChat($"❌ 本回合已使用{MAX_USES_PER_ROUND}次！");
            return;
        }

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null)
            return;

        // 增加使用次数
        _usageCount.AddOrUpdate(player.SteamID, 1, (key, old) => old + 1);

        Console.WriteLine($"[木头人] {player.PlayerName} 使用了木头人技能（本回合第{_usageCount[player.SteamID]}次）");

        // 获取敌方队伍
        var enemyTeam = player.Team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;

        // 开始检测移动
        StartDetection(player, enemyTeam);

        player.PrintToCenter("🪵 木头人技能已激活！");
        player.PrintToChat($"🪵 已使用{count + 1}/{MAX_USES_PER_ROUND}次！");
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
                player.PrintToChat("🪵 木头人技能生效！3秒内移动将被透视！");
            }
        }

        Server.PrintToChatAll($"🪵 木头人开始检测移动！{DETECTION_TIME}秒内移动将被透视！");

        // 开始显示倒计时HUD
        ShowCountdownHUD(COUNTDOWN_TIME + DETECTION_TIME);

        // 注册 OnTick 监听
        if (Plugin != null)
        {
            Plugin.RegisterListener<Listeners.OnTick>(OnDetectionTick);
        }

        // 检测时间结束后移除监听并清理
        Plugin?.AddTimer(COUNTDOWN_TIME + DETECTION_TIME, () =>
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
    /// 显示倒计时HUD（类似开局HUD）
    /// </summary>
    private void ShowCountdownHUD(float duration)
    {
        // 获取被检测的所有玩家
        var playersToNotify = _detectedPlayers.Values.Select(p => p.Player).Where(p => p != null && p.IsValid).ToList();

        if (playersToNotify.Count == 0)
            return;

        float updateInterval = 0.1f; // 每0.1秒更新一次

        // 创建倒计时更新动作
        Action<float> updateHUD = null;
        updateHUD = (float elapsedTime) =>
        {
            float remainingTime = Math.Max(0, duration - elapsedTime);

            if (remainingTime <= 0)
                return;

            // 显示倒计时HUD
            foreach (var player in playersToNotify)
            {
                if (!player.IsValid)
                    continue;

                // 根据剩余时间改变颜色和文字
                string color;
                string warningText;
                if (remainingTime <= DETECTION_TIME)
                {
                    // 倒数阶段
                    if (remainingTime > DETECTION_TIME * 0.66f)
                    {
                        color = "#FFFF00"; // 黄色
                        warningText = "⏱️ 保持不动！";
                    }
                    else if (remainingTime > DETECTION_TIME * 0.33f)
                    {
                        color = "#FF6600"; // 橙红色
                        warningText = "⚠️ 最后警告！";
                    }
                    else
                    {
                        color = "#FF0000"; // 红色
                        warningText = "⚠️ 别动！";
                    }
                }
                else
                {
                    // 检测阶段
                    color = "#FF0000"; // 红色
                    warningText = "👁️ 检测中！";
                }

                string htmlContent = $"<div style='background-color: rgba(0, 0, 0, 0.85); border: 4px solid {color}; border-radius: 12px; padding: 25px 50px; margin: 15px;'>"
                    + $"<font style='font-size: 42px; color: {color}; font-weight: bold;'>{warningText}</font><br><br>"
                    + $"<font style='font-size: 32px; color: #FFFFFF; font-weight: bold;'>{remainingTime:F1} 秒</font><br><br>"
                    + $"<font style='font-size: 22px; color: #FF6666;'>移动将被透视！</font>"
                    + $"</div>";

                player.PrintToCenterHtml(htmlContent);
            }

            // 继续下一次更新
            if (remainingTime > updateInterval)
            {
                Plugin?.AddTimer(updateInterval, () => updateHUD(elapsedTime + updateInterval));
            }
        };

        // 立即开始第一次更新
        updateHUD(0);
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
                ApplyGlowToPlayer(info.Player);
            }
        }
    }

    /// <summary>
    /// 检查玩家是否移动
    /// </summary>
    private bool IsPlayerMoving(Vector initialPosition, Vector currentPosition)
    {
        // 计算移动距离（忽略高度变化）
        float deltaX = currentPosition.X - initialPosition.X;
        float deltaY = currentPosition.Y - initialPosition.Y;
        float distance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

        return distance > MOVEMENT_THRESHOLD;
    }

    /// <summary>
    /// 对玩家施加透视效果
    /// </summary>
    private void ApplyGlowToPlayer(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        try
        {
            bool success = ApplyEntityGlowEffect(pawn, player.Team, out var relayIndex, out var glowIndex);
            if (success)
            {
                _glowingPlayers[player.Slot] = (relayIndex, glowIndex);
                Console.WriteLine($"[木头人] 为 {player.PlayerName} 添加透视发光效果");

                // GLOW_DURATION秒后移除透视
                Plugin?.AddTimer(GLOW_DURATION, () =>
                {
                    RemoveGlowFromPlayer(player);
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[木头人] 施加透视效果时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 应用实体发光效果（复制自 XrayEvent）
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
    /// 从玩家移除透视效果
    /// </summary>
    private void RemoveGlowFromPlayer(CCSPlayerController player)
    {
        if (player == null || !_glowingPlayers.ContainsKey(player.Slot))
            return;

        var (relayIndex, glowIndex) = _glowingPlayers[player.Slot];

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

        _glowingPlayers.Remove(player.Slot);

        Console.WriteLine($"[木头人] {player.PlayerName} 的透视效果已移除");
    }

    /// <summary>
    /// 移除所有透视效果
    /// </summary>
    private void RemoveGlowEffects()
    {
        foreach (var (slot, (relayIndex, glowIndex)) in _glowingPlayers)
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

        _glowingPlayers.Clear();
        Console.WriteLine("[木头人] 已移除所有透视效果");
    }

    /// <summary>
    /// 回合开始时清理使用记录
    /// </summary>
    public static void OnRoundStart()
    {
        _usageCount.Clear();
        Console.WriteLine("[木头人] 新回合开始，清空使用记录");
    }

    /// <summary>
    /// 木头人玩家信息
    /// </summary>
    private class WoodManPlayerInfo
    {
        public CCSPlayerController? Player { get; set; }
        public Vector InitialPosition { get; set; }
        public float DetectionStartTime { get; set; }
        public bool IsMoving { get; set; }
    }
}
