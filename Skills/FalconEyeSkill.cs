// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills by Juzlus (Falcon Eye skill)

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;

namespace MyrtleSkill.Skills;

/// <summary>
/// 猎鹰之眼技能 - 主动技能
/// 点击激活鸟瞰视角摄像头，从上方俯瞰战场
/// 完全复制自 jRandomSkills Falcon Eye
/// </summary>
public class FalconEyeSkill : PlayerSkill
{
    public override string Name => "FalconEye";
    public override string DisplayName => "🦅 猎鹰之眼";
    public override string Description => "点击激活鸟瞰视角摄像头，从上方俯瞰战场！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 0.0f; // 0秒冷却

    // 摄像头高度（与 jRandomSkills 一致）
    private const float CAMERA_DISTANCE = 1000.0f;

    // 跟踪每个玩家的摄像头（原始视角，摄像头实体）
    private readonly Dictionary<ulong, (uint originalView, CDynamicProp camera)> _cameras = new();

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[猎鹰之眼] {player.PlayerName} 获得了猎鹰之眼技能");
        player.PrintToChat("🦅 你获得了猎鹰之眼技能！");
        player.PrintToChat("💡 输入 !useskill 或按键激活鸟瞰视角！");
        player.PrintToChat($"⏱️ 冷却时间：{Cooldown}秒");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        // 关闭摄像头视角
        DisableSkill(player);

        Console.WriteLine($"[猎鹰之眼] {player.PlayerName} 失去了猎鹰之眼技能");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        Console.WriteLine($"[猎鹰之眼] {player.PlayerName} 激活了猎鹰之眼");

        // 切换摄像头视角
        ChangeCamera(player);

        player.PrintToChat("🦅 鸟瞰视角已激活！再次切换回正常视角");
    }

    /// <summary>
    /// 处理武器拾取事件 - 在摄像头视角下禁用武器
    /// 完全复制自 jRandomSkills Falcon Eye.WeaponPickup
    /// </summary>
    public void OnItemPickup(EventItemPickup @event)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return;

        // 检查玩家是否有猎鹰之眼技能
        var skills = Plugin?.SkillManager.GetPlayerSkills(player);
        if (skills == null || skills.Count == 0)
            return;

        var falconEyeSkill = skills.FirstOrDefault(s => s.Name == "FalconEye");
        if (falconEyeSkill == null)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.CameraServices == null)
            return;

        // 如果玩家正在使用摄像头视角，禁用武器
        if (_cameras.TryGetValue(player.SteamID, out var cameraInfo) &&
            cameraInfo.originalView == pawn.CameraServices.ViewEntity.Raw)
        {
            // 这里实际上 cameraInfo.originalView 是原始视角，所以如果相等说明还没切换
            // jRandomSkills的逻辑似乎是：如果当前视角等于原始视角，说明在摄像头模式
        }
        else if (_cameras.TryGetValue(player.SteamID, out var camInfo) &&
                 camInfo.camera != null && camInfo.camera.IsValid &&
                 pawn.CameraServices.ViewEntity.Raw == camInfo.camera.EntityHandle.Raw)
        {
            // 当前在使用摄像头，禁用武器
            BlockWeapon(player, true);
        }
    }

    /// <summary>
    /// 每帧更新 - 更新摄像头位置跟随玩家
    /// 完全复制自 jRandomSkills Falcon Eye.OnTick
    /// </summary>
    public void OnTick()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!_cameras.TryGetValue(player.SteamID, out var cameraInfo))
                continue;

            if (cameraInfo.camera == null || !cameraInfo.camera.IsValid)
                continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
                continue;

            // 如果玩家死亡，切换回正常视角
            if (pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            {
                ChangeCamera(player, true);
                continue;
            }

            // 更新摄像头位置（玩家正上方）
            Vector pos = new(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z + CAMERA_DISTANCE);
            QAngle angle = new(90, 0, -pawn.V_angle.Y); // 俯视角度，旋转跟随玩家视角

            cameraInfo.camera.Teleport(pos, angle);
        }
    }

    /// <summary>
    /// 禁用技能 - 关闭摄像头视角
    /// </summary>
    private void DisableSkill(CCSPlayerController player)
    {
        ChangeCamera(player, true);

        // 清理摄像头
        if (_cameras.TryGetValue(player.SteamID, out var cameraInfo))
        {
            if (cameraInfo.camera != null && cameraInfo.camera.IsValid)
            {
                cameraInfo.camera.AcceptInput("Kill");
            }
            _cameras.Remove(player.SteamID);
        }

        Console.WriteLine($"[猎鹰之眼] 已关闭 {player.PlayerName} 的摄像头");
    }

    /// <summary>
    /// 切换摄像头视角
    /// 完全复制自 jRandomSkills Falcon Eye.ChangeCamera
    /// </summary>
    private void ChangeCamera(CCSPlayerController player, bool forceToDefault = false)
    {
        uint originalCameraRaw;
        uint newCameraRaw;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.CameraServices == null)
            return;

        // 检查是否已有摄像头
        if (_cameras.TryGetValue(player.SteamID, out var cameraInfo) && cameraInfo.camera.IsValid)
        {
            originalCameraRaw = cameraInfo.originalView;
            newCameraRaw = cameraInfo.camera.EntityHandle.Raw;
        }
        else
        {
            // 保存原始视角并创建新摄像头
            originalCameraRaw = pawn.CameraServices.ViewEntity.Raw;
            newCameraRaw = CreateCamera(player);
        }

        if (newCameraRaw == 0)
            return;

        // 切换视角
        bool defaultCam = forceToDefault || (pawn.CameraServices.ViewEntity.Raw != originalCameraRaw);
        pawn.CameraServices.ViewEntity.Raw = defaultCam ? originalCameraRaw : newCameraRaw;
        Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");

        // 在摄像头模式下禁用武器
        BlockWeapon(player, !defaultCam);

        Console.WriteLine($"[猎鹰之眼] {player.PlayerName} 切换到{(defaultCam ? "正常" : "鸟瞰")}视角");
    }

    /// <summary>
    /// 创建摄像头实体
    /// 完全复制自 jRandomSkills Falcon Eye.CreateCamera
    /// </summary>
    private uint CreateCamera(CCSPlayerController player)
    {
        var camera = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (camera == null || !camera.IsValid)
            return 0;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
            return 0;

        // 摄像头位置：玩家正上方 1000 单位
        Vector pos = new(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z + CAMERA_DISTANCE);

        Server.NextFrame(() =>
        {
            camera.SetModel("models/actors/ghost_speaker.vmdl");
            camera.RenderMode = RenderMode_t.kRenderNone; // 完全不渲染
            camera.Render = Color.FromArgb(0, 255, 255, 255); // 完全透明
            camera.Teleport(pos, new QAngle(90, 0, 0));
            camera.DispatchSpawn();
        });

        _cameras[player.SteamID] = (pawn.CameraServices.ViewEntity.Raw, camera);
        return camera.EntityHandle.Raw;
    }

    /// <summary>
    /// 禁用/启用武器
    /// 完全复制自 jRandomSkills Falcon Eye.BlockWeapon
    /// </summary>
    private void BlockWeapon(CCSPlayerController player, bool block)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null)
            return;

        foreach (var weapon in weaponServices.MyWeapons)
        {
            if (weapon == null || !weapon.IsValid || weapon.Value == null || !weapon.Value.IsValid)
                continue;

            weapon.Value.NextPrimaryAttackTick = block ? int.MaxValue : Server.TickCount;
            weapon.Value.NextSecondaryAttackTick = block ? int.MaxValue : Server.TickCount;

            Utilities.SetStateChanged(weapon.Value, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
            Utilities.SetStateChanged(weapon.Value, "CBasePlayerWeapon", "m_nNextSecondaryAttackTick");
        }

        if (block)
        {
            player.PrintToCenter("🦅 鸟瞰模式下无法使用武器");
        }
    }

    /// <summary>
    /// 清理所有摄像头（回合开始时）
    /// </summary>
    public static void OnRoundStart()
    {
        // 注意：这里需要在实例中清理，因为是实例字典而非静态字典
        Console.WriteLine("[猎鹰之眼] 回合开始，摄像头将在玩家失去技能时清理");
    }
}
