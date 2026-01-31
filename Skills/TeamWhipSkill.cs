using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 鞭策队友技能 - 射击队友可以治疗他们
/// </summary>
public class TeamWhipSkill : PlayerSkill
{
    public override string Name => "TeamWhip";
    public override string DisplayName => "💉 鞭策队友";
    public override string Description => "射击队友可以治疗他们！伤害转化为治疗量！";
    public override bool IsActive => false; // 被动技能

    // 治疗倍数（1.0 = 100%伤害转化为治疗）
    private const float HEAL_MULTIPLIER = 1.0f;

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[鞭策队友] {player.PlayerName} 获得了鞭策队友技能");
        player.PrintToChat("💉 你获得了鞭策队友技能！");
        player.PrintToChat("💡 射击队友可以治疗他们！");
        player.PrintToChat("⚠️ 伤害量100%转化为治疗量！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        Console.WriteLine($"[鞭策队友] {player.PlayerName} 失去了鞭策队友技能");
    }

    /// <summary>
    /// 处理玩家受伤事件
    /// </summary>
    public static void HandlePlayerHurt(EventPlayerHurt @event)
    {
        var damage = @event.DmgHealth;
        var victim = @event.Userid;
        var attacker = @event.Attacker;
        var weapon = @event.Weapon;

        // 忽略手雷伤害
        if (weapon.Contains("grenade") || weapon.Contains("inferno") || weapon.Contains("flashbang") || weapon.Contains("smoke") || weapon.Contains("decoy"))
            return;

        // 检查有效性
        if (victim == null || !victim.IsValid || victim.PlayerPawn.Value == null)
            return;

        if (attacker == null || !attacker.IsValid || attacker == victim)
            return;

        var victimPawn = victim.PlayerPawn.Value;
        if (victimPawn == null || !victimPawn.IsValid)
            return;

        // 检查是否是队友
        if (attacker.Team != victim.Team)
            return;

        // 获取技能管理器（需要从MyrtleSkill实例获取）
        var plugin = MyrtleSkillPlugin;
        if (plugin?.SkillManager == null)
            return;

        // 检查攻击者是否有鞭策队友技能
        var attackerSkill = plugin.SkillManager.GetPlayerSkill(attacker);
        if (attackerSkill == null || attackerSkill.Name != "TeamWhip")
            return;

        // 计算治疗量
        int healAmount = (int)(damage * HEAL_MULTIPLIER);

        // 获取当前血量和最大血量
        int currentHealth = victimPawn.Health;
        int maxHealth = victimPawn.MaxHealth;

        // 如果当前血量已经大于等于最大血量，不治疗
        if (currentHealth >= maxHealth)
        {
            Console.WriteLine($"[鞭策队友] {victim.PlayerName} 血量已满 ({currentHealth}/{maxHealth})，跳过治疗");
            return;
        }

        // 添加血量（不会超过最大值）
        AddHealth(victimPawn, healAmount, maxHealth);

        Console.WriteLine($"[鞭策队友] {attacker.PlayerName} 射击了队友 {victim.PlayerName}，治疗 {healAmount} 点血");

        // 显示提示
        attacker.PrintToChat($"💉 治疗了 {victim.PlayerName} +{healAmount} HP");
        victim.PrintToChat($"💉 被 {attacker.PlayerName} 鞭策治疗 +{healAmount} HP");
    }

    /// <summary>
    /// 添加血量（不超过最大值）
    /// </summary>
    private static void AddHealth(CCSPlayerPawn pawn, int amount, int maxHealth)
    {
        if (pawn == null || !pawn.IsValid)
            return;

        int currentHealth = pawn.Health;
        int newHealth = Math.Min(currentHealth + amount, maxHealth);

        pawn.Health = newHealth;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        // 显示血量增加效果
        if (amount > 0)
        {
            // 可以在这里添加粒子效果或其他视觉效果
        }
    }

    // 插件实例引用（需要在MyrtleSkill中设置）
    public static MyrtleSkill? MyrtleSkillPlugin { get; set; }
}
