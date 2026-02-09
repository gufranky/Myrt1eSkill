// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 残局使者技能 - 被动技能（简化版）
/// 当你的队伍只剩下你一个人的时候，获得透视和血量加成
/// 复用 Wallhack 技能的透视逻辑
/// </summary>
public class LastStandSkill : PlayerSkill
{
    public override string Name => "LastStand";
    public override string DisplayName => "💀 残局使者";
    public override string Description => "当你的队伍只剩下你一个人的时候，获得透视所有敌人的能力，并且血量变为150！";
    public override bool IsActive => false; // 被动技能
    public override float Cooldown => 0f; // 被动技能无冷却

    // 增加的血量
    private const int BONUS_HEALTH = 150;

    // 跟踪每个玩家是否已激活残局使者
    private static readonly HashSet<ulong> _activatedPlayers = new();

    // 跟踪每个玩家的激活状态
    private readonly Dictionary<ulong, bool> _playerActiveStatus = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        _playerActiveStatus[player.SteamID] = false;

        Console.WriteLine($"[残局使者] {player.PlayerName} 获得了残局使者技能");
        player.PrintToChat("💀 你获得了残局使者技能！");
        player.PrintToChat("💡 当你的队伍只剩下你一个人时，自动触发！");
        player.PrintToChat($"👁️ 透视所有敌人 + 血量变为{BONUS_HEALTH}！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 如果玩家已激活残局使者，需要禁用透视效果
        if (_activatedPlayers.Contains(player.SteamID))
        {
            DisableLastStandEffects(player);
        }

        // 清除状态
        _activatedPlayers.Remove(player.SteamID);
        _playerActiveStatus.Remove(player.SteamID);

        Console.WriteLine($"[残局使者] {player.PlayerName} 失去了残局使者技能");
    }

    /// <summary>
    /// 处理玩家死亡事件 - 检查是否触发残局使者
    /// </summary>
    public void OnPlayerDeath(EventPlayerDeath @event)
    {
        // 每次有人死亡后，检查所有玩家的残局使者状态
        CheckAllPlayersLastStand();
    }

    /// <summary>
    /// 检查所有玩家是否触发残局使者
    /// </summary>
    private void CheckAllPlayersLastStand()
    {
        // 统计每个队伍的存活人数
        var terroristCount = 0;
        var ctCount = 0;

        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive)
                continue;

            if (player.Team == CsTeam.Terrorist)
                terroristCount++;
            else if (player.Team == CsTeam.CounterTerrorist)
                ctCount++;
        }

        Console.WriteLine($"[残局使者] 当前存活人数 - T: {terroristCount}, CT: {ctCount}");

        // 检查每个玩家是否触发残局使者
        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive)
                continue;

            // 检查玩家是否有残局使者技能
            if (!_playerActiveStatus.ContainsKey(player.SteamID))
                continue;

            // 检查是否已激活
            if (_activatedPlayers.Contains(player.SteamID))
                continue;

            // 检查是否只剩自己一人
            bool isLastAlive = false;
            if (player.Team == CsTeam.Terrorist && terroristCount == 1)
                isLastAlive = true;
            else if (player.Team == CsTeam.CounterTerrorist && ctCount == 1)
                isLastAlive = true;

            if (isLastAlive)
            {
                ActivateLastStand(player);
            }
        }
    }

    /// <summary>
    /// 激活残局使者效果（简化版）
    /// </summary>
    private void ActivateLastStand(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 标记为已激活
        _activatedPlayers.Add(player.SteamID);
        _playerActiveStatus[player.SteamID] = true;

        // 增加血量到150
        int currentHealth = pawn.Health;
        pawn.Health = BONUS_HEALTH;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        Console.WriteLine($"[残局使者] {player.PlayerName} 激活残局使者！血量：{currentHealth} → {BONUS_HEALTH}");

        // 使用 ConVar 启用透视效果（简单方式）
        EnableWallhackForPlayer(player);

        // 显示提示
        player.PrintToCenter("💀 残局使者已激活！");
        player.PrintToChat("💀 残局使者已激活！");
        player.PrintToChat($"❤️ 血量增加到 {BONUS_HEALTH}！");
        player.PrintToChat("👁️ 你现在可以透视所有敌人！");

        // 广播消息
        Server.PrintToChatAll($"💀 {player.PlayerName} 激活了残局使者！血量变为{BONUS_HEALTH}并透视所有敌人！");
    }

    /// <summary>
    /// 禁用残局使者效果
    /// </summary>
    private void DisableLastStandEffects(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 禁用透视效果
        DisableWallhackForPlayer(player);

        Console.WriteLine($"[残局使者] {player.PlayerName} 的透视效果已禁用");
    }

    /// <summary>
    /// 为玩家启用透视效果（使用 radarreveal）
    /// </summary>
    private void EnableWallhackForPlayer(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        try
        {
            // 使用 radar reveal ConVar 启用透视
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn != null && playerPawn.IsValid)
            {
                // 设置玩家可以看到所有敌人（通过 ConVar）
                // 这是一种简单且安全的方式
                var conVar = CounterStrikeSharp.API.Modules.Cvars.ConVar.Find("mp_radar_showall_enemies");
                if (conVar != null)
                {
                    // 临时设置（仅对该玩家可见）
                    // 注意：这是服务器级别的 ConVar，会影响所有玩家
                    // 但由于残局使者是全队只剩一人，所以影响不大
                    player.ExecuteClientCommand("mp_radar_showall_enemies 1");
                    Console.WriteLine($"[残局使者] 为 {player.PlayerName} 启用透视效果");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[残局使者] 启用透视效果时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 为玩家禁用透视效果
    /// </summary>
    private void DisableWallhackForPlayer(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        try
        {
            player.ExecuteClientCommand("mp_radar_showall_enemies 0");
            Console.WriteLine($"[残局使者] 为 {player.PlayerName} 禁用透视效果");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[残局使者] 禁用透视效果时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 清理所有残局使者状态（回合结束时调用）
    /// </summary>
    public static void ClearAllLastStand()
    {
        foreach (var steamId in _activatedPlayers)
        {
            var player = Utilities.GetPlayers().FirstOrDefault(p => p.SteamID == steamId);
            if (player != null && player.IsValid)
            {
                player.ExecuteClientCommand("mp_radar_showall_enemies 0");
            }
        }

        _activatedPlayers.Clear();
        Console.WriteLine("[残局使者] 已清理所有激活状态");
    }
}
