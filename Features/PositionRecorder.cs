using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;

namespace MyrtleSkill.Features;

/// <summary>
/// 位置记录器 - 记录玩家移动历史
/// </summary>
public class PositionRecorder
{
    private readonly MyrtleSkill _plugin;
    private readonly ConcurrentDictionary<ulong, PlayerPositionHistory> _playerHistories = new();
    private const float RECORD_INTERVAL = 5.0f; // 记录间隔（秒）
    private const int MAX_POSITIONS = 100; // 最多记录100个位置
    private const float MOVE_THRESHOLD = 10.0f; // 移动阈值（单位）- 移动超过这个距离才记录

    public PositionRecorder(MyrtleSkill plugin)
    {
        _plugin = plugin;
    }

    /// <summary>
    /// 启动位置记录器
    /// </summary>
    public void Start()
    {
        Console.WriteLine("[位置记录器] 📍 位置记录器已启动");

        // 立即记录一次初始位置
        RecordAllPlayerPositions();

        // 启动定时器，每5秒记录一次
        StartRecordingTimer();
    }

    /// <summary>
    /// 启动循环定时器
    /// </summary>
    private void StartRecordingTimer()
    {
        _plugin?.AddTimer(RECORD_INTERVAL, () =>
        {
            RecordAllPlayerPositions();

            // 继续下一次循环
            StartRecordingTimer();
        });
    }

    /// <summary>
    /// 记录所有玩家的位置
    /// </summary>
    private void RecordAllPlayerPositions()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive)
                continue;

            RecordPlayerPosition(player);
        }
    }

    /// <summary>
    /// 记录单个玩家的位置
    /// </summary>
    private void RecordPlayerPosition(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
            return;

        // 直接使用 pawn.AbsOrigin（它本身就是 Vector 类型）
        Vector currentPosition = pawn.AbsOrigin;
        ulong steamID = player.SteamID;

        // 获取或创建玩家历史记录
        var history = _playerHistories.GetOrAdd(steamID, _ => new PlayerPositionHistory
        {
            PlayerName = player.PlayerName,
            Positions = new Queue<PositionEntry>(MAX_POSITIONS)
        });

        // 检查是否需要记录（与上次位置对比）
        if (history.LastPosition != null)
        {
            float distance = VectorDistance(currentPosition, history.LastPosition);

            // 如果移动距离小于阈值，不记录
            if (distance < MOVE_THRESHOLD)
            {
                return;
            }
        }

        // 创建位置记录
        var positionEntry = new PositionEntry
        {
            Position = currentPosition,
            Timestamp = Server.CurrentTime,
            MapName = Server.MapName,
            Team = player.TeamNum,
            Health = pawn.Health,
            Armor = pawn.ArmorValue
        };

        // 添加到队列（循环队列，超过100个时移除最旧的）
        history.Positions.Enqueue(positionEntry);
        if (history.Positions.Count > MAX_POSITIONS)
        {
            history.Positions.TryDequeue(out _);
        }

        // 更新上次位置
        history.LastPosition = currentPosition;

        // 控制台日志（可选，每10次记录输出一次）
        if (history.Positions.Count % 10 == 0)
        {
            Console.WriteLine($"[位置记录器] {player.PlayerName} 已记录 {history.Positions.Count} 个位置点");
        }
    }

    /// <summary>
    /// 计算两个位置之间的距离
    /// </summary>
    private float VectorDistance(Vector v1, Vector v2)
    {
        float dx = v1.X - v2.X;
        float dy = v1.Y - v2.Y;
        float dz = v1.Z - v2.Z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>
    /// 获取玩家的位置历史
    /// </summary>
    public PlayerPositionHistory? GetPlayerHistory(ulong steamID)
    {
        if (_playerHistories.TryGetValue(steamID, out var history))
        {
            return history;
        }
        return null;
    }

    /// <summary>
    /// 获取玩家的位置历史（通过玩家对象）
    /// </summary>
    public PlayerPositionHistory? GetPlayerHistory(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return null;

        return GetPlayerHistory(player.SteamID);
    }

    /// <summary>
    /// 清除玩家的位置历史
    /// </summary>
    public void ClearPlayerHistory(ulong steamID)
    {
        _playerHistories.TryRemove(steamID, out _);
    }

    /// <summary>
    /// 清除玩家的位置历史（通过玩家对象）
    /// </summary>
    public void ClearPlayerHistory(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        ClearPlayerHistory(player.SteamID);
    }

    /// <summary>
    /// 清除所有玩家的位置历史
    /// </summary>
    public void ClearAllHistory()
    {
        _playerHistories.Clear();
        Console.WriteLine("[位置记录器] 已清除所有玩家的位置历史");
    }

    /// <summary>
    /// 显示玩家的位置历史信息
    /// </summary>
    public void ShowPlayerHistory(CCSPlayerController player, int count = 10)
    {
        if (player == null || !player.IsValid)
            return;

        var history = GetPlayerHistory(player);
        if (history == null || history.Positions.Count == 0)
        {
            player.PrintToChat("📍 [位置记录器] 没有记录到你的位置信息");
            return;
        }

        var positions = history.Positions.ToArray();
        int showCount = Math.Min(count, positions.Length);

        player.PrintToChat($"───────────────────");
        player.PrintToChat($"📍 最近 {showCount} 个位置记录（共 {positions.Length} 个）");
        player.PrintToChat($"───────────────────");

        for (int i = Math.Max(0, positions.Length - showCount); i < positions.Length; i++)
        {
            var entry = positions[i];
            float timeAgo = Server.CurrentTime - entry.Timestamp;
            player.PrintToChat($"  [{i + 1}] {timeAgo:F0}秒前: X={entry.Position.X:F0}, Y={entry.Position.Y:F0}, Z={entry.Position.Z:F0} | 血量={entry.Health} | 护甲={entry.Armor}");
        }

        player.PrintToChat($"───────────────────");
    }

    /// <summary>
    /// 获取统计信息
    /// </summary>
    public void ShowStatistics()
    {
        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║       📍 位置记录器统计信息           ║");
        Console.WriteLine("╠════════════════════════════════════════╣");
        Console.WriteLine($"║ 地图名称: {Server.MapName,-30} ║");
        Console.WriteLine($"║ 记录间隔: {RECORD_INTERVAL} 秒{(RECORD_INTERVAL == 5.0f ? "" : " ")}                      ║");
        Console.WriteLine($"║ 移动阈值: {MOVE_THRESHOLD} 单位                      ║");
        Console.WriteLine($"║ 最大记录: {MAX_POSITIONS} 个位置                      ║");
        Console.WriteLine($"║ 已记录玩家: {_playerHistories.Count} 人                        ║");
        Console.WriteLine("╠════════════════════════════════════════╣");

        foreach (var kvp in _playerHistories.OrderBy(x => x.Value.PlayerName))
        {
            var history = kvp.Value;
            Console.WriteLine($"║ {history.PlayerName,-20} {history.Positions.Count,3} 个位置 ║");
        }

        Console.WriteLine("╚════════════════════════════════════════╝");
    }

    /// <summary>
    /// 停止位置记录器
    /// </summary>
    public void Stop()
    {
        _playerHistories.Clear();
        Console.WriteLine("[位置记录器] 位置记录器已停止");
    }
}

/// <summary>
/// 玩家位置历史
/// </summary>
public class PlayerPositionHistory
{
    public string PlayerName { get; set; } = string.Empty;
    public Queue<PositionEntry> Positions { get; set; } = new();
    public Vector? LastPosition { get; set; }
}

/// <summary>
/// 位置记录条目
/// </summary>
public class PositionEntry
{
    public required Vector Position { get; set; }
    public float Timestamp { get; set; }
    public string MapName { get; set; } = string.Empty;
    public int Team { get; set; }
    public int Health { get; set; }
    public int Armor { get; set; }
}
