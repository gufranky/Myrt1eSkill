// MyrtleSkill Plugin - GNU GPL v3.0
// Based on jRandomSkills Anomaly by Juzlus

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;

namespace MyrtleSkill.Skills;

/// <summary>
/// 时间回溯技能 - 主动技能
/// 使用后回到 5 秒前的位置、视角和血量状态
/// </summary>
public class TimeRecallSkill : PlayerSkill
{
    public override string Name => "TimeRecall";
    public override string DisplayName => "⏪ 时间回溯";
    public override string Description => "使用后回到 5 秒前的位置、视角和血量状态！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 15.0f; // 15 秒冷却时间

    // 记录频率（每 64 ticks 记录一次，约 1 秒）
    private const int TICK_RATE = 64;

    // 记录时长（秒）
    private const int SECONDS_IN_BACK = 5;

    // 跟踪每个玩家的历史状态
    private readonly ConcurrentDictionary<ulong, PlayerHistoryState> _playerStates = new();

    // 是否已注册 OnTick 监听
    private bool _isTickRegistered = false;

    /// <summary>
    /// 玩家历史状态
    /// </summary>
    private class PlayerHistoryState
    {
        public ConcurrentQueue<HistorySnapshot> Snapshots { get; set; } = new();
        public ulong SteamID { get; set; }
    }

    /// <summary>
    /// 历史快照（包含位置、视角、血量）
    /// </summary>
    private class HistorySnapshot
    {
        public required Vector Position { get; set; }
        public required QAngle Rotation { get; set; }
        public int Health { get; set; }
        public int Armor { get; set; }
    }

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[时间回溯] {player.PlayerName} 获得了时间回溯技能");

        // 初始化玩家状态
        _playerStates.TryAdd(player.SteamID, new PlayerHistoryState
        {
            SteamID = player.SteamID
        });

        player.PrintToChat("⏪ 你获得了时间回溯技能！");
        player.PrintToChat("💡 按键使用后回到 5 秒前的状态！");
        player.PrintToChat($"⏱️ 冷却时间：{Cooldown} 秒");

        // 注册 OnTick 监听（如果还没注册）
        if (!_isTickRegistered && Plugin != null)
        {
            Plugin.RegisterListener<Listeners.OnTick>(OnTick);
            _isTickRegistered = true;
        }
    }

    public override void OnRevert(CCSPlayerController player)
    {
        // 清理玩家状态
        _playerStates.TryRemove(player.SteamID, out _);

        Console.WriteLine($"[时间回溯] {player.PlayerName} 失去了时间回溯技能");

        // 如果没有玩家使用此技能，移除监听
        if (_playerStates.IsEmpty && Plugin != null)
        {
            Plugin.RemoveListener<Listeners.OnTick>(OnTick);
            _isTickRegistered = false;
        }
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid)
            return;

        if (!_playerStates.TryGetValue(player.SteamID, out var playerState))
            return;

        var snapshots = playerState.Snapshots;
        if (snapshots == null || snapshots.IsEmpty)
        {
            player.PrintToChat("⏪ 没有历史记录可回溯！");
            return;
        }

        // 获取最早的快照（5秒前的状态）
        if (!snapshots.TryDequeue(out var snapshot))
        {
            player.PrintToChat("⏪ 没有历史记录可回溯！");
            return;
        }

        Console.WriteLine($"[时间回溯] {player.PlayerName} 使用时间回溯，回溯到 {SECONDS_IN_BACK} 秒前的状态");

        // 回溯位置和视角
        playerPawn.Teleport(snapshot.Position, snapshot.Rotation, new Vector(0, 0, 0));

        // 回溯血量
        playerPawn.Health = snapshot.Health;
        Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_iHealth");

        // 回溯护甲
        if (playerPawn.ItemServices != null)
        {
            playerPawn.ArmorValue = snapshot.Armor;
            Utilities.SetStateChanged(playerPawn, "CCSPlayerPawn", "m_ArmorValue");
        }

        player.PrintToCenter("⏪ 时间已回溯！");
        player.PrintToChat($"⏪ 你回到了 {SECONDS_IN_BACK} 秒前的状态！");
        player.PrintToChat($"❤️ 血量恢复至 {snapshot.Health}，🛡️ 护甲恢复至 {snapshot.Armor:F0}");
    }

    /// <summary>
    /// 每帧更新 - 记录玩家状态
    /// </summary>
    private void OnTick()
    {
        // 只在特定 tick 记录（节省性能）
        if (Server.TickCount % TICK_RATE != 0)
            return;

        foreach (var kvp in _playerStates)
        {
            var steamID = kvp.Key;
            var playerState = kvp.Value;

            // 查找玩家
            var player = Utilities.GetPlayers().FirstOrDefault(p => p.SteamID == steamID);
            if (player == null || !player.IsValid || !player.PawnIsAlive)
                continue;

            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null || playerPawn.AbsRotation == null)
                continue;

            // 创建新快照
            var snapshot = new HistorySnapshot
            {
                Position = new Vector(playerPawn.AbsOrigin.X, playerPawn.AbsOrigin.Y, playerPawn.AbsOrigin.Z),
                Rotation = new QAngle(playerPawn.AbsRotation.X, playerPawn.AbsRotation.Y, playerPawn.AbsRotation.Z),
                Health = playerPawn.Health,
                Armor = playerPawn.ArmorValue
            };

            // 添加到队列
            playerState.Snapshots.Enqueue(snapshot);

            // 保持队列大小（移除超过 SECONDS_IN_BACK 秒的快照）
            while (playerState.Snapshots.Count > SECONDS_IN_BACK)
            {
                playerState.Snapshots.TryDequeue(out _);
            }
        }
    }
}
