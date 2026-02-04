// DeafEvent.cs
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
// - UserMessage sound interception from jRandomSkills Deaf skill
// - Recipient filtering mechanism from jRandomSkills implementation
//
// Modifications:
// - Adapted to MyrtleSkill event architecture
// - Changed from targeted skill to random event affecting enemies
// - Integrated with entertainment event system

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill;

/// <summary>
/// 失聪事件 - 随机敌人听不到所有声音
/// 参考 jRandomSkills 的 Deaf 技能实现
/// </summary>
public class DeafEvent : EntertainmentEvent
{
    public override string Name => "Deaf";
    public override string DisplayName => "🔇 失聪";
    public override string Description => "随机敌人听不到所有声音！";

    // 被静音的玩家列表
    private readonly HashSet<CCSPlayerController> _deafPlayers = new();

    public override void OnApply()
    {
        Console.WriteLine("[失聪] 事件已激活");

        // 获取所有玩家
        var players = Utilities.GetPlayers().Where(p =>
            p.IsValid && p.PawnIsAlive &&
            !p.IsBot && !p.IsHLTV &&
            p.Team != CsTeam.Spectator && p.Team != CsTeam.None
        ).ToList();

        if (players.Count == 0)
        {
            Console.WriteLine("[失聪] 没有符合条件的玩家");
            return;
        }

        // 随机选择一半的玩家作为失聪者
        var random = new Random();
        int deafCount = Math.Max(1, players.Count / 2);

        // 随机打乱玩家列表
        for (int i = 0; i < players.Count; i++)
        {
            int j = random.Next(i, players.Count);
            (players[i], players[j]) = (players[j], players[i]);
        }

        // 选择前 deafCount 个玩家
        for (int i = 0; i < deafCount && i < players.Count; i++)
        {
            _deafPlayers.Add(players[i]);
            players[i].PrintToChat("🔇 你失聪了！听不到任何声音！");
        }

        // 注册 UserMessage 监听（拦截所有声音）
        if (Plugin != null)
        {
            Plugin.HookUserMessage(208, OnPlayerMakeSound);
        }

        Console.WriteLine($"[失聪] 已让 {_deafPlayers.Count} 名玩家失聪");
    }

    public override void OnRevert()
    {
        Console.WriteLine("[失聪] 事件已恢复");

        // 移除 UserMessage 监听
        if (Plugin != null)
        {
            Plugin.UnhookUserMessage(208, OnPlayerMakeSound);
        }

        // 通知所有失聪玩家恢复听觉
        foreach (var player in _deafPlayers)
        {
            if (player.IsValid)
            {
                player.PrintToChat("🔊 你的听觉恢复了！");
            }
        }

        _deafPlayers.Clear();
    }

    /// <summary>
    /// 拦截声音 UserMessage，移除失聪玩家
    /// 参考 jRandomSkills Deaf 技能的 PlayerMakeSound 实现
    /// </summary>
    private HookResult OnPlayerMakeSound(UserMessage um)
    {
        // 从声音接收者列表中移除所有失聪玩家
        foreach (var deafPlayer in _deafPlayers)
        {
            if (deafPlayer.IsValid)
            {
                um.Recipients.Remove(deafPlayer);
            }
        }

        return HookResult.Continue;
    }
}
