using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace HelloWorldPlugin;

/// <summary>
/// 吸血鬼事件 - 造成伤害时吸取等量生命值
/// </summary>
public class VampireEvent : EntertainmentEvent
{
    public override string Name => "Vampire";
    public override string DisplayName => "吸血鬼";
    public override string Description => "造成伤害时吸取等量生命值！";

    public override void OnApply()
    {
        Console.WriteLine("[吸血鬼] 事件已激活");
    }

    /// <summary>
    /// 处理玩家受伤事件（在主文件的 OnPlayerHurt 中调用）
    /// </summary>
    public void HandlePlayerHurt(EventPlayerHurt @event)
    {
        var attacker = @event.Attacker;
        if (attacker == null || !attacker.IsValid || !attacker.PawnIsAlive)
            return;

        var attackerPawn = attacker.PlayerPawn.Get();
        if (attackerPawn == null || !attackerPawn.IsValid)
            return;

        int damage = @event.DmgHealth;
        if (damage <= 0)
            return;

        // 吸取等量生命值（无上限）
        int newHealth = attackerPawn.Health + damage;

        attackerPawn.Health = newHealth;
        Utilities.SetStateChanged(attackerPawn, "CBaseEntity", "m_iHealth");

        Console.WriteLine($"[吸血鬼] {attacker.PlayerName} 吸取了 {damage} 点生命值，当前生命: {newHealth}");
    }

    /// <summary>
    /// 处理玩家死亡事件（在主文件的 OnPlayerDeath 中调用）
    /// </summary>
    public void HandlePlayerDeath(EventPlayerDeath @event)
    {
        var attacker = @event.Attacker;
        if (attacker == null || !attacker.IsValid || !attacker.PawnIsAlive)
            return;

        var attackerPawn = attacker.PlayerPawn.Get();
        if (attackerPawn == null || !attackerPawn.IsValid)
            return;

        // 击杀额外奖励50生命值（无上限）
        int newHealth = attackerPawn.Health + 50;

        attackerPawn.Health = newHealth;
        Utilities.SetStateChanged(attackerPawn, "CBaseEntity", "m_iHealth");

        Console.WriteLine($"[吸血鬼] {attacker.PlayerName} 击杀奖励50生命值，当前生命: {newHealth}");
    }
}
