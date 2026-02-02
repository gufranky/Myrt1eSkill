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
    public override string Description => "致命伤害时取消伤害并获得0.5秒无敌！每回合限用一次！";
    public override bool IsActive => false; // 被动技能

    // 无敌持续时间（秒）
    private const float INVINCIBLE_DURATION = 0.5f;

    // 跟踪每回合已使用名刀的玩家
    private static readonly ConcurrentDictionary<int, byte> _meitoUsed = new();

    // 跟踪无敌状态到期的玩家
    private static readonly ConcurrentDictionary<int, DateTime> _invinciblePlayers = new();

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[名刀] {player.PlayerName} 获得了名刀技能");
        player.PrintToChat("⚔️ 你获得了名刀技能！");
        player.PrintToChat("💡 致命伤害会被抵消并获得0.5秒无敌！");
        player.PrintToChat("⚠️ 每回合只能触发一次！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        // 移除技能时清理记录
        _meitoUsed.TryRemove(player.Slot, out _);
        _invinciblePlayers.TryRemove(player.Slot, out _);

        Console.WriteLine($"[名刀] {player.PlayerName} 失去了名刀技能");
    }

    /// <summary>
    /// 在伤害造成前处理（Pre阶段）
    /// 如果伤害致命且本回合未使用过名刀，则取消伤害并给予无敌
    /// </summary>
    /// <summary>
    /// 在伤害造成前处理（Pre阶段）
    /// 如果伤害致命且本回合未使用过名刀，则取消伤害并给予无敌
    /// </summary>
    /// <summary>
    /// 在伤害造成前处理（Pre阶段）
    /// 如果伤害致命且本回合未使用过名刀，则取消伤害并给予无敌
    /// </summary>
    public static float? HandleDamagePre(CCSPlayerPawn player, CTakeDamageInfo info, float currentMultiplier = 1.0f)
    {
        // 获取受害者控制器
        var victimController = player.Controller.Value;
        if (victimController == null || !victimController.IsValid)
            return null;

        // 转换为 CCSPlayerController
        if (victimController is not CCSPlayerController csVictimController)
            return null;

        // 检查玩家是否有名刀技能
        var plugin = MyrtleSkill.Instance;
        if (plugin?.SkillManager == null)
            return null;

        var skill = plugin.SkillManager.GetPlayerSkill(csVictimController);
        if (skill == null || skill.Name != "Meito")
            return null;

        // 检查是否在无敌状态
        if (_invinciblePlayers.ContainsKey(csVictimController.Slot))
        {
            var invincibleExpireTime = _invinciblePlayers[csVictimController.Slot];
            if (DateTime.Now < invincibleExpireTime)
            {
                // 无敌状态中，取消所有伤害
                Console.WriteLine($"[名刀] {csVictimController.PlayerName} 处于无敌状态，取消伤害");
                return 0.0f;
            }
            else
            {
                // 无敌状态已过期，清理
                _invinciblePlayers.TryRemove(csVictimController.Slot, out _);
            }
        }

        // 检查本回合是否已使用过名刀
        if (_meitoUsed.ContainsKey(csVictimController.Slot))
            return null;

        // 获取伤害值
        float damage = info.Damage;
        if (damage <= 0)
            return null;

        // 获取当前血量
        int currentHealth = player.Health;

        // 计算应用倍数后的实际伤害
        float actualDamage = damage * currentMultiplier;

        // 检查伤害是否致命（当前血量 - 实际伤害 <= 0）
        if (currentHealth - actualDamage > 0)
            return null; // 不是致命伤害，不处理

        Console.WriteLine($"[名刀] {csVictimController.PlayerName} 受到致命伤害 (血量:{currentHealth} 原始伤害:{damage} 倍数:{currentMultiplier} 实际:{actualDamage})，触发名刀效果");

        // 标记本回合已使用
        _meitoUsed.TryAdd(csVictimController.Slot, 0);

        // 设置无敌状态
        DateTime expireTime = DateTime.Now.AddSeconds(INVINCIBLE_DURATION);
        _invinciblePlayers[csVictimController.Slot] = expireTime;

        // 取消此次伤害
        Console.WriteLine($"[名刀] {csVictimController.PlayerName} 取消了致命伤害，获得 {INVINCIBLE_DURATION} 秒无敌");

        // 显示提示
        csVictimController.PrintToCenter("⚔️ 名刀御守！");
        csVictimController.PrintToChat($"⚔️ 名刀抵消了致命伤害！获得 {INVINCIBLE_DURATION} 秒无敌！");

        // 返回0倍数，完全取消伤害
        return 0.0f;
    }

    /// <summary>
    /// 回合开始时清理使用记录
    /// </summary>
    public static void OnRoundStart()
    {
        _meitoUsed.Clear();
        _invinciblePlayers.Clear();
        Console.WriteLine("[名刀] 新回合开始，清空使用记录");
    }
}
