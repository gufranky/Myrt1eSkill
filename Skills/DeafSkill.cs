// DeafSkill.cs
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
// This skill includes code and design concepts from jRandomSkills by Juzlus
// Original project: https://github.com/Juzlus/jRandomSkills
// Licensed under GNU General Public License v3.0
//
// Specific references:
// - UserMessage sound interception from jRandomSkills Deaf skill
// - Recipient filtering mechanism from jRandomSkills implementation
//
// Modifications:
// - Adapted to MyrtleSkill active skill architecture
// - Changed from targeted menu selection to random enemy selection
// - Integrated with skill cooldown system

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 失聪技能 - 随机让一名敌人听不到所有声音
/// 参考 jRandomSkills 的 Deaf 技能实现
/// </summary>
public class DeafSkill : PlayerSkill
{
    public override string Name => "Deaf";
    public override string DisplayName => "🔇 失聪";
    public override string Description => "随机让一名敌人听不到所有声音！持续10秒！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 30.0f; // 30秒冷却

    // 失聪效果持续时间（秒）
    private const float DEAF_DURATION = 10.0f;

    // 跟踪被施加失聪效果的玩家及其结束时间
    private readonly Dictionary<CCSPlayerController, float> _deafPlayers = new();

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[失聪] {player.PlayerName} 获得了失聪技能");
        player.PrintToChat("🔇 你获得了失聪技能！");
        player.PrintToChat("💡 输入 !useskill 或按键激活！");
        player.PrintToChat($"⏱️ 冷却时间：{Cooldown}秒");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        // 移除该玩家施加的所有失聪效果
        RemoveAllDeaf(player);
        Console.WriteLine($"[失聪] {player.PlayerName} 失去了失聪技能");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        Console.WriteLine($"[失聪] {player.PlayerName} 尝试使用失聪技能");

        // 获取所有敌方玩家
        var enemies = Utilities.GetPlayers()
            .Where(p => p.IsValid && p.PawnIsAlive && p.Team != player.Team && !p.IsBot && !p.IsHLTV)
            .ToList();

        if (enemies.Count == 0)
        {
            player.PrintToChat("🔇 没有可用的目标！");
            return;
        }

        // 随机选择一名敌人
        var random = new Random();
        var targetEnemy = enemies[random.Next(enemies.Count)];

        // 施加失聪效果
        ApplyDeaf(player, targetEnemy);

        player.PrintToChat($"🔇 你让 {targetEnemy.PlayerName} 失聪了！持续 {DEAF_DURATION} 秒！");
        targetEnemy.PrintToCenter("🔇 你失聪了！听不到任何声音！");
        targetEnemy.PrintToChat($"🔇 你被 {player.PlayerName} 施加了失聪效果，持续 {DEAF_DURATION} 秒！");
    }

    /// <summary>
    /// 对敌人施加失聪效果
    /// </summary>
    private void ApplyDeaf(CCSPlayerController caster, CCSPlayerController target)
    {
        if (Plugin == null)
            return;

        // 记录失聪玩家和结束时间
        _deafPlayers[target] = Server.CurrentTime + DEAF_DURATION;

        // 如果这是第一个失聪玩家，注册 OnTick 监听
        // 注意：不再需要 HookUserMessage，因为主插件已经全局注册了
        if (_deafPlayers.Count == 1)
        {
            Plugin.RegisterListener<Listeners.OnTick>(OnTick);
        }

        Console.WriteLine($"[失聪] {caster.PlayerName} 对 {target.PlayerName} 施加了失聪效果");
    }

    /// <summary>
    /// 移除玩家施加的所有失聪效果
    /// </summary>
    private void RemoveAllDeaf(CCSPlayerController player)
    {
        bool hadDeafPlayers = _deafPlayers.Count > 0;

        // 移除所有失聪玩家
        _deafPlayers.Clear();

        // 移除 OnTick 监听（不再需要 UnhookUserMessage，因为主插件会一直注册）
        if (hadDeafPlayers && Plugin != null)
        {
            Plugin.RemoveListener<Listeners.OnTick>(OnTick);
        }

        Console.WriteLine($"[失聪] 已移除所有失聪效果");
    }

    /// <summary>
    /// 每帧检查失聪效果是否过期
    /// </summary>
    private void OnTick()
    {
        float currentTime = Server.CurrentTime;

        // 查找过期的失聪效果
        var expiredPlayers = _deafPlayers
            .Where(kvp => kvp.Value <= currentTime)
            .Select(kvp => kvp.Key)
            .ToList();

        // 移除过期效果
        foreach (var player in expiredPlayers)
        {
            _deafPlayers.Remove(player);

            if (player.IsValid)
            {
                player.PrintToChat("🔊 你的听觉恢复了！");
            }

            Console.WriteLine($"[失聪] {player.PlayerName} 的失聪效果已过期");
        }

        // 如果没有失聪玩家了，移除 OnTick 监听
        // 注意：不再需要 UnhookUserMessage，因为主插件会一直注册 Hook 208
        if (_deafPlayers.Count == 0 && Plugin != null)
        {
            Plugin.RemoveListener<Listeners.OnTick>(OnTick);
        }
    }

    /// <summary>
    /// 处理声音 UserMessage，移除失聪玩家
    /// 由主插件统一调用，不再自己注册 Hook
    /// </summary>
    public void HandlePlayerMakeSound(UserMessage um)
    {
        // 从声音接收者列表中移除所有失聪玩家
        foreach (var deafPlayer in _deafPlayers.Keys)
        {
            if (deafPlayer.IsValid)
            {
                um.Recipients.Remove(deafPlayer);
            }
        }
    }
}
