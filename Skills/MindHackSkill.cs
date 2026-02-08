// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 精神骇入技能 - 主动技能
/// 点击使用可观战随机敌人
/// </summary>
public class MindHackSkill : PlayerSkill
{
    public override string Name => "MindHack";
    public override string DisplayName => "🧠 精神骇入";
    public override string Description => "点击 [css_useSkill] 即可观战随机敌人！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 0.0f; // 0秒冷却

    // 观战持续时间（秒，0表示无限直到切换）
    private const float SPECTATE_DURATION = 0.0f;

    // 跟踪每个玩家的观战状态
    private readonly Dictionary<ulong, MindHackInfo> _playerStates = new();

    // 观战状态信息
    private class MindHackInfo
    {
        public uint OriginalCameraHandle { get; set; }
        public CCSPlayerController? Target { get; set; }
        public bool IsActive { get; set; }
    }

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[精神骇入] {player.PlayerName} 获得了精神骇入技能");
        player.PrintToChat("🧠 你获得了精神骇入技能！");
        player.PrintToChat("💡 点击 [css_useSkill] 或按E键观战随机敌人！");
        player.PrintToChat("⚠️ 再次按键切换回自己的视角！");
        player.PrintToChat($"⏱️ 冷却时间：{Cooldown}秒");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 切换回自己的视角
        ExitMindHack(player);
        _playerStates.Remove(player.SteamID);

        Console.WriteLine($"[精神骇入] {player.PlayerName} 失去了精神骇入技能");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[精神骇入] {player.PlayerName} 使用了精神骇入技能");

        // 切换观战状态
        if (_playerStates.TryGetValue(player.SteamID, out var state) && state.IsActive)
        {
            // 如果正在观战，切换回自己的视角
            ExitMindHack(player);
        }
        else
        {
            // 如果未观战，随机选择一个敌人进行观战
            EnterMindHack(player);
        }
    }

    /// <summary>
    /// 进入观战模式
    /// </summary>
    private void EnterMindHack(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn?.CameraServices == null)
            return;

        // 获取所有敌人
        var enemies = GetEnemies(player);
        if (enemies.Count == 0)
        {
            player.PrintToChat("❌ 没有可观的敌人！");
            return;
        }

        // 随机选择一个敌人
        var random = new Random();
        var target = enemies[random.Next(enemies.Count)];

        if (target == null || !target.IsValid || !target.PawnIsAlive)
        {
            player.PrintToChat("❌ 目标无效！");
            return;
        }

        var targetPawn = target.PlayerPawn.Value;
        if (targetPawn == null || !targetPawn.IsValid)
        {
            player.PrintToChat("❌ 目标无效！");
            return;
        }

        // 保存原始视角
        uint originalCameraHandle = playerPawn.CameraServices.ViewEntity.Raw;

        // 切换到敌人的视角
        playerPawn.CameraServices.ViewEntity.Raw = targetPawn.EntityHandle.Raw;

        // 通知客户端更新
        Utilities.SetStateChanged(playerPawn, "CBasePlayerPawn", "m_pCameraServices");

        // 保存状态
        _playerStates[player.SteamID] = new MindHackInfo
        {
            OriginalCameraHandle = originalCameraHandle,
            Target = target,
            IsActive = true
        };

        Console.WriteLine($"[精神骇入] {player.PlayerName} 正在观战 {target.PlayerName}");

        player.PrintToChat($"🧠 你正在观战 {target.PlayerName}！");
        player.PrintToCenter($"🧠 观战中：{target.PlayerName}");
        player.PrintToChat("⚠️ 再次按键切换回自己的视角！");

        // 如果目标死亡，自动切换回自己的视角
        if (SPECTATE_DURATION > 0 && Plugin != null)
        {
            Plugin.AddTimer(SPECTATE_DURATION, () =>
            {
                if (_playerStates.TryGetValue(player.SteamID, out var state) && state.IsActive)
                {
                    ExitMindHack(player);
                }
            });
        }
    }

    /// <summary>
    /// 退出观战模式
    /// </summary>
    private void ExitMindHack(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        if (!_playerStates.TryGetValue(player.SteamID, out var state))
            return;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn?.CameraServices == null)
            return;

        // 切换回原始视角
        playerPawn.CameraServices.ViewEntity.Raw = state.OriginalCameraHandle;

        // 通知客户端更新
        Utilities.SetStateChanged(playerPawn, "CBasePlayerPawn", "m_pCameraServices");

        // 更新状态
        state.IsActive = false;

        Console.WriteLine($"[精神骇入] {player.PlayerName} 切换回自己的视角");

        player.PrintToChat("🧠 已切换回自己的视角！");
        player.PrintToCenter("🧠 视角已恢复");

        // 如果目标存在，通知目标
        if (state.Target != null && state.Target.IsValid)
        {
            state.Target.PrintToChat($"⚠️ {player.PlayerName} 停止观战你！");
        }
    }

    /// <summary>
    /// 获取所有敌人
    /// </summary>
    private List<CCSPlayerController> GetEnemies(CCSPlayerController player)
    {
        var enemies = new List<CCSPlayerController>();

        foreach (var p in Utilities.GetPlayers())
        {
            if (p == null || !p.IsValid)
                continue;

            if (p == player)
                continue;

            if (!p.PawnIsAlive)
                continue;

            // 只能选择敌人（不同队伍）
            if (player.PlayerPawn.Value?.TeamNum != p.PlayerPawn.Value?.TeamNum)
            {
                enemies.Add(p);
            }
        }

        return enemies;
    }

    /// <summary>
    /// 检查目标是否存活（每帧检查）
    /// </summary>
    public void OnTick()
    {
        // 检查所有观战中的玩家
        foreach (var kvp in _playerStates.ToList())
        {
            var steamID = kvp.Key;
            var state = kvp.Value;

            if (!state.IsActive || state.Target == null)
                continue;

            // 如果目标死亡，切换回自己的视角
            if (!state.Target.IsValid || !state.Target.PawnIsAlive)
            {
                var player = Utilities.GetPlayerFromSteamId(steamID);
                if (player != null && player.IsValid)
                {
                    ExitMindHack(player);
                    player.PrintToChat("⚠️ 目标已死亡，自动切换回自己的视角！");
                }
            }
        }
    }

    /// <summary>
    /// 清理所有观战状态（回合结束时调用）
    /// </summary>
    public static void OnRoundStart()
    {
        // 注意：这里不需要静态清理，因为每个玩家移除技能时会自动清理
        Console.WriteLine("[精神骇入] 回合开始，观战状态保持");
    }
}
