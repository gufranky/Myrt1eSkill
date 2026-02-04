// AutoBhopEvent.cs
// Copyright (C) 2026 MyrtleSkill Plugin Contributors
//
// This file is part of MyrtleSkill Plugin
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
//
// This event includes code and design concepts from jRandomSkills by Juzlus
// Original project: https://github.com/Juzlus/jRandomSkills
// Licensed under GNU General Public License v3.0
//
// Specific references:
// - Auto bunnyhop mechanics from jRandomSkills BunnyHop skill
// - Velocity scaling and speed limiting from jRandomSkills implementation
// - Jump button detection with tick-based buffer
//
// Modifications:
// - Adapted to MyrtleSkill event architecture
// - Changed from per-player skill to global event affecting all players
// - Integrated with entertainment event system

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill;

/// <summary>
/// 自动Bhop事件 - 真正的自动连跳，速度倍数放大
/// 参考 jRandomSkills 的 BunnyHop 技能实现
/// </summary>
public class AutoBhopEvent : EntertainmentEvent
{
    public override string Name => "AutoBhop";
    public override string DisplayName => "🐰 自动Bhop";
    public override string Description => "真正的自动连跳！按住跳跃自动连续跳跃！速度倍数放大！";

    // 跳跃参数（参考 jRandomSkills）
    private const float JUMP_VELOCITY = 300.0f;      // 跳跃垂直速度
    private const float MAX_SPEED = 500.0f;          // 最大水平速度
    private const float JUMP_BOOST = 2.0f;           // 速度倍数放大
    private const int JUMP_BUFFER_TICKS = 20;        // 跳跃按键缓冲时间（tick）

    // 跟踪每个玩家最后一次跳跃的 tick
    private readonly Dictionary<ulong, int> _playersLastJump = new();

    public override void OnApply()
    {
        Console.WriteLine("[自动Bhop] 事件已激活");

        // 清空之前的记录
        _playersLastJump.Clear();

        // 注册 OnTick 监听
        if (Plugin != null)
        {
            Plugin.RegisterListener<Listeners.OnTick>(OnTick);
        }

        // 显示提示（保留聊天框提示，移除屏幕中间提示，统一由HUD显示）
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("🐰 自动Bhop模式已启用！");
                player.PrintToChat("⚡ 按住空格键自动连跳！速度提升2倍！");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[自动Bhop] 事件已恢复");

        // 移除 OnTick 监听
        if (Plugin != null)
        {
            Plugin.RemoveListener<Listeners.OnTick>(OnTick);
        }

        // 清空记录
        _playersLastJump.Clear();

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("🐰 自动Bhop模式已禁用");
            }
        }
    }

    /// <summary>
    /// 每帧检测并应用自动跳跃效果
    /// 参考 jRandomSkills BunnyHop 的 OnTick 实现
    /// </summary>
    private void OnTick()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive)
                continue;

            GiveAutoBhop(player);
        }
    }

    /// <summary>
    /// 对玩家应用自动跳跃效果
    /// 参考 jRandomSkills BunnyHop 的 GiveBunnyHop 实现
    /// </summary>
    private void GiveAutoBhop(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        var movementServices = pawn.MovementServices;
        if (movementServices == null)
            return;

        // 检测跳跃按键状态
        bool jumpJustPressed = (movementServices.QueuedButtonChangeMask & (ulong)PlayerButtons.Jump) != 0;
        if (jumpJustPressed)
        {
            // 记录跳跃按键的 tick
            _playersLastJump[player.SteamID] = Server.TickCount;
        }

        // 检查玩家是否按了跳跃键（当前按住或最近按过）
        bool jumpPressed = player.Buttons.HasFlag(PlayerButtons.Jump) ||
                          (_playersLastJump.TryGetValue(player.SteamID, out int lastJumpTick) &&
                           lastJumpTick + JUMP_BUFFER_TICKS >= Server.TickCount);

        // 获取玩家标志
        var flags = (PlayerFlags)pawn.Flags;

        // 检查是否在地面且不在梯子上
        if (jumpPressed && flags.HasFlag(PlayerFlags.FL_ONGROUND) && !pawn.MoveType.HasFlag(MoveType_t.MOVETYPE_LADDER))
        {
            // 设置跳跃垂直速度
            pawn.AbsVelocity.Z = JUMP_VELOCITY;

            // 获取当前水平速度
            var vX = pawn.AbsVelocity.X;
            var vY = pawn.AbsVelocity.Y;
            var speed2D = Math.Sqrt(vX * vX + vY * vY);

            // 计算速度缩放因子
            double scale = 1.0;

            if (speed2D < MAX_SPEED)
            {
                // 速度低于最大值，应用跳跃加速
                var newSpeed = Math.Min(speed2D * JUMP_BOOST, MAX_SPEED);
                scale = newSpeed / (speed2D == 0 ? 1 : speed2D);
            }
            else if (speed2D > MAX_SPEED)
            {
                // 速度超过最大值，限制到最大值
                scale = MAX_SPEED / speed2D;
            }

            // 应用水平速度缩放
            pawn.AbsVelocity.X = (float)(vX * scale);
            pawn.AbsVelocity.Y = (float)(vY * scale);

            // 通知客户端更新
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_vecAbsVelocity");
        }
    }
}
