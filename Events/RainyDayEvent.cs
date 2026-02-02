using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Drawing;

namespace MyrtleSkill;

/// <summary>
/// 下雨天事件 - 所有玩家隐身，随机每隔3~10秒显形2秒
/// </summary>
public class RainyDayEvent : EntertainmentEvent
{
    public override string Name => "RainyDay";
    public override string DisplayName => "🌧️ 下雨天";
    public override string Description => "所有玩家隐身！随机每隔3~10秒显形2秒！";

    private const float MinInvisibleInterval = 3.0f; // 最小隐身间隔
    private const float MaxInvisibleInterval = 10.0f; // 最大隐身间隔
    private const float VisibleDuration = 2.0f; // 显形持续时间
    private readonly Random _random = new();

    private readonly Dictionary<ulong, bool> _playerVisibleState = new();
    private System.Threading.Timer? _revealTimer;
    private bool _isActive = false;

    public override void OnApply()
    {
        Console.WriteLine("[下雨天] 事件已激活");

        _isActive = true;

        // 初始化所有玩家为隐身状态
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive) continue;

            _playerVisibleState[player.SteamID] = false;
            SetPlayerVisibility(player, false);
        }

        // 显示事件提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToCenter("🌧️ 下雨天！你进入了隐身状态！");
            }
        }

        // 注册玩家生成和死亡事件
        if (Plugin != null)
        {
            Plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
            Plugin.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Post);
        }

        // 启动随机显形定时器
        ScheduleNextReveal();
    }

    public override void OnRevert()
    {
        Console.WriteLine("[下雨天] 事件已恢复");

        _isActive = false;

        // 停止定时器
        _revealTimer?.Dispose();
        _revealTimer = null;

        // 移除事件监听
        if (Plugin != null)
        {
            Plugin.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
            Plugin.DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Post);
        }

        // 恢复所有玩家可见
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            SetPlayerVisibility(player, true);
        }

        _playerVisibleState.Clear();
    }

    /// <summary>
    /// 调度下一次显形
    /// </summary>
    private void ScheduleNextReveal()
    {
        // 如果事件不再活跃，不调度新的显形
        if (!_isActive)
            return;

        float interval = (float)(_random.NextDouble() * (MaxInvisibleInterval - MinInvisibleInterval) + MinInvisibleInterval);

        _revealTimer = new System.Threading.Timer(callback =>
        {
            Server.NextFrame(() =>
            {
                // 再次检查事件是否仍然活跃
                if (!_isActive)
                    return;

                // 显形2秒
                RevealAllPlayers();

                // 2秒后重新隐身
                Plugin?.AddTimer(VisibleDuration, () =>
                {
                    if (!_isActive)
                        return;

                    HideAllPlayers();

                    // 调度下一次显形
                    ScheduleNextReveal();
                });
            });
        }, null, (int)(interval * 1000), Timeout.Infinite);
    }

    /// <summary>
    /// 让所有玩家显形
    /// </summary>
    private void RevealAllPlayers()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive) continue;

            _playerVisibleState[player.SteamID] = true;
            SetPlayerVisibility(player, true);

            player.PrintToChat("⚡ 闪电！你现形了！");
            player.PrintToCenter("⚡ 闪电！所有人现形！");
        }
    }

    /// <summary>
    /// 让所有玩家重新隐身
    /// </summary>
    private void HideAllPlayers()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive) continue;

            _playerVisibleState[player.SteamID] = false;
            SetPlayerVisibility(player, false);

            player.PrintToChat("🌧️ 你重新进入隐身状态！");
        }
    }

    /// <summary>
    /// 玩家生成时设置初始状态
    /// </summary>
    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        _playerVisibleState[player.SteamID] = false;
        SetPlayerVisibility(player, false);
        player.PrintToCenter("🌧️ 下雨天！你进入了隐身状态！");

        return HookResult.Continue;
    }

    /// <summary>
    /// 玩家死亡时清理状态
    /// </summary>
    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        _playerVisibleState.Remove(player.SteamID);

        return HookResult.Continue;
    }

    /// <summary>
    /// 设置玩家可见性
    /// </summary>
    private void SetPlayerVisibility(CCSPlayerController player, bool visible)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        var color = visible ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(0, 255, 255, 255);
        var shadowStrength = visible ? 1.0f : 0.0f;

        pawn.Render = color;
        pawn.ShadowStrength = shadowStrength;
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }
}
