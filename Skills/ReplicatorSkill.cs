// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills by Juzlus (Replicator skill)

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 复制品技能 - 主动技能
/// 点击创建一个复制品，该复制品会在击中时造成伤害
/// 完全复制自 jRandomSkills Replicator
/// </summary>
public class ReplicatorSkill : PlayerSkill
{
    public override string Name => "Replicator";
    public override string DisplayName => "🎭 复制品";
    public override string Description => "点击创建一个复制品，该复制品会在击中时造成伤害！持续15秒！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 15.0f; // 15秒冷却（与 jRandomSkills 一致）

    // 伤害值（与 jRandomSkills 一致）
    private const int YOUR_TEAM_DAMAGE = 10;
    private const int ENEMY_TEAM_DAMAGE = 20;

    // 复制品生成距离（与 jRandomSkills 一致）
    private const float SPAWN_DISTANCE = 40.0f;

    // 复制品持续时间（秒）
    private const float REPLICA_LIFETIME = 15.0f;

    // 跟踪所有复制品
    private readonly Dictionary<ulong, List<uint>> _playerReplicas = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[复制品] {player.PlayerName} 获得了复制品技能");
        player.PrintToChat("🎭 你获得了复制品技能！");
        player.PrintToChat("💡 输入 !useskill 或按键创建复制品！");
        player.PrintToChat($"⏱️ 冷却时间：{Cooldown}秒");
        player.PrintToChat($"⚔️ 敌人击中复制品造成{ENEMY_TEAM_DAMAGE}伤害，队友击中造成{YOUR_TEAM_DAMAGE}伤害");

        // 初始化复制品列表
        if (!_playerReplicas.ContainsKey(player.SteamID))
            _playerReplicas[player.SteamID] = new List<uint>();
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 移除该玩家的所有复制品
        RemoveAllReplicas(player);

        _playerReplicas.Remove(player.SteamID);

        Console.WriteLine($"[复制品] {player.PlayerName} 失去了复制品技能");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        Console.WriteLine($"[复制品] {player.PlayerName} 激活了复制品技能");

        // 创建复制品
        CreateReplica(player);

        player.PrintToChat("🎭 复制品已创建！");
        player.PrintToChat($"💡 复制品持续 {REPLICA_LIFETIME} 秒，被击中时会对攻击者造成伤害！");
    }

    /// <summary>
    /// 创建玩家复制品
    /// 完全复制自 jRandomSkills Replicator.CreateReplica
    /// </summary>
    private void CreateReplica(CCSPlayerController player)
    {
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null || playerPawn.AbsRotation == null)
            return;

        // 创建复制品实体
        var replica = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
        if (replica == null || !replica.IsValid)
            return;

        // 计算生成位置（玩家前方）
        Vector pos = playerPawn.AbsOrigin + GetForwardVector(playerPawn.AbsRotation) * SPAWN_DISTANCE;

        // 如果玩家在蹲下，调整高度
        if (((PlayerFlags)playerPawn.Flags).HasFlag(PlayerFlags.FL_DUCKING))
            pos.Z -= 19;

        // 设置复制品属性
        replica.Flags = playerPawn.Flags;
        replica.Flags |= (uint)Flags_t.FL_DUCKING;
        replica.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
        replica.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(replica.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));

        // 设置模型（使用玩家的模型）
        replica.SetModel(playerPawn.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName);

        // 设置实体名称（用于识别队伍）
        string teamSuffix = player.Team == CsTeam.CounterTerrorist ? "CT" : "TT";
        replica.Entity!.Name = replica.Globalname = $"Replica_{Server.TickCount}_{teamSuffix}";

        // 传送到位置并生成
        replica.Teleport(pos, playerPawn.AbsRotation, null);
        replica.DispatchSpawn();

        // 记录复制品
        if (!_playerReplicas.ContainsKey(player.SteamID))
            _playerReplicas[player.SteamID] = new List<uint>();

        _playerReplicas[player.SteamID].Add(replica.EntityHandle.Raw);

        Console.WriteLine($"[复制品] {player.PlayerName} 创建了复制品，位置: ({pos.X}, {pos.Y}, {pos.Z})");

        // 15秒后自动销毁
        if (Plugin != null)
        {
            Plugin.AddTimer(REPLICA_LIFETIME, () =>
            {
                if (replica != null && replica.IsValid)
                {
                    replica.AcceptInput("Kill");
                    _playerReplicas[player.SteamID]?.Remove(replica.EntityHandle.Raw);
                    Console.WriteLine($"[复制品] {player.PlayerName} 的复制品已过期销毁");
                }
            });
        }
    }

    /// <summary>
    /// 处理复制品受到伤害事件
    /// 完全复制自 jRandomSkills Replicator.OnTakeDamage
    /// </summary>
    public void OnEntityTakeDamage(DynamicHook hook)
    {
        // 获取伤害参数
        var entity = hook.GetParam<CEntityInstance>(0);
        var damageInfo = hook.GetParam<CTakeDamageInfo>(1);

        if (entity == null || entity.Entity == null || damageInfo == null)
            return;

        if (damageInfo.Attacker == null || damageInfo.Attacker.Value == null)
            return;

        // 检查是否是复制品
        if (string.IsNullOrEmpty(entity.Entity.Name))
            return;

        if (!entity.Entity.Name.StartsWith("Replica_"))
            return;

        var replica = entity.As<CPhysicsPropMultiplayer>();
        if (replica == null || !replica.IsValid)
            return;

        // 播放破碎声音并销毁复制品
        replica.EmitSound("GlassBottle.BulletImpact", volume: 1f);
        replica.AcceptInput("Kill");

        // 从玩家列表中移除
        foreach (var kvp in _playerReplicas)
        {
            kvp.Value.Remove(replica.EntityHandle.Raw);
        }

        // 获取攻击者
        CCSPlayerPawn attackerPawn = new(damageInfo.Attacker.Value.Handle);
        if (attackerPawn.DesignerName != "player")
            return;

        // 判断攻击者队伍
        var attackerTeam = attackerPawn.TeamNum;
        var replicaTeam = replica.Globalname.EndsWith("CT") ? 3 : 2;

        // 对攻击者造成伤害（队友击中10伤害，敌人击中20伤害）
        int damage = attackerTeam != replicaTeam ? ENEMY_TEAM_DAMAGE : YOUR_TEAM_DAMAGE;

        // 扣除血量
        attackerPawn.Health -= damage;

        // 检查是否死亡
        if (attackerPawn.Health <= 0)
        {
            attackerPawn.CommitSuicide(false, true);
        }

        Utilities.SetStateChanged(attackerPawn, "CBaseEntity", "m_iHealth");

        Console.WriteLine($"[复制品] 攻击者击中复制品，受到 {damage} 点伤害");

        // 通知攻击者
        var attacker = Utilities.GetPlayers().FirstOrDefault(p => p?.PlayerPawn?.Value?.Index == attackerPawn.Index);
        if (attacker != null && attacker.IsValid)
        {
            attacker.PrintToCenter($"🎭 击中复制品！受到 {damage} 点伤害！");
        }
    }

    /// <summary>
    /// 移除玩家的所有复制品
    /// </summary>
    private void RemoveAllReplicas(CCSPlayerController player)
    {
        if (!_playerReplicas.TryGetValue(player.SteamID, out var replicas))
            return;

        foreach (var replicaHandle in replicas)
        {
            var entity = Utilities.GetEntityFromIndex<CBaseEntity>((int)replicaHandle);
            if (entity != null && entity.IsValid)
            {
                entity.AcceptInput("Kill");
            }
        }

        _playerReplicas.Remove(player.SteamID);

        Console.WriteLine($"[复制品] 已移除 {player.PlayerName} 的所有复制品");
    }

    /// <summary>
    /// 计算前方向量
    /// 复制自 jRandomSkills SkillUtils.GetForwardVector
    /// </summary>
    private static Vector GetForwardVector(QAngle angles)
    {
        float radiansY = angles.Y * (float)Math.PI / 180.0f;

        return new Vector(
            (float)Math.Cos(radiansY),
            (float)Math.Sin(radiansY),
            0
        );
    }
}
