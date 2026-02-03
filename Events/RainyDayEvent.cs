// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills by Juzlus (Ghost skill)

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
    private readonly Dictionary<ulong, HashSet<uint>> _invisibleEntities = new(); // 记录隐藏实体索引
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
            Plugin.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
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
            Plugin.RemoveListener<Listeners.CheckTransmit>(OnCheckTransmit);
        }

        // 恢复所有玩家可见
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            SetPlayerVisibility(player, true);
        }

        _playerVisibleState.Clear();
        _invisibleEntities.Clear();
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
        _invisibleEntities.Remove(player.SteamID);

        return HookResult.Continue;
    }

    /// <summary>
    /// 检查传输时控制玩家可见性
    /// 参考 jRandomSkills Ghost 的 CheckTransmit 实现
    /// </summary>
    /// <summary>
    /// 检查传输时控制玩家可见性
    /// 参考 jRandomSkills Ghost 的 CheckTransmit 实现
    /// </summary>
    /// <summary>
    /// 检查传输时控制玩家可见性
    /// 参考 jRandomSkills Ghost 的 CheckTransmit 实现
    /// </summary>
    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (_invisibleEntities.Count == 0)
            return;

        foreach (var (info, observer) in infoList)
        {
            if (observer == null || !observer.IsValid)
                continue;

            // 遍历所有玩家
            foreach (var kvp in _invisibleEntities)
            {
                ulong playerSteamID = kvp.Key;
                var hiddenEntities = kvp.Value;

                // 不移除观察者自己的实体
                if (observer.SteamID == playerSteamID)
                    continue;

                // 检查玩家是否处于隐身状态（false = 隐身）
                bool playerIsVisible = _playerVisibleState.GetValueOrDefault(playerSteamID, true);

                // 如果玩家可见，不需要隐藏实体
                if (playerIsVisible)
                    continue;

                // 玩家不可见，移除所有隐藏实体的传输
                foreach (var entityIndex in hiddenEntities)
                {
                    info.TransmitEntities.Remove(entityIndex);
                }
            }
        }
    }

    /// <summary>
    /// 设置玩家可见性（包括武器和所有附着物）
    /// 参考 jRandomSkills Ghost 的实现，使用 CheckTransmit 机制
    /// </summary>
    private void SetPlayerVisibility(CCSPlayerController player, bool visible)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 设置玩家身体透明度和阴影
        var color = visible ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(0, 255, 255, 255);
        var shadowStrength = visible ? 1.0f : 0.0f;

        pawn.Render = color;
        pawn.ShadowStrength = shadowStrength;
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");

        // 记录或移除隐藏实体（武器、手套等）
        RecordInvisibleEntities(player, !visible);
    }

    /// <summary>
    /// 记录或清除不可见实体索引（武器、手套等附着物）
    /// 参考 jRandomSkills Ghost 的 SetWeaponVisibility 实现
    /// </summary>
    private void RecordInvisibleEntities(CCSPlayerController player, bool shouldHide)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        if (shouldHide)
        {
            // 隐藏：记录所有需要隐藏的实体索引
            var entities = new HashSet<uint>();

            // 记录玩家 Pawn 索引
            entities.Add(pawn.Index);

            // 记录所有武器索引
            if (pawn.WeaponServices != null)
            {
                foreach (var weapon in pawn.WeaponServices.MyWeapons)
                {
                    if (weapon != null && weapon.IsValid)
                    {
                        entities.Add(weapon.Index);
                    }
                }
            }

            _invisibleEntities[player.SteamID] = entities;
        }
        else
        {
            // 显示：清除记录
            _invisibleEntities.Remove(player.SteamID);
        }
    }
}
