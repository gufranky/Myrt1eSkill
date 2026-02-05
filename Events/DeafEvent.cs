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
/// 失聪事件 - 所有人都听不到所有声音
/// 参考 jRandomSkills 的 Deaf 技能实现
/// </summary>
public class DeafEvent : EntertainmentEvent
{
    public override string Name => "Deaf";
    public override string DisplayName => "🔇 失聪";
    public override string Description => "所有人都听不到所有声音！全员失聪！";

    // 被静音的玩家列表
    private static readonly HashSet<CCSPlayerController> _deafPlayers = new();

    // 静态实例引用（用于静态回调方法）
    private static MyrtleSkill? _pluginInstance;

    public override void OnApply()
    {
        Console.WriteLine("[失聪] 事件已激活");

        // 保存静态实例引用
        _pluginInstance = Plugin;

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

        // 让所有玩家都失聪
        foreach (var player in players)
        {
            _deafPlayers.Add(player);
            player.PrintToChat("🔇 你失聪了！听不到任何声音！");
        }

        Console.WriteLine($"[失聪] 已让 {_deafPlayers.Count} 名玩家失聪（全员失聪）");
    }

    public override void OnRevert()
    {
        Console.WriteLine("[失聪] 事件已恢复");

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
    /// 这是一个静态方法，在主插件的 Load 中全局注册
    /// </summary>
    public static HookResult OnPlayerMakeSound(UserMessage um)
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
