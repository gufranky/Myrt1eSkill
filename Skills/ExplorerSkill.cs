// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills by Juzlus (Replicator + Darkness)

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 探索者技能 - 主动技能
/// 制造一个复制品慢慢往前移动，击中的人屏幕黑暗2.5秒
/// 参考实现：jRandomSkills Replicator + Darkness
/// </summary>
public class ExplorerSkill : PlayerSkill
{
    public override string Name => "Explorer";
    public override string DisplayName => "🔍 探索者";
    public override string Description => "制造一个复制品慢慢往前移动，击中的人屏幕黑暗2.5秒！持续5秒！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 20.0f; // 20秒冷却

    // 黑暗效果持续时间（秒）
    private const float DARKNESS_DURATION = 2.5f;

    // 复制品持续时间（秒）
    private const float EXPLORER_LIFETIME = 5.0f;

    // 移动速度（单位/秒）
    private const float MOVE_SPEED = 100.0f;

    // 跟踪所有复制品（SteamID, List<实体Handle>)
    private readonly Dictionary<ulong, List<uint>> _playerExplorers = new();

    // 跟踪复制品的移动方向（实体Handle, 方向向量）
    private readonly Dictionary<uint, Vector> _explorerDirections = new();

    // 跟踪被黑暗的玩家（玩家, 结束时间）
    private readonly Dictionary<CCSPlayerController, float> _darkenedPlayers = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[探索者] {player.PlayerName} 获得了探索者技能");
        player.PrintToChat("🔍 你获得了探索者技能！");
        player.PrintToChat("💡 输入 !useskill 或按键创建探索者复制品！");
        player.PrintToChat($"⏱️ 冷却时间：{Cooldown}秒");
        player.PrintToChat($"⚠️ 复制品持续{EXPLORER_LIFETIME}秒，击中人使其屏幕黑暗{DARKNESS_DURATION}秒");

        // 初始化复制品列表
        if (!_playerExplorers.ContainsKey(player.SteamID))
            _playerExplorers[player.SteamID] = new List<uint>();

        // 注册 OnTick 监听（用于移动复制品和检查黑暗效果）
        if (_playerExplorers.Count == 1 && Plugin != null)
        {
            Plugin.RegisterListener<Listeners.OnTick>(OnTick);
        }
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 移除该玩家的所有复制品
        RemoveAllExplorers(player);

        // 移除该玩家施加的所有黑暗效果
        RemoveAllDarkness(player);

        _playerExplorers.Remove(player.SteamID);

        // 如果没有玩家了，移除 OnTick 监听
        if (_playerExplorers.Count == 0 && Plugin != null)
        {
            Plugin.RemoveListener<Listeners.OnTick>(OnTick);
        }

        Console.WriteLine($"[探索者] {player.PlayerName} 失去了探索者技能");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        Console.WriteLine($"[探索者] {player.PlayerName} 激活了探索者技能");

        // 创建探索者复制品
        CreateExplorer(player);

        player.PrintToChat("🔍 探索者复制品已创建！");
        player.PrintToChat($"💡 复制品会向前移动{EXPLORER_LIFETIME}秒，击中敌人使其黑暗{DARKNESS_DURATION}秒！");
    }

    /// <summary>
    /// 创建探索者复制品（参考 Replicator）
    /// </summary>
    private void CreateExplorer(CCSPlayerController player)
    {
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null || playerPawn.AbsRotation == null)
            return;

        // 创建复制品实体
        var explorer = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
        if (explorer == null || !explorer.IsValid)
            return;

        // 计算生成位置（玩家前方）
        Vector forward = GetForwardVector(playerPawn.AbsRotation);
        Vector pos = playerPawn.AbsOrigin + forward * 40.0f;

        // 如果玩家在蹲下，调整高度
        if (((PlayerFlags)playerPawn.Flags).HasFlag(PlayerFlags.FL_DUCKING))
            pos.Z -= 19;

        // 设置复制品属性
        explorer.Flags = playerPawn.Flags;
        explorer.Flags |= (uint)Flags_t.FL_DUCKING;
        explorer.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
        explorer.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(explorer.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));

        // 设置模型（使用玩家的模型）
        explorer.SetModel(playerPawn.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName);

        // 设置实体名称（用于识别）
        string teamSuffix = player.Team == CsTeam.CounterTerrorist ? "CT" : "TT";
        explorer.Entity!.Name = explorer.Globalname = $"Explorer_{Server.TickCount}_{teamSuffix}";

        // 传送到位置并生成
        explorer.Teleport(pos, playerPawn.AbsRotation, null);
        explorer.DispatchSpawn();

        // 记录复制品
        if (!_playerExplorers.ContainsKey(player.SteamID))
            _playerExplorers[player.SteamID] = new List<uint>();

        _playerExplorers[player.SteamID].Add(explorer.EntityHandle.Raw);

        // 记录移动方向（水平方向，不包含垂直分量）
        Vector moveDirection = new Vector(forward.X, forward.Y, 0);
        // 归一化方向
        float length = (float)Math.Sqrt(moveDirection.X * moveDirection.X + moveDirection.Y * moveDirection.Y);
        if (length > 0.001f)
        {
            moveDirection.X /= length;
            moveDirection.Y /= length;
        }
        _explorerDirections[explorer.EntityHandle.Raw] = moveDirection;

        Console.WriteLine($"[探索者] {player.PlayerName} 创建了探索者，方向: ({moveDirection.X}, {moveDirection.Y}, 0)");

        // 5秒后自动销毁
        if (Plugin != null)
        {
            Plugin.AddTimer(EXPLORER_LIFETIME, () =>
            {
                if (explorer != null && explorer.IsValid)
                {
                    explorer.AcceptInput("Kill");
                    _playerExplorers[player.SteamID]?.Remove(explorer.EntityHandle.Raw);
                    _explorerDirections.Remove(explorer.EntityHandle.Raw);
                    Console.WriteLine($"[探索者] {player.PlayerName} 的探索者已过期销毁");
                }
            });
        }
    }

    /// <summary>
    /// 每帧更新 - 移动探索者复制品，检查黑暗效果
    /// </summary>
    public void OnTick()
    {
        // 1. 移动所有探索者复制品
        foreach (var player in Utilities.GetPlayers())
        {
            if (!_playerExplorers.TryGetValue(player.SteamID, out var explorers))
                continue;

            foreach (var explorerHandle in explorers)
            {
                var explorer = Utilities.GetEntityFromIndex<CDynamicProp>((int)explorerHandle);
                if (explorer == null || !explorer.IsValid || explorer.AbsOrigin == null || explorer.AbsVelocity == null)
                    continue;

                // 获取移动方向
                if (!_explorerDirections.TryGetValue(explorerHandle, out Vector direction))
                    continue;

                // 计算速度（每帧移动）
                // 假设 64 tick/s，每帧速度 = MOVE_SPEED / 64
                float speedPerTick = MOVE_SPEED / 64.0f;
                Vector newVelocity = direction * speedPerTick;

                // 设置速度（保持现有垂直速度，只修改水平速度）
                explorer.AbsVelocity.X = newVelocity.X;
                explorer.AbsVelocity.Y = newVelocity.Y;
                // Z轴保持为0（不增加垂直速度）
                explorer.AbsVelocity.Z = 0;

                // 通知状态改变
                Utilities.SetStateChanged(explorer, "CBaseEntity", "m_vecAbsVelocity");
            }
        }

        // 2. 检查黑暗效果是否过期
        float currentTime = Server.CurrentTime;
        var expiredPlayers = _darkenedPlayers
            .Where(kvp => kvp.Value <= currentTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var player in expiredPlayers)
        {
            RemoveDarkness(player);
        }
    }

    /// <summary>
    /// 处理探索者受到伤害事件
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

        // 检查是否是探索者
        if (string.IsNullOrEmpty(entity.Entity.Name))
            return;

        if (!entity.Entity.Name.StartsWith("Explorer_"))
            return;

        var explorer = entity.As<CPhysicsPropMultiplayer>();
        if (explorer == null || !explorer.IsValid)
            return;

        // 播放破碎声音并销毁探索者
        explorer.EmitSound("GlassBottle.BulletImpact", volume: 1f);
        explorer.AcceptInput("Kill");

        // 从列表中移除
        foreach (var kvp in _playerExplorers)
        {
            kvp.Value.Remove(explorer.EntityHandle.Raw);
        }
        _explorerDirections.Remove(explorer.EntityHandle.Raw);

        // 获取攻击者
        CCSPlayerPawn attackerPawn = new(damageInfo.Attacker.Value.Handle);
        if (attackerPawn.DesignerName != "player")
            return;

        var attacker = Utilities.GetPlayers().FirstOrDefault(p => p?.PlayerPawn?.Value?.Index == attackerPawn.Index);
        if (attacker == null || !attacker.IsValid)
            return;

        // 对攻击者施加黑暗效果
        ApplyDarkness(attacker);

        Console.WriteLine($"[探索者] {attacker.PlayerName} 击中探索者，屏幕黑暗{DARKNESS_DURATION}秒");

        // 通知攻击者
        attacker.PrintToCenter($"🔍 你击中了探索者！屏幕黑暗{DARKNESS_DURATION}秒！");
    }

    /// <summary>
    /// 对玩家施加黑暗效果（参考 DarknessSkill）
    /// </summary>
    private void ApplyDarkness(CCSPlayerController target)
    {
        if (target == null || !target.IsValid)
            return;

        var pawn = target.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.CameraServices == null)
            return;

        // 移除旧的黑暗效果
        RemoveDarkness(target);

        // 创建后处理体积
        var postProcessing = Utilities.CreateEntityByName<CPostProcessingVolume>("post_processing_volume");
        if (postProcessing == null || !postProcessing.IsValid)
            return;

        // 设置为完全黑暗
        postProcessing.ExposureControl = true;
        postProcessing.MaxExposure = 0.0f;
        postProcessing.MinExposure = 0.0f;

        // 替换所有PostProcessingVolumes
        foreach (var postProcessingVolume in pawn.CameraServices.PostProcessingVolumes)
        {
            if (postProcessingVolume != null && postProcessingVolume.Value != null)
            {
                postProcessingVolume.Raw = postProcessing.EntityHandle.Raw;
            }
        }

        Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");

        // 保存黑暗状态（这里简化，只保存时间）
        _darkenedPlayers[target] = Server.CurrentTime + DARKNESS_DURATION;

        // 移除实体（不需要保留，因为已经替换了视图）
        postProcessing.AcceptInput("Kill");

        Console.WriteLine($"[探索者] 对 {target.PlayerName} 施加黑暗，持续 {DARKNESS_DURATION} 秒");
    }

    /// <summary>
    /// 移除玩家的黑暗效果
    /// </summary>
    private void RemoveDarkness(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        _darkenedPlayers.Remove(player);

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.CameraServices == null)
            return;

        // 恢复默认的 PostProcessingVolumes（清除所有）
        foreach (var postProcessingVolume in pawn.CameraServices.PostProcessingVolumes)
        {
            if (postProcessingVolume != null && postProcessingVolume.Value != null)
            {
                // 清空引用
                postProcessingVolume.Raw = 0;
            }
        }

        Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");

        player.PrintToChat("🔍 你的视觉恢复了！");

        Console.WriteLine($"[探索者] 已移除 {player.PlayerName} 的黑暗效果");
    }

    /// <summary>
    /// 移除玩家施加的所有黑暗效果
    /// </summary>
    private void RemoveAllDarkness(CCSPlayerController player)
    {
        // 注意：这里简化处理，只移除该玩家作为施法者的黑暗效果
        // 由于我们使用的是单一字典存储所有黑暗效果，这里不做区分
        // 在实际使用中，可以改为按施法者分组存储
    }

    /// <summary>
    /// 移除玩家的所有探索者
    /// </summary>
    private void RemoveAllExplorers(CCSPlayerController player)
    {
        if (!_playerExplorers.TryGetValue(player.SteamID, out var explorers))
            return;

        foreach (var explorerHandle in explorers)
        {
            var entity = Utilities.GetEntityFromIndex<CBaseEntity>((int)explorerHandle);
            if (entity != null && entity.IsValid)
            {
                entity.AcceptInput("Kill");
            }
            _explorerDirections.Remove(explorerHandle);
        }

        _playerExplorers.Remove(player.SteamID);

        Console.WriteLine($"[探索者] 已移除 {player.PlayerName} 的所有探索者");
    }

    /// <summary>
    /// 计算前方向量
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
