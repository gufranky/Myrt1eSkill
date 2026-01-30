using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace HelloWorldPlugin;

/// <summary>
/// 击中交换事件 - 击中敌人时交换位置
/// </summary>
public class SwapOnHitEvent : EntertainmentEvent
{
    public override string Name => "SwapOnHit";
    public override string DisplayName => "击中交换";
    public override string Description => "击中敌人时会交换位置！";

    public override void OnApply()
    {
        Console.WriteLine("[击中交换] 事件已激活");
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

        var attackerPawn = attacker.PlayerPawn.Get();
        var victimPawn = victim.PlayerPawn.Get();

        if (attackerPawn == null || !attackerPawn.IsValid)
            return;

        if (victimPawn == null || !victimPawn.IsValid)
            return;

        // 保存位置和角度
        var attackerPos = attackerPawn.AbsOrigin;
        var attackerAngle = attackerPawn.AbsRotation;

        var victimPos = victimPawn.AbsOrigin;
        var victimAngle = victimPawn.AbsRotation;

        if (attackerPos == null || victimPos == null)
            return;

        // 交换位置
        attackerPawn.Teleport(
            new Vector(victimPos.X, victimPos.Y, victimPos.Z),
            victimAngle,
            new Vector(0, 0, 0)
        );

        victimPawn.Teleport(
            new Vector(attackerPos.X, attackerPos.Y, attackerPos.Z),
            attackerAngle,
            new Vector(0, 0, 0)
        );

        attacker.PrintToCenter("💫 位置交换！");
        victim.PrintToCenter("💫 位置交换！");

        Console.WriteLine($"[击中交换] {attacker.PlayerName} 和 {victim.PlayerName} 交换了位置");
    }
}
