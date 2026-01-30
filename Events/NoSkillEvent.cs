using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace HelloWorldPlugin;

/// <summary>
/// 没有技能事件 - 这是更加平静的一天，所有玩家都不会获得技能
/// </summary>
public class NoSkillEvent : EntertainmentEvent
{
    public override string Name => "NoSkill";
    public override string DisplayName => "😌 没有技能";
    public override string Description => "这是更加平静的一天，所有人都没有技能！";

    public override void OnApply()
    {
        Console.WriteLine("[没有技能] 事件已激活 - 本回合所有玩家不会获得技能");

        // 设置标志，禁用本回合的技能分配
        if (Plugin != null)
        {
            Plugin.DisableSkillsThisRound = true;
        }

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToCenter("😌 没有技能！\n这是更加平静的一天！");
                player.PrintToChat("😌 本回合所有人都没有技能，享受纯粹的游戏吧！");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[没有技能] 事件已恢复");

        // 恢复技能系统
        if (Plugin != null)
        {
            Plugin.DisableSkillsThisRound = false;
        }

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("😌 没有技能事件已结束");
            }
        }
    }
}
