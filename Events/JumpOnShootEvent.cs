using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MyrtleSkill;

/// <summary>
/// 射击跳跃事件 - 开枪时自动跳跃（仅在地面时触发）
/// </summary>
public class JumpOnShootEvent : EntertainmentEvent
{
    public override string Name => "JumpOnShoot";
    public override string DisplayName => "射击跳跃";
    public override string Description => "开枪时会自动跳跃！仅在地面时触发！";

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

        // 检查玩家是否在地面上
        const uint FL_ONGROUND = 1 << 0; // 1

        if ((pawn.Flags & FL_ONGROUND) == 0)
        {
            // 玩家在空中，不触发跳跃，防止无限连跳
            return;
        }

        // 给予向上的速度，模拟跳跃
        var velocity = pawn.AbsVelocity;
        if (velocity != null)
        {
            pawn.AbsVelocity.Z = 300.0f;
            Console.WriteLine($"[射击跳跃] {player.PlayerName} 开枪跳跃");
        }
    }
}
