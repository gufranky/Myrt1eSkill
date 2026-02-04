using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MyrtleSkill;

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
    private bool _isActive = false;

    // 定位音效列表（简短、能指示位置的声音）- 使用 CS2 有效的音效名称
    private readonly string[] _positionSounds = new string[]
    {
        "C4.PlantSoundB",          // 种弹声
        "C4.Explode",              // C4爆炸声
        "Healthshot.Success",      // 治疗成功声
        "Player.DamageBody.Onlooker", // 受伤声
        "UIPanorama.tab_mainmenu_news", // UI提示音
        "c4.disarmstart",          // 拆弹开始声
        "c4.plant"                 // 种弹声（备选）
    };

    public override void OnApply()
    {
        Console.WriteLine("[怪叫兔] 事件已激活");

        _isActive = true;

        // 显示事件提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
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

        _isActive = false;

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
        // 如果事件不再活跃，不调度新的尖叫
        if (!_isActive)
            return;

        _screamTimer = new System.Threading.Timer(callback =>
        {
            Server.NextFrame(() =>
            {
                // 再次检查事件是否仍然活跃
                if (!_isActive)
                    return;

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
            if (player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid) continue;

            // 为每个玩家随机选择一个音效
            int soundIndex = _random.Next(_positionSounds.Length);
            string soundName = _positionSounds[soundIndex];

            // 使用 EmitSound 播放音效（服务器端 API，更可靠）
            player.PlayerPawn.Value.EmitSound(soundName, volume: 1.0f);

            // 显示提示
            player.PrintToChat($"🐰 你发出了音效：{soundName}");
        }

        Server.NextFrame(() =>
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (player.IsValid)
                {
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

        player.PrintToChat("🐰 怪叫兔：每隔15秒你会自动发出音效暴露位置！");

        return HookResult.Continue;
    }
}
