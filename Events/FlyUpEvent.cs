// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Events;

/// <summary>
/// 起飞咯事件 - 所有玩家击中敌人时会让敌人起飞
/// </summary>
public class FlyUpEvent : EntertainmentEvent
{
    public override string Name => "FlyUp";
    public override string DisplayName => "🚀 起飞咯";
    public override string Description => "击中敌人时会让敌人起飞！打谁谁飞！";
    public override int Weight { get; set; } = 15;

    // 起飞参数
    private const float UP_VELOCITY = 800.0f;  // 向上速度（主要分量）
    private const float HORIZONTAL_KNOCKBACK = 200.0f;  // 水平击退（轻微）

    // 标志：事件是否激活
    private bool _isActive = false;

    public override void OnApply()
    {
        Console.WriteLine("[起飞咯] 事件已激活");
        _isActive = true;

        // 注册玩家伤害事件监听
        if (Plugin != null)
        {
            Plugin.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Post);
        }

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("🚀 起飞咯事件已启用！");
                player.PrintToChat("✈️ 击中敌人时会让他们飞起来！");
                player.PrintToCenter("🚀 起飞咯！打谁谁飞！");

                // 播放音效
                player.EmitSound("UI.Pause");
            }
        }

        Server.PrintToChatAll("🌍 所有人都变成了发射器！击中敌人让他们起飞！");
    }

    public override void OnRevert()
    {
        Console.WriteLine("[起飞咯] 事件已结束");
        _isActive = false;

        // 移除事件监听
        if (Plugin != null)
        {
            Plugin.DeregisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        }

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("🚀 起飞咯事件已结束");
                player.EmitSound("UI.RoundStart");
            }
        }
    }

    /// <summary>
    /// 处理玩家伤害事件 - 让敌人起飞
    /// </summary>
    private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        if (!_isActive)
            return HookResult.Continue;

        // 检查攻击者和受害者
        if (@event.Attacker == null || @event.Userid == null)
            return HookResult.Continue;

        if (@event.Userid == @event.Attacker)
            return HookResult.Continue;

        var attacker = @event.Attacker;
        var victim = @event.Userid;

        if (!attacker.IsValid || !victim.IsValid)
            return HookResult.Continue;

        var attackerPawn = attacker.PlayerPawn.Get();
        var victimPawn = victim.PlayerPawn.Get();

        if (attackerPawn == null || !attackerPawn.IsValid ||
            victimPawn == null || !victimPawn.IsValid)
            return HookResult.Continue;

        if (attackerPawn.AbsOrigin == null || victimPawn.AbsOrigin == null)
            return HookResult.Continue;

        if (victimPawn.AbsVelocity == null)
            return HookResult.Continue;

        // 检查受害者是否存活（避免对尸体击飞）
        if (!victim.PawnIsAlive)
            return HookResult.Continue;

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

        Console.WriteLine($"[起飞咯] {attacker.PlayerName} 让 {victim.PlayerName} 起飞了！");

        return HookResult.Continue;
    }
}
