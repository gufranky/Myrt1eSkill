// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on ThirdEye skill from jRandomSkills by Juzlus

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;
using System.Drawing;

namespace MyrtleSkill.Skills;

/// <summary>
/// 第三只眼技能 - 点击激活第三人称视角
/// </summary>
public class ThirdEyeSkill : PlayerSkill
{
    public override string Name => "ThirdEye";
    public override string DisplayName => "👁️ 第三只眼";
    public override string Description => "点击激活第三人称视角！再次点击切换回第一人称！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 0.0f; // 0秒冷却

    // 第三人称距离
    private const float CAMERA_DISTANCE = 100f;

    // 跟踪每个玩家的摄像头状态
    private readonly ConcurrentDictionary<ulong, ThirdEyeCameraInfo> _playerCameras = new();

    // 摄像头信息
    private class ThirdEyeCameraInfo
    {
        public uint OriginalCameraHandle { get; set; }
        public CDynamicProp? Camera { get; set; }
    }

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[第三只眼] {player.PlayerName} 获得了第三只眼技能");

        player.PrintToChat("👁️ 你获得了第三只眼技能！");
        player.PrintToChat("💡 点击技能键激活第三人称视角！");
        player.PrintToChat("⚠️ 再次点击切换回第一人称！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        // 切换回第一人称并清理摄像头
        ChangeCamera(player, true);
        _playerCameras.TryRemove(player.SteamID, out _);

        Console.WriteLine($"[第三只眼] {player.PlayerName} 失去了第三只眼技能");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        Console.WriteLine($"[第三只眼] {player.PlayerName} 使用了第三只眼技能");

        // 切换视角
        ChangeCamera(player);
    }

    /// <summary>
    /// 切换摄像头视角
    /// 参考 jRandomSkills ThirdEye.ChangeCamera
    /// </summary>
    private void ChangeCamera(CCSPlayerController player, bool forceToDefault = false)
    {
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn?.CameraServices == null)
            return;

        uint originalCameraHandle;
        uint newCameraHandle;

        // 获取或创建摄像头
        if (_playerCameras.TryGetValue(player.SteamID, out var cameraInfo) &&
            cameraInfo.Camera != null && cameraInfo.Camera.IsValid)
        {
            originalCameraHandle = cameraInfo.OriginalCameraHandle;
            newCameraHandle = cameraInfo.Camera.EntityHandle.Raw;
        }
        else
        {
            originalCameraHandle = playerPawn.CameraServices.ViewEntity.Raw;
            newCameraHandle = CreateCamera(player);
        }

        if (newCameraHandle == 0)
            return;

        // 切换视角
        if (forceToDefault)
        {
            playerPawn.CameraServices.ViewEntity.Raw = originalCameraHandle;
            player.PrintToChat("👁️ 已切换回第一人称视角");
        }
        else
        {
            // 如果当前是原始视角，切换到第三人称；否则切换回原始视角
            if (playerPawn.CameraServices.ViewEntity.Raw == originalCameraHandle)
            {
                playerPawn.CameraServices.ViewEntity.Raw = newCameraHandle;
                player.PrintToChat("👁️ 已切换到第三人称视角");

                // 注册 OnTick 监听（如果有玩家使用第三只眼技能）
                if (_playerCameras.Any(kvp => kvp.Value.Camera != null && kvp.Value.Camera.IsValid) && Plugin != null)
                {
                    Plugin.RegisterListener<Listeners.OnTick>(OnTick);
                }
            }
            else
            {
                playerPawn.CameraServices.ViewEntity.Raw = originalCameraHandle;
                player.PrintToChat("👁️ 已切换回第一人称视角");

                // 如果没有玩家使用第三只眼技能，移除监听
                if (!_playerCameras.Any(kvp => kvp.Value.Camera != null && kvp.Value.Camera.IsValid) && Plugin != null)
                {
                    Plugin.RemoveListener<Listeners.OnTick>(OnTick);
                }
            }
        }

        // 通知客户端更新
        Utilities.SetStateChanged(playerPawn, "CBasePlayerPawn", "m_pCameraServices");
    }

    /// <summary>
    /// 创建摄像头实体
    /// 参考 jRandomSkills ThirdEye.CreateCamera
    /// </summary>
    private uint CreateCamera(CCSPlayerController player)
    {
        var camera = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (camera == null || !camera.IsValid)
            return 0;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null)
            return 0;

        Server.NextFrame(() =>
        {
            if (!camera.IsValid)
                return;

            // 设置Spawnflags（关键：256 = 可发射）
            camera.Spawnflags = 256u;

            // 清除Entity Flags中的EFL_NO_PHYSCOLLISION (第2位)
            if (camera.CBodyComponent != null && camera.CBodyComponent.SceneNode != null)
            {
                var owner = camera.CBodyComponent.SceneNode.Owner;
                if (owner != null && owner.Entity != null)
                {
                    owner.Entity.Flags &= ~(uint)(1 << 2);
                }
            }

            // 不设置模型文件，避免显示ERROR模型
            // camera.SetModel("models/actors/ghost_speaker.vmdl");

            // 完全隐藏渲染
            camera.RenderMode = RenderMode_t.kRenderNone;
            camera.Render = Color.FromArgb(0, 255, 255, 255);

            if (playerPawn.AbsOrigin != null && playerPawn.EyeAngles != null)
            {
                camera.Teleport(playerPawn.AbsOrigin, playerPawn.EyeAngles);
            }

            camera.DispatchSpawn();
        });

        // 保存摄像头信息
        _playerCameras.AddOrUpdate(
            player.SteamID,
            new ThirdEyeCameraInfo
            {
                OriginalCameraHandle = playerPawn.CameraServices!.ViewEntity.Raw,
                Camera = camera
            },
            (key, old) => new ThirdEyeCameraInfo
            {
                OriginalCameraHandle = old.OriginalCameraHandle,
                Camera = camera
            }
        );

        return camera.EntityHandle.Raw;
    }

    /// <summary>
    /// 每帧更新 - 更新摄像头位置
    /// 参考 jRandomSkills ThirdEye.OnTick
    /// </summary>
    public void OnTick()
    {
        // 如果没有玩家使用第三只眼技能，移除监听
        if (!_playerCameras.Any(kvp => kvp.Value.Camera != null && kvp.Value.Camera.IsValid) && Plugin != null)
        {
            Plugin.RemoveListener<Listeners.OnTick>(OnTick);
            return;
        }

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

            // 如果玩家死亡，切换回第一人称
            if (playerPawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            {
                ChangeCamera(player, true);
                continue;
            }

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
