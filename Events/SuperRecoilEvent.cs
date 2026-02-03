// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill;

/// <summary>
/// 超强反冲事件 - 开枪时玩家会被向后强力推开
/// </summary>
public class SuperRecoilEvent : EntertainmentEvent
{
    public override string Name => "SuperRecoil";
    public override string DisplayName => "💥 超强反冲";
    public override string Description => "开枪时会有超强后坐力！把自己弹飞！";

    // 反冲力度基数（越大推力越强）
    private const float RECOIL_FORCE = 500.0f;

    // 最大反冲力上限（防止被弹飞太远）
    private const float MAX_RECOIL_SPEED = 600.0f;

    // 标志：事件是否激活
    private bool _isActive = false;

    public override void OnApply()
    {
        Console.WriteLine("[超强反冲] 事件已激活");
        _isActive = true;

        // 注册武器射击事件监听
        if (Plugin != null)
        {
            Plugin.RegisterEventHandler<EventWeaponFire>(OnWeaponFire, HookMode.Post);
        }

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToCenter("💥 超强反冲！\n开枪就会向后飞！");
                player.PrintToChat("💥 超强反冲模式已启用！");
                player.PrintToChat("⚠️ 开枪时会有超强后坐力，把自己弹飞！");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[超强反冲] 事件已恢复");
        _isActive = false;

        // 移除事件监听
        if (Plugin != null)
        {
            Plugin.DeregisterEventHandler<EventWeaponFire>(OnWeaponFire);
        }

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("💥 超强反冲模式已结束");
            }
        }
    }

    /// <summary>
    /// 处理武器射击事件 - 施加超强反冲力
    /// </summary>
    private HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        if (!_isActive)
            return HookResult.Continue;

        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        var pawn = player.PlayerPawn.Get();
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        if (pawn.AbsOrigin == null || pawn.AbsRotation == null)
            return HookResult.Continue;

        // 获取玩家视角方向（通过 AbsRotation）
        var angle = pawn.AbsRotation;

        // 将角度转换为方向向量
        // Y 轴旋转（偏航角）决定了玩家朝向
        float yaw = angle.Y;

        // 计算玩家朝向的单位向量
        Vector forwardDirection = new Vector(
            (float)Math.Cos(yaw * Math.PI / 180),
            (float)Math.Sin(yaw * Math.PI / 180),
            0
        );

        // 反方向（向后推）
        Vector recoilDirection = new Vector(
            -forwardDirection.X,
            -forwardDirection.Y,
            0.3f  // 稍微向上的分量，让玩家稍微跳起
        );

        // 计算反冲力向量
        Vector recoilVelocity = new Vector(
            recoilDirection.X * RECOIL_FORCE,
            recoilDirection.Y * RECOIL_FORCE,
            recoilDirection.Z * RECOIL_FORCE
        );

        // 获取玩家当前速度
        var currentVelocity = pawn.AbsVelocity;
        if (currentVelocity == null)
            return HookResult.Continue;

        // 计算新的速度
        Vector newVelocity = new Vector(
            currentVelocity.X + recoilVelocity.X,
            currentVelocity.Y + recoilVelocity.Y,
            currentVelocity.Z + recoilVelocity.Z
        );

        // 限制最大速度（防止被弹飞太远）
        float speed = (float)Math.Sqrt(
            newVelocity.X * newVelocity.X +
            newVelocity.Y * newVelocity.Y +
            newVelocity.Z * newVelocity.Z
        );

        if (speed > MAX_RECOIL_SPEED)
        {
            float scale = MAX_RECOIL_SPEED / speed;
            newVelocity = new Vector(
                newVelocity.X * scale,
                newVelocity.Y * scale,
                newVelocity.Z * scale
            );
        }

        // 应用反冲力（直接修改速度分量）
        if (pawn.AbsVelocity != null)
        {
            pawn.AbsVelocity.X += recoilVelocity.X;
            pawn.AbsVelocity.Y += recoilVelocity.Y;
            pawn.AbsVelocity.Z += recoilVelocity.Z;

            // 通知客户端更新
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_vecAbsVelocity");
        }

        Console.WriteLine($"[超强反冲] {player.PlayerName} 开枪，速度: {speed:F1}");

        return HookResult.Continue;
    }
}
