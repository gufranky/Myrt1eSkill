// ExplosiveShotSkill.cs
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
// This skill includes code derived from jRandomSkills by Juzlus
// Original project: https://github.com/Juzlus/jRandomSkills
// Licensed under GNU General Public License v3.0
//
// Modifications:
// - Adapted to MyrtleSkill plugin architecture

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 爆炸射击技能 - 射击时有随机几率在伤害位置引发爆炸
/// 完全基于 jRandomSkills ExplosiveShot 实现
/// </summary>
public class ExplosiveShotSkill : PlayerSkill
{
    public override string Name => "ExplosiveShot";
    public override string DisplayName => "💥 爆炸射击";
    public override string Description => "射击时有随机几率在伤害位置引发爆炸！";
    public override bool IsActive => false; // 被动技能

    // 爆炸参数（与 jRandomSkills 保持一致）
    private const float EXPLOSION_DAMAGE = 25.0f;
    private const float EXPLOSION_RADIUS = 210.0f;
    private const float CHANCE_FROM = 0.15f; // 15%
    private const float CHANCE_TO = 0.30f;   // 30%

    // 特殊角度用于识别自己创建的爆炸
    private static readonly QAngle IDENTIFIER_ANGLE = new QAngle(5, 10, -4);

    // 防止同一tick重复触发
    private static int _lastTick = 0;

    // 静态随机数生成器
    private static readonly Random _staticRandom = new();

    // 每个玩家的爆炸概率
    private static readonly Dictionary<ulong, float> _playerChances = new();

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[爆炸射击] {player.PlayerName} 获得了爆炸射击技能");

        // 为玩家随机分配一个概率
        float chance = (float)(_staticRandom.NextDouble() * (CHANCE_TO - CHANCE_FROM)) + CHANCE_FROM;
        _playerChances[player.SteamID] = chance;

        player.PrintToChat("💥 你获得了爆炸射击技能！");
        player.PrintToChat($"💡 射击时有{chance * 100:F0}%几率引发爆炸！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        Console.WriteLine($"[爆炸射击] {player.PlayerName} 失去了爆炸射击技能");
        _playerChances.Remove(player.SteamID);
    }

    /// <summary>
    /// 创建爆炸（与 jRandomSkills 完全一致）
    /// </summary>
    private static void SpawnExplosion(Vector position)
    {
        _lastTick = Server.TickCount;
        CreateHEGrenadeProjectile(position, IDENTIFIER_ANGLE, new Vector(0, 0, 0), 0);
        Console.WriteLine($"[爆炸射击] 在位置 ({position.X:F1}, {position.Y:F1}, {position.Z:F1}) 创建爆炸");
    }

    /// <summary>
    /// 处理实体生成事件（与 jRandomSkills 完全一致）
    /// </summary>
    public static void OnEntitySpawned(CEntityInstance entity)
    {
        if (entity.DesignerName != "hegrenade_projectile")
            return;

        var heProjectile = entity.As<CBaseCSGrenadeProjectile>();
        if (heProjectile == null || !heProjectile.IsValid || heProjectile.AbsRotation == null)
            return;

        Server.NextFrame(() =>
        {
            if (heProjectile == null || !heProjectile.IsValid)
                return;

            // 检查是否是我们创建的爆炸（通过特殊角度识别）
            if (!(NearlyEquals(IDENTIFIER_ANGLE.X, heProjectile.AbsRotation.X) &&
                  NearlyEquals(IDENTIFIER_ANGLE.Y, heProjectile.AbsRotation.Y) &&
                  NearlyEquals(IDENTIFIER_ANGLE.Z, heProjectile.AbsRotation.Z)))
                return;

            // 修改爆炸属性（与 jRandomSkills 完全一致）
            heProjectile.TicksAtZeroVelocity = 100;
            heProjectile.TeamNum = (byte)CsTeam.None; // 中立伤害
            heProjectile.Damage = EXPLOSION_DAMAGE;
            heProjectile.DmgRadius = EXPLOSION_RADIUS;
            heProjectile.DetonateTime = 0; // 立即爆炸

            Console.WriteLine($"[爆炸射击] 修改手雷属性：伤害={EXPLOSION_DAMAGE}，半径={EXPLOSION_RADIUS}");
        });
    }

    /// <summary>
    /// 浮点数近似相等判断
    /// </summary>
    private static bool NearlyEquals(float a, float b, float epsilon = 0.001f)
    {
        return Math.Abs(a - b) < epsilon;
    }

    /// <summary>
    /// 创建HE手雷弹道（与 jRandomSkills SkillUtils 一致）
    /// </summary>
    private static void CreateHEGrenadeProjectile(Vector pos, QAngle angle, Vector vel, int teamNum)
    {
        try
        {
            var function = new MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int>(
                GameData.GetSignature("HEGrenadeProjectile_CreateFunc")
            );
            // 参数6使用44（与 jRandomSkills 保持一致）
            function.Invoke(pos.Handle, angle.Handle, vel.Handle, vel.Handle, IntPtr.Zero, new IntPtr(44), teamNum);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[爆炸射击] 创建HE手雷失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理玩家伤害前事件（与 jRandomSkills OnTakeDamage 一致）
    /// 在伤害发生时在伤害位置创建爆炸
    /// </summary>
    public static void OnTakeDamagePre(CCSPlayerPawn player, CTakeDamageInfo info)
    {
        // 防止同一tick重复触发
        if (_lastTick == Server.TickCount)
            return;

        // 检查攻击者
        if (info.Attacker == null || info.Attacker.Value == null)
            return;

        var attackerPawn = info.Attacker.Value.As<CCSPlayerPawn>();
        if (attackerPawn == null)
            return;

        if (attackerPawn.DesignerName != "player")
            return;

        if (attackerPawn.Controller?.Value == null)
            return;

        var attacker = attackerPawn.Controller.Value.As<CCSPlayerController>();
        if (attacker == null || !attacker.IsValid)
            return;

        // 检查攻击者是否有爆炸射击技能
        if (!_playerChances.TryGetValue(attacker.SteamID, out float chance))
            return;

        // 随机概率触发爆炸
        if (_staticRandom.NextDouble() > chance)
            return;

        // 使用伤害位置创建爆炸
        var damagePosition = info.DamagePosition;
        if (damagePosition != null)
        {
            SpawnExplosion(damagePosition);
            attacker.PrintToChat("💥 你的射击引发了爆炸！");
            Console.WriteLine($"[爆炸射击] {attacker.PlayerName} 在伤害位置创建爆炸");
        }
    }
}
