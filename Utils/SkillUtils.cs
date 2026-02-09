using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using System.Runtime.InteropServices;

namespace MyrtleSkill.Utils;

/// <summary>
/// 技能工具类
/// </summary>
public static class SkillUtils
{
    /// <summary>
    /// 对玩家造成真正的伤害（类似枪械伤害）
    /// 使用CS2的伤害系统，会被护甲减免，计入击杀信息等
    /// </summary>
    /// <param name="attacker">攻击者</param>
    /// <param name="victim">受害者</param>
    /// <param name="damage">伤害值</param>
    /// <param name="damageType">伤害类型（默认为DMG_GENERIC）</param>
    public static void DealDamage(CCSPlayerController attacker, CCSPlayerController victim, int damage,
        DamageTypes_t damageType = DamageTypes_t.DMG_GENERIC)
    {
        if (attacker == null || !attacker.IsValid || attacker.PlayerPawn.Value == null)
            return;

        if (victim == null || !victim.IsValid || victim.PlayerPawn.Value == null)
            return;

        var victimPawn = victim.PlayerPawn.Value;
        if (!victimPawn.IsValid || victimPawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            return;

        // 分配并初始化 CTakeDamageInfo
        var damageInfoSize = Schema.GetClassSize("CTakeDamageInfo");
        var damageInfoPtr = Marshal.AllocHGlobal(damageInfoSize);

        // 清零内存
        for (var i = 0; i < damageInfoSize; i++)
            Marshal.WriteByte(damageInfoPtr, i, 0);

        var damageInfo = new CTakeDamageInfo(damageInfoPtr);

        // 获取攻击者的Pawn handle
        var attackerPawnHandle = attacker.PlayerPawn;
        var attackerPawn = attackerPawnHandle.Value;

        // 设置攻击者和伤害信息（使用CHandle的Raw属性）
        Schema.SetSchemaValue(damageInfo.Handle, "CTakeDamageInfo", "m_hInflictor",
            attackerPawnHandle.Raw);
        Schema.SetSchemaValue(damageInfo.Handle, "CTakeDamageInfo", "m_hAttacker",
            attackerPawnHandle.Raw);

        damageInfo.Damage = damage;
        damageInfo.BitsDamageType = damageType;

        // 分配并初始化 CTakeDamageResult
        var damageResultSize = Schema.GetClassSize("CTakeDamageResult");
        var damageResultPtr = Marshal.AllocHGlobal(damageResultSize);

        for (var i = 0; i < damageResultSize; i++)
            Marshal.WriteByte(damageResultPtr, i, 0);

        var damageResult = new CTakeDamageResult(damageResultPtr);
        Schema.SetSchemaValue(damageResult.Handle, "CTakeDamageResult", "m_pOriginatingInfo", damageInfo.Handle);

        damageResult.HealthBefore = victimPawn.Health;
        damageResult.HealthLost = damage;
        damageResult.DamageDealt = damage;
        damageResult.PreModifiedDamage = damage;
        damageResult.TotalledHealthLost = damage;
        damageResult.TotalledDamageDealt = damage;
        damageResult.WasDamageSuppressed = false;

        // 调用游戏伤害函数
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Invoke(victimPawn, damageInfo, damageResult);

        // 释放内存
        Marshal.FreeHGlobal(damageInfoPtr);
        Marshal.FreeHGlobal(damageResultPtr);

        Console.WriteLine($"[SkillUtils] {attacker.PlayerName} 对 {victim.PlayerName} 造成了 {damage} 点伤害");
    }

    /// <summary>
    /// 直接扣除生命值（复制自 jRandomSkills SkillUtils.TakeHealth）
    /// 简单的生命值扣除，不会触发伤害事件
    /// </summary>
    /// <param name="pawn">玩家Pawn</param>
    /// <param name="damage">伤害值</param>
    public static void TakeHealth(CCSPlayerPawn pawn, int damage)
    {
        if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            return;

        int newHealth = (int)(pawn.Health - damage);
        pawn.Health = newHealth;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        // 检查是否死亡（复制自 jRandomSkills）
        if (pawn.Health <= 0)
        {
            Server.NextFrame(() =>
            {
                pawn?.CommitSuicide(false, true);
            });
        }

        Console.WriteLine($"[SkillUtils] TakeHealth: 扣除 {damage} 点生命值，剩余生命: {newHealth}");
    }

    /// <summary>
    /// 检查位置是否与其他玩家重合（碰撞检测）
    /// </summary>
    /// <param name="position">要检查的位置</param>
    /// <param name="excludePlayer">要排除的玩家（通常是自己）</param>
    /// <param name="distanceThreshold">距离阈值（默认50单位）</param>
    /// <returns>如果位置安全（没有玩家重合）返回 true，否则返回 false</returns>
    public static bool IsPositionSafe(Vector position, CCSPlayerController? excludePlayer = null, float distanceThreshold = 50.0f)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            // 排除无效玩家和指定玩家
            if (!player.IsValid || !player.PawnIsAlive)
                continue;

            if (excludePlayer != null && player.SteamID == excludePlayer.SteamID)
                continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
                continue;

            // 计算距离
            float dx = position.X - pawn.AbsOrigin.X;
            float dy = position.Y - pawn.AbsOrigin.Y;
            float dz = position.Z - pawn.AbsOrigin.Z;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);

            // 如果距离小于阈值，说明位置不安全
            if (distance < distanceThreshold)
            {
                Console.WriteLine($"[SkillUtils] 位置不安全：与玩家 {player.PlayerName} 距离 {distance:F1} 单位");
                return false;
            }
        }

        return true;
    }
}
