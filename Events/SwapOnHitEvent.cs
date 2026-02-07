using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;

namespace MyrtleSkill;

/// <summary>
/// 击中交换事件 - 击中敌人时交换位置和朝向
/// </summary>
public class SwapOnHitEvent : EntertainmentEvent
{
    public override string Name => "SwapOnHit";
    public override string DisplayName => "击中交换";
    public override string Description => "击中敌人时会交换位置和朝向！";

    // 交换冷却时间（秒）
    private const float SWAP_COOLDOWN = 0.5f;

    // 跟踪每个玩家的交换冷却时间
    private readonly ConcurrentDictionary<int, float> _swapCooldowns = new();

    public override void OnApply()
    {
        Console.WriteLine("[击中交换] 事件已激活");
        _swapCooldowns.Clear();
    }

    public override void OnRevert()
    {
        Console.WriteLine("[击中交换] 事件已恢复");
        _swapCooldowns.Clear();
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

        var attackerPawn = attacker.PlayerPawn.Value;
        var victimPawn = victim.PlayerPawn.Value;

        if (attackerPawn == null || !attackerPawn.IsValid)
            return;

        if (victimPawn == null || !victimPawn.IsValid)
            return;

        // 检查攻击者的冷却时间
        if (_swapCooldowns.TryGetValue(attacker.Slot, out float attackerCooldown))
        {
            if (Server.CurrentTime < attackerCooldown)
            {
                float remaining = attackerCooldown - Server.CurrentTime;
                Console.WriteLine($"[击中交换] {attacker.PlayerName} 冷却中，剩余 {remaining:F2} 秒");
                return;
            }
        }

        // 检查受害者的冷却时间
        if (_swapCooldowns.TryGetValue(victim.Slot, out float victimCooldown))
        {
            if (Server.CurrentTime < victimCooldown)
            {
                float remaining = victimCooldown - Server.CurrentTime;
                Console.WriteLine($"[击中交换] {victim.PlayerName} 冷却中，剩余 {remaining:F2} 秒");
                return;
            }
        }

        // 保存位置
        var attackerPos = new Vector(attackerPawn.AbsOrigin!.X,
                                    attackerPawn.AbsOrigin.Y,
                                    attackerPawn.AbsOrigin.Z);
        var victimPos = new Vector(victimPawn.AbsOrigin!.X,
                                 victimPawn.AbsOrigin.Y,
                                 victimPawn.AbsOrigin.Z);

        // 保存朝向（交换朝向：攻击者获得受害者的朝向，受害者获得攻击者的朝向）
        var attackerAngle = new QAngle(attackerPawn.AbsRotation.X,
                                       attackerPawn.AbsRotation.Y,
                                       attackerPawn.AbsRotation.Z);
        var victimAngle = new QAngle(victimPawn.AbsRotation.X,
                                    victimPawn.AbsRotation.Y,
                                    victimPawn.AbsRotation.Z);

        Console.WriteLine($"[击中交换] {attacker.PlayerName} 位置: ({attackerPos.X}, {attackerPos.Y}, {attackerPos.Z})");
        Console.WriteLine($"[击中交换] {victim.PlayerName} 位置: ({victimPos.X}, {victimPos.Y}, {victimPos.Z})");

        // 交换位置和朝向（攻击者获得受害者的位置和朝向，反之亦然）
        attackerPawn.Teleport(victimPos, victimAngle, new Vector(0, 0, 0));
        victimPawn.Teleport(attackerPos, attackerAngle, new Vector(0, 0, 0));

        // 设置冷却时间
        float expireTime = Server.CurrentTime + SWAP_COOLDOWN;
        _swapCooldowns.AddOrUpdate(attacker.Slot, expireTime, (key, old) => expireTime);
        _swapCooldowns.AddOrUpdate(victim.Slot, expireTime, (key, old) => expireTime);

        attacker.PrintToCenter($"💫 位置交换！冷却 {SWAP_COOLDOWN} 秒");
        victim.PrintToCenter($"💫 位置交换！冷却 {SWAP_COOLDOWN} 秒");

        Console.WriteLine($"[击中交换] {attacker.PlayerName} 和 {victim.PlayerName} 交换了位置和朝向");
    }

    /// <summary>
    /// 每帧更新（清理过期的冷却时间）
    /// </summary>
    public void OnTick()
    {
        var currentTime = Server.CurrentTime;
        var expiredSlots = new List<int>();

        foreach (var kvp in _swapCooldowns)
        {
            if (currentTime >= kvp.Value)
            {
                expiredSlots.Add(kvp.Key);
            }
        }

        foreach (var slot in expiredSlots)
        {
            _swapCooldowns.TryRemove(slot, out float _);
        }
    }
}
