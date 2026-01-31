using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill;

/// <summary>
/// 雾蒙蒙事件 - 全员20%亮度
/// </summary>
public class FoggyEvent : EntertainmentEvent
{
    public override string Name => "Foggy";
    public override string DisplayName => "🌫 雾蒙蒙";
    public override string Description => "全员20%亮度！视野一片模糊！";

    // 雾蒙蒙亮度（20%）
    private const float FOGGY_BRIGHTNESS = 0.2f;

    // 保存所有玩家的原始PostProcessingVolumes
    private readonly Dictionary<int, List<CPostProcessingVolume>> _originalVolumes = new();

    // 创建的后处理实体
    private readonly List<CPostProcessingVolume> _createdPostProcessings = new();

    public override void OnApply()
    {
        Console.WriteLine("[雾蒙蒙] 事件已激活");

        // 给所有玩家施加雾蒙蒙效果
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive) continue;

            ApplyFoggy(player);
        }

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToCenter("🌫 雾蒙蒙！\n全员20%亮度！");
                player.PrintToChat("🌫 雾蒙蒙模式已启用！");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[雾蒙蒙] 事件已恢复");

        // 移除所有玩家的雾蒙蒙效果
        RemoveAllFoggy();

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("🌫 雾蒙蒙模式已禁用");
            }
        }

        _originalVolumes.Clear();
        _createdPostProcessings.Clear();
    }

    /// <summary>
    /// 对玩家施加雾蒙蒙效果
    /// </summary>
    private void ApplyFoggy(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.CameraServices == null)
            return;

        // 保存原始的PostProcessingVolumes
        var originalVolumesList = new List<CPostProcessingVolume>();
        foreach (var postProcessingVolume in pawn.CameraServices.PostProcessingVolumes)
        {
            if (postProcessingVolume != null && postProcessingVolume.Value != null)
            {
                originalVolumesList.Add(postProcessingVolume.Value);
            }
        }
        _originalVolumes[player.Slot] = originalVolumesList;

        // 创建新的雾蒙蒙后处理体积
        var postProcessing = Utilities.CreateEntityByName<CPostProcessingVolume>("post_processing_volume");
        if (postProcessing != null && postProcessing.IsValid)
        {
            postProcessing.ExposureControl = true;
            postProcessing.MaxExposure = FOGGY_BRIGHTNESS;
            postProcessing.MinExposure = FOGGY_BRIGHTNESS;

            // 替换所有PostProcessingVolumes
            foreach (var postProcessingVolume in pawn.CameraServices.PostProcessingVolumes)
            {
                if (postProcessingVolume != null && postProcessingVolume.Value != null)
                {
                    postProcessingVolume.Raw = postProcessing.EntityHandle.Raw;
                }
            }

            Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");

            _createdPostProcessings.Add(postProcessing);

            Console.WriteLine($"[雾蒙蒙] 已对 {player.PlayerName} 施加雾蒙蒙效果（20%亮度）");
        }
    }

    /// <summary>
    /// 移除单个玩家的雾蒙蒙效果
    /// </summary>
    private void RemoveFoggy(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.CameraServices == null)
            return;

        if (!_originalVolumes.TryGetValue(player.Slot, out var originalVolumesList))
            return;

        // 恢复原始的PostProcessingVolumes
        int i = 0;
        foreach (var postProcessingVolume in pawn.CameraServices.PostProcessingVolumes)
        {
            if (postProcessingVolume != null && postProcessingVolume.Value != null && i < originalVolumesList.Count)
            {
                postProcessingVolume.Raw = originalVolumesList[i].EntityHandle.Raw;
                i++;
            }
        }

        Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");

        Console.WriteLine($"[雾蒙蒙] 已移除 {player.PlayerName} 的雾蒙蒙效果");
    }

    /// <summary>
    /// 移除所有玩家的雾蒙蒙效果
    /// </summary>
    private void RemoveAllFoggy()
    {
        // 先恢复所有玩家的原始设置
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                RemoveFoggy(player);
            }
        }

        // 删除创建的实体
        foreach (var postProcessing in _createdPostProcessings)
        {
            if (postProcessing != null && postProcessing.IsValid)
            {
                postProcessing.AcceptInput("Kill");
            }
        }
    }
}
