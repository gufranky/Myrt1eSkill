using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 敌人旋转技能 - 攻击敌人时有40%几率使其旋转180度
/// </summary>
public class EnemySpinSkill : PlayerSkill
{
    public override string Name => "EnemySpin";
    public override string DisplayName => "🔄 敌人旋转";
    public override string Description => "攻击敌人时有40%几率使其旋转180度！让敌人迷失方向！";
    public override bool IsActive => false; // 被动技能

    // 旋转概率（40%）
    private const float SPIN_CHANCE = 0.4f;

    // 旋转角度（180度）
    private const float SPIN_ANGLE = 180.0f;

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[敌人旋转] {player.PlayerName} 获得了敌人旋转技能");
        player.PrintToChat("🔄 你获得了敌人旋转技能！");
        player.PrintToChat($"💡 攻击敌人时有{SPIN_CHANCE * 100:F0}%几率使其旋转180度！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        Console.WriteLine($"[敌人旋转] {player.PlayerName} 失去了敌人旋转技能");
    }

    /// <summary>
    /// 处理玩家受伤事件
    /// </summary>
    public static void HandlePlayerHurt(EventPlayerHurt @event, PlayerSkillManager skillManager)
    {
        var attacker = @event.Attacker;
        var victim = @event.Userid;

        if (attacker == null || !attacker.IsValid)
            return;

        if (victim == null || !victim.IsValid || attacker == victim)
            return;

        // 检查攻击者是否有敌人旋转技能（修复：检查所有技能）
        var attackerSkills = skillManager.GetPlayerSkills(attacker);
        if (attackerSkills.Count == 0)
            return;

        var enemySpinSkill = attackerSkills.FirstOrDefault(s => s.Name == "EnemySpin");
        if (enemySpinSkill == null)
            return;

        // 检查受害者是否存活
        if (!victim.PawnIsAlive)
            return;

        // 40%概率触发旋转
        if (_staticRandom.NextDouble() >= SPIN_CHANCE)
            return;

        Console.WriteLine($"[敌人旋转] {attacker.PlayerName} 的攻击触发了旋转效果，目标：{victim.PlayerName}");

        // 旋转敌人180度
        RotateEnemy(victim);

        attacker.PrintToChat($"🔄 你让 {victim.PlayerName} 旋转了180度！");
        victim.PrintToChat($"🔄 被 {attacker.PlayerName} 的攻击导致旋转180度！");
    }

    /// <summary>
    /// 旋转敌人180度
    /// </summary>
    private static void RotateEnemy(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.LifeState != (int)LifeState_t.LIFE_ALIVE)
            return;

        // 获取当前位置和角度
        var currentPosition = pawn.AbsOrigin;
        var currentAngles = pawn.EyeAngles;

        // 创建新角度（Y轴旋转180度）
        QAngle newAngles = new(
            currentAngles.X,
            currentAngles.Y + SPIN_ANGLE,
            currentAngles.Z
        );

        // 传送（保持位置，只改变角度）
        pawn.Teleport(currentPosition, newAngles, new Vector(0, 0, 0));

        Console.WriteLine($"[敌人旋转] {player.PlayerName} 旋转了180度");
    }

    // 静态随机数生成器（用于HandlePlayerHurt静态方法中）
    private static readonly Random _staticRandom = new();
}
