// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills and MeitoSkill implementation

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;

namespace MyrtleSkill.Skills;

/// <summary>
/// 杀人无敌技能 - 被动技能
/// 击杀一个敌人后可以获得2秒的无敌时间
/// </summary>
public class KillInvincibilitySkill : PlayerSkill
{
    public override string Name => "KillInvincibility";
    public override string DisplayName => "💀 杀人无敌";
    public override string Description => "击杀敌人后获得2秒无敌！连续击杀可以刷新无敌时间！";
    public override bool IsActive => false; // 被动技能

    // 无敌持续时间（秒）
    private const float INVINCIBLE_DURATION = 2.0f;

    // 跟踪无敌状态到期的玩家
    private static readonly ConcurrentDictionary<int, DateTime> _invinciblePlayers = new();

    // 跟踪无敌期间保护的血量
    private static readonly ConcurrentDictionary<int, int> _protectedHealth = new();

    // 跟踪击杀数量（用于统计）
    private static readonly ConcurrentDictionary<int, int> _killCounts = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[杀人无敌] {player.PlayerName} 获得了杀人无敌技能");
        player.PrintToChat("💀 你获得了杀人无敌技能！");
        player.PrintToChat("💡 击杀敌人后获得2秒无敌！");
        player.PrintToChat("🔄 连续击杀可以刷新无敌时间！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 移除技能时清理记录
        _invinciblePlayers.TryRemove(player.Slot, out _);
        _protectedHealth.TryRemove(player.Slot, out _);
        _killCounts.TryRemove(player.Slot, out _);

        Console.WriteLine($"[杀人无敌] {player.PlayerName} 失去了杀人无敌技能");
    }

    /// <summary>
    /// 处理玩家死亡事件（检查击杀者）
    /// </summary>
    public static void HandlePlayerDeath(EventPlayerDeath @event)
    {
        var attacker = @event.Attacker;
        if (attacker == null || !attacker.IsValid)
            return;

        var victim = @event.Userid;
        if (victim == null || !victim.IsValid)
            return;

        // 不能通过自杀/击杀队友获得无敌
        if (attacker.SteamID == victim.SteamID)
            return;

        var attackerPawn = attacker.PlayerPawn.Value;
        if (attackerPawn == null || !attackerPawn.IsValid)
            return;

        // 检查攻击者是否有杀人无敌技能
        var plugin = MyrtleSkill.Instance;
        if (plugin?.SkillManager == null)
            return;

        var skills = plugin.SkillManager.GetPlayerSkills(attacker);
        if (skills.Count == 0)
            return;

        var killInvincibilitySkill = skills.FirstOrDefault(s => s.Name == "KillInvincibility");
        if (killInvincibilitySkill == null)
            return;

        Console.WriteLine($"[杀人无敌] {attacker.PlayerName} 击杀了 {victim.PlayerName}");

        // 增加击杀计数
        _killCounts.AddOrUpdate(attacker.Slot, 1, (key, old) => old + 1);
        int killCount = _killCounts[attacker.Slot];

        // 设置或刷新无敌状态
        DateTime expireTime = DateTime.Now.AddSeconds(INVINCIBLE_DURATION);
        _invinciblePlayers.AddOrUpdate(attacker.Slot, expireTime, (key, old) => expireTime);

        // 保存当前血量（用于无敌期间保护）
        _protectedHealth.AddOrUpdate(attacker.Slot, attackerPawn.Health, (key, old) => attackerPawn.Health);

        Console.WriteLine($"[杀人无敌] {attacker.PlayerName} 获得 {INVINCIBLE_DURATION} 秒无敌（当前击杀数: {killCount}）");

        // 显示提示
        attacker.PrintToCenter($"💀 击杀无敌！{INVINCIBLE_DURATION}秒无敌！");
        attacker.PrintToChat($"💀 你击杀了 {victim.PlayerName}！获得 {INVINCIBLE_DURATION} 秒无敌！");

        // 如果连续击杀，显示特殊提示
        if (killCount >= 2)
        {
            Server.PrintToChatAll($"💀 {attacker.PlayerName} 连续击杀 {killCount} 人！保持无敌状态！");
        }
    }

    /// <summary>
    /// 处理玩家受伤事件（无敌期间保护）
    /// </summary>
    public static void HandlePlayerHurt(EventPlayerHurt @event)
    {
        var victim = @event.Userid;
        if (victim == null || !victim.IsValid)
            return;

        // 检查玩家是否在无敌状态
        if (!_invinciblePlayers.ContainsKey(victim.Slot))
            return;

        var invincibleExpireTime = _invinciblePlayers[victim.Slot];
        if (DateTime.Now >= invincibleExpireTime)
        {
            // 无敌状态已过期，清理
            _invinciblePlayers.TryRemove(victim.Slot, out _);
            _protectedHealth.TryRemove(victim.Slot, out _);
            return;
        }

        var victimPawn = victim.PlayerPawn.Value;
        if (victimPawn == null || !victimPawn.IsValid)
            return;

        // 无敌状态中，恢复到保存的血量
        if (_protectedHealth.TryGetValue(victim.Slot, out int savedHealth))
        {
            // 只有在血量低于保存值时才恢复
            if (victimPawn.Health < savedHealth)
            {
                victimPawn.Health = savedHealth;
                Utilities.SetStateChanged(victimPawn, "CBaseEntity", "m_iHealth");

                var timeRemaining = (invincibleExpireTime - DateTime.Now).TotalSeconds;
                Console.WriteLine($"[杀人无敌] {victim.PlayerName} 处于无敌状态，血量恢复为 {savedHealth}（剩余 {timeRemaining:F2}s）");
            }
        }
    }

    /// <summary>
    /// 回合开始时清理所有记录
    /// </summary>
    public static void OnRoundStart()
    {
        _invinciblePlayers.Clear();
        _protectedHealth.Clear();
        _killCounts.Clear();
        Console.WriteLine("[杀人无敌] 新回合开始，清空所有记录");
    }

    /// <summary>
    /// 每帧更新（清理过期的无敌状态）
    /// </summary>
    public static void OnTick()
    {
        var currentTime = DateTime.Now;
        var expiredSlots = new List<int>();

        foreach (var kvp in _invinciblePlayers)
        {
            if (currentTime >= kvp.Value)
            {
                expiredSlots.Add(kvp.Key);
            }
        }

        foreach (var slot in expiredSlots)
        {
            _invinciblePlayers.TryRemove(slot, out _);
            _protectedHealth.TryRemove(slot, out _);

            // 找到玩家并通知无敌结束
            var player = Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && p.Slot == slot);
            if (player != null)
            {
                var remainingKills = _killCounts.TryGetValue(slot, out int kills) ? kills : 0;
                if (remainingKills > 0)
                {
                    player.PrintToChat($"💀 无敌时间结束！本回合击杀数: {remainingKills}");
                }
            }
        }
    }
}
