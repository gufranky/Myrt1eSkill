// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on Pilot skill from jRandomSkills by Juzlus

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;

namespace MyrtleSkill.Skills;

/// <summary>
/// 飞行员技能 - 限时飞行，按住 E 键飞行
/// </summary>
public class PilotSkill : PlayerSkill
{
    public override string Name => "Pilot";
    public override string DisplayName => "✈️ 飞行员";
    public override string Description => "按住 E 键飞行！燃料有限，会自动恢复！";
    public override bool IsActive => false; // 被动技能

    // 飞行参数（参考 jRandomSkills Pilot）
    private const float MAXIMUM_FUEL = 150f;          // 最大燃料
    private const float FUEL_CONSUMPTION = 0.64f;     // 每帧消耗（按住E时）
    private const float REFUELLING = 0.1f;            // 每帧恢复（不按E时）
    private const float HORIZONTAL_SPEED = 5.0f;      // 水平飞行速度
    private const float VERTICAL_SPEED = 12.0f;       // 垂直飞行速度

    // 跟踪每个玩家的燃料状态
    private readonly ConcurrentDictionary<ulong, PilotPlayerInfo> _playerFuelInfo = new();

    // 玩家燃料信息
    private class PilotPlayerInfo
    {
        public ulong SteamID { get; set; }
        public float Fuel { get; set; }
    }

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[飞行员] {player.PlayerName} 获得了飞行员技能");

        // 初始化燃料
        _playerFuelInfo.TryAdd(player.SteamID, new PilotPlayerInfo
        {
            SteamID = player.SteamID,
            Fuel = MAXIMUM_FUEL
        });

        player.PrintToChat("✈️ 你获得了飞行员技能！");
        player.PrintToChat("💡 按住 E 键飞行！燃料会自动恢复！");
        player.PrintToChat($"⛽ 最大燃料：{MAXIMUM_FUEL:F0}");

        // 注册 OnTick 监听（如果有玩家使用飞行员技能）
        if (_playerFuelInfo.Count > 0 && Plugin != null)
        {
            Plugin.RegisterListener<Listeners.OnTick>(OnTick);
        }
    }

    public override void OnRevert(CCSPlayerController player)
    {
        // 移除燃料记录
        _playerFuelInfo.TryRemove(player.SteamID, out _);

        Console.WriteLine($"[飞行员] {player.PlayerName} 失去了飞行员技能");

        // 如果没有玩家使用飞行员技能，移除监听
        if (_playerFuelInfo.Count == 0 && Plugin != null)
        {
            Plugin.RemoveListener<Listeners.OnTick>(OnTick);
        }
    }

    /// <summary>
    /// 每帧更新 - 处理飞行逻辑
    /// 参考 jRandomSkills Pilot.OnTick
    /// </summary>
    public void OnTick()
    {
        // 如果没有玩家使用飞行员技能，移除监听
        if (_playerFuelInfo.Count == 0 && Plugin != null)
        {
            Plugin.RemoveListener<Listeners.OnTick>(OnTick);
            return;
        }

        foreach (var kvp in _playerFuelInfo)
        {
            var steamID = kvp.Key;
            var pilotInfo = kvp.Value;

            // 查找玩家
            var player = Utilities.GetPlayers().FirstOrDefault(p => p.SteamID == steamID);
            if (player == null || !player.IsValid || !player.PawnIsAlive)
                continue;

            // 检查玩家是否还有飞行员技能
            var skills = Plugin?.SkillManager.GetPlayerSkills(player);
            if (skills == null || skills.Count == 0)
                continue;

            var pilotSkill = skills.FirstOrDefault(s => s.Name == "Pilot");
            if (pilotSkill == null)
                continue;

            // 处理飞行逻辑
            HandlePilot(player, pilotInfo);
        }
    }

    /// <summary>
    /// 处理飞行逻辑
    /// 参考 jRandomSkills Pilot.HandlePilot
    /// </summary>
    private void HandlePilot(CCSPlayerController player, PilotPlayerInfo pilotInfo)
    {
        var buttons = player.Buttons;
        var isPressingE = buttons.HasFlag(PlayerButtons.Use);

        // 更新燃料：按E时消耗，不按时恢复
        if (isPressingE)
        {
            pilotInfo.Fuel = Math.Max(0, pilotInfo.Fuel - FUEL_CONSUMPTION);
        }
        else
        {
            pilotInfo.Fuel = Math.Min(MAXIMUM_FUEL, pilotInfo.Fuel + REFUELLING);
        }

        // 如果按住E且有燃料，应用飞行效果
        if (isPressingE && pilotInfo.Fuel > 0)
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn != null && playerPawn.IsValid && !playerPawn.IsDefusing)
            {
                ApplyPilotEffect(playerPawn);
            }
        }

        // 更新HUD（每10帧更新一次，避免频繁刷新）
        if (Server.TickCount % 10 == 0)
        {
            UpdateFuelHUD(player, pilotInfo);
        }
    }

    /// <summary>
    /// 应用飞行效果
    /// 参考 jRandomSkills Pilot.ApplyPilotEffect
    /// </summary>
    private void ApplyPilotEffect(CCSPlayerPawn playerPawn)
    {
        if (playerPawn.CBodyComponent == null)
            return;

        // 获取玩家视角角度
        QAngle eyeAngle = playerPawn.EyeAngles;
        double pitch = (Math.PI / 180) * eyeAngle.X;
        double yaw = (Math.PI / 180) * eyeAngle.Y;

        // 计算视角方向向量
        Vector eyeVector = new(
            (float)(Math.Cos(yaw) * Math.Cos(pitch)),
            (float)(Math.Sin(yaw) * Math.Cos(pitch)),
            (float)(-Math.Sin(pitch))
        );

        // 获取当前速度
        Vector currentVelocity = playerPawn.AbsVelocity;

        // 计算喷射背包速度
        Vector jetpackVelocity = new(
            eyeVector.X * HORIZONTAL_SPEED,
            eyeVector.Y * HORIZONTAL_SPEED,
            VERTICAL_SPEED
        );

        // 应用新速度
        playerPawn.AbsVelocity.X = currentVelocity.X + jetpackVelocity.X;
        playerPawn.AbsVelocity.Y = currentVelocity.Y + jetpackVelocity.Y;
        playerPawn.AbsVelocity.Z = currentVelocity.Z + jetpackVelocity.Z;

        // 通知客户端更新
        Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_vecAbsVelocity");
    }

    /// <summary>
    /// 更新燃料HUD显示
    /// 参考 jRandomSkills Pilot.UpdateHUD
    /// </summary>
    private void UpdateFuelHUD(CCSPlayerController player, PilotPlayerInfo pilotInfo)
    {
        float fuelPercentage = (pilotInfo.Fuel / MAXIMUM_FUEL) * 100;
        string fuelColor = GetFuelColor(pilotInfo.Fuel);

        // 使用 PrintToCenter 显示燃料百分比
        player.PrintToCenter($"⛽ 燃料: <font color='{fuelColor}'>{fuelPercentage:F0}%</font>");
    }

    /// <summary>
    /// 获取燃料颜色
    /// 参考 jRandomSkills Pilot.GetFuelColor
    /// </summary>
    private string GetFuelColor(float fuel)
    {
        if (fuel > (MAXIMUM_FUEL / 2f))
            return "#00FF00"; // 绿色
        if (fuel > (MAXIMUM_FUEL / 4f))
            return "#FFFF00"; // 黄色
        return "#FF0000";    // 红色
    }
}
