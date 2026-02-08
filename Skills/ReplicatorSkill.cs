// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills by Juzlus (Replicator skill)

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using MyrtleSkill.Utils;

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

    // 跟踪每个复制体是否已经被击中（每个复制体只能触发一次伤害）
    private readonly Dictionary<uint, bool> _replicaTriggered = new();

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
    }

    /// <summary>
    /// 创建玩家复制品（参考 FortniteSkill 的两步创建法）
    /// </summary>
    private void CreateReplica(CCSPlayerController player)
    {
        var playerPawn = player.PlayerPawn.Value;
        var replica = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
        if (replica == null || playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null || playerPawn.AbsRotation == null)
            return;

        float distance = 40;
        Vector pos = playerPawn.AbsOrigin + GetForwardVector(playerPawn.AbsRotation) * distance;

        if (((PlayerFlags)playerPawn.Flags).HasFlag(PlayerFlags.FL_DUCKING))
            pos.Z -= 19;

        // 设置实体属性（在生成前）
        replica.Flags = playerPawn.Flags;
        replica.Flags |= (uint)Flags_t.FL_DUCKING;
        replica.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
        replica.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(replica.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));

        // 设置名称（用于识别）
        replica.Entity!.Name = replica.Globalname = $"Replica_{Server.TickCount}_{(player.Team == CsTeam.CounterTerrorist ? "CT" : "TT")}";

        // 第一步：先生成实体
        replica.DispatchSpawn();

        // 标记为未触发（每个复制体只能造成一次伤害）
        _replicaTriggered[replica.Index] = false;

        // 第二步：在下一帧设置模型和位置（参考 FortniteSkill）
        Server.NextFrame(() =>
        {
            if (!replica.IsValid)
                return;

            try
            {
                // 获取玩家模型
                string playerModel = playerPawn!.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName;

                // 设置模型
                replica.SetModel(playerModel);

                // 设置位置和旋转
                replica.Teleport(pos, playerPawn.AbsRotation, null);

                Console.WriteLine($"[复制品] 为 {player.PlayerName} 创建了复制品");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[复制品] 创建复制品时出错: {ex.Message}");
                replica.AcceptInput("Kill");
            }
        });
    }

    /// <summary>
    /// 处理复制品受到伤害事件
    /// 完全复制 jRandomSkills Replicator.OnTakeDamage - 唯一修改是保存 Globalname 避免崩溃
    /// </summary>
    public void OnEntityTakeDamage(DynamicHook h)
    {
        CEntityInstance param = h.GetParam<CEntityInstance>(0);
        CTakeDamageInfo param2 = h.GetParam<CTakeDamageInfo>(1);

        if (param == null || param.Entity == null || param2 == null || param2.Attacker == null || param2.Attacker.Value == null)
            return;

        if (string.IsNullOrEmpty(param.Entity.Name)) return;
        if (!param.Entity.Name.StartsWith("Replica_")) return;

        var replica = param.As<CPhysicsPropMultiplayer>();
        if (replica == null || !replica.IsValid) return;

        // 调试：输出每次调用
        Console.WriteLine($"[复制品] OnEntityTakeDamage 被调用，实体索引: {replica.Index}, Flag状态: {(_replicaTriggered.TryGetValue(replica.Index, out bool flag) ? flag : false)}");

        // 检查该复制体是否已经被击中过（每个复制体只能触发一次伤害）
        if (_replicaTriggered.TryGetValue(replica.Index, out bool triggered) && triggered)
        {
            Console.WriteLine($"[复制品] 复制体 {replica.Index} 已经触发过，跳过");
            return;
        }

        // 关键修改：在 Kill 之前保存 Globalname（避免崩溃）
        string replicaGlobalName = replica.Globalname ?? "";

        // 立即标记为已触发（必须在 Kill 之前！）
        _replicaTriggered[replica.Index] = true;

        Console.WriteLine($"[复制品] 设置复制体 {replica.Index} Flag = true");

        replica.EmitSound("GlassBottle.BulletImpact", volume: 1f);
        replica.AcceptInput("Kill");

        CCSPlayerPawn attackerPawn = new(param2.Attacker.Value.Handle);
        if (attackerPawn.DesignerName != "player")
            return;

        var attackerTeam = attackerPawn.TeamNum;
        // 使用保存的 Globalname
        var replicaTeam = replicaGlobalName.EndsWith("CT") ? 3 : 2;

        Console.WriteLine($"[复制品] 准备调用 TakeHealth，攻击者队伍: {attackerTeam}, 复制体队伍: {replicaTeam}");

        SkillUtils.TakeHealth(attackerPawn, attackerTeam != replicaTeam ? ENEMY_TEAM_DAMAGE : YOUR_TEAM_DAMAGE);

        Console.WriteLine($"[复制品] 复制体 {replica.Index} 被击中，造成 {(attackerTeam != replicaTeam ? ENEMY_TEAM_DAMAGE : YOUR_TEAM_DAMAGE)} 点伤害");
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
            // 清理 flag
            _replicaTriggered.Remove(replicaHandle);
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
