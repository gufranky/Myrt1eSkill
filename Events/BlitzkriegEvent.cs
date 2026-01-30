using CounterStrikeSharp.API;

namespace MyrtleSkill;

/// <summary>
/// 闪击行动事件 - 游戏速度2倍
/// </summary>
public class BlitzkriegEvent : EntertainmentEvent
{
    public override string Name => "Blitzkrieg";
    public override string DisplayName => "⚡ 闪击行动";
    public override string Description => "游戏速度提升至2倍，一切都在加速进行！";

    public override void OnApply()
    {
        // 设置游戏速度为2倍
        Server.ExecuteCommand("host_timescale 2.0");
        Console.WriteLine("[闪击行动] 游戏速度已设置为2倍");
    }

    public override void OnRevert()
    {
        // 恢复正常游戏速度
        Server.ExecuteCommand("host_timescale 1.0");
        Console.WriteLine("[闪击行动] 游戏速度已恢复正常");
    }
}
