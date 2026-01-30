using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Numerics;

namespace HelloWorldPlugin;

/// <summary>
/// 射击跳跃事件 - 开枪时自动跳跃
/// </summary>
public class JumpOnShootEvent : EntertainmentEvent
{
    public override string Name => "JumpOnShoot";
    public override string DisplayName => "射击跳跃";
    public override string Description => "开枪时会自动跳跃！";

    public override void OnApply()
    {
        Console.WriteLine("[射击跳跃] 事件已激活");
    }

    /// <summary>
    /// 处理武器射击事件（在主文件的 OnWeaponFire 中调用）
    /// </summary>
    public void HandleWeaponFire(EventWeaponFire @event)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        var pawn = player.PlayerPawn.Get();
        if (pawn == null || !pawn.IsValid)
            return;

        // 给予向上的速度，模拟跳跃
        var velocity = pawn.AbsVelocity;
        if (velocity != null)
        {
            pawn.AbsVelocity.Z = 300.0f;
            Console.WriteLine($"[射击跳跃] {player.PlayerName} 开枪跳跃");
        }
    }
}
