using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace MyrtleSkill;

/// <summary>
/// 慢动作事件 - 游戏速度变为0.5倍
/// </summary>
public class SlowMotionEvent : EntertainmentEvent
{
    public override string Name => "SlowMotion";
    public override string DisplayName => "🎬 慢动作";
    public override string Description => "游戏速度变为0.5倍！一切都变慢了！";

    // ConVars
    private ConVar? _timescaleConVar;
    private float _originalTimescale = 1.0f;

    public override void OnApply()
    {
        Console.WriteLine("[慢动作] 事件已激活");

        // 设置游戏时间流速为0.5倍
        _timescaleConVar = ConVar.Find("host_timescale");
        if (_timescaleConVar != null)
        {
            _originalTimescale = _timescaleConVar.GetPrimitiveValue<float>();
            _timescaleConVar.SetValue(0.5f);
            Console.WriteLine($"[慢动作] host_timescale 已设置为 0.5 (原值: {_originalTimescale})");
        }
        else
        {
            Console.WriteLine("[慢动作] 警告：无法找到 host_timescale ConVar");
        }

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToCenter("🎬 慢动作模式！\n游戏速度变为0.5倍！");
                player.PrintToChat("🎬 慢动作模式已启用！一切都变慢了！");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[慢动作] 事件已恢复");

        // 恢复游戏时间流速
        if (_timescaleConVar != null)
        {
            _timescaleConVar.SetValue(_originalTimescale);
            Console.WriteLine($"[慢动作] host_timescale 已恢复为 {_originalTimescale}");
        }

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("🎬 慢动作模式已禁用");
            }
        }
    }
}
