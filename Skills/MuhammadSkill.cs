// MuhammadSkill.cs
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
// - HEGrenadeProjectile creation from jRandomSkills Muhammed skill
// - OnEntitySpawned modification from jRandomSkills implementation
//
// Modifications:
// - Adapted to MyrtleSkill passive skill architecture
// - Integrated with skill cooldown system
// - Changed name from "Muhammed" to "Muhammad"

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 穆罕默德技能 - 被动技能
/// 你死后会爆炸，杀死附近的玩家
/// 参考实现：jRandomSkills Muhammed 技能
/// </summary>
public class MuhammadSkill : PlayerSkill
{
    public override string Name => "Muhammad";
    public override string DisplayName => "💀 穆罕默德";
    public override string Description => "你死后会爆炸，杀死附近的玩家！立即爆炸！";
    public override bool IsActive => false; // 被动技能
    public override float Cooldown => 0f; // 被动技能无冷却

    // 爆炸参数
    private const int EXPLOSION_DAMAGE = 999;
    private const float EXPLOSION_RADIUS = 500.0f;

    // 手雷抛射角度
    private static readonly QAngle EXPLOSION_ANGLE = new(10, -5, 9);

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[穆罕默德] {player.PlayerName} 获得了穆罕默德技能");
        player.PrintToChat("💀 你获得了穆罕默德技能！");
        player.PrintToChat("💡 你死后会爆炸，杀死附近的玩家！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[穆罕默德] {player.PlayerName} 失去了穆罕默德技能");
    }

    /// <summary>
    /// 处理玩家死亡事件 - 创建爆炸
    /// </summary>
    public static void HandlePlayerDeath(EventPlayerDeath @event)
    {
        var victim = @event.Userid;
        if (victim == null || !victim.IsValid)
            return;

        // 获取受害者实体
        var pawn = victim.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 检查受害者是否有穆罕默德技能
        var plugin = MyrtleSkill.Instance;
        if (plugin?.SkillManager == null)
            return;

        var skill = plugin.SkillManager.GetPlayerSkill(victim);
        if (skill == null || skill.Name != "Muhammad")
            return;

        // 创建爆炸
        CreateExplosion(victim, pawn);

        // 播放语音
        var fileNames = new[] { "radiobotfallback01", "radiobotfallback02", "radiobotfallback04" };
        var randomFile = fileNames[new Random().Next(fileNames.Length)];
        victim.ExecuteClientCommand($"play vo/agents/balkan/{randomFile}.vsnd");

        Console.WriteLine($"[穆罕默德] {victim.PlayerName} 死亡，触发爆炸！");
    }

    /// <summary>
    /// 创建爆炸
    /// </summary>
    private static void CreateExplosion(CCSPlayerController player, CBasePlayerPawn pawn)
    {
        if (pawn.AbsOrigin == null)
            return;

        // 获取玩家位置（稍微抬高一点）
        var pos = pawn.AbsOrigin;
        pos.Z += 10;

        // 创建 HE 手雷抛射物
        CreateHEGrenadeProjectile(pos, EXPLOSION_ANGLE, new Vector(0, 0, -10), (int)player.TeamNum);
    }

    /// <summary>
    /// 创建 HE 手雷抛射物
    /// 参考 jRandomSkills 的 SkillUtils.CreateHEGrenadeProjectile 实现
    /// </summary>
    private static void CreateHEGrenadeProjectile(Vector pos, QAngle angle, Vector vel, int teamNum)
    {
        // 使用 MemoryFunction 创建 HE 手雷抛射物
        // 注意：这需要游戏数据签名 "HEGrenadeProjectile_CreateFunc"
        // 如果签名不可用，这个技能将无法工作

        // 由于我们没有这个签名，我们需要使用替代方法
        // 在 EntitySpawned 监听器中修改创建的 HE 手雷

        // 创建 HE 手雷实体
        var grenade = Utilities.CreateEntityByName<CHEGrenade>("hegrenade_projectile");
        if (grenade == null || !grenade.IsValid)
        {
            Console.WriteLine("[穆罕默德] 无法创建 HE 手雷实体");
            return;
        }

        // 设置位置
        if (grenade.AbsOrigin != null)
        {
            grenade.AbsOrigin.X = pos.X;
            grenade.AbsOrigin.Y = pos.Y;
            grenade.AbsOrigin.Z = pos.Z;
            Utilities.SetStateChanged(grenade, "CBaseEntity", "m_vecAbsOrigin");
        }

        // 设置角度
        if (grenade.AbsRotation != null)
        {
            grenade.AbsRotation.X = angle.X;
            grenade.AbsRotation.Y = angle.Y;
            grenade.AbsRotation.Z = angle.Z;
            Utilities.SetStateChanged(grenade, "CBaseEntity", "m_angRotation");
        }

        // 设置速度
        if (grenade.Velocity != null)
        {
            grenade.Velocity.X = vel.X;
            grenade.Velocity.Y = vel.Y;
            grenade.Velocity.Z = vel.Z;
            Utilities.SetStateChanged(grenade, "CBaseEntity", "m_vecVelocity");
        }

        // 设置队伍
        grenade.TeamNum = (byte)teamNum;
        Utilities.SetStateChanged(grenade, "CBaseEntity", "m_iTeamNum");

        // 修改手雷属性，使其立即爆炸并造成大量伤害
        // 注意：这些属性可能需要在 NextFrame 中设置
        Server.NextFrame(() =>
        {
            if (!grenade.IsValid)
                return;

            // 设置伤害
            // grenade.Damage = EXPLOSION_DAMAGE;  // 如果属性可用
            // grenade.DmgRadius = EXPLOSION_RADIUS;  // 如果属性可用

            // 设置立即爆炸
            // grenade.DetonateTime = 0;  // 如果属性可用

            Console.WriteLine($"[穆罕默德] HE 手雷已创建，伤害：{EXPLOSION_DAMAGE}，半径：{EXPLOSION_RADIUS}");
        });

        grenade.DispatchSpawn();
    }

    /// <summary>
    /// 处理实体生成事件 - 修改 HE 手雷属性
    /// </summary>
    public static void OnEntitySpawned(CEntityInstance entity)
    {
        // 只处理 HE 手雷
        if (entity.DesignerName != "hegrenade_projectile")
            return;

        var grenade = entity.As<CBaseCSGrenadeProjectile>();
        if (grenade == null || !grenade.IsValid || grenade.AbsRotation == null)
            return;

        Server.NextFrame(() =>
        {
            if (!grenade.IsValid)
                return;

            // 检查是否是穆罕默德技能创建的手雷
            // 通过检查角度判断（我们的特殊角度是 10, -5, 9）
            var angle = grenade.AbsRotation;
            if (angle == null)
                return;

            // 使用近似比较
            if (!NearlyEquals(angle.X, EXPLOSION_ANGLE.X) ||
                !NearlyEquals(angle.Y, EXPLOSION_ANGLE.Y) ||
                !NearlyEquals(angle.Z, EXPLOSION_ANGLE.Z))
            {
                return;
            }

            // 这是穆罕默德技能的手雷，修改属性（与jRandomSkills保持一致）
            grenade.TicksAtZeroVelocity = 100;
            grenade.Damage = EXPLOSION_DAMAGE;
            grenade.DmgRadius = EXPLOSION_RADIUS;
            grenade.DetonateTime = 0;

            Console.WriteLine($"[穆罕默德] 爆炸手雷已修改，伤害：{EXPLOSION_DAMAGE}，半径：{EXPLOSION_RADIUS}");
        });
    }

    /// <summary>
    /// 浮点数近似比较
    /// </summary>
    private static bool NearlyEquals(float a, float b, float epsilon = 0.001f)
    {
        return Math.Abs(a - b) < epsilon;
    }
}
