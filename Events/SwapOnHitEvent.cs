using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill;

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

        var attackerPawn = attacker.PlayerPawn.Value;
        var victimPawn = victim.PlayerPawn.Value;

        if (attackerPawn == null || !attackerPawn.IsValid)
            return;

        if (victimPawn == null || !victimPawn.IsValid)
            return;

        // 保存位置（只交换位置，不交换朝向）
        var attackerPos = new Vector(attackerPawn.AbsOrigin.X, attackerPawn.AbsOrigin.Y, attackerPawn.AbsOrigin.Z);
        var victimPos = new Vector(victimPawn.AbsOrigin.X, victimPawn.AbsOrigin.Y, victimPawn.AbsOrigin.Z);

        // 保存各自的朝向
        var attackerAngle = new QAngle(attackerPawn.AbsRotation.X, attackerPawn.AbsRotation.Y, attackerPawn.AbsRotation.Z);
        var victimAngle = new QAngle(victimPawn.AbsRotation.X, victimPawn.AbsRotation.Y, victimPawn.AbsRotation.Z);

        Console.WriteLine($"[击中交换-DEBUG] {attacker.PlayerName} 位置: ({attackerPos.X}, {attackerPos.Y}, {attackerPos.Z})");
        Console.WriteLine($"[击中交换-DEBUG] {victim.PlayerName} 位置: ({victimPos.X}, {victimPos.Y}, {victimPos.Z})");

        // 交换位置，保持各自朝向
        attackerPawn.Teleport(victimPos, attackerAngle, new Vector(0, 0, 0));
        victimPawn.Teleport(attackerPos, victimAngle, new Vector(0, 0, 0));

        attacker.PrintToCenter("💫 位置交换！");
        victim.PrintToCenter("💫 位置交换！");

        Console.WriteLine($"[击中交换] {attacker.PlayerName} 和 {victim.PlayerName} 交换了位置");
    }
}
