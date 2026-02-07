// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills by Juzlus (Push skill)
// Complete replication of the original implementation

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 推手技能 - 被动技能
/// 攻击敌人时有一定几率将其击退
/// 完全复制自 jRandomSkills Push 技能
/// </summary>
public class PushSkill : PlayerSkill
{
    public override string Name => "Push";
    public override string DisplayName => "✋ 推手";
    public override string Description => "攻击敌人时有一定几率将其击退！";
    public override bool IsActive => false; // 被动技能

    // 与其他移动技能互斥
    public override List<string> ExcludedSkills => new() { "HeavyArmor", "Sprint" };

    // 推力参数（与 jRandomSkills 保持一致）
    private const float CHANCE_FROM = 0.3f;  // 30%
    private const float CHANCE_TO = 0.4f;    // 40%
    private const float JUMP_VELOCITY = 300f;  // 向上速度
    private const float PUSH_VELOCITY = 400f;   // 推力速度

    // 每个玩家的随机几率（技能分配时生成）
    private readonly Dictionary<ulong, float> _playerChances = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 为玩家随机分配一个几率（30% ~ 40%）
        float chance = (float)(new Random().NextDouble() * (CHANCE_TO - CHANCE_FROM)) + CHANCE_FROM;
        _playerChances[player.SteamID] = chance;

        Console.WriteLine($"[推手] {player.PlayerName} 获得了推手技能，几率: {chance * 100:F1}%");

        player.PrintToChat("✋ 你获得了推手技能！");
        player.PrintToChat($"💡 攻击敌人时有{chance * 100:F0}%几率将其击退！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        _playerChances.Remove(player.SteamID);

        Console.WriteLine($"[推手] {player.PlayerName} 失去了推手技能");
    }

    /// <summary>
    /// 处理玩家受伤事件（在主文件的 OnPlayerHurt 中调用）
    /// 完全复制自 jRandomSkills 的 PlayerHurt 实现
    /// </summary>
    public void HandlePlayerHurt(EventPlayerHurt @event)
    {
        var attacker = @event.Attacker;
        var victim = @event.Userid;

        if (attacker == null || !attacker.IsValid || !attacker.PawnIsAlive)
            return;

        if (victim == null || !victim.IsValid || !victim.PawnIsAlive)
            return;

        // 不能是同一个人
        if (attacker == victim)
            return;

        // 获取攻击者的技能几率
        if (!_playerChances.TryGetValue(attacker.SteamID, out float skillChance))
            return;

        // 概率检查
        var random = new Random();
        if (random.NextDouble() > skillChance)
            return;

        // 执行击退
        PushEnemy(victim, attacker.PlayerPawn.Value!.EyeAngles);

        Console.WriteLine($"[推手] {attacker.PlayerName} 触发推手，击退 {victim.PlayerName}");
    }

    /// <summary>
    /// 击退敌人
    /// 完全复制自 jRandomSkills 的 PushEnemy 实现
    /// </summary>
    private void PushEnemy(CCSPlayerController player, QAngle attackerAngle)
    {
        if (player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn.LifeState != (int)LifeState_t.LIFE_ALIVE)
            return;

        // 获取当前位置和视角
        var currentPosition = pawn.AbsOrigin;
        var currentAngles = pawn.EyeAngles;

        // 计算新的速度向量（基于攻击者的朝向）
        Vector newVelocity = GetForwardVector(attackerAngle) * PUSH_VELOCITY;
        newVelocity.Z = pawn.AbsVelocity.Z + JUMP_VELOCITY;

        // 使用 Teleport 设置新速度（完全复制 jRandomSkills 的实现）
        pawn.Teleport(currentPosition, currentAngles, newVelocity);

        Console.WriteLine($"[推手] {player.PlayerName} 被击退！速度: ({newVelocity.X}, {newVelocity.Y}, {newVelocity.Z})");
    }

    /// <summary>
    /// 计算前方向量（复制自 jRandomSkills 的 SkillUtils.GetForwardVector）
    /// </summary>
    private Vector GetForwardVector(QAngle angles)
    {
        float radiansY = angles.Y * (float)Math.PI / 180.0f;

        return new Vector(
            (float)Math.Cos(radiansY),
            (float)Math.Sin(radiansY),
            0
        );
    }
}
