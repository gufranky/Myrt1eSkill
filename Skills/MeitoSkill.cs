using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;

namespace MyrtleSkill.Skills;

/// <summary>
/// 名刀技能 - 致命伤害时取消伤害并短暂无敌
/// </summary>
public class MeitoSkill : PlayerSkill
{
    public override string Name => "Meito";
    public override string DisplayName => "⚔️ 名刀";
    public override string Description => "致命伤害时取消伤害并获得0.75秒无敌！每回合限用一次！";
    public override bool IsActive => false; // 被动技能

    // 与第二次机会互斥
    public override List<string> ExcludedSkills => new() { "SecondChance" };

    // 无敌持续时间（秒）
    private const float INVINCIBLE_DURATION = 0.75f;

    // 跟踪每回合已使用名刀的玩家
    private static readonly ConcurrentDictionary<int, byte> _meitoUsed = new();

    // 跟踪无敌状态到期的玩家
    private static readonly ConcurrentDictionary<int, DateTime> _invinciblePlayers = new();

    // 跟踪名刀恢复后的血量（用于无敌期间保护）
    private static readonly ConcurrentDictionary<int, int> _protectedHealth = new();

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[名刀] {player.PlayerName} 获得了名刀技能");
        player.PrintToChat("⚔️ 你获得了名刀技能！");
        player.PrintToChat("💡 致命伤害会被抵消并获得0.75秒无敌！");
        player.PrintToChat("⚠️ 每回合只能触发一次！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        // 移除技能时清理记录
        _meitoUsed.TryRemove(player.Slot, out _);
        _invinciblePlayers.TryRemove(player.Slot, out _);
        _protectedHealth.TryRemove(player.Slot, out _);

        Console.WriteLine($"[名刀] {player.PlayerName} 失去了名刀技能");
    }

    /// <summary>
    /// 处理玩家受伤事件（在伤害造成后触发）
    /// 如果血量<=0且本回合未使用过名刀，则恢复血量并给予无敌
    /// 参考第二次机会的实现，使用 EventPlayerHurt 而不是 OnPlayerTakeDamagePre
    /// </summary>
    public static void HandlePlayerHurt(EventPlayerHurt @event)
    {
        Console.WriteLine($"[名刀-DEBUG] HandlePlayerHurt 被调用");

        var victim = @event.Userid;
        if (victim == null || !victim.IsValid)
        {
            Console.WriteLine($"[名刀-DEBUG] victim 无效，返回");
            return;
        }

        var victimPawn = victim.PlayerPawn.Value;
        if (victimPawn == null || !victimPawn.IsValid)
        {
            Console.WriteLine($"[名刀-DEBUG] victimPawn 无效，返回");
            return;
        }

        Console.WriteLine($"[名刀-DEBUG] 玩家: {victim.PlayerName}, 当前血量: {victimPawn.Health}");

        // 检查玩家是否有名刀技能
        var plugin = MyrtleSkill.Instance;
        if (plugin?.SkillManager == null)
        {
            Console.WriteLine($"[名刀-DEBUG] plugin 或 SkillManager 为 null，返回");
            return;
        }

        // 获取玩家的所有技能（修复：检查所有技能，而不是只检查第一个）
        var skills = plugin.SkillManager.GetPlayerSkills(victim);
        if (skills.Count == 0)
        {
            Console.WriteLine($"[名刀-DEBUG] {victim.PlayerName} 没有技能，返回");
            return;
        }

        Console.WriteLine($"[名刀-DEBUG] {victim.PlayerName} 拥有 {skills.Count} 个技能");

        // 检查是否有名刀技能
        var meitoSkill = skills.FirstOrDefault(s => s.Name == "Meito");
        if (meitoSkill == null)
        {
            Console.WriteLine($"[名刀-DEBUG] {victim.PlayerName} 的技能中没有名刀");
            foreach (var s in skills)
            {
                Console.WriteLine($"[名刀-DEBUG]   - {s.Name}: {s.DisplayName}");
            }
            return;
        }

        Console.WriteLine($"[名刀-DEBUG] {victim.PlayerName} 确认有名刀技能");

        // 检查是否在无敌状态
        if (_invinciblePlayers.ContainsKey(victim.Slot))
        {
            var invincibleExpireTime = _invinciblePlayers[victim.Slot];
            var timeRemaining = (invincibleExpireTime - DateTime.Now).TotalSeconds;

            Console.WriteLine($"[名刀-DEBUG] {victim.PlayerName} 在无敌状态中，剩余 {timeRemaining:F2} 秒");

            if (DateTime.Now < invincibleExpireTime)
            {
                // 无敌状态中，恢复到名刀保存的血量
                if (victimPawn.Health <= 0)
                {
                    if (_protectedHealth.TryGetValue(victim.Slot, out int savedHealth))
                    {
                        victimPawn.Health = savedHealth;
                        Utilities.SetStateChanged(victimPawn, "CBaseEntity", "m_iHealth");
                        Console.WriteLine($"[名刀] {victim.PlayerName} 处于无敌状态，血量恢复为 {savedHealth}");
                    }
                }
                return;
            }
            else
            {
                // 无敌状态已过期，清理
                Console.WriteLine($"[名刀-DEBUG] {victim.PlayerName} 的无敌状态已过期，清理");
                _invinciblePlayers.TryRemove(victim.Slot, out _);
            }
        }

        // 检查本回合是否已使用过名刀
        if (_meitoUsed.ContainsKey(victim.Slot))
        {
            Console.WriteLine($"[名刀-DEBUG] {victim.PlayerName} 本回合已使用过名刀，返回");
            return;
        }

        Console.WriteLine($"[名刀-DEBUG] {victim.PlayerName} 本回合未使用过名刀");

        // 检查是否死亡（血量 <= 0）
        if (victimPawn.Health > 0)
        {
            Console.WriteLine($"[名刀-DEBUG] {victim.PlayerName} 未死亡（血量: {victimPawn.Health}），返回");
            return;
        }

        Console.WriteLine($"[名刀-DEBUG] {victim.PlayerName} 死亡！触发名刀复活！");

        // 获取受伤害前的血量（EventPlayerHurt.DmgHealth是实际伤害值）
        int damageTaken = @event.DmgHealth;
        int healthBeforeDeath = victimPawn.Health + damageTaken;

        Console.WriteLine($"[名刀-DEBUG] {victim.PlayerName} 受到 {damageTaken} 伤害，死亡前血量: {healthBeforeDeath}");

        // 标记本回合已使用
        _meitoUsed.TryAdd(victim.Slot, 0);
        Console.WriteLine($"[名刀-DEBUG] 已标记 {victim.PlayerName} 本回合使用过名刀");

        // 恢复血量
        victimPawn.Health = healthBeforeDeath;
        Utilities.SetStateChanged(victimPawn, "CBaseEntity", "m_iHealth");
        Console.WriteLine($"[名刀] {victim.PlayerName} 名刀触发！血量恢复为 {healthBeforeDeath}");

        // 保存恢复后的血量（用于无敌期间保护）
        _protectedHealth[victim.Slot] = healthBeforeDeath;

        // 设置无敌状态（0.5秒）
        DateTime expireTime = DateTime.Now.AddSeconds(INVINCIBLE_DURATION);
        _invinciblePlayers[victim.Slot] = expireTime;
        Console.WriteLine($"[名刀-DEBUG] 设置 {victim.PlayerName} 无敌到 {expireTime:HH:mm:ss.fff}");

        // 显示提示
        victim.PrintToCenter("⚔️ 名刀御守！");
        victim.PrintToChat($"⚔️ 名刀抵消了致命伤害！恢复 {healthBeforeDeath} 血！获得 {INVINCIBLE_DURATION} 秒无敌！");

        Server.PrintToChatAll($"⚔️ {victim.PlayerName} 使用了名刀！");
    }

    /// <summary>
    /// 处理玩家死亡事件
    /// 清理状态并显示名刀使用信息
    /// </summary>
    public static void HandlePlayerDeath(EventPlayerDeath @event)
    {
        var victim = @event.Userid;
        if (victim == null || !victim.IsValid)
            return;

        // 检查玩家是否有名刀技能
        var plugin = MyrtleSkill.Instance;
        if (plugin?.SkillManager == null)
            return;

        // 获取玩家的所有技能（修复：检查所有技能，而不是只检查第一个）
        var skills = plugin.SkillManager.GetPlayerSkills(victim);
        if (skills.Count == 0)
            return;

        // 检查是否有名刀技能
        var meitoSkill = skills.FirstOrDefault(s => s.Name == "Meito");
        if (meitoSkill == null)
            return;

        // 检查本回合是否触发过名刀
        bool usedMeito = _meitoUsed.ContainsKey(victim.Slot);

        // 清理玩家的无敌状态（虽然已经死亡，但为了保持数据一致性）
        _invinciblePlayers.TryRemove(victim.Slot, out _);

        // 显示死亡提示
        if (usedMeito)
        {
            Console.WriteLine($"[名刀] {victim.PlayerName} 死亡（本回合已触发过名刀）");
            victim.PrintToChat("⚔️ 你本回合已使用过名刀，但最终仍战死沙场！");
        }
        else
        {
            Console.WriteLine($"[名刀] {victim.PlayerName} 死亡（本回合未触发名刀）");
        }
    }

    /// <summary>
    /// 回合开始时清理使用记录
    /// </summary>
    public static void OnRoundStart()
    {
        _meitoUsed.Clear();
        _invinciblePlayers.Clear();
        _protectedHealth.Clear();
        Console.WriteLine("[名刀] 新回合开始，清空使用记录");
    }
}
