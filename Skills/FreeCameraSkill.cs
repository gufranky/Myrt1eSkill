// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on free camera concept

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CS2TraceRay.Class;
using CS2TraceRay.Struct;
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

    // 视野检测参数
    private const float MAX_VIEW_DISTANCE = 2000.0f;  // 最大视野距离
    private const float FOV_THRESHOLD = 0.707f;      // 视野角度阈值（90度）
    private const float GLOW_DURATION = 3.0f;         // 透视标记持续时间（秒）

    // 跟踪每个玩家的摄像头状态
    private readonly ConcurrentDictionary<ulong, FreeCameraInfo> _playerCameras = new();

    // 跟踪发光效果的敌人
    private readonly Dictionary<int, (int relayIndex, int glowIndex)> _glowingEnemies = new();

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

        // 检测视野内的敌人并施加透视效果
        var visibleEnemies = GetVisibleEnemies(cameraInfo.Position, cameraInfo.Angle, player);
        if (visibleEnemies.Count > 0)
        {
            player.PrintToCenter($"📷 已退出自由视角！标记 {visibleEnemies.Count} 个敌人！");
            player.PrintToChat($"📷 视野内发现 {visibleEnemies.Count} 个敌人！标记 {GLOW_DURATION} 秒！");

            // 对每个敌人施加透视效果
            foreach (var enemy in visibleEnemies)
            {
                ApplyGlowToEnemy(enemy);
            }

            // 显示所有被标记的敌人名称
            string enemyNames = string.Join(", ", visibleEnemies.Select(e => e.PlayerName));
            Server.PrintToChatAll($"📷 {player.PlayerName} 从自由视角发现了: {enemyNames}！");

            // 持续 3 秒后移除发光效果
            Plugin?.AddTimer(GLOW_DURATION, () =>
            {
                RemoveGlowEffects();
                if (player.IsValid)
                {
                    player.PrintToChat("📷 透视标记已消失！");
                }
            });
        }
        else
        {
            player.PrintToCenter("📷 已退出自由视角");
            player.PrintToChat("📷 自由视角已退出！");
        }

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

    /// <summary>
    /// 检测玩家是否在摄像头视野内
    /// 使用 TraceRay 检查障碍物 + 角度计算
    /// </summary>
    private bool IsPlayerInView(Vector cameraPos, QAngle cameraAngle, CCSPlayerController targetPlayer)
    {
        var targetPawn = targetPlayer.PlayerPawn.Value;
        if (targetPawn == null || !targetPawn.IsValid || targetPawn.AbsOrigin == null)
            return false;

        // 计算摄像头到玩家的向量
        Vector toPlayer = new(
            targetPawn.AbsOrigin.X - cameraPos.X,
            targetPawn.AbsOrigin.Y - cameraPos.Y,
            targetPawn.AbsOrigin.Z - cameraPos.Z
        );

        // 计算距离
        float distance = (float)Math.Sqrt(toPlayer.X * toPlayer.X + toPlayer.Y * toPlayer.Y + toPlayer.Z * toPlayer.Z);

        // 超出最大视野距离
        if (distance > MAX_VIEW_DISTANCE)
            return false;

        // 计算摄像头前方向量
        Vector cameraForward = GetForwardVector(cameraAngle);

        // 计算到玩家的方向（归一化）
        Vector toPlayerDir = new(
            toPlayer.X / distance,
            toPlayer.Y / distance,
            toPlayer.Z / distance
        );

        // 计算点积（判断角度）
        float dotProduct = cameraForward.X * toPlayerDir.X +
                          cameraForward.Y * toPlayerDir.Y +
                          cameraForward.Z * toPlayerDir.Z;

        // 如果不在视野角度范围内（90度 FOV）
        if (dotProduct < FOV_THRESHOLD)
            return false;

        // 使用 TraceRay 检查是否有障碍物
        return !IsObstacleBetween(cameraPos, targetPawn.AbsOrigin, targetPlayer);
    }

    /// <summary>
    /// 检查两点之间是否有障碍物
    /// 参考 HologramSkill.CheckPosition 实现
    /// </summary>
    private unsafe bool IsObstacleBetween(Vector startPos, Vector endPos, CCSPlayerController player)
    {
        // 稍微抬高起点和终点，避免地面检测
        Vector eyePos = new(startPos.X, startPos.Y, startPos.Z + 25.0f);
        Vector targetPos = new(endPos.X, endPos.Y, endPos.Z + 25.0f);

        // 获取碰撞掩码
        ulong mask = player.PlayerPawn.Value?.Collision.CollisionAttribute.InteractsWith ?? 0;
        ulong contents = player.PlayerPawn.Value?.Collision.CollisionGroup ?? 0;

        // 发射射线
        CGameTrace trace = TraceRay.TraceShape(eyePos, targetPos, mask, contents, player);

        // 如果击中了物体，说明有障碍物
        return trace.DidHit();
    }

    /// <summary>
    /// 获取所有在摄像头视野内的敌人
    /// </summary>
    private List<CCSPlayerController> GetVisibleEnemies(Vector cameraPos, QAngle cameraAngle, CCSPlayerController observer)
    {
        var visibleEnemies = new List<CCSPlayerController>();

        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive)
                continue;

            // 跳过观察者自己
            if (player == observer)
                continue;

            // 跳过队友
            if (player.Team == observer.Team)
                continue;

            // 检查玩家是否在视野内
            if (IsPlayerInView(cameraPos, cameraAngle, player))
            {
                visibleEnemies.Add(player);
            }
        }

        return visibleEnemies;
    }

    /// <summary>
    /// 对敌人施加透视发光效果
    /// 参考 DecoyXRaySkill 的实现
    /// </summary>
    private void ApplyGlowToEnemy(CCSPlayerController enemy)
    {
        if (enemy == null || !enemy.IsValid)
            return;

        var pawn = enemy.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        try
        {
            bool success = ApplyEntityGlowEffect(pawn, enemy.Team, out var relayIndex, out var glowIndex);
            if (success)
            {
                _glowingEnemies[enemy.Slot] = (relayIndex, glowIndex);
                Console.WriteLine($"[自由视角] 为 {enemy.PlayerName} 添加透视发光效果");

                // 注册 CheckTransmit 监听器
                if (Plugin != null && _glowingEnemies.Count == 1)
                {
                    Plugin.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[自由视角] 添加发光效果时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 应用实体发光效果（复制自 DecoyXRaySkill）
    /// </summary>
    private bool ApplyEntityGlowEffect(CBaseEntity entity, CsTeam team, out int relayIndex, out int glowIndex)
    {
        relayIndex = -1;
        glowIndex = -1;

        if (entity == null || !entity.IsValid)
            return false;

        var sceneNode = entity.CBodyComponent?.SceneNode;
        if (sceneNode == null)
            return false;

        var skeletonInstance = sceneNode.GetSkeletonInstance();
        if (skeletonInstance == null)
            return false;

        var modelName = skeletonInstance.ModelState.ModelName;
        if (string.IsNullOrEmpty(modelName))
            return false;

        var modelRelay = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        var modelGlow = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");

        if (modelRelay == null || !modelRelay.IsValid || modelGlow == null || !modelGlow.IsValid)
            return false;

        // 设置 modelRelay
        modelRelay.Spawnflags = 256u;
        modelRelay.RenderMode = RenderMode_t.kRenderNone;

        if (modelRelay.CBodyComponent != null && modelRelay.CBodyComponent.SceneNode != null)
        {
            var owner = modelRelay.CBodyComponent.SceneNode.Owner;
            if (owner != null && owner.Entity != null)
            {
                owner.Entity.Flags &= ~(uint)(1 << 2);
            }
        }

        modelRelay.SetModel(modelName);
        modelRelay.DispatchSpawn();
        modelRelay.AcceptInput("FollowEntity", entity, modelRelay, "!activator");

        // 设置 modelGlow
        if (modelGlow.CBodyComponent != null && modelGlow.CBodyComponent.SceneNode != null)
        {
            var owner = modelGlow.CBodyComponent.SceneNode.Owner;
            if (owner != null && owner.Entity != null)
            {
                owner.Entity.Flags &= ~(uint)(1 << 2);
            }
        }

        modelGlow.SetModel(modelName);
        modelGlow.DispatchSpawn();
        modelGlow.AcceptInput("FollowEntity", modelRelay, modelGlow, "!activator");

        // 设置颜色（根据队伍）
        Color glowColor = team == CsTeam.Terrorist ? Color.FromArgb(255, 0, 0) : Color.FromArgb(0, 0, 255);
        modelGlow.Render = glowColor;

        relayIndex = (int)modelRelay.Index;
        glowIndex = (int)modelGlow.Index;

        return true;
    }

    /// <summary>
    /// 移除所有发光效果
    /// </summary>
    private void RemoveGlowEffects()
    {
        foreach (var (slot, (relayIndex, glowIndex)) in _glowingEnemies)
        {
            var relay = Utilities.GetEntityFromIndex<CDynamicProp>(relayIndex);
            var glow = Utilities.GetEntityFromIndex<CDynamicProp>(glowIndex);

            if (relay != null && relay.IsValid)
            {
                relay.AcceptInput("Kill");
            }

            if (glow != null && glow.IsValid)
            {
                glow.AcceptInput("Kill");
            }
        }

        _glowingEnemies.Clear();
        Console.WriteLine("[自由视角] 已移除所有发光效果");

        // 移除 CheckTransmit 监听器
        Plugin?.RemoveListener<Listeners.CheckTransmit>(OnCheckTransmit);
    }

    /// <summary>
    /// 检查传输时控制发光效果的可见性
    /// 参考 DecoyXRaySkill 的实现
    /// </summary>
    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        // 所有发光效果对所有玩家可见
        // 这里只是为了确保发光效果能够正常传输
    }
}
