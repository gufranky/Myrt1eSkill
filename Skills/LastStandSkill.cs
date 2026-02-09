// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills Xray event

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;

namespace MyrtleSkill.Skills;

/// <summary>
/// 残局使者技能 - 被动技能
/// 当你的队伍只剩下你一个人的时候，可以透视对方所有人，并且血量变为150
/// 使用 Xray 类型的发光效果
/// </summary>
public class LastStandSkill : PlayerSkill
{
    public override string Name => "LastStand";
    public override string DisplayName => "💀 残局使者";
    public override string Description => "当你的队伍只剩下你一个人的时候，可以透视对方所有人，并且血量变为150！";
    public override bool IsActive => false; // 被动技能
    public override float Cooldown => 0f; // 被动技能无冷却

    // 增加的血量
    private const int BONUS_HEALTH = 150;

    // 跟踪每个玩家是否已激活残局使者
    private readonly HashSet<ulong> _activatedPlayers = new();

    // 跟踪每个玩家的激活状态
    private readonly Dictionary<ulong, bool> _playerActiveStatus = new();

    // 跟踪被透视的敌人（用于清理）
    private readonly Dictionary<int, (int relayIndex, int glowIndex)> _glowingEnemies = new();

    // CheckTransmit 监听器是否已注册
    private bool _checkTransmitRegistered = false;

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        _playerActiveStatus[player.SteamID] = false;

        Console.WriteLine($"[残局使者] {player.PlayerName} 获得了残局使者技能");
        player.PrintToChat("💀 你获得了残局使者技能！");
        player.PrintToChat("💡 当你的队伍只剩下你一个人时，自动触发！");
        player.PrintToChat($"👁️ 透视所有敌人 + 血量变为{BONUS_HEALTH}！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 如果玩家已激活残局使者，清理透视效果
        if (_activatedPlayers.Contains(player.SteamID))
        {
            RemoveAllGlowEffects();

            // 移除监听器
            if (_checkTransmitRegistered && Plugin != null)
            {
                Plugin.RemoveListener<Listeners.CheckTransmit>(OnCheckTransmit);
                _checkTransmitRegistered = false;
            }
        }

        // 清除状态
        _activatedPlayers.Remove(player.SteamID);
        _playerActiveStatus.Remove(player.SteamID);

        Console.WriteLine($"[残局使者] {player.PlayerName} 失去了残局使者技能");
    }

    /// <summary>
    /// 处理玩家死亡事件 - 检查是否触发残局使者
    /// </summary>
    public void OnPlayerDeath(EventPlayerDeath @event)
    {
        // 获取正在死亡的玩家
        var dyingPlayer = @event.Userid;

        // 延迟一帧再检查，确保死亡状态已更新
        Server.NextFrame(() =>
        {
            CheckAllPlayersLastStand();
        });
    }

    /// <summary>
    /// 检查所有玩家是否触发残局使者
    /// </summary>
    private void CheckAllPlayersLastStand()
    {
        // 统计每个队伍的存活人数
        var terroristCount = 0;
        var ctCount = 0;

        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive)
                continue;

            if (player.Team == CsTeam.Terrorist)
                terroristCount++;
            else if (player.Team == CsTeam.CounterTerrorist)
                ctCount++;
        }

        Console.WriteLine($"[残局使者] 当前存活人数 - T: {terroristCount}, CT: {ctCount}");

        // 检查每个玩家是否触发残局使者
        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive)
                continue;

            // 检查玩家是否有残局使者技能
            if (!_playerActiveStatus.ContainsKey(player.SteamID))
                continue;

            // 检查是否已激活
            if (_activatedPlayers.Contains(player.SteamID))
                continue;

            // 检查是否只剩自己一人
            bool isLastAlive = false;
            if (player.Team == CsTeam.Terrorist && terroristCount == 1)
                isLastAlive = true;
            else if (player.Team == CsTeam.CounterTerrorist && ctCount == 1)
                isLastAlive = true;

            if (isLastAlive)
            {
                ActivateLastStand(player);
            }
        }
    }

    /// <summary>
    /// 激活残局使者效果（使用 Xray 类型发光）
    /// </summary>
    private void ActivateLastStand(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 标记为已激活
        _activatedPlayers.Add(player.SteamID);
        _playerActiveStatus[player.SteamID] = true;

        // 增加血量到150
        int currentHealth = pawn.Health;
        pawn.Health = BONUS_HEALTH;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        Console.WriteLine($"[残局使者] {player.PlayerName} 激活残局使者！血量：{currentHealth} → {BONUS_HEALTH}");

        // 获取敌方队伍
        var enemyTeam = player.Team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;

        // 对所有敌方玩家施加透视效果（使用 Xray 方法）
        foreach (var enemy in Utilities.GetPlayers())
        {
            if (enemy == null || !enemy.IsValid || !enemy.PawnIsAlive)
                continue;

            if (enemy.Team == enemyTeam)
            {
                ApplyGlowToEnemy(enemy);
            }
        }

        // 注册 CheckTransmit 监听器
        if (_glowingEnemies.Count > 0 && Plugin != null && !_checkTransmitRegistered)
        {
            Plugin.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
            _checkTransmitRegistered = true;
        }

        // 显示提示
        player.PrintToCenter("💀 残局使者已激活！");
        player.PrintToChat("💀 残局使者已激活！");
        player.PrintToChat($"❤️ 血量增加到 {BONUS_HEALTH}！");
        player.PrintToChat("👁️ 所有敌人已被透视！");

        // 广播消息
        Server.PrintToChatAll($"💀 {player.PlayerName} 激活了残局使者！血量变为{BONUS_HEALTH}并透视所有敌人！");
    }

    /// <summary>
    /// 对敌人施加透视发光效果（复制自 XrayEvent）
    /// </summary>
    private void ApplyGlowToEnemy(CCSPlayerController enemy)
    {
        if (enemy == null || !enemy.IsValid)
            return;

        var pawn = enemy.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 检查是否已经有透视效果（避免重复）
        if (_glowingEnemies.ContainsKey(enemy.Slot))
            return;

        try
        {
            bool success = ApplyEntityGlowEffect(pawn, enemy.Team, out var relayIndex, out var glowIndex);
            if (success)
            {
                _glowingEnemies[enemy.Slot] = (relayIndex, glowIndex);
                Console.WriteLine($"[残局使者] 为 {enemy.PlayerName} 添加透视发光效果");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[残局使者] 施加透视效果时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 应用实体发光效果（完全复制 XrayEvent.ApplyEntityGlowEffect）
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
    /// CheckTransmit 监听器 - 控制发光效果的可见性（复制自 XrayEvent）
    /// </summary>
    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (_glowingEnemies.Count == 0)
            return;

        // 实时清理已死亡的敌人
        CleanUpDeadEnemiesGlow();

        if (_glowingEnemies.Count == 0)
            return;

        foreach (var (info, player) in infoList)
        {
            if (player == null || !player.IsValid)
                continue;

            // 让拥有残局使者技能的玩家能看到敌人的发光效果
            if (_activatedPlayers.Contains(player.SteamID))
            {
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
    }

    /// <summary>
    /// 移除所有透视效果
    /// </summary>
    private void RemoveAllGlowEffects()
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
        Console.WriteLine("[残局使者] 已移除所有透视效果");
    }

    /// <summary>
    /// 清理已死亡敌人的透视效果
    /// </summary>
    private void CleanUpDeadEnemiesGlow()
    {
        var toRemove = new List<int>();

        foreach (var (slot, (relayIndex, glowIndex)) in _glowingEnemies)
        {
            var enemy = Utilities.GetPlayerFromSlot(slot);
            if (enemy == null || !enemy.IsValid || !enemy.PawnIsAlive)
            {
                // 敌人已死亡，移除透视效果
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

        if (toRemove.Count > 0)
        {
            Console.WriteLine($"[残局使者] 清理了 {toRemove.Count} 个已死亡敌人的透视效果");
        }
    }

    /// <summary>
    /// 清理所有残局使者状态（回合结束时调用）
    /// </summary>
    public static void ClearAllLastStand()
    {
        Console.WriteLine("[残局使者] 已清理所有激活状态");
    }
}
