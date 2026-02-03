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
// - Added proper error handling and logging
// - Integrated with PlayerSkill base class

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 爆炸射击技能 - 射击时有随机几率发射爆炸子弹
/// </summary>
public class ExplosiveShotSkill : PlayerSkill
{
    public override string Name => "ExplosiveShot";
    public override string DisplayName => "💥 爆炸射击";
    public override string Description => "射击时有20%-30%几率在目标位置引发爆炸！";
    public override bool IsActive => false; // 被动技能

    // 爆炸概率范围
    private const float CHANCE_FROM = 0.2f; // 20%
    private const float CHANCE_TO = 0.3f;   // 30%

    // 爆炸伤害和半径
    private const float EXPLOSION_DAMAGE = 25.0f;
    private const float EXPLOSION_RADIUS = 210.0f;

    // 特殊角度用于识别自己创建的爆炸
    private static readonly QAngle IDENTIFIER_ANGLE = new QAngle(5, 10, -4);

    // 防止同一tick重复触发
    private static int _lastTick = 0;

    // 静态随机数生成器（用于HandlePlayerDamagePre静态方法）
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
    /// 处理玩家伤害前事件（旧实现，保留用于向后兼容）
    /// </summary>
    public static void HandlePlayerDamagePre(CCSPlayerPawn player, CTakeDamageInfo info)
    {
        // 这个方法已弃用，现在使用 HandleWeaponFire 代替
        // 但保留以防需要基于伤害触发
    }

    /// <summary>
    /// 处理武器开火事件
    /// 在射击时使用射线追踪获取击中位置并创建爆炸
    /// </summary>
    public static void HandleWeaponFire(EventWeaponFire @event)
    {
        // 防止同一tick重复触发
        if (_lastTick == Server.TickCount)
            return;

        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || !player.PawnIsAlive)
            return;

        // 检查攻击者是否有爆炸射击技能
        if (!_playerChances.TryGetValue(player.SteamID, out float chance))
            return;

        // 20%-30%概率触发爆炸
        if (_staticRandom.NextDouble() > chance)
            return;

        // 获取玩家当前位置和视角
        var origin = pawn.AbsOrigin;
        if (origin == null)
            return;

        var eyeAngles = pawn.EyeAngles;

        // 计算射击方向
        Vector shootDirection = GetForwardVector(eyeAngles);

        // 使用较短的距离（800单位），更接近实际射击距离
        float explosionDistance = 800.0f;

        // 计算爆炸位置（从玩家位置延伸）
        var explosionPosition = new Vector(
            origin.X + shootDirection.X * explosionDistance,
            origin.Y + shootDirection.Y * explosionDistance,
            origin.Z + shootDirection.Z * explosionDistance
        );

        Console.WriteLine($"[爆炸射击] {player.PlayerName} 射击方向: ({shootDirection.X:F2}, {shootDirection.Y:F2}, {shootDirection.Z:F2})");
        Console.WriteLine($"[爆炸射击] {player.PlayerName} 在 ({explosionPosition.X:F1}, {explosionPosition.Y:F1}, {explosionPosition.Z:F1}) 创建爆炸");

        // 创建爆炸
        SpawnExplosion(explosionPosition);

        player.PrintToChat($"💥 你的射击引发了爆炸！");
    }

    /// <summary>
    /// 创建爆炸
    /// </summary>
    private static void SpawnExplosion(Vector position)
    {
        _lastTick = Server.TickCount;
        CreateHEGrenadeProjectile(position, IDENTIFIER_ANGLE, new Vector(0, 0, 0), 0);
        Console.WriteLine($"[爆炸射击] 在位置 ({position.X:F1}, {position.Y:F1}, {position.Z:F1}) 创建了爆炸");
    }

    /// <summary>
    /// 使用射线追踪获取射击击中位置
    /// 由于API限制，使用简化方法：向玩家视线方向延伸一定距离
    /// </summary>
    private static Vector? TraceRay(Vector start, Vector direction)
    {
        try
        {
            // 简化实现：向射击方向延伸固定距离（2000单位）
            // 这不是真正的射线追踪，但对于大多数情况足够有效
            float maxDistance = 2000.0f;

            Vector end = new Vector(
                start.X + direction.X * maxDistance,
                start.Y + direction.Y * maxDistance,
                start.Z + direction.Z * maxDistance
            );

            return end;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[爆炸射击] 计算爆炸位置失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 根据角度获取前向向量
    /// </summary>
    private static Vector GetForwardVector(QAngle angles)
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
    /// 处理实体生成事件
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
            if (!NearlyEquals(IDENTIFIER_ANGLE.X, heProjectile.AbsRotation.X) ||
                !NearlyEquals(IDENTIFIER_ANGLE.Y, heProjectile.AbsRotation.Y) ||
                !NearlyEquals(IDENTIFIER_ANGLE.Z, heProjectile.AbsRotation.Z))
                return;

            // 修改爆炸属性
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
    /// 创建HE手雷弹道
    /// </summary>
    private static void CreateHEGrenadeProjectile(Vector pos, QAngle angle, Vector vel, int teamNum)
    {
        try
        {
            var function = new MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int>(
                GameData.GetSignature("HEGrenadeProjectile_CreateFunc")
            );
            // 参数6使用44（与jRandomSkills保持一致）
            function.Invoke(pos.Handle, angle.Handle, vel.Handle, vel.Handle, IntPtr.Zero, new IntPtr(44), teamNum);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[爆炸射击] 创建HE手雷失败: {ex.Message}");
        }
    }
}
