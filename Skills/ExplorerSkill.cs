// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills by Juzlus (Darkness skill for ApplyScreenColor)

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using MyrtleSkill.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 探索者技能 - 被动技能
/// 创建会移动的探索者实体，玩家击中探索者会被致盲（黑暗效果）
/// </summary>
public class ExplorerSkill : PlayerSkill
{
    public override string Name => "Explorer";
    public override string DisplayName => "🔭 探索者";
    public override string Description => "点击 [css_useSkill] 创建探索者！敌人击中它会被致盲2.5秒！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 15.0f; // 15秒冷却

    // 黑暗效果参数（参考 jRandomSkills Darkness）
    private const float DARKNESS_BRIGHTNESS = 0.01f;  // 曝光度（0.01 = 接近全黑）
    private const float DARKNESS_DURATION = 2.5f;     // 持续时间（秒）

    // 探索者生成距离（参考 ReplicatorSkill）
    private const float SPAWN_DISTANCE = 40.0f;

    // 探索者持续时间（秒）
    private const float EXPLORER_LIFETIME = 15.0f;

    // 跟踪玩家的探索者实体
    private readonly Dictionary<ulong, List<CDynamicProp>> _playerExplorers = new();

    // 跟踪每个探索者是否已经被击中（每个探索者只能触发一次致盲）
    private readonly Dictionary<uint, bool> _explorerTriggered = new();

    // 跟踪玩家的默认 PostProcessingVolume（用于恢复）
    private readonly Dictionary<ulong, List<CPostProcessingVolume>> _defaultPostProcessings = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        _playerExplorers[player.SteamID] = new List<CDynamicProp>();

        Console.WriteLine($"[探索者] {player.PlayerName} 获得了探索者技能");

        player.PrintToChat("🔭 你获得了探索者技能！");
        player.PrintToChat("💡 输入 !useskill 或按键创建探索者！");
        player.PrintToChat($"⏱️ 冷却时间：{Cooldown}秒");
        player.PrintToChat("⚔️ 敌人击中探索者会被致盲2.5秒！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 移除该玩家的所有探索者
        RemoveAllExplorers(player);

        // 恢复玩家的 PostProcessingVolume（如果处于黑暗状态）
        if (_defaultPostProcessings.ContainsKey(player.SteamID))
        {
            RemoveDarkness(player);
        }

        _playerExplorers.Remove(player.SteamID);

        Console.WriteLine($"[探索者] {player.PlayerName} 失去了探索者技能");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        Console.WriteLine($"[探索者] {player.PlayerName} 激活了探索者技能");

        // 创建探索者
        CreateExplorer(player);
    }

    /// <summary>
    /// 创建探索者实体（参考 FortniteSkill 的两步创建法）
    /// </summary>
    private void CreateExplorer(CCSPlayerController player)
    {
        var playerPawn = player.PlayerPawn.Value;
        var explorer = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
        if (explorer == null || playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null || playerPawn.AbsRotation == null)
            return;

        Vector pos = playerPawn.AbsOrigin + GetForwardVector(playerPawn.AbsRotation) * SPAWN_DISTANCE;

        if (((PlayerFlags)playerPawn.Flags).HasFlag(PlayerFlags.FL_DUCKING))
            pos.Z -= 19;

        // 设置实体属性（在生成前）
        explorer.Flags = playerPawn.Flags;
        explorer.Flags |= (uint)Flags_t.FL_DUCKING;
        explorer.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
        explorer.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(explorer.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));

        // 设置名称（用于识别）
        explorer.Entity!.Name = explorer.Globalname = $"Explorer_{Server.TickCount}_{(player.Team == CsTeam.CounterTerrorist ? "CT" : "TT")}";

        // 第一步：先生成实体
        explorer.DispatchSpawn();

        // 标记为未触发（每个探索者只能造成一次致盲）
        _explorerTriggered[explorer.Index] = false;

        // 添加到玩家的探索者列表
        if (!_playerExplorers.ContainsKey(player.SteamID))
            _playerExplorers[player.SteamID] = new List<CDynamicProp>();
        _playerExplorers[player.SteamID].Add(explorer);

        // 第二步：在下一帧设置模型和位置（参考 FortniteSkill）
        Server.NextFrame(() =>
        {
            if (!explorer.IsValid)
                return;

            try
            {
                // 获取玩家模型
                string playerModel = playerPawn!.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName;

                // 设置模型
                explorer.SetModel(playerModel);

                // 设置位置和旋转
                explorer.Teleport(pos, playerPawn.AbsRotation, null);

                Console.WriteLine($"[探索者] 为 {player.PlayerName} 创建了探索者实体");

                // 设置自动销毁（不移动，静置）
                Plugin?.AddTimer(EXPLORER_LIFETIME, () =>
                {
                    if (explorer != null && explorer.IsValid)
                    {
                        explorer.AcceptInput("Kill");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[探索者] 创建探索者时出错: {ex.Message}");
                explorer.AcceptInput("Kill");
            }
        });
    }

    /// <summary>
    /// 处理探索者受到伤害事件（参考 ReplicatorSkill）
    /// </summary>
    public void OnEntityTakeDamage(DynamicHook h)
    {
        CEntityInstance param = h.GetParam<CEntityInstance>(0);
        CTakeDamageInfo param2 = h.GetParam<CTakeDamageInfo>(1);

        if (param == null || param.Entity == null || param2 == null || param2.Attacker == null || param2.Attacker.Value == null)
            return;

        if (string.IsNullOrEmpty(param.Entity.Name)) return;
        if (!param.Entity.Name.StartsWith("Explorer_")) return;

        var explorer = param.As<CDynamicProp>();
        if (explorer == null || !explorer.IsValid) return;

        // 检查该探索者是否已经被击中过（每个探索者只能触发一次致盲）
        if (_explorerTriggered.TryGetValue(explorer.Index, out bool triggered) && triggered)
        {
            Console.WriteLine($"[探索者] 探索者 {explorer.Index} 已经触发过，跳过");
            return;
        }

        // 关键：在 Kill 之前保存 Globalname（避免崩溃）
        string explorerGlobalName = explorer.Globalname ?? "";

        // 立即标记为已触发（必须在 Kill 之前！）
        _explorerTriggered[explorer.Index] = true;

        explorer.EmitSound("GlassBottle.BulletImpact", volume: 1f);
        explorer.AcceptInput("Kill");

        CCSPlayerPawn attackerPawn = new(param2.Attacker.Value.Handle);
        if (attackerPawn.DesignerName != "player")
            return;

        var attacker = attackerPawn.OriginalController.Value;
        if (attacker == null || !attacker.IsValid)
            return;

        // 应用黑暗效果
        ApplyDarkness(attacker);

        Console.WriteLine($"[探索者] {attacker.PlayerName} 击中了探索者，被致盲！");
    }

    /// <summary>
    /// 应用黑暗效果（完全复制 jRandomSkills Darkness.SetUpPostProcessing）
    /// </summary>
    private void ApplyDarkness(CCSPlayerController target)
    {
        if (target == null || !target.IsValid)
            return;

        var pawn = target.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.CameraServices == null)
            return;

        try
        {
            // 初始化默认 PostProcessingVolume 列表
            if (!_defaultPostProcessings.ContainsKey(target.SteamID))
                _defaultPostProcessings[target.SteamID] = new List<CPostProcessingVolume>();

            int i = 0;
            foreach (var postProcessingVolume in pawn.CameraServices.PostProcessingVolumes)
            {
                if (postProcessingVolume == null || postProcessingVolume.Value == null)
                    continue;

                // 保存原始的 PostProcessingVolume
                if (_defaultPostProcessings.TryGetValue(target.SteamID, out var defaultList))
                    defaultList.Add(postProcessingVolume.Value);

                // 创建新的 PostProcessingVolume（复制自 jRandomSkills）
                var postProcessing = Utilities.CreateEntityByName<CPostProcessingVolume>("post_processing_volume");
                if (postProcessing == null)
                    continue;

                // 设置曝光度为接近全黑（复制自 jRandomSkills）
                postProcessing.ExposureControl = true;
                postProcessing.MaxExposure = DARKNESS_BRIGHTNESS;
                postProcessing.MinExposure = DARKNESS_BRIGHTNESS;

                // 替换原来的 PostProcessingVolume
                postProcessingVolume.Raw = postProcessing.EntityHandle.Raw;

                Console.WriteLine($"[探索者] 对 {target.PlayerName} 应用了黑暗效果（曝光度：{DARKNESS_BRIGHTNESS}）");
                i++;
            }

            // 通知状态变更
            Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");

            // 设置定时恢复
            Plugin?.AddTimer(DARKNESS_DURATION, () =>
            {
                RemoveDarkness(target);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[探索者] 应用黑暗效果时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 移除黑暗效果（完全复制 jRandomSkills Darkness.SetUpPostProcessing(player, true)）
    /// </summary>
    private void RemoveDarkness(CCSPlayerController target)
    {
        if (target == null || !target.IsValid)
            return;

        var pawn = target.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.CameraServices == null)
            return;

        try
        {
            int i = 0;
            foreach (var postProcessingVolume in pawn.CameraServices.PostProcessingVolumes)
            {
                if (postProcessingVolume == null || postProcessingVolume.Value == null)
                    continue;

                // 恢复默认的 PostProcessingVolume（复制自 jRandomSkills）
                if (_defaultPostProcessings.TryGetValue(target.SteamID, out var defaultList) && i < defaultList.Count)
                    postProcessingVolume.Raw = defaultList[i].EntityHandle.Raw;

                i++;
            }

            // 通知状态变更
            Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");

            // 移除保存的默认值
            _defaultPostProcessings.Remove(target.SteamID);

            Console.WriteLine($"[探索者] {target.PlayerName} 的视野恢复了");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[探索者] 移除黑暗效果时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 移除玩家的所有探索者
    /// </summary>
    private void RemoveAllExplorers(CCSPlayerController player)
    {
        if (!_playerExplorers.TryGetValue(player.SteamID, out var explorers))
            return;

        foreach (var explorer in explorers.ToList())
        {
            if (explorer != null && explorer.IsValid)
            {
                explorer.AcceptInput("Kill");
            }
            // 清理 flag
            _explorerTriggered.Remove(explorer.Index);
        }

        _playerExplorers.Remove(player.SteamID);

        Console.WriteLine($"[探索者] 已移除 {player.PlayerName} 的所有探索者");
    }

    /// <summary>
    /// 计算前方向量（参考 ReplicatorSkill）
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
