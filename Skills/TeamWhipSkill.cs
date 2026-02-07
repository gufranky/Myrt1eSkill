using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 鞭策队友技能 - 射击队友将伤害转化为治疗（取消伤害）
/// </summary>
public class TeamWhipSkill : PlayerSkill
{
    public override string Name => "TeamWhip";
    public override string DisplayName => "💉 鞭策队友";
    public override string Description => "射击队友将伤害转化为治疗量！不会造成友军伤害！";
    public override bool IsActive => false; // 被动技能

    // 治疗倍数（1.0 = 100%伤害转化为治疗）
    private const float HEAL_MULTIPLIER = 1.0f;

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[鞭策队友] {player.PlayerName} 获得了鞭策队友技能");

        // 禁用自动踢出，防止友军伤害被踢
        Server.ExecuteCommand("mp_autokick 0");

        player.PrintToChat("💉 你获得了鞭策队友技能！");
        player.PrintToChat("💡 射击队友可以治疗他们！");
        player.PrintToChat("⚠️ 伤害量100%转化为治疗量！不会造成友军伤害！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        Console.WriteLine($"[鞭策队友] {player.PlayerName} 失去了鞭策队友技能");
    }

    /// <summary>
    /// 在伤害造成前处理（Pre阶段）
    /// 如果攻击者有鞭策队友技能且受害者是队友，取消伤害并治疗
    /// </summary>
    public static void HandleDamagePre(CCSPlayerPawn victimPawn, CTakeDamageInfo info)
    {
        // 获取攻击者实体
        var attackerEntity = info.Attacker?.Value;
        if (attackerEntity == null || !attackerEntity.IsValid)
            return;

        // 转换为 PlayerPawn
        var attackerPawn = attackerEntity.As<CCSPlayerPawn>();
        if (attackerPawn == null || !attackerPawn.IsValid)
            return;

        var attackerController = attackerPawn.Controller.Value;
        if (attackerController == null || !attackerController.IsValid)
            return;

        // 检查受害者是否有效
        if (victimPawn == null || !victimPawn.IsValid)
            return;

        var victimController = victimPawn.Controller.Value;
        if (victimController == null || !victimController.IsValid)
            return;

        // 检查是否是队友
        if (attackerController.TeamNum != victimController.TeamNum)
            return;

        // 检查受害者是否存活
        if (victimPawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            return;

        // 转换为 CCSPlayerController
        if (attackerController is not CCSPlayerController csAttackerController)
            return;

        if (victimController is not CCSPlayerController csVictimController)
            return;

        // 获取技能管理器
        var plugin = MyrtleSkillPlugin;
        if (plugin?.SkillManager == null)
            return;

        // 检查攻击者是否有鞭策队友技能（修复：检查所有技能）
        var attackerSkills = plugin.SkillManager.GetPlayerSkills(csAttackerController);
        if (attackerSkills.Count == 0)
            return;

        var teamWhipSkill = attackerSkills.FirstOrDefault(s => s.Name == "TeamWhip");
        if (teamWhipSkill == null)
            return;

        // 获取伤害值（必须在修改info.Damage之前保存）
        float originalDamage = info.Damage;

        // 如果伤害为0，不做处理
        if (originalDamage <= 0)
            return;

        // 直接取消伤害（设置为0）
        info.Damage = 0;

        // 治疗队友（如果血量未满）
        if (victimPawn.Health < victimPawn.MaxHealth)
        {
            int healAmount = (int)(originalDamage * HEAL_MULTIPLIER);
            int currentHealth = (int)victimPawn.Health;
            AddHealth(victimPawn, healAmount, (int)victimPawn.MaxHealth);

            // 计算实际治疗量
            int actualHealed = (int)victimPawn.Health - currentHealth;

            Console.WriteLine($"[鞭策队友] {csAttackerController.PlayerName} 射击了队友 {csVictimController.PlayerName}，取消伤害 {originalDamage}，治疗 {actualHealed} HP");

            // 显示提示
            csAttackerController.PrintToChat($"💉 治疗了 {csVictimController.PlayerName} +{actualHealed} HP");
            csVictimController.PrintToChat($"💉 被 {csAttackerController.PlayerName} 鞭策治疗 +{actualHealed} HP");
        }
        else
        {
            Console.WriteLine($"[鞭策队友] {csVictimController.PlayerName} 血量已满 ({victimPawn.Health}/{victimPawn.MaxHealth})，取消伤害 {originalDamage}");
        }
    }

    /// <summary>
    /// 添加血量（不超过最大值）
    /// 参考 jRandomSkills SkillUtils.AddHealth
    /// </summary>
    private static void AddHealth(CCSPlayerPawn pawn, int amount, int maxHealth)
    {
        if (pawn == null || !pawn.IsValid)
            return;

        if (pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            return;

        int newHealth = (int)(pawn.Health + amount);
        pawn.Health = Math.Min(newHealth, maxHealth);
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        // 同时设置最大血量（与 jRandomSkills 一致）
        pawn.MaxHealth = maxHealth;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
    }

    // 插件实例引用（需要在MyrtleSkill中设置）
    public static MyrtleSkill? MyrtleSkillPlugin { get; set; }
}
