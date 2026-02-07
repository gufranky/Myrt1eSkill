// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on free camera concept

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;
using System.Drawing;

namespace MyrtleSkill.Skills;

/// <summary>
/// 自由视角技能 - 点击激活，WASD控制摄像头移动，玩家本体不移动
/// </summary>
public class FreeCameraSkill : PlayerSkill
{
    public override string Name => "FreeCamera";
    public override string DisplayName => "📷 自由视角";
    public override string Description => "点击激活自由视角！WASD控制摄像头移动，玩家本体不移动！再次点击退出！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 0.0f; // 0秒冷却

    // 摄像头移动速度
    private const float CAMERA_SPEED = 200.0f;  // 每秒移动速度

    // 跟踪每个玩家的摄像头状态
    private readonly ConcurrentDictionary<ulong, FreeCameraInfo> _playerCameras = new();

    // 摄像头信息
    private class FreeCameraInfo
    {
        public uint OriginalCameraHandle { get; set; }
        public CDynamicProp? Camera { get; set; }
        public Vector Position { get; set; } = new Vector(0, 0, 0);
        public QAngle Angle { get; set; } = new QAngle(0, 0, 0);
        public bool IsActive { get; set; }
    }

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[自由视角] {player.PlayerName} 获得了自由视角技能");

        player.PrintToChat("📷 你获得了自由视角技能！");
        player.PrintToChat("💡 点击技能键激活自由视角！");
        player.PrintToChat("🎮 WASD移动摄像头，鼠标控制视角");
        player.PrintToChat("⚠️ 玩家本体不会移动！再次点击退出！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        // 退出自由视角并清理摄像头
        ExitFreeCamera(player);
        _playerCameras.TryRemove(player.SteamID, out _);

        Console.WriteLine($"[自由视角] {player.PlayerName} 失去了自由视角技能");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        Console.WriteLine($"[自由视角] {player.PlayerName} 使用了自由视角技能");

        // 切换自由视角状态
        if (_playerCameras.TryGetValue(player.SteamID, out var cameraInfo) && cameraInfo.IsActive)
        {
            ExitFreeCamera(player);
        }
        else
        {
            EnterFreeCamera(player);
        }
    }

    /// <summary>
    /// 进入自由视角模式
    /// </summary>
    private void EnterFreeCamera(CCSPlayerController player)
    {
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn?.CameraServices == null)
            return;

        // 创建摄像头
        var camera = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (camera == null || !camera.IsValid)
            return;

        Server.NextFrame(() =>
        {
            camera.SetModel("models/actors/ghost_speaker.vmdl");
            camera.Render = Color.FromArgb(0, 255, 255, 255); // 完全透明

            // 初始位置：玩家当前位置
            Vector initialPos;
            QAngle initialAngle;
            if (playerPawn.AbsOrigin != null && playerPawn.EyeAngles != null)
            {
                initialPos = new Vector(playerPawn.AbsOrigin.X, playerPawn.AbsOrigin.Y, playerPawn.AbsOrigin.Z);
                initialAngle = new QAngle(playerPawn.EyeAngles.X, playerPawn.EyeAngles.Y, playerPawn.EyeAngles.Z);
            }
            else
            {
                initialPos = new Vector(0, 0, 0);
                initialAngle = new QAngle(0, 0, 0);
            }

            camera.Teleport(initialPos, initialAngle);
            camera.DispatchSpawn();
        });

        // 保存原始摄像头句柄和初始位置
        _playerCameras.AddOrUpdate(
            player.SteamID,
            new FreeCameraInfo
            {
                OriginalCameraHandle = playerPawn.CameraServices.ViewEntity.Raw,
                Camera = camera,
                Position = playerPawn.AbsOrigin != null ? new Vector(playerPawn.AbsOrigin.X, playerPawn.AbsOrigin.Y, playerPawn.AbsOrigin.Z) : new Vector(0, 0, 0),
                Angle = playerPawn.EyeAngles != null ? new QAngle(playerPawn.EyeAngles.X, playerPawn.EyeAngles.Y, playerPawn.EyeAngles.Z) : new QAngle(0, 0, 0),
                IsActive = true
            },
            (key, old) =>
            {
                old.Camera = camera;
                old.IsActive = true;
                return old;
            }
        );

        // 切换到摄像头视角
        Server.NextFrame(() =>
        {
            if (camera != null && camera.IsValid && playerPawn.CameraServices != null)
            {
                playerPawn.CameraServices.ViewEntity.Raw = camera.EntityHandle.Raw;
                Utilities.SetStateChanged(playerPawn, "CBasePlayerPawn", "m_pCameraServices");
            }
        });

        // 注册 OnTick 监听
        if (_playerCameras.Any(kvp => kvp.Value.IsActive) && Plugin != null)
        {
            Plugin.RegisterListener<Listeners.OnTick>(OnTick);
        }

        player.PrintToCenter("📷 自由视角已激活！WASD移动");
        player.PrintToChat("📷 自由视角已激活！玩家本体不会移动！");
    }

    /// <summary>
    /// 退出自由视角模式
    /// </summary>
    private void ExitFreeCamera(CCSPlayerController player)
    {
        if (!_playerCameras.TryGetValue(player.SteamID, out var cameraInfo))
            return;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn?.CameraServices == null)
            return;

        // 恢复原始视角
        playerPawn.CameraServices.ViewEntity.Raw = cameraInfo.OriginalCameraHandle;
        Utilities.SetStateChanged(playerPawn, "CBasePlayerPawn", "m_pCameraServices");

        // 销毁摄像头
        if (cameraInfo.Camera != null && cameraInfo.Camera.IsValid)
        {
            cameraInfo.Camera.AcceptInput("Kill");
        }

        // 标记为未激活
        cameraInfo.IsActive = false;

        player.PrintToCenter("📷 已退出自由视角");
        player.PrintToChat("📷 自由视角已退出！");

        // 如果没有玩家使用自由视角，移除监听
        if (!_playerCameras.Any(kvp => kvp.Value.IsActive) && Plugin != null)
        {
            Plugin.RemoveListener<Listeners.OnTick>(OnTick);
        }
    }

    /// <summary>
    /// 每帧更新 - 移动摄像头
    /// </summary>
    public void OnTick()
    {
        // 如果没有玩家使用自由视角，移除监听
        if (!_playerCameras.Any(kvp => kvp.Value.IsActive) && Plugin != null)
        {
            Plugin.RemoveListener<Listeners.OnTick>(OnTick);
            return;
        }

        float deltaTime = 1.0f / 64.0f; // 假设 64 tick/s

        foreach (var kvp in _playerCameras)
        {
            var steamID = kvp.Key;
            var cameraInfo = kvp.Value;

            if (!cameraInfo.IsActive || cameraInfo.Camera == null || !cameraInfo.Camera.IsValid)
                continue;

            // 查找玩家
            var player = Utilities.GetPlayers().FirstOrDefault(p => p.SteamID == steamID);
            if (player == null || !player.IsValid || !player.PawnIsAlive)
                continue;

            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid)
                continue;

            // 获取玩家按键
            var buttons = player.Buttons;

            // 计算移动方向
            Vector moveDirection = new Vector(0, 0, 0);

            // W 前进
            if (buttons.HasFlag(PlayerButtons.Forward))
            {
                var forward = GetForwardVector(cameraInfo.Angle);
                moveDirection.X += forward.X;
                moveDirection.Y += forward.Y;
                moveDirection.Z += forward.Z;
            }

            // S 后退
            if (buttons.HasFlag(PlayerButtons.Back))
            {
                var forward = GetForwardVector(cameraInfo.Angle);
                moveDirection.X -= forward.X;
                moveDirection.Y -= forward.Y;
                moveDirection.Z -= forward.Z;
            }

            // A 左移
            if (buttons.HasFlag(PlayerButtons.Moveleft))
            {
                var left = GetLeftVector(cameraInfo.Angle);
                moveDirection.X += left.X;
                moveDirection.Y += left.Y;
                moveDirection.Z += left.Z;
            }

            // D 右移
            if (buttons.HasFlag(PlayerButtons.Moveright))
            {
                var right = GetRightVector(cameraInfo.Angle);
                moveDirection.X += right.X;
                moveDirection.Y += right.Y;
                moveDirection.Z += right.Z;
            }

            // 如果有移动，更新摄像头位置
            if (moveDirection.X != 0 || moveDirection.Y != 0 || moveDirection.Z != 0)
            {
                // 归一化移动方向
                float length = (float)Math.Sqrt(moveDirection.X * moveDirection.X + moveDirection.Y * moveDirection.Y + moveDirection.Z * moveDirection.Z);
                if (length > 0.001f)
                {
                    moveDirection.X /= length;
                    moveDirection.Y /= length;
                    moveDirection.Z /= length;
                }

                // 计算新位置
                float speed = CAMERA_SPEED * deltaTime;
                cameraInfo.Position.X += moveDirection.X * speed;
                cameraInfo.Position.Y += moveDirection.Y * speed;
                cameraInfo.Position.Z += moveDirection.Z * speed;

                // 更新摄像头位置
                if (cameraInfo.Camera.AbsOrigin != null && cameraInfo.Camera.AbsRotation != null)
                {
                    cameraInfo.Camera.Teleport(cameraInfo.Position, cameraInfo.Angle);
                }

                // 阻止玩家实体移动
                playerPawn.AbsVelocity.X = 0;
                playerPawn.AbsVelocity.Y = 0;
                playerPawn.AbsVelocity.Z = 0;
                Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_vecAbsVelocity");
            }

            // 更新摄像头角度（跟随玩家视角）
            if (playerPawn.EyeAngles != null)
            {
                cameraInfo.Angle.X = playerPawn.EyeAngles.X;
                cameraInfo.Angle.Y = playerPawn.EyeAngles.Y;
                cameraInfo.Angle.Z = playerPawn.EyeAngles.Z;
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

    /// <summary>
    /// 计算左方向量
    /// </summary>
    private static Vector GetLeftVector(QAngle angles)
    {
        float radiansY = (angles.Y - 90) * (float)Math.PI / 180.0f;

        return new Vector(
            (float)Math.Cos(radiansY),
            (float)Math.Sin(radiansY),
            0
        );
    }

    /// <summary>
    /// 计算右方向量
    /// </summary>
    private static Vector GetRightVector(QAngle angles)
    {
        float radiansY = (angles.Y + 90) * (float)Math.PI / 180.0f;

        return new Vector(
            (float)Math.Cos(radiansY),
            (float)Math.Sin(radiansY),
            0
        );
    }
}
