using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MyrtleSkill;

/// <summary>
/// 技能++事件 - 每个玩家获得3个技能
/// 超级力量！三重技能！
/// </summary>
public class SkillsPlusPlusEvent : EntertainmentEvent
{
    public override string Name => "SkillsPlusPlus";
    public override string DisplayName => "🚀 技能++";
    public override string Description => "每个玩家获得3个技能！超级力量！三重技能！";

    private int _originalSkillsPerPlayer = 1;

    public override void OnApply()
    {
        Console.WriteLine("[技能++] 事件已激活");

        // 保存原始配置
        _originalSkillsPerPlayer = Plugin?.SkillManager.SkillsPerPlayer ?? 1;

        // 设置每个玩家获得3个技能
        if (Plugin != null)
        {
            Plugin.SkillManager.SkillsPerPlayer = 3;
            Console.WriteLine("[技能++] 每个玩家将获得 3 个技能");
        }

        // 显示提示（保留聊天框提示，移除屏幕中间提示，统一由HUD显示）
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("🚀 技能++事件已激活！");
                player.PrintToChat("💡 本回合你将获得 3 个技能！");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[技能++] 事件已恢复");

        // 恢复原始配置
        if (Plugin != null)
        {
            Plugin.SkillManager.SkillsPerPlayer = _originalSkillsPerPlayer;
            Console.WriteLine("[技能++] 每个玩家技能数量已恢复为 " + _originalSkillsPerPlayer);
        }
    }
}
