// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill;

/// <summary>
/// 超强推背事件 - 造成伤害时强力击退敌人
/// </summary>
public class SuperKnockbackEvent : EntertainmentEvent
{
    public override string Name => "SuperKnockback";
    public override string DisplayName => "💪 超强推背";
    public override string Description => "造成伤害时强力击退敌人！把你打飞！";

    // 击退力度基数（越大击退越强）
    private const float KNOCKBACK_FORCE = 1500.0f;  // 非常强的击退力

    // 最大击退速度上限
    private const float MAX_KNOCKBACK_SPEED = 1000.0f;

    // 标志：事件是否激活
    private bool _isActive = false;

    public override void OnApply()
    {
        Console.WriteLine("[超强推背] 事件已激活");
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
                player.PrintToCenter("💪 超强推背！\n造成伤害会击退敌人！");
                player.PrintToChat("💪 超强推背模式已启用！");
                player.PrintToChat("⚠️ 造成伤害时会强力击退敌人！");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[超强推背] 事件已恢复");
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
                player.PrintToChat("💪 超强推背模式已结束");
            }
        }
    }

    /// <summary>
    /// 处理玩家伤害事件 - 施加超强击退
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

        // 检查受害者是否存活（避免对尸体击退）
        if (!victim.PawnIsAlive)
            return HookResult.Continue;

        // 计算从攻击者指向受害者的方向向量
        float directionX = victimPawn.AbsOrigin.X - attackerPawn.AbsOrigin.X;
        float directionY = victimPawn.AbsOrigin.Y - attackerPawn.AbsOrigin.Y;
        float directionZ = victimPawn.AbsOrigin.Z - attackerPawn.AbsOrigin.Z;

        // 计算距离
        double distanceSquared = directionX * directionX + directionY * directionY + directionZ * directionZ;
        double distance = Math.Sqrt(distanceSquared);

        // 防止除以零
        if (distance < 0.001)
            distance = 0.001;

        // 计算缩放因子（距离越近，击退越强）
        float scale = KNOCKBACK_FORCE / (float)distance;

        // 计算击退速度向量
        float knockbackX = directionX * scale;
        float knockbackY = directionY * scale;
        float knockbackZ = directionZ * scale;

        // 稍微向上的分量，让敌人被击飞到空中
        knockbackZ += 100.0f;

        // 累加到受害者当前速度
        float newVelocityX = victimPawn.AbsVelocity.X + knockbackX;
        float newVelocityY = victimPawn.AbsVelocity.Y + knockbackY;
        float newVelocityZ = victimPawn.AbsVelocity.Z + knockbackZ;

        // 限制最大速度
        float speed = (float)Math.Sqrt(
            newVelocityX * newVelocityX +
            newVelocityY * newVelocityY +
            newVelocityZ * newVelocityZ
        );

        if (speed > MAX_KNOCKBACK_SPEED)
        {
            float scaleDown = MAX_KNOCKBACK_SPEED / speed;
            newVelocityX *= scaleDown;
            newVelocityY *= scaleDown;
            newVelocityZ *= scaleDown;
        }

        // 应用击退
        victimPawn.AbsVelocity.X = newVelocityX;
        victimPawn.AbsVelocity.Y = newVelocityY;
        victimPawn.AbsVelocity.Z = newVelocityZ;

        // 通知客户端更新
        Utilities.SetStateChanged(victimPawn, "CBaseEntity", "m_vecAbsVelocity");

        // 给击退者发送提示
        attacker.PrintToChat($"💪 你击退了 {victim.PlayerName}！速度: {speed:F1}");

        // 给被击退者发送提示
        victim.PrintToCenter("💪 你被击飞了！");

        Console.WriteLine($"[超强推背] {attacker.PlayerName} 击退了 {victim.PlayerName}，击退速度: {speed:F1}");

        return HookResult.Continue;
    }
}
