// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on SecondChanceSkill by MyrtleSkill

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;

namespace MyrtleSkill.Skills;

/// <summary>
/// 凤凰技能 - 死亡后有20-40%几率复活
/// </summary>
public class PhoenixSkill : PlayerSkill
{
    public override string Name => "Phoenix";
    public override string DisplayName => "🔥 凤凰";
    public override string Description => "死亡后有20-40%几率复活！每回合限用一次！";
    public override bool IsActive => false; // 被动技能

    // 与第二次机会和名刀互斥
    public override List<string> ExcludedSkills => new() { "SecondChance", "Meito" };

    // 复活血量
    private const int REVIVE_HEALTH = 100;

    // 最小复活几率（%）
    private const int MIN_REVIVE_CHANCE = 20;

    // 最大复活几率（%）
    private const int MAX_REVIVE_CHANCE = 40;

    // 跟踪已使用凤凰复活的玩家
    private static readonly ConcurrentDictionary<int, byte> _phoenixUsed = new();
    // 跟踪玩家死亡前的护甲值
    private static readonly ConcurrentDictionary<int, int> _playerArmor = new();
    // 跟踪每个玩家的复活几率
    private static readonly ConcurrentDictionary<int, int> _playerReviveChance = new();
    private static readonly Random _random = new();

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[凤凰] {player.PlayerName} 获得了凤凰技能");

        // 为每个玩家随机生成复活几率（20-40%）
        int reviveChance = _random.Next(MIN_REVIVE_CHANCE, MAX_REVIVE_CHANCE + 1);
        _playerReviveChance[player.Slot] = reviveChance;

        player.PrintToChat("🔥 你获得了凤凰技能！");
        player.PrintToChat($"💀 死亡后有 {reviveChance}% 几率以 {REVIVE_HEALTH} 血复活！");
        player.PrintToChat("⚠️ 每回合只能使用一次！护甲会保留！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        // 移除技能时清理记录
        _phoenixUsed.TryRemove(player.Slot, out _);
        _playerArmor.TryRemove(player.Slot, out _);
        _playerReviveChance.TryRemove(player.Slot, out _);

        Console.WriteLine($"[凤凰] {player.PlayerName} 失去了凤凰技能");
    }

    /// <summary>
    /// 处理玩家受伤事件
    /// </summary>
    public static void HandlePlayerHurt(EventPlayerHurt @event)
    {
        var victim = @event.Userid;
        if (victim == null || !victim.IsValid)
            return;

        var victimPawn = victim.PlayerPawn.Value;
        if (victimPawn == null || !victimPawn.IsValid)
            return;

        // 检查玩家是否有凤凰技能
        var skillManager = MyrtleSkill.Instance?.SkillManager;
        if (skillManager == null)
            return;

        // 获取玩家的所有技能
        var skills = skillManager.GetPlayerSkills(victim);
        if (skills.Count == 0)
            return;

        // 检查是否有凤凰技能
        var phoenixSkill = skills.FirstOrDefault(s => s.Name == "Phoenix");
        if (phoenixSkill == null)
            return;

        // 检查是否死亡（血量 <= 0）且还没使用过凤凰复活
        if (victimPawn.Health > 0 || _phoenixUsed.ContainsKey(victim.Slot))
            return;

        // 获取玩家的复活几率
        if (!_playerReviveChance.TryGetValue(victim.Slot, out int reviveChance))
        {
            reviveChance = _random.Next(MIN_REVIVE_CHANCE, MAX_REVIVE_CHANCE + 1);
            _playerReviveChance[victim.Slot] = reviveChance;
        }

        // 检查是否触发复活（20-40%几率）
        int roll = _random.Next(1, 101); // 1-100
        if (roll > reviveChance)
        {
            Console.WriteLine($"[凤凰] {victim.PlayerName} 死亡，复活失败（需要 {reviveChance}%，掷出 {roll}%）");
            victim.PrintToChat($"🔥 凤凰未能重生...（需要 {reviveChance}%，掷出 {roll}%）");
            return;
        }

        Console.WriteLine($"[凤凰] {victim.PlayerName} 死亡，触发凤凰复活（需要 {reviveChance}%，掷出 {roll}%）");

        // 保存当前护甲值
        int currentArmor = victimPawn.ArmorValue;
        _playerArmor[victim.Slot] = currentArmor;

        // 标记已使用
        _phoenixUsed.TryAdd(victim.Slot, 0);

        // 复活时只设置血量，不影响护甲
        SetHealthOnly(victim, REVIVE_HEALTH);

        // 恢复护甲
        if (currentArmor > 0)
        {
            victimPawn.ArmorValue = currentArmor;
            Utilities.SetStateChanged(victimPawn, "CCSPlayerPawn", "m_ArmorValue");
            Console.WriteLine($"[凤凰] {victim.PlayerName} 恢复护甲: {currentArmor}");
        }

        var spawn = GetSpawnVector(victim);
        if (spawn != null)
        {
            victimPawn.Teleport(spawn, victimPawn.AbsRotation, new Vector(0, 0, 0));
        }

        // 显示提示
        victim.PrintToCenter($"🔥 凤凰涅槃！({roll}% ≤ {reviveChance}%)");
        victim.PrintToChat($"🔥 凤凰涅槃！以 {REVIVE_HEALTH} 血复活！护甲已保留！");

        Server.PrintToChatAll($"🔥 {victim.PlayerName} 凤凰涅槃！({reviveChance}% 成功复活！");
    }

    /// <summary>
    /// 回合开始时清理使用记录
    /// </summary>
    public static void OnRoundStart()
    {
        _phoenixUsed.Clear();
        _playerArmor.Clear();

        // 重新生成所有玩家的复活几率
        foreach (var slot in _playerReviveChance.Keys.ToList())
        {
            int newReviveChance = _random.Next(MIN_REVIVE_CHANCE, MAX_REVIVE_CHANCE + 1);
            _playerReviveChance[slot] = newReviveChance;
        }

        Console.WriteLine("[凤凰] 新回合开始，清空使用记录并重新生成复活几率");
    }

    /// <summary>
    /// 只设置玩家血量，不影响护甲
    /// </summary>
    private static void SetHealthOnly(CCSPlayerController player, int health)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        pawn.Health = health;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        Console.WriteLine($"[凤凰] {player.PlayerName} 血量设置为 {health}，护甲保持不变");
    }

    /// <summary>
    /// 获取出生点位置
    /// </summary>
    private static Vector? GetSpawnVector(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return null;

        var absOrigin = pawn.AbsOrigin;

        // 根据队伍选择出生点
        string spawnPointName = player.Team == CsTeam.Terrorist
            ? "info_player_terrorist"
            : "info_player_counterterrorist";

        var spawns = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>(spawnPointName).ToList();
        if (spawns.Count != 0)
        {
            var random = new Random();
            var randomSpawn = spawns[random.Next(spawns.Count)];
            return randomSpawn.AbsOrigin;
        }

        // 如果找不到出生点，返回当前位置
        return absOrigin;
    }
}
