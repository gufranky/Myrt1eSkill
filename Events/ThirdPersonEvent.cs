// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on ThirdEye skill from jRandomSkills by Juzlus

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;
using System.Drawing;

namespace MyrtleSkill.Events;

/// <summary>
/// 第三人称事件 - 全员使用第三人称视角
/// </summary>
public class ThirdPersonEvent : EntertainmentEvent
{
    public override string Name => "ThirdPerson";
    public override string DisplayName => "👁️ 第三人称";
    public override string Description => "全员使用第三人称视角！从身后观察自己的角色！";

    // 摄像头距离
    private const float CAMERA_DISTANCE = 100f;

    // 跟踪所有玩家的摄像头
    private readonly ConcurrentDictionary<ulong, ThirdPersonCameraInfo> _playerCameras = new();

    // 摄像头信息
    private class ThirdPersonCameraInfo
    {
        public uint OriginalCameraHandle { get; set; }
        public CDynamicProp? Camera { get; set; }
    }

    public override void OnApply()
    {
        Console.WriteLine("[第三人称] 事件已激活");

        // 为所有玩家创建第三人称摄像头
        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive)
                continue;

            CreateCameraForPlayer(player);
        }

        // 注册 OnTick 监听
        if (Plugin != null)
        {
            Plugin.RegisterListener<Listeners.OnTick>(OnTick);
        }

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("👁️ 第三人称模式已启用！");
                player.PrintToChat("💡 你现在可以从身后观察自己的角色！");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[第三人称] 事件已恢复");

        // 移除 OnTick 监听
        if (Plugin != null)
        {
            Plugin.RemoveListener<Listeners.OnTick>(OnTick);
        }

        // 恢复所有玩家的第一人称视角并销毁摄像头
        foreach (var kvp in _playerCameras)
        {
            var steamID = kvp.Key;
            var cameraInfo = kvp.Value;

            // 销毁摄像头实体
            if (cameraInfo.Camera != null && cameraInfo.Camera.IsValid)
            {
                cameraInfo.Camera.AcceptInput("Kill");
            }

            // 恢复玩家的原始视角
            var player = Utilities.GetPlayers().FirstOrDefault(p => p.SteamID == steamID);
            if (player != null && player.IsValid && player.PlayerPawn.Value?.CameraServices != null)
            {
                player.PlayerPawn.Value.CameraServices.ViewEntity.Raw = cameraInfo.OriginalCameraHandle;
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBasePlayerPawn", "m_pCameraServices");
            }
        }

        // 清空摄像头记录
        _playerCameras.Clear();

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("👁️ 第三人称模式已禁用");
            }
        }
    }

    /// <summary>
    /// 为玩家创建第三人称摄像头
    /// 参考 jRandomSkills ThirdEye.CreateCamera
    /// </summary>
    private void CreateCameraForPlayer(CCSPlayerController player)
    {
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn?.CameraServices == null)
            return;

        // 创建摄像头实体
        var camera = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (camera == null || !camera.IsValid)
            return;

        Server.NextFrame(() =>
        {
            camera.SetModel("models/actors/ghost_speaker.vmdl");
            camera.Render = Color.FromArgb(0, 255, 255, 255); // 完全透明

            if (playerPawn.AbsOrigin != null && playerPawn.EyeAngles != null)
            {
                camera.Teleport(playerPawn.AbsOrigin, playerPawn.EyeAngles);
            }

            camera.DispatchSpawn();
        });

        // 保存原始摄像头句柄
        uint originalCameraHandle = playerPawn.CameraServices.ViewEntity.Raw;

        // 保存摄像头信息
        _playerCameras.TryAdd(player.SteamID, new ThirdPersonCameraInfo
        {
            OriginalCameraHandle = originalCameraHandle,
            Camera = camera
        });

        // 切换到第三人称视角
        Server.NextFrame(() =>
        {
            if (camera != null && camera.IsValid && playerPawn.CameraServices != null)
            {
                playerPawn.CameraServices.ViewEntity.Raw = camera.EntityHandle.Raw;
                Utilities.SetStateChanged(playerPawn, "CBasePlayerPawn", "m_pCameraServices");
            }
        });

        Console.WriteLine($"[第三人称] 为 {player.PlayerName} 创建了第三人称摄像头");
    }

    /// <summary>
    /// 每帧更新 - 更新所有玩家的摄像头位置
    /// 参考 jRandomSkills ThirdEye.OnTick
    /// </summary>
    private void OnTick()
    {
        foreach (var kvp in _playerCameras)
        {
            var steamID = kvp.Key;
            var cameraInfo = kvp.Value;

            if (cameraInfo.Camera == null || !cameraInfo.Camera.IsValid)
                continue;

            // 查找玩家
            var player = Utilities.GetPlayers().FirstOrDefault(p => p.SteamID == steamID);
            if (player == null || !player.IsValid || !player.PawnIsAlive)
                continue;

            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null)
                continue;

            // 计算摄像头位置（玩家背后）
            var forwardVector = GetForwardVector(playerPawn.EyeAngles);
            var pos = new Vector(
                playerPawn.AbsOrigin.X - forwardVector.X * CAMERA_DISTANCE,
                playerPawn.AbsOrigin.Y - forwardVector.Y * CAMERA_DISTANCE,
                playerPawn.AbsOrigin.Z + playerPawn.ViewOffset.Z
            );

            // 更新摄像头位置和角度
            if (cameraInfo.Camera.AbsOrigin != null && cameraInfo.Camera.AbsRotation != null)
            {
                cameraInfo.Camera.Teleport(pos, playerPawn.V_angle);
            }
        }
    }

    /// <summary>
    /// 计算前方向量
    /// </summary>
    private static Vector GetForwardVector(QAngle angles)
    {
        float radiansY = angles.Y * (float)Math.PI / 180.0f;
        float radiansX = angles.X * (float)Math.PI / 180.0f;

        return new Vector(
            (float)(Math.Cos(radiansY) * Math.Cos(radiansX)),
            (float)(Math.Sin(radiansY) * Math.Cos(radiansX)),
            (float)(-Math.Sin(radiansX))
        );
    }
}
