// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace MyrtleSkill.Skills;

/// <summary>
/// 剑圣技能 - 被动技能
/// 手持刀具时，有很高几率格挡射击
/// </summary>
public class BladeMasterSkill : PlayerSkill
{
    public override string Name => "BladeMaster";
    public override string DisplayName => "⚔️ 剑圣";
    public override string Description => "手持刀具时，躯干部位 95% 几率格挡射击，腿部 80% 几率格挡！移动速度 15% 提升！";
    public override bool IsActive => false; // 被动技能

    // 格挡概率
    private const float TORSO_BLOCK_CHANCE = 0.95f;  // 躯干格挡概率 95%
    private const float LEG_BLOCK_CHANCE = 0.80f;    // 腿部格挡概率 80%
    private const float VELOCITY_MODIFIER = 0.85f;   // 移动速度修正（1.0/0.85 ≈ 1.15 = +15%速度）

    // 排除的武器（这些武器不能被格挡）
    private static readonly string[] ExcludedWeapons =
    {
        "weapon_inferno",      // 燃烧瓶
        "weapon_flashbang",    // 闪光弹
        "weapon_smokegrenade", // 烟雾弹
        "weapon_decoy",        // 诱饵弹
        "weapon_hegrenade",    // 高爆手雷
        "weapon_knife",        // 刀具
        "weapon_taser"         // 电击枪
    };

    // 跟踪持有该技能的玩家
    private readonly HashSet<ulong> _activePlayers = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        _activePlayers.Add(player.SteamID);

        Console.WriteLine($"[剑圣] {player.PlayerName} 获得了剑圣技能");

        player.PrintToChat("⚔️ 你获得了剑圣技能！");
        player.PrintToChat("💡 手持刀具时有很高几率格挡射击！");
        player.PrintToChat("💡 移动速度提升 15%！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        _activePlayers.Remove(player.SteamID);

        // 恢复移动速度
        if (player.PlayerPawn.Value is CCSPlayerPawn pawn && pawn.IsValid)
        {
            pawn.VelocityModifier = 1.0f;
            Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_flVelocityModifier");
        }

        Console.WriteLine($"[剑圣] {player.PlayerName} 失去了剑圣技能");
    }

    /// <summary>
    /// 每帧更新（处理移动速度修正）
    /// </summary>
    public void OnTick(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        // 只对持有该技能的玩家生效
        if (!_activePlayers.Contains(player.SteamID))
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 检查是否持有刀具
        var activeWeapon = pawn.WeaponServices?.ActiveWeapon.Value;
        if (activeWeapon == null || !activeWeapon.IsValid)
            return;

        var weaponName = activeWeapon.DesignerName;
        if (!weaponName.Contains("knife"))
        {
            // 如果没有持有刀具，恢复移动速度
            if (pawn.VelocityModifier != 1.0f)
            {
                pawn.VelocityModifier = 1.0f;
                Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_flVelocityModifier");
            }
            return;
        }

        // 持有刀具时应用移动速度修正
        if (pawn.VelocityModifier != VELOCITY_MODIFIER)
        {
            pawn.VelocityModifier = VELOCITY_MODIFIER;
            Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_flVelocityModifier");
        }
    }

    /// <summary>
    /// 处理玩家受伤事件 - 格挡逻辑
    /// </summary>
    public void HandlePlayerHurt(EventPlayerHurt @event, PlayerSkillManager skillManager)
    {
        if (@event == null)
            return;

        var victim = @event.Userid;
        if (victim == null || !victim.IsValid)
            return;

        // 检查受害者是否有剑圣技能
        if (!_activePlayers.Contains(victim.SteamID))
            return;

        var pawn = victim.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 检查是否持有刀具
        var activeWeapon = pawn.WeaponServices?.ActiveWeapon.Value;
        if (activeWeapon == null || !activeWeapon.IsValid)
            return;

        var weaponName = activeWeapon.DesignerName;
        if (!weaponName.Contains("knife"))
            return;

        // 检查攻击者使用的武器
        var attacker = @event.Attacker;
        if (attacker == null || !attacker.IsValid)
            return;

        var attackerWeapon = attacker.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
        if (attackerWeapon == null || !attackerWeapon.IsValid)
            return;

        var attackerWeaponName = attackerWeapon.DesignerName;

        // 检查是否是排除的武器
        foreach (var excluded in ExcludedWeapons)
        {
            if (attackerWeaponName.Contains(excluded))
                return;
        }

        // 获取命中部位
        var hitgroup = @event.Hitgroup;

        // 根据命中部位决定格挡概率
        float blockChance;
        string hitLocation;

        switch (hitgroup)
        {
            case (int)HitGroup_t.HITGROUP_LEFTLEG:
            case (int)HitGroup_t.HITGROUP_RIGHTLEG:
                blockChance = LEG_BLOCK_CHANCE;
                hitLocation = "腿部";
                break;

            default:
                // 躯干部位（头部、胸部、腹部、手臂）
                blockChance = TORSO_BLOCK_CHANCE;
                hitLocation = "躯干";
                break;
        }

        // 随机判定是否格挡
        var random = new Random().NextDouble();
        if (random > blockChance)
            return; // 格挡失败

        // 格挡成功 - 恢复生命值
        int damage = @event.DmgHealth;
        int newHealth = pawn.Health + damage;
        int maxHealth = pawn.MaxHealth;

        // 不超过最大生命值
        if (newHealth > maxHealth)
            newHealth = maxHealth;

        pawn.Health = newHealth;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        Console.WriteLine($"[剑圣] {victim.PlayerName} 格挡了 {attacker.PlayerName} 的射击 ({hitLocation})，恢复 {damage} 点生命值");

        // 显示格挡提示
        victim.PrintToCenter($"⚔️ 格挡成功！\n+{damage} HP");
    }
}
