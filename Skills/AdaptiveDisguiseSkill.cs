// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 自适应伪装技能 - 主动技能
/// 伪装成一名敌方玩家的样子，受到伤害后变回原样
/// </summary>
public class AdaptiveDisguiseSkill : PlayerSkill
{
    public override string Name => "AdaptiveDisguise";
    public override string DisplayName => "🎭 自适应伪装";
    public override string Description => "按 [css_useskill] 伪装成敌方玩家！受伤后变回原样！冷却30秒！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 30.0f; // 30秒冷却

    // 跟踪每个玩家的伪装状态
    private readonly Dictionary<ulong, DisguiseState> _playerDisguises = new();

    // 跟踪每个玩家是否已伪装（用于受伤检测）
    private readonly HashSet<ulong> _isDisguised = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[自适应伪装] {player.PlayerName} 获得了自适应伪装技能");

        player.PrintToChat("🎭 你获得了自适应伪装技能！");
        player.PrintToChat("💡 输入 !useskill 或按键伪装成敌方玩家！");
        player.PrintToChat($"⏱️ 冷却时间：{Cooldown}秒");
        player.PrintToChat("⚠️ 受到伤害后会立即变回原样！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 如果正在伪装，恢复原样
        RemoveDisguise(player);

        _playerDisguises.Remove(player.SteamID);
        _isDisguised.Remove(player.SteamID);

        Console.WriteLine($"[自适应伪装] {player.PlayerName} 失去了自适应伪装技能");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        // 如果已经伪装，先移除
        if (_isDisguised.Contains(player.SteamID))
        {
            RemoveDisguise(player);
            player.PrintToChat("🎭 伪装已解除！");
            return;
        }

        Console.WriteLine($"[自适应伪装] {player.PlayerName} 激活了伪装技能");

        // 尝试伪装
        ApplyDisguise(player);
    }

    /// <summary>
    /// 对玩家应用伪装效果
    /// </summary>
    private void ApplyDisguise(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid)
            return;

        // 保存原始模型
        string originalModel = playerPawn.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName;

        // 查找敌方玩家
        var targetPlayer = FindRandomEnemyPlayer(player);
        if (targetPlayer == null)
        {
            player.PrintToChat("❌ 没有找到可以伪装的敌方玩家！");
            return;
        }

        var targetPawn = targetPlayer.PlayerPawn.Value;
        if (targetPawn == null || !targetPawn.IsValid)
            return;

        // 获取敌方玩家模型
        string enemyModel = targetPawn.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName;

        // 保存伪装状态
        _playerDisguises[player.SteamID] = new DisguiseState
        {
            OriginalModel = originalModel,
            DisguiseModel = enemyModel,
            TargetPlayerName = targetPlayer.PlayerName
        };

        // 下一帧设置模型（避免帧问题）
        Server.NextFrame(() =>
        {
            if (!playerPawn.IsValid)
                return;

            try
            {
                // 设置模型
                playerPawn.SetModel(enemyModel);

                // 标记为已伪装
                _isDisguised.Add(player.SteamID);

                // 通知状态变更
                Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_nModelIndex");

                Console.WriteLine($"[自适应伪装] {player.PlayerName} 伪装成 {targetPlayer.PlayerName}");
                player.PrintToChat($"🎭 你伪装成了 {targetPlayer.PlayerName}！");
                player.PrintToCenter("🎭 伪装成功！");

                // 播放音效
                player.EmitSound("GlassBottle.BulletImpact");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[自适应伪装] 伪装时出错: {ex.Message}");
                _playerDisguises.Remove(player.SteamID);
            }
        });
    }

    /// <summary>
    /// 移除伪装效果
    /// </summary>
    private void RemoveDisguise(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        if (!_playerDisguises.TryGetValue(player.SteamID, out var state))
            return;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid)
            return;

        try
        {
            // 恢复原始模型
            playerPawn.SetModel(state.OriginalModel);

            // 标记为未伪装
            _isDisguised.Remove(player.SteamID);

            // 通知状态变更
            Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_nModelIndex");

            Console.WriteLine($"[自适应伪装] {player.PlayerName} 恢复了原样");

            if (_isDisguised.Contains(player.SteamID))
            {
                player.PrintToChat("🎭 伪装已解除！");
            }

            // 播放音效
            player.EmitSound("GlassBottle.BulletImpact");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[自适应伪装] 恢复时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 查找随机敌方玩家
    /// </summary>
    private CCSPlayerController? FindRandomEnemyPlayer(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return null;

        var playerTeam = player.Team;
        if (playerTeam == CsTeam.None || playerTeam == CsTeam.Spectator)
            return null;

        // 查找所有敌方玩家
        var enemyPlayers = Utilities.GetPlayers()
            .Where(p => p.IsValid && p.PawnIsAlive && p.Team != playerTeam && p.Team != CsTeam.Spectator)
            .ToList();

        if (enemyPlayers.Count == 0)
            return null;

        // 随机选择一个
        var random = new Random();
        return enemyPlayers[random.Next(enemyPlayers.Count)];
    }

    /// <summary>
    /// 处理玩家受伤事件 - 检查是否需要移除伪装
    /// </summary>
    public void OnPlayerHurt(EventPlayerHurt @event)
    {
        if (@event == null)
            return;

        var victim = @event.Userid;
        if (victim == null || !victim.IsValid)
            return;

        // 检查受害者是否有伪装技能且正在伪装
        if (!_isDisguised.Contains(victim.SteamID))
            return;

        // 移除伪装
        RemoveDisguise(victim);
        victim.PrintToCenter("💥 你受到了伤害，伪装已解除！");
    }

    /// <summary>
    /// 伪装状态
    /// </summary>
    private class DisguiseState
    {
        public string OriginalModel { get; set; } = string.Empty;
        public string DisguiseModel { get; set; } = string.Empty;
        public string TargetPlayerName { get; set; } = string.Empty;
    }
}
