using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 超级闪光技能 - 被动技能
/// 你的闪光弹总会让所有敌人屏幕变黑3秒（使用PostProcessingVolume实现）
/// </summary>
public class SuperFlashSkill : PlayerSkill
{
    public override string Name => "SuperFlash";
    public override string DisplayName => "💥 超级闪光";
    public override string Description => "你的闪光弹会让所有敌人屏幕变黑3秒！无视距离和遮挡！";
    public override bool IsActive => false; // 被动技能

    // 与其他闪光弹技能互斥
    public override List<string> ExcludedSkills => new() { "AntiFlash", "FlashJump", "KillerFlash" };

    // 黑暗效果持续时间（秒）
    private const float DARKNESS_DURATION = 3.0f;

    // 黑暗参数（完全黑屏：brightness = 0）
    private const float DARKNESS_BRIGHTNESS = 0.0f;

    // 跟踪被施加黑暗效果的玩家
    private readonly Dictionary<int, DarknessState> _darknessStates = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[超级闪光] {player.PlayerName} 获得了超级闪光技能");

        // 给予1个闪光弹
        player.GiveNamedItem("weapon_flashbang");

        player.PrintToChat("💥 你获得了超级闪光技能！");
        player.PrintToChat("💡 你的闪光弹会让所有敌人屏幕变黑3秒！");
        player.PrintToChat("⚠️ 无视距离和遮挡！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[超级闪光] {player.PlayerName} 失去了超级闪光技能");
    }

    /// <summary>
    /// 处理闪光弹爆炸事件 - 让所有敌人屏幕变黑3秒（无视距离和遮挡）
    /// </summary>
    public void OnFlashbangDetonate(EventFlashbangDetonate @event)
    {
        var attacker = @event.Userid;
        if (attacker == null || !attacker.IsValid)
            return;

        Console.WriteLine($"[超级闪光] {attacker.PlayerName} 的闪光弹爆炸了！");

        // 延迟执行，确保在游戏引擎处理完闪光弹后再施加黑暗效果
        Server.NextFrame(() =>
        {
            int blindedCount = 0;

            // 让所有敌方玩家屏幕变黑（无视距离和遮挡）
            foreach (var player in Utilities.GetPlayers())
            {
                if (player == null || !player.IsValid || !player.PawnIsAlive)
                    continue;

                // 不闪自己
                if (player == attacker)
                    continue;

                // 只闪敌方玩家
                if (player.Team == attacker.Team)
                    continue;

                // 施加黑暗效果
                ApplyDarkness(player, DARKNESS_DURATION);
                blindedCount++;

                Console.WriteLine($"[超级闪光] {player.PlayerName} 被变黑 {DARKNESS_DURATION} 秒");
                player.PrintToCenter($"💥 被超级闪光弹致盲 {DARKNESS_DURATION} 秒！");
            }

            if (blindedCount > 0)
            {
                attacker.PrintToChat($"💥 超级闪光弹！{blindedCount} 个敌人屏幕变黑 {DARKNESS_DURATION} 秒！");
                Console.WriteLine($"[超级闪光] {attacker.PlayerName} 的闪光弹让 {blindedCount} 个敌人屏幕变黑");
            }
        });
    }

    /// <summary>
    /// 对玩家施加黑暗效果
    /// </summary>
    private void ApplyDarkness(CCSPlayerController target, float duration)
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
                OriginalVolumes = originalVolumes,
                PostProcessingEntity = postProcessing,
                EndTime = Server.CurrentTime + duration
            };

            Console.WriteLine($"[超级闪光] 对 {target.PlayerName} 施加黑暗，持续 {duration} 秒");
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

        Console.WriteLine($"[超级闪光] 已移除 {target.PlayerName} 的黑暗效果");
    }

    /// <summary>
    /// 移除所有黑暗效果
    /// </summary>
    private void RemoveAllDarkness()
    {
        var toRemove = _darknessStates.Keys.ToList();

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
                state.TargetPlayer?.PrintToChat("💥 超级闪光效果已消退");
            }
        }
    }

    /// <summary>
    /// 黑暗效果状态
    /// </summary>
    private class DarknessState
    {
        public CCSPlayerController TargetPlayer { get; set; } = null!;
        public List<CPostProcessingVolume> OriginalVolumes { get; set; } = null!;
        public CPostProcessingVolume? PostProcessingEntity { get; set; }
        public float EndTime { get; set; }
    }
}
