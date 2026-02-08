// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;

namespace MyrtleSkill;

/// <summary>
/// 选择狂欢事件 - 所有玩家的技能强制为三选一
/// 每个人都能从3个随机技能中选择一个！
/// </summary>
public class ChooseCarnivalEvent : EntertainmentEvent
{
    public override string Name => "ChooseCarnival";
    public override string DisplayName => "🎰 选择狂欢";
    public override string Description => "所有玩家获得三选一技能！从3个随机技能中选择！";

    public override void OnApply()
    {
        Console.WriteLine("[选择狂欢] 事件已激活");

        // 设置强制技能列表为三选一
        var forcedSkills = new List<string> { "ChooseOneOfThree" };
        Plugin?.SkillManager.SetForcedSkills(forcedSkills);

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("🎰 选择狂欢事件已激活！");
                player.PrintToChat("🎰 你将获得三选一技能！");
                player.PrintToChat("💡 输入 !useskill 或按键 E 从3个随机技能中选择！");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[选择狂欢] 事件已恢复");

        // 清除强制技能列表
        Plugin?.SkillManager.ClearForcedSkills();
    }
}
