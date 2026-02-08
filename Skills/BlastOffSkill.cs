// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on PushSkill implementation

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 击飞咯技能 - 被动技能
/// 击中敌人时会让敌人起飞
/// 与起飞咯事件互斥
/// </summary>
public class BlastOffSkill : PlayerSkill
{
    public override string Name => "BlastOff";
    public override string DisplayName => "🚀 击飞咯";
    public override string Description => "击中敌人时会让敌人起飞！打谁谁飞！";
    public override bool IsActive => false; // 被动技能

    // 与起飞咯事件互斥
    public override List<string> ExcludedEvents => new() { "FlyUp" };

    // 与其他移动技能互斥
    public override List<string> ExcludedSkills => new() { "Push", "Sprint" };

    // 起飞参数（与 FlyUp 事件保持一致）
    private const float CHANCE_FROM = 0.2f;  // 20%
    private const float CHANCE_TO = 0.4f;    // 40%
    private const float UP_VELOCITY = 800.0f;  // 向上速度（主要分量）
    private const float HORIZONTAL_KNOCKBACK = 200.0f;  // 水平击退（轻微）

    // 每个玩家的随机几率（技能分配时生成）
    private readonly Dictionary<ulong, float> _playerChances = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 为玩家随机分配一个几率（20% ~ 40%）
        float chance = (float)(new Random().NextDouble() * (CHANCE_TO - CHANCE_FROM)) + CHANCE_FROM;
        _playerChances[player.SteamID] = chance;

        Console.WriteLine($"[击飞咯] {player.PlayerName} 获得了击飞咯技能，几率: {chance * 100:F1}%");

        player.PrintToChat("🚀 你获得了击飞咯技能！");
        player.PrintToChat($"💡 击中敌人时有{chance * 100:F0}%几率让他们起飞！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        _playerChances.Remove(player.SteamID);

        Console.WriteLine($"[击飞咯] {player.PlayerName} 失去了击飞咯技能");
    }

    /// <summary>
    /// 处理玩家受伤事件（在主文件的 OnPlayerHurt 中调用）
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

        // 执行击飞
        LaunchEnemy(victim, attacker);

        Console.WriteLine($"[击飞咯] {attacker.PlayerName} 触发击飞，让 {victim.PlayerName} 起飞");
    }

    /// <summary>
    /// 让敌人起飞
    /// 基于 FlyUp 事件的实现
    /// </summary>
    private void LaunchEnemy(CCSPlayerController victim, CCSPlayerController attacker)
    {
        var attackerPawn = attacker.PlayerPawn.Get();
        var victimPawn = victim.PlayerPawn.Get();

        if (attackerPawn == null || !attackerPawn.IsValid ||
            victimPawn == null || !victimPawn.IsValid)
            return;

        if (attackerPawn.AbsOrigin == null || victimPawn.AbsOrigin == null)
            return;

        if (victimPawn.AbsVelocity == null)
            return;

        // 检查受害者是否存活（避免对尸体击飞）
        if (!victim.PawnIsAlive)
            return;

        // 计算从攻击者指向受害者的方向向量（水平面）
        float directionX = victimPawn.AbsOrigin.X - attackerPawn.AbsOrigin.X;
        float directionY = victimPawn.AbsOrigin.Y - attackerPawn.AbsOrigin.Y;

        // 计算水平距离
        double distanceSquared = directionX * directionX + directionY * directionY;
        double distance = Math.Sqrt(distanceSquared);

        // 防止除以零
        if (distance < 0.001)
            distance = 0.001;

        // 归一化方向向量并应用轻微水平击退
        float knockbackX = (directionX / (float)distance) * HORIZONTAL_KNOCKBACK;
        float knockbackY = (directionY / (float)distance) * HORIZONTAL_KNOCKBACK;

        // 主要向上的速度（让敌人飞起来）
        float knockbackZ = UP_VELOCITY;

        // 累加到受害者当前速度
        float newVelocityX = victimPawn.AbsVelocity.X + knockbackX;
        float newVelocityY = victimPawn.AbsVelocity.Y + knockbackY;
        float newVelocityZ = victimPawn.AbsVelocity.Z + knockbackZ;

        // 应用起飞速度
        victimPawn.AbsVelocity.X = newVelocityX;
        victimPawn.AbsVelocity.Y = newVelocityY;
        victimPawn.AbsVelocity.Z = newVelocityZ;

        // 通知客户端更新
        Utilities.SetStateChanged(victimPawn, "CBaseEntity", "m_vecAbsVelocity");

        // 给攻击者发送提示
        attacker.PrintToChat($"🚀 你让 {victim.PlayerName} 起飞了！");

        // 给被击飞者发送提示
        victim.PrintToCenter("🚀 你起飞了！");

        Console.WriteLine($"[击飞咯] {attacker.PlayerName} 让 {victim.PlayerName} 起飞了！速度: ({newVelocityX:F1}, {newVelocityY:F1}, {newVelocityZ:F1})");
    }
}
