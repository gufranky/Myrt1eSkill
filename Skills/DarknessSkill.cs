// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills by Juzlus (Darkness skill)

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 黑暗技能 - 随机对一名敌人施加黑暗效果
/// </summary>
public class DarknessSkill : PlayerSkill
{
    public override string Name => "Darkness";
    public override string DisplayName => "🌑 黑暗";
    public override string Description => "随机对一名敌人施加黑暗效果，让他们视野一片漆黑！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 30.0f; // 30秒冷却

    // 与雾蒙蒙事件互斥
    public override List<string> ExcludedEvents => new() { "Foggy" };

    // 黑暗参数（参考jRandomSkills：brightness = 0.01）
    private const float DARKNESS_BRIGHTNESS = 0.01f;

    // 黑暗效果持续时间（秒）
    private const float DARKNESS_DURATION = 10.0f;

    // 跟踪被施加黑暗效果的玩家
    private readonly Dictionary<int, DarknessState> _darknessStates = new();

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[黑暗] {player.PlayerName} 获得了黑暗技能");
        player.PrintToChat("🌑 你获得了黑暗技能！");
        player.PrintToChat("💡 输入 !useskill 或按键激活！");
        player.PrintToChat($"⏱️ 冷却时间：{Cooldown}秒");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        // 移除该玩家施加的所有黑暗效果
        RemoveAllDarkness(player);
        Console.WriteLine($"[黑暗] {player.PlayerName} 失去了黑暗技能");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        Console.WriteLine($"[黑暗] {player.PlayerName} 尝试使用黑暗技能");

        // 获取所有敌方玩家
        var enemies = Utilities.GetPlayers()
            .Where(p => p.IsValid && p.PawnIsAlive && p.Team != player.Team && !p.IsBot && !p.IsHLTV)
            .ToList();

        if (enemies.Count == 0)
        {
            player.PrintToChat("🌑 没有可用的目标！");
            return;
        }

        // 随机选择一名敌人
        var random = new Random();
        var targetEnemy = enemies[random.Next(enemies.Count)];

        // 施加黑暗效果
        ApplyDarkness(player, targetEnemy, DARKNESS_DURATION);

        player.PrintToChat($"🌑 你对 {targetEnemy.PlayerName} 施加了黑暗！");
        targetEnemy.PrintToChat($"🌑 你被 {player.PlayerName} 施加了黑暗效果，持续 {DARKNESS_DURATION} 秒！");

        Server.PrintToChatAll($"🌑 {targetEnemy.PlayerName} 陷入了黑暗！");

        Console.WriteLine($"[黑暗] {player.PlayerName} 对 {targetEnemy.PlayerName} 施加了黑暗效果");
    }

    /// <summary>
    /// 对玩家施加黑暗效果
    /// </summary>
    private void ApplyDarkness(CCSPlayerController caster, CCSPlayerController target, float duration)
    {
        if (target == null || !target.IsValid)
            return;

        var pawn = target.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.CameraServices == null)
            return;

        // 移除旧的黑暗效果
        RemoveDarkness(target);

        // 保存原始的PostProcessingVolumes
        var originalVolumes = new List<CPostProcessingVolume>();
        foreach (var postProcessingVolume in pawn.CameraServices.PostProcessingVolumes)
        {
            if (postProcessingVolume != null && postProcessingVolume.Value != null)
            {
                originalVolumes.Add(postProcessingVolume.Value);
            }
        }

        // 创建新的黑暗后处理体积
        var postProcessing = Utilities.CreateEntityByName<CPostProcessingVolume>("post_processing_volume");
        if (postProcessing != null && postProcessing.IsValid)
        {
            postProcessing.ExposureControl = true;
            postProcessing.MaxExposure = DARKNESS_BRIGHTNESS;
            postProcessing.MinExposure = DARKNESS_BRIGHTNESS;

            // 替换所有PostProcessingVolumes
            foreach (var postProcessingVolume in pawn.CameraServices.PostProcessingVolumes)
            {
                if (postProcessingVolume != null && postProcessingVolume.Value != null)
                {
                    postProcessingVolume.Raw = postProcessing.EntityHandle.Raw;
                }
            }

            Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");

            // 保存状态
            _darknessStates[target.Slot] = new DarknessState
            {
                TargetPlayer = target,
                CasterSteamID = caster.SteamID,
                OriginalVolumes = originalVolumes,
                PostProcessingEntity = postProcessing,
                EndTime = Server.CurrentTime + duration
            };

            Console.WriteLine($"[黑暗] {caster.PlayerName} 对 {target.PlayerName} 施加黑暗，持续 {duration} 秒");
        }
    }

    /// <summary>
    /// 移除玩家的黑暗效果
    /// </summary>
    private void RemoveDarkness(CCSPlayerController target)
    {
        if (target == null || !target.IsValid)
            return;

        if (!_darknessStates.TryGetValue(target.Slot, out var state))
            return;

        var pawn = target.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.CameraServices == null)
            return;

        // 恢复原始的PostProcessingVolumes
        int i = 0;
        foreach (var postProcessingVolume in pawn.CameraServices.PostProcessingVolumes)
        {
            if (postProcessingVolume != null && postProcessingVolume.Value != null && i < state.OriginalVolumes.Count)
            {
                postProcessingVolume.Raw = state.OriginalVolumes[i].EntityHandle.Raw;
                i++;
            }
        }

        Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");

        // 移除创建的实体
        if (state.PostProcessingEntity != null && state.PostProcessingEntity.IsValid)
        {
            state.PostProcessingEntity.AcceptInput("Kill");
        }

        _darknessStates.Remove(target.Slot);

        Console.WriteLine($"[黑暗] 已移除 {target.PlayerName} 的黑暗效果");
    }

    /// <summary>
    /// 移除该玩家施加的所有黑暗效果
    /// </summary>
    private void RemoveAllDarkness(CCSPlayerController caster)
    {
        var toRemove = _darknessStates
            .Where(kvp => kvp.Value.CasterSteamID == caster.SteamID)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var slot in toRemove)
        {
            if (_darknessStates.TryGetValue(slot, out var state))
            {
                RemoveDarkness(state.TargetPlayer);
            }
        }
    }

    /// <summary>
    /// 每帧更新（检查黑暗效果持续时间）
    /// </summary>
    public void OnTick()
    {
        var currentTime = Server.CurrentTime;
        var expiredSlots = new List<int>();

        foreach (var kvp in _darknessStates)
        {
            if (currentTime >= kvp.Value.EndTime)
            {
                expiredSlots.Add(kvp.Key);
            }
        }

        foreach (var slot in expiredSlots)
        {
            if (_darknessStates.TryGetValue(slot, out var state))
            {
                RemoveDarkness(state.TargetPlayer);
                state.TargetPlayer?.PrintToChat("🌑 黑暗效果已消退");
            }
        }
    }

    /// <summary>
    /// 黑暗效果状态
    /// </summary>
    private class DarknessState
    {
        public CCSPlayerController TargetPlayer { get; set; } = null!;
        public ulong CasterSteamID { get; set; }
        public List<CPostProcessingVolume> OriginalVolumes { get; set; } = null!;
        public CPostProcessingVolume? PostProcessingEntity { get; set; }
        public float EndTime { get; set; }
    }
}
