using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace HelloWorldPlugin;

/// <summary>
/// 怪叫兔事件 - 每隔15秒所有玩家发出短促的定位音效，暴露位置
/// </summary>
public class ScreamingRabbitEvent : EntertainmentEvent
{
    public override string Name => "ScreamingRabbit";
    public override string DisplayName => "🐰 怪叫兔";
    public override string Description => "每隔15秒所有玩家发出定位音效！暴露位置！";

    private const float ScreamInterval = 15.0f; // 尖叫间隔（秒）

    private readonly Random _random = new();
    private System.Threading.Timer? _screamTimer;

    // 定位音效列表（简短、能指示位置的声音）
    private readonly string[] _positionSounds = new string[]
    {
        "Chicken.Alert",           // 鸡叫声（短促）
        "Chicken.Idle",            // 鸡闲聊声
        "Chicken.Panic",           // 鸡惊恐声
        "C4.DisarmStart",          // 拆弹开始声
        "C4.Plant",                // 种弹声
        "Weapon.Empty",            // 空弹夹声
        "Bullet.Impact",           // 子弹击中声
        "Player.Footstep",         // 脚步声
        "Player.Death",            // 死亡声（短促）
        "Physics.ImpactSoft"       // 轻微撞击声
    };

    public override void OnApply()
    {
        Console.WriteLine("[怪叫兔] 事件已激活");

        // 显示事件提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToCenter("🐰 怪叫兔事件开始！\n每15秒会发出定位音效！");
                player.PrintToChat("🐰 怪叫兔：每隔15秒你会自动发出音效暴露位置！");
            }
        }

        // 注册玩家生成事件
        if (Plugin != null)
        {
            Plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
        }

        // 启动尖叫定时器
        ScheduleNextScream();
    }

    public override void OnRevert()
    {
        Console.WriteLine("[怪叫兔] 事件已恢复");

        // 停止定时器
        _screamTimer?.Dispose();
        _screamTimer = null;

        // 移除事件监听
        if (Plugin != null)
        {
            Plugin.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
        }
    }

    /// <summary>
    /// 调度下一次尖叫
    /// </summary>
    private void ScheduleNextScream()
    {
        _screamTimer = new System.Threading.Timer(callback =>
        {
            Server.NextFrame(() =>
            {
                // 开始倒计时
                StartCountdown();
            });
        }, null, (int)(ScreamInterval * 1000), Timeout.Infinite);
    }

    /// <summary>
    /// 开始倒计时 3 2 1
    /// </summary>
    private void StartCountdown()
    {
        // 倒计时 3
        Plugin?.AddTimer(0.0f, () => ShowCountdown("3"));

        // 倒计时 2
        Plugin?.AddTimer(1.0f, () => ShowCountdown("2"));

        // 倒计时 1
        Plugin?.AddTimer(2.0f, () => ShowCountdown("1"));

        // 倒计时结束，播放音效
        Plugin?.AddTimer(3.0f, () =>
        {
            PlayPositionSoundToAll();

            // 调度下一次尖叫
            ScheduleNextScream();
        });
    }

    /// <summary>
    /// 显示倒计时
    /// </summary>
    private void ShowCountdown(string number)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToCenter($"🐰 {number}");
            }
        }
    }

    /// <summary>
    /// 对所有玩家播放定位音效
    /// </summary>
    private void PlayPositionSoundToAll()
    {
        Console.WriteLine("[怪叫兔] 播放定位音效");

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive) continue;

            // 为每个玩家随机选择一个音效
            int soundIndex = _random.Next(_positionSounds.Length);
            string soundName = _positionSounds[soundIndex];

            // 播放音效
            player.ExecuteClientCommand($"play {soundName}");

            // 显示提示
            player.PrintToChat($"🐰 你发出了音效：{soundName}");
        }

        Server.NextFrame(() =>
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (player.IsValid)
                {
                    player.PrintToCenter("🐰 嘎嘎！！！");
                }
            }
        });
    }

    /// <summary>
    /// 玩家生成时显示提示
    /// </summary>
    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        player.PrintToCenter("🐰 怪叫兔事件进行中！\n每15秒会发出定位音效！");
        player.PrintToChat("🐰 怪叫兔：每隔15秒你会自动发出音效暴露位置！");

        return HookResult.Continue;
    }
}
