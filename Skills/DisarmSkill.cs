using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 裁军技能 - 击中敌人时有一定几率使其掉落武器
/// </summary>
public class DisarmSkill : PlayerSkill
{
    public override string Name => "Disarm";
    public override string DisplayName => "✂ 裁军";
    public override string Description => "击中敌人时有30%几率使其掉落武器！";
    public override bool IsActive => false; // 被动技能

    // 掉落武器概率（30%）
    private const float DISARM_CHANCE = 0.3f;

    // 随机数生成器（静态，用于HandlePlayerHurt静态方法）
    private static readonly Random _staticRandom = new();

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[裁军] {player.PlayerName} 获得了裁军技能");
        player.PrintToChat("✂ 你获得了裁军技能！");
        player.PrintToChat($"💡 攻击敌人时有{DISARM_CHANCE * 100:F0}%几率使其掉落武器！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        Console.WriteLine($"[裁军] {player.PlayerName} 失去了裁军技能");
    }

    /// <summary>
    /// 处理玩家受伤事件
    /// </summary>
    public static void HandlePlayerHurt(EventPlayerHurt @event, PlayerSkillManager skillManager)
    {
        var attacker = @event.Attacker;
        var victim = @event.Userid;
        var weapon = @event.Weapon;

        if (attacker == null || !attacker.IsValid)
            return;

        if (victim == null || !victim.IsValid || attacker == victim)
            return;

        // 忽略手雷伤害
        if (weapon.Contains("grenade") || weapon.Contains("inferno") ||
            weapon.Contains("flashbang") || weapon.Contains("smoke") ||
            weapon.Contains("decoy"))
            return;

        // 检查攻击者是否有裁军技能
        var skill = skillManager.GetPlayerSkill(attacker);
        if (skill == null || skill.Name != "Disarm")
            return;

        // 检查受害者是否存活
        if (!victim.PawnIsAlive)
            return;

        // 30%概率触发裁军
        if (_staticRandom.NextDouble() >= DISARM_CHANCE)
            return;

        Console.WriteLine($"[裁军] {attacker.PlayerName} 的攻击触发了裁军效果，目标：{victim.PlayerName}");

        // 移除敌人所有武器
        RemoveAllWeapons(victim);

        attacker.PrintToChat($"✂ 你让 {victim.PlayerName} 掉落了武器！");
        victim.PrintToChat($"✂ 你被 {attacker.PlayerName} 裁掉了武器！");
    }

    /// <summary>
    /// 移除玩家的所有武器
    /// </summary>
    private static void RemoveAllWeapons(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null)
            return;

        // 移除所有武器
        foreach (var weaponHandle in weaponServices.MyWeapons)
        {
            if (weaponHandle.IsValid)
            {
                var weapon = weaponHandle.Get();
                if (weapon != null && weapon.IsValid)
                {
                    weapon.Remove();
                }
            }
        }

        Console.WriteLine($"[裁军] 移除了 {player.PlayerName} 的所有武器");
    }
}
