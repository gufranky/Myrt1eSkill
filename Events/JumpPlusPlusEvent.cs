using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace HelloWorldPlugin;

/// <summary>
/// 超级跳跃事件 - 开枪自动跳跃且无扩散
/// </summary>
public class JumpPlusPlusEvent : EntertainmentEvent
{
    public override string Name => "JumpPlusPlus";
    public override string DisplayName => "超级跳跃";
    public override string Description => "开枪自动跳跃且无扩散！";

    public override void OnApply()
    {
        Console.WriteLine("[超级跳跃] 事件已激活，启用无扩散");

        // 启用无扩散
        Server.ExecuteCommand("weapon_accuracy_nospread true");
    }

    public override void OnRevert()
    {
        Console.WriteLine("[超级跳跃] 事件已结束，禁用无扩散");

        // 禁用无扩散
        Server.ExecuteCommand("weapon_accuracy_nospread false");
    }

    /// <summary>
    /// 处理武器射击事件（在主文件的 OnWeaponFire 中调用）
    /// 开枪时自动获得向上速度，不检测是否在地面
    /// </summary>
    public void HandleWeaponFire(EventWeaponFire @event)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        var pawn = player.PlayerPawn.Get();
        if (pawn == null || !pawn.IsValid)
            return;

        // 给予向上速度（不检测是否在地面）
        pawn.AbsVelocity.Z = 400.0f;

        Console.WriteLine($"[超级跳跃] {player.PlayerName} 开枪跳跃");
    }
}
