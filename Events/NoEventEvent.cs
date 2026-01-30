using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MyrtleSkill;

/// <summary>
/// 无事件 - 回合正常进行
/// </summary>
public class NoEventEvent : EntertainmentEvent
{
    public override string Name => "NoEvent";
    public override string DisplayName => "正常回合";
    public override string Description => "本回合无特殊效果，正常进行";
    public override int Weight { get; set; } = 40;

    public override void OnApply()
    {
        // 无操作
        Console.WriteLine("[无事件] 正常回合开始");
    }
}
