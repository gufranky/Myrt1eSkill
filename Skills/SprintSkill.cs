using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;

namespace MyrtleSkill.Skills;

/// <summary>
/// 短跑技能 - 进行第二次跳跃以冲刺
/// </summary>
public class SprintSkill : PlayerSkill
{
    public override string Name => "Sprint";
    public override string DisplayName => "💨 短跑";
    public override string Description => "进行第二次跳跃以冲刺！按住移动方向键可以冲刺到该方向！";
    public override bool IsActive => false; // 被动技能

    // 与其他移动技能互斥
    public override List<string> ExcludedSkills => new() { "SpeedBoost" };

    // 冲刺参数
    private const float JUMP_VELOCITY = 150f;  // 向上跳跃速度
    private const float DASH_VELOCITY = 600f;   // 水平冲刺速度

    // 每个玩家的状态跟踪
    private readonly Dictionary<int, PlayerSprintState> _playerStates = new();

    public override void OnApply(CCSPlayerController player)
    {
        // 初始化玩家状态
        if (!_playerStates.ContainsKey(player.Slot))
        {
            _playerStates[player.Slot] = new PlayerSprintState();
        }

        Console.WriteLine($"[短跑] {player.PlayerName} 获得了短跑技能");
        player.PrintToChat("💨 你获得了短跑技能！");
        player.PrintToChat("💡 进行第二次跳跃以冲刺！");
        player.PrintToChat("⌨️ 按住WASD键控制冲刺方向！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        // 清理玩家状态
        _playerStates.Remove(player.Slot);
        Console.WriteLine($"[短跑] {player.PlayerName} 失去了短跑技能");
    }

    /// <summary>
    /// 每帧更新（在MyrtleSkill的OnServerPostEntityThink中调用）
    /// </summary>
    public void OnTick(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || !player.PawnIsAlive)
            return;

        // 获取或创建玩家状态
        if (!_playerStates.TryGetValue(player.Slot, out var state))
        {
            state = new PlayerSprintState();
            _playerStates[player.Slot] = state;
        }

        HandleSprint(player, pawn, state);
    }

    /// <summary>
    /// 处理冲刺逻辑
    /// </summary>
    private void HandleSprint(CCSPlayerController player, CCSPlayerPawn pawn, PlayerSprintState state)
    {
        var flags = (PlayerFlags)pawn.Flags;
        var buttons = player.Buttons;

        // 如果在地面，重置跳跃计数
        if ((flags & PlayerFlags.FL_ONGROUND) != 0)
        {
            state.JumpCount = 0;
        }

        // 检测跳跃按键（从未按下到按下）
        bool jumpPressed = (buttons & PlayerButtons.Jump) != 0;
        bool jumpWasPressed = (state.LastButtons & PlayerButtons.Jump) != 0;

        // 如果从空中起跳（第一次跳跃）
        if ((state.LastFlags & PlayerFlags.FL_ONGROUND) != 0 && (flags & PlayerFlags.FL_ONGROUND) == 0 && jumpPressed)
        {
            state.JumpCount = 1;
        }
        // 如果是第二次跳跃（空中再按跳跃）
        else if (!jumpWasPressed && jumpPressed && state.JumpCount == 1)
        {
            state.JumpCount = 2;

            // 计算冲刺方向
            float moveX = 0;
            float moveY = 0;

            if (buttons.HasFlag(PlayerButtons.Forward))
                moveY += 1;
            if (buttons.HasFlag(PlayerButtons.Back))
                moveY -= 1;
            if (buttons.HasFlag(PlayerButtons.Moveleft))
                moveX += 1;
            if (buttons.HasFlag(PlayerButtons.Moveright))
                moveX -= 1;

            // 如果没有按方向键，默认向前冲
            if (moveX == 0 && moveY == 0)
                moveY = 1;

            // 计算冲刺角度
            float moveAngle = MathF.Atan2(moveX, moveY) * (180f / MathF.PI);
            QAngle dashAngles = new(0, pawn.EyeAngles.Y + moveAngle, 0);

            // 计算新速度
            Vector newVelocity = GetForwardVector(dashAngles) * DASH_VELOCITY;
            newVelocity.Z = pawn.AbsVelocity.Z + JUMP_VELOCITY;

            // 应用速度
            pawn.AbsVelocity.X = newVelocity.X;
            pawn.AbsVelocity.Y = newVelocity.Y;
            pawn.AbsVelocity.Z = newVelocity.Z;

            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_vecAbsVelocity");

            Console.WriteLine($"[短跑] {player.PlayerName} 触发冲刺！方向: X={moveX}, Y={moveY}");

            // 显示提示
            player.PrintToCenter("💨 冲刺！");
        }

        // 保存当前状态
        state.LastFlags = flags;
        state.LastButtons = buttons;
    }

    /// <summary>
    /// 获取前方向量（参考Dash实现）
    /// </summary>
    private Vector GetForwardVector(QAngle angles)
    {
        float radiansX = angles.X * (MathF.PI / 180f);
        float radiansY = angles.Y * (MathF.PI / 180f);

        float sinX = MathF.Sin(radiansX);
        float cosX = MathF.Cos(radiansX);

        float sinY = MathF.Sin(radiansY);
        float cosY = MathF.Cos(radiansY);

        return new Vector(cosY * cosX, sinY * cosX, -sinX);
    }

    /// <summary>
    /// 玩家冲刺状态
    /// </summary>
    private class PlayerSprintState
    {
        public int JumpCount { get; set; }
        public PlayerFlags LastFlags { get; set; }
        public PlayerButtons LastButtons { get; set; }
    }
}
