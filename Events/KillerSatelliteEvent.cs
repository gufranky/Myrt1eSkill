using CounterStrikeSharp.API;

namespace MyrtleSkill;

/// <summary>
/// 杀手卫星事件 - 强制所有玩家获得杀手闪电和名刀
/// 危险的卫星系统降临，致命一击与瞬间致盲！
/// </summary>
public class KillerSatelliteEvent : EntertainmentEvent
{
    public override string Name => "KillerSatellite";
    public override string DisplayName => "🛰️ 杀手卫星";
    public override string Description => "所有人获得杀手闪电和名刀！致命闪光与名刀御守！";

    public override void OnApply()
    {
        Console.WriteLine("[杀手卫星] 事件已激活");

        // 设置强制技能列表，技能系统会自动使用这个列表进行分配
        var forcedSkills = new List<string> { "KillerFlash", "Meito" };
        Plugin?.SkillManager.SetForcedSkills(forcedSkills);

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("🛰️ 杀手卫星事件已激活！");
                player.PrintToChat("⚡ 你将获得杀手闪电！");
                player.PrintToChat("⚔️ 你将获得名刀！");
                player.PrintToChat("💡 致盲即死，名刀御命！");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[杀手卫星] 事件已恢复");

        // 确保清除强制技能列表（如果还存在的化）
        Plugin?.SkillManager.ClearForcedSkills();
    }
}
