// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills by Juzlus (Iana/Hologram skill)

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using CS2TraceRay.Class;
using CS2TraceRay.Struct;
using System.Collections.Concurrent;

namespace MyrtleSkill.Skills;

/// <summary>
/// 全息图技能 - 主动技能
/// 创建你的全息影像，让你控制它几秒钟来迷惑敌人
/// </summary>
public class HologramSkill : PlayerSkill
{
    public override string Name => "Hologram";
    public override string DisplayName => "👥 全息图";
    public override string Description => "点击 [css_useSkill] 创建你的全息影像数秒！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 30.0f; // 30秒冷却

    // 全息图持续时间（秒）
    private const float HOLOGRAM_DURATION = 10.0f;

    // 传送距离（单位）
    private const float TELEPORT_DISTANCE = 50.0f;

    // 跟踪玩家的全息图状态
    private static readonly ConcurrentDictionary<ulong, HologramState> _hologramStates = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[全息图] {player.PlayerName} 获得了全息图技能");
        player.PrintToChat("👥 你获得了全息图技能！");
        player.PrintToChat("💡 输入 !useskill 或按键激活！");
        player.PrintToChat($"⏱️ 冷却时间：{Cooldown}秒");
        player.PrintToChat($"📌 持续时间：{HOLOGRAM_DURATION}秒");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 移除玩家的全息图效果
        KillHologram(player);
        _hologramStates.TryRemove(player.SteamID, out _);

        Console.WriteLine($"[全息图] {player.PlayerName} 失去了全息图技能");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid)
            return;

        Console.WriteLine($"[全息图] {player.PlayerName} 尝试使用全息图技能");

        // 获取或创建状态
        if (!_hologramStates.TryGetValue(player.SteamID, out var state))
        {
            state = new HologramState
            {
                Player = player,
                CloneProp = null
            };
            _hologramStates.TryAdd(player.SteamID, state);
        }

        // 如果已有全息图，销毁它并传送回去
        if (state.CloneProp != null)
        {
            KillHologram(player);
            player.PrintToChat("👥 你传送回全息图位置！");
        }
        // 否则创建新的全息图
        else
        {
            CreateHologram(player, state);
        }
    }

    /// <summary>
    /// 创建全息图克隆体
    /// </summary>
    private void CreateHologram(CCSPlayerController player, HologramState state)
    {
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid ||
            playerPawn.AbsOrigin == null ||
            playerPawn.AbsRotation == null)
        {
            return;
        }

        // 计算传送位置（玩家前方）
        Vector forward = GetForwardVector(playerPawn.AbsRotation);
        Vector teleportPos = playerPawn.AbsOrigin + forward * TELEPORT_DISTANCE;
        Vector cloneCheckPos = playerPawn.AbsOrigin + forward * (TELEPORT_DISTANCE + 25.0f);

        // 检查位置是否有效
        if (!CheckPosition(player, cloneCheckPos) ||
            !((PlayerFlags)playerPawn.Flags).HasFlag(PlayerFlags.FL_ONGROUND))
        {
            player.PrintToCenter("❌ 前方空间不足！");
            return;
        }

        // 创建克隆体
        var clone = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (clone == null || !clone.IsValid)
        {
            player.PrintToCenter("❌ 创建全息图失败！");
            return;
        }

        // 设置克隆体属性（与 jRandomSkills 一致）
        clone.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
        clone.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(clone.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));
        clone.Entity!.Name = clone.Globalname = $"HologramClone_{Server.TickCount}_{player.SteamID}";
        clone.DispatchSpawn();

        Server.NextFrame(() =>
        {
            if (!player.IsValid || !player.PawnIsAlive)
            {
                clone.AcceptInput("Kill");
                return;
            }

            var currentPawn = player.PlayerPawn.Value;
            if (currentPawn == null || !currentPawn.IsValid)
            {
                clone.AcceptInput("Kill");
                return;
            }

            // 设置克隆体模型为玩家模型
            string? modelName = currentPawn.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState?.ModelName;
            if (!string.IsNullOrEmpty(modelName))
            {
                clone.SetModel(modelName);
            }

            // 克隆体在玩家当前位置
            QAngle cloneAngle = new(0, currentPawn.V_angle.Y, 0);
            clone.Teleport(currentPawn.AbsOrigin!, cloneAngle);

            // 玩家传送到前方位置
            Vector teleportPosition = currentPawn.AbsOrigin + forward * TELEPORT_DISTANCE;
            currentPawn.Teleport(teleportPosition);

            // 禁用玩家武器
            BlockWeaponStatic(player, true);

            // 播放音效
            player.EmitSound("SolidMetal.BulletImpact");

            // 更新状态
            state.CloneProp = clone;
            state.UseTime = Server.CurrentTime;

            player.PrintToChat("👥 全息图已激活！");
            player.PrintToCenter($"👥 全息图持续 {HOLOGRAM_DURATION} 秒");

            Console.WriteLine($"[全息图] {player.PlayerName} 创建了全息图");

            // 设置持续时间后自动销毁
            Plugin?.AddTimer(HOLOGRAM_DURATION, () =>
            {
                if (_hologramStates.TryGetValue(player.SteamID, out var s) && s.CloneProp != null)
                {
                    KillHologram(player);
                }
            });
        });
    }

    /// <summary>
    /// 销毁全息图并传送玩家回去
    /// </summary>
    private static void KillHologram(CCSPlayerController player)
    {
        if (!_hologramStates.TryGetValue(player.SteamID, out var state))
            return;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid)
            return;

        // 播放音效
        player.EmitSound("SolidMetal.BulletImpact");

        if (state.CloneProp != null && state.CloneProp.IsValid &&
            state.CloneProp.AbsOrigin != null &&
            state.CloneProp.AbsRotation != null)
        {
            // 获取克隆体位置
            Vector clonePos = new(state.CloneProp.AbsOrigin.X,
                                   state.CloneProp.AbsOrigin.Y,
                                   state.CloneProp.AbsOrigin.Z);
            QAngle cloneAngle = new(state.CloneProp.AbsRotation.X,
                                     state.CloneProp.AbsRotation.Y,
                                     state.CloneProp.AbsRotation.Z);

            // 传送玩家到克隆体位置
            Server.NextFrame(() =>
            {
                if (playerPawn.IsValid)
                {
                    playerPawn.Teleport(clonePos, cloneAngle);
                }
            });

            // 销毁克隆体
            state.CloneProp.AcceptInput("Kill");
            state.CloneProp = null;

            Console.WriteLine($"[全息图] {player.PlayerName} 传送回全息图位置");
        }

        // 恢复武器
        BlockWeaponStatic(player, false);
    }

    /// <summary>
    /// 检查位置是否有效（不被阻挡）
    /// </summary>
    private unsafe bool CheckPosition(CCSPlayerController player, Vector endPos)
    {
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null)
            return false;

        Vector eyePos = new(playerPawn.AbsOrigin.X, playerPawn.AbsOrigin.Y, playerPawn.AbsOrigin.Z + 25.0f);
        Vector targetPos = new(endPos.X, endPos.Y, endPos.Z + 25.0f);

        ulong mask = playerPawn.Collision.CollisionAttribute.InteractsWith;
        ulong contents = playerPawn.Collision.CollisionGroup;

        CGameTrace trace = TraceRay.TraceShape(eyePos, targetPos, mask, contents, player);

        return !trace.DidHit();
    }

    /// <summary>
    /// 禁用/启用玩家武器
    /// </summary>
    private void BlockWeapon(CCSPlayerController player, bool block)
    {
        if (player == null || !player.IsValid)
            return;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid || playerPawn.WeaponServices == null)
            return;

        foreach (var weapon in playerPawn.WeaponServices.MyWeapons)
        {
            if (weapon != null && weapon.IsValid && weapon.Value != null && weapon.Value.IsValid)
            {
                weapon.Value.NextPrimaryAttackTick = block ? int.MaxValue : Server.TickCount;
                weapon.Value.NextSecondaryAttackTick = block ? int.MaxValue : Server.TickCount;

                Utilities.SetStateChanged(weapon.Value, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
                Utilities.SetStateChanged(weapon.Value, "CBasePlayerWeapon", "m_nNextSecondaryAttackTick");
            }
        }
    }

    /// <summary>
    /// 禁用/启用玩家武器（静态版本）
    /// </summary>
    private static void BlockWeaponStatic(CCSPlayerController player, bool block)
    {
        if (player == null || !player.IsValid)
            return;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid || playerPawn.WeaponServices == null)
            return;

        foreach (var weapon in playerPawn.WeaponServices.MyWeapons)
        {
            if (weapon != null && weapon.IsValid && weapon.Value != null && weapon.Value.IsValid)
            {
                weapon.Value.NextPrimaryAttackTick = block ? int.MaxValue : Server.TickCount;
                weapon.Value.NextSecondaryAttackTick = block ? int.MaxValue : Server.TickCount;

                Utilities.SetStateChanged(weapon.Value, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
                Utilities.SetStateChanged(weapon.Value, "CBasePlayerWeapon", "m_nNextSecondaryAttackTick");
            }
        }
    }

    /// <summary>
    /// 计算前方向量
    /// </summary>
    private Vector GetForwardVector(QAngle angles)
    {
        float radiansY = angles.Y * (float)Math.PI / 180.0f;

        return new Vector(
            (float)Math.Cos(radiansY),
            (float)Math.Sin(radiansY),
            0
        );
    }

    /// <summary>
    /// 处理克隆体受到伤害
    /// </summary>
    public static void HandleCloneDamage(CEntityInstance entity, CTakeDamageInfo info)
    {
        if (entity?.Entity == null || entity.Entity.Name == null)
            return;

        if (!entity.Entity.Name.StartsWith("HologramClone_"))
            return;

        // 解析玩家 SteamID
        string[] nameParts = entity.Entity.Name.Split('_');
        if (nameParts.Length < 3)
            return;

        if (!ulong.TryParse(nameParts[2], out ulong steamID) || steamID == 0)
            return;

        var player = Utilities.GetPlayerFromSteamId(steamID);
        if (player == null || !player.IsValid)
            return;

        if (!_hologramStates.TryGetValue(player.SteamID, out var state))
            return;

        if (state.CloneProp == null)
            return;

        Console.WriteLine($"[全息图] {player.PlayerName} 的全息图受到伤害");

        // 计算伤害
        float damage = info.Damage;

        // 销毁全息图
        KillHologram(player);

        // 对玩家造成相同伤害
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn != null && playerPawn.IsValid)
        {
            playerPawn.Health -= (int)damage;

            // 检查是否死亡
            if (playerPawn.Health <= 0)
            {
                playerPawn.CommitSuicide(false, true);
            }

            Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_iHealth");
        }

        player.PrintToCenter($"💥 全息图被摧毁！承受 {damage} 点伤害");
    }

    /// <summary>
    /// 处理玩家受伤事件（玩家受伤时销毁全息图）
    /// </summary>
    public static void HandlePlayerHurt(CCSPlayerController victim)
    {
        if (victim == null || !victim.IsValid)
            return;

        if (!_hologramStates.TryGetValue(victim.SteamID, out var state))
            return;

        if (state.CloneProp != null)
        {
            Console.WriteLine($"[全息图] {victim.PlayerName} 受伤，销毁全息图");

            KillHologram(victim);

            victim.PrintToCenter("💥 你受伤了！全息图消失");
        }
    }

    /// <summary>
    /// 清理所有全息图（回合结束时调用）
    /// </summary>
    public static void ClearAllHolograms()
    {
        foreach (var state in _hologramStates.Values)
        {
            if (state.CloneProp != null && state.CloneProp.IsValid)
            {
                state.CloneProp.AcceptInput("Kill");
            }
        }

        _hologramStates.Clear();
        Console.WriteLine("[全息图] 已清理所有全息图");
    }

    /// <summary>
    /// 全息图状态
    /// </summary>
    private class HologramState
    {
        public required CCSPlayerController Player { get; set; }
        public CDynamicProp? CloneProp { get; set; }
        public float UseTime { get; set; }
    }
}
