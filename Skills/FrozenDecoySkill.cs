// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills by Juzlus (Frozen Decoy skill)

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;

namespace MyrtleSkill.Skills;

/// <summary>
/// 冷冻诱饵技能 - 被动技能
/// 你的诱饵弹会冻结附近所有玩家
/// 完全复制自 jRandomSkills Frozen Decoy
/// </summary>
public class FrozenDecoySkill : PlayerSkill
{
    public override string Name => "FrozenDecoy";
    public override string DisplayName => "❄️ 冷冻诱饵";
    public override string Description => "你的诱饵弹会冻结附近所有玩家！开局获得1颗（投掷后自动补充1次）！";
    public override bool IsActive => false; // 被动技能

    // 影响半径和减速倍数（与 jRandomSkills 一致）
    private const float TRIGGER_RADIUS = 180.0f;
    private const int SLOWNESS_MULTIPLIER = 5;

    // 诱饵数量和补充次数
    private const int DECOY_COUNT = 1;
    private const int MAX_REPLENISH_COUNT = 1; // 最多补充1次

    // 计数器：跟踪每个玩家的诱饵数量
    private readonly Dictionary<ulong, int> _decoyCounters = new();

    // 跟踪每回合已补充次数
    private readonly Dictionary<ulong, int> _replenishedCount = new();

    // 记录所有激活的诱饵位置（使用 ConcurrentDictionary 线程安全）
    private static readonly ConcurrentDictionary<Vector, byte> _decoys = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[冷冻诱饵] {player.PlayerName} 获得了冷冻诱饵技能");

        // 设置计数器为1，补充次数为0
        _decoyCounters[player.SteamID] = DECOY_COUNT;
        _replenishedCount[player.SteamID] = 0;

        // 给予1个诱饵弹
        GiveDecoys(player, DECOY_COUNT);

        player.PrintToChat("❄️ 你获得了冷冻诱饵技能！");
        player.PrintToChat($"💣 获得了 {DECOY_COUNT} 颗诱饵弹（投掷后自动补充{MAX_REPLENISH_COUNT}次）！");
        player.PrintToChat($"💡 你的诱饵弹会冻结半径 {TRIGGER_RADIUS} 内的所有玩家！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 清除计数器
        _decoyCounters.Remove(player.SteamID);
        _replenishedCount.Remove(player.SteamID);

        Console.WriteLine($"[冷冻诱饵] {player.PlayerName} 失去了冷冻诱饵技能");
    }

    /// <summary>
    /// 处理诱饵开始事件 - 记录诱饵位置
    /// 完全复制自 jRandomSkills Frozen Decoy.DecoyStarted
    /// </summary>
    public void OnDecoyStarted(EventDecoyStarted @event)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return;

        // 检查玩家是否有冷冻诱饵技能
        var skills = Plugin?.SkillManager.GetPlayerSkills(player);
        if (skills == null || skills.Count == 0)
            return;

        var frozenDecoySkill = skills.FirstOrDefault(s => s.Name == "FrozenDecoy");
        if (frozenDecoySkill == null)
            return;

        // 记录诱饵位置
        var decoyPos = new Vector(@event.X, @event.Y, @event.Z);
        _decoys.TryAdd(decoyPos, 0);

        Console.WriteLine($"[冷冻诱饵] {player.PlayerName} 的诱饵已放置在位置 ({@event.X}, {@event.Y}, {@event.Z})");

        // 自动补充1次（最多1次）
        if (!_decoyCounters.ContainsKey(player.SteamID))
            return;

        if (_replenishedCount.TryGetValue(player.SteamID, out var count) && count >= MAX_REPLENISH_COUNT)
        {
            Console.WriteLine($"[冷冻诱饵] {player.PlayerName} 本回合已补充{count}次，达到上限({MAX_REPLENISH_COUNT}次)，不再补充");
            return;
        }

        // 延迟补充（等待诱饵投掷完成）
        Server.NextFrame(() =>
        {
            if (player.IsValid && player.PawnIsAlive)
            {
                GiveDecoys(player, 1);
                _replenishedCount[player.SteamID] = count + 1;

                player.PrintToChat($"❄️ 诱饵弹已补充！({_replenishedCount[player.SteamID]}/{MAX_REPLENISH_COUNT})");
                Console.WriteLine($"[冷冻诱饵] {player.PlayerName} 的诱饵弹已补充 ({_replenishedCount[player.SteamID]}/{MAX_REPLENISH_COUNT})");
            }
        });
    }

    /// <summary>
    /// 处理诱饵爆炸事件 - 移除诱饵
    /// 完全复制自 jRandomSkills Frozen Decoy.DecoyDetonate
    /// </summary>
    public void OnDecoyDetonate(EventDecoyDetonate @event)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return;

        // 检查玩家是否有冷冻诱饵技能
        var skills = Plugin?.SkillManager.GetPlayerSkills(player);
        if (skills == null || skills.Count == 0)
            return;

        var frozenDecoySkill = skills.FirstOrDefault(s => s.Name == "FrozenDecoy");
        if (frozenDecoySkill == null)
            return;

        // 移除该位置的诱饵
        foreach (var decoy in _decoys.Keys.Where(v => v.X == @event.X && v.Y == @event.Y && v.Z == @event.Z))
        {
            _decoys.TryRemove(decoy, out _);
            Console.WriteLine($"[冷冻诱饵] 诱饵在 ({@event.X}, {@event.Y}, {@event.Z}) 爆炸并移除");
        }
    }

    /// <summary>
    /// 每帧更新 - 冻结诱饵附近的玩家
    /// 完全复制自 jRandomSkills Frozen Decoy.OnTick
    /// </summary>
    public void OnTick()
    {
        // 如果没有诱饵，直接返回
        if (_decoys.IsEmpty)
            return;

        foreach (var decoyPos in _decoys.Keys)
        {
            foreach (var player in Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist || p.Team == CsTeam.CounterTerrorist))
            {
                var pawn = player.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
                    continue;

                // 计算距离
                double distance = GetDistance(decoyPos, pawn.AbsOrigin);

                // 如果在影响范围内
                if (distance <= TRIGGER_RADIUS)
                {
                    // 距离越近，冻结效果越强
                    double modifier = Math.Clamp(distance / TRIGGER_RADIUS, 0f, 1f);
                    pawn.VelocityModifier = (float)Math.Pow(modifier, SLOWNESS_MULTIPLIER);
                }
            }
        }
    }

    /// <summary>
    /// 清理所有诱饵（回合开始时）
    /// </summary>
    public static void OnRoundStart()
    {
        _decoys.Clear();
        Console.WriteLine("[冷冻诱饵] 已清理所有诱饵");
    }

    /// <summary>
    /// 给予玩家指定数量的诱饵弹
    /// </summary>
    private void GiveDecoys(CCSPlayerController player, int count)
    {
        if (player == null || !player.IsValid)
            return;

        try
        {
            for (int i = 0; i < count; i++)
            {
                player.GiveNamedItem("weapon_decoy");
            }

            Console.WriteLine($"[冷冻诱饵] 给予 {player.PlayerName} {count} 个诱饵弹");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[冷冻诱饵] 给予诱饵弹时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 计算两点之间的距离
    /// 复制自 jRandomSkills SkillUtils.GetDistance
    /// </summary>
    private static double GetDistance(Vector pos1, Vector pos2)
    {
        float dx = pos1.X - pos2.X;
        float dy = pos1.Y - pos2.Y;
        float dz = pos1.Z - pos2.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}
