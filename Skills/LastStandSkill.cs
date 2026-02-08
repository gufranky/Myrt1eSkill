// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;
using System.Drawing;

namespace MyrtleSkill.Skills;

/// <summary>
/// 残局使者技能 - 被动技能
/// 当你的队伍只剩下你一个人的时候，可以透视对方所有人，并且血量变为150
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
    private static readonly ConcurrentDictionary<ulong, bool> _activatedPlayers = new();

    // 跟踪被透视的敌人（用于清理）
    private readonly Dictionary<int, (int relayIndex, int glowIndex)> _glowingEnemies = new();

    // 跟踪每个玩家的激活状态
    private readonly Dictionary<ulong, bool> _playerActiveStatus = new();

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

        // 清除激活状态和透视效果
        _activatedPlayers.TryRemove(player.SteamID, out _);
        _playerActiveStatus.Remove(player.SteamID);

        // 移除所有透视效果
        RemoveAllGlowEffects();

        Console.WriteLine($"[残局使者] {player.PlayerName} 失去了残局使者技能");
    }

    /// <summary>
    /// 处理玩家死亡事件 - 检查是否触发残局使者
    /// </summary>
    public void OnPlayerDeath(EventPlayerDeath @event)
    {
        // 每次有人死亡后，检查所有玩家的残局使者状态
        CheckAllPlayersLastStand();
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
            var skills = Plugin?.SkillManager.GetPlayerSkills(player);
            if (skills == null || skills.Count == 0)
                continue;

            var lastStandSkill = skills.FirstOrDefault(s => s.Name == "LastStand");
            if (lastStandSkill == null)
                continue;

            // 检查是否已激活
            if (_activatedPlayers.ContainsKey(player.SteamID))
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
    /// 激活残局使者效果
    /// </summary>
    private void ActivateLastStand(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 标记为已激活
        _activatedPlayers.TryAdd(player.SteamID, true);
        _playerActiveStatus[player.SteamID] = true;

        // 增加血量到150
        int currentHealth = pawn.Health;
        pawn.Health = BONUS_HEALTH;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        Console.WriteLine($"[残局使者] {player.PlayerName} 激活残局使者！血量：{currentHealth} → {BONUS_HEALTH}");

        // 获取敌方队伍
        var enemyTeam = player.Team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;

        // 对所有敌方玩家施加透视效果
        foreach (var enemy in Utilities.GetPlayers())
        {
            if (enemy == null || !enemy.IsValid || !enemy.PawnIsAlive)
                continue;

            if (enemy.Team == enemyTeam)
            {
                ApplyGlowToEnemy(enemy);
            }
        }

        // 注册 CheckTransmit 监听器（如果有敌人被透视）
        if (_glowingEnemies.Count > 0 && Plugin != null)
        {
            Plugin.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
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
    /// 对敌人施加透视发光效果
    /// 参考 WoodManSkill 的实现
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
                Console.WriteLine($"[残局使者] 为 {enemy.PlayerName} 添加透视发光效果");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[残局使者] 施加透视效果时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 应用实体发光效果
    /// 参考 WoodManSkill 的实现
    /// </summary>
    private unsafe bool ApplyEntityGlowEffect(CCSPlayerPawn pawn, CsTeam team, out int relayIndex, out int glowIndex)
    {
        relayIndex = -1;
        glowIndex = -1;

        try
        {
            // 创建 relay 实体
            var modelRelay = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            if (modelRelay == null || !modelRelay.IsValid)
                return false;

            // 创建 glow 实体
            var modelGlow = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            if (modelGlow == null || !modelGlow.IsValid)
                return false;

            modelRelay.DispatchSpawn();
            modelGlow.DispatchSpawn();

            // 设置 relay 属性
            modelRelay.Entity!.Name = modelRelay.Globalname = $"LastStandRelay_{pawn.Index}";
            modelRelay.Teleport(pawn.AbsOrigin!, pawn.AbsRotation);
            modelRelay.SetModel("models/dev/dev_reflection.vmdl");
            modelRelay.Render = Color.FromArgb(0, 255, 255, 255);

            // 设置 glow 属性
            modelGlow.Entity!.Name = modelGlow.Globalname = $"LastStandGlow_{pawn.Index}";
            modelGlow.Teleport(pawn.AbsOrigin!, pawn.AbsRotation);

            Server.NextFrame(() =>
            {
                if (modelRelay.IsValid && modelGlow.IsValid && pawn.IsValid)
                {
                    modelGlow.SetModel($"models/{(team == CsTeam.Terrorist ? "player" : "player")}/customplayer/tm_jumpsuit_variantb.mdl");
                }
            });

            // 设置颜色（根据队伍）
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
        catch (Exception ex)
        {
            Console.WriteLine($"[残局使者] 创建发光效果时出错: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// CheckTransmit 监听器 - 确保透视效果对所有人可见
    /// 参考 WoodManSkill 的实现
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
    /// 清理所有残局使者状态（回合结束时调用）
    /// </summary>
    public static void ClearAllLastStand()
    {
        _activatedPlayers.Clear();
        Console.WriteLine("[残局使者] 已清理所有激活状态");
    }
}
