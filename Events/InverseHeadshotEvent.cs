using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;

namespace MyrtleSkill.Events;

/// <summary>
/// 反向爆头事件 - 所有玩家受到的伤害：头部1/4倍，身体4倍
/// </summary>
public class InverseHeadshotEvent : EntertainmentEvent
{
    public override string Name => "InverseHeadshot";
    public override string DisplayName => "🎯 反向爆头";
    public override string Description => "头部伤害变为 1/4 倍！身体伤害变为 4 倍！";

    // 命中部位（使用 HitGroup_t 枚举）
    private const HitGroup_t HITGROUP_HEAD = HitGroup_t.HITGROUP_HEAD;

    public override void OnApply()
    {
        Console.WriteLine("[反向爆头] 事件已激活");

        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("🎯 反向爆头事件已激活！");
                player.PrintToChat("💡 头部伤害变为 1/4 倍！");
                player.PrintToChat("💡 身体伤害变为 4 倍！");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[反向爆头] 事件已恢复");
    }

    /// <summary>
    /// 处理伤害倍数 - 检查命中部位并返回伤害倍数
    /// </summary>
    public static float? HandleDamagePre(CCSPlayerPawn victim, CTakeDamageInfo info)
    {
        // 检查是否有反向爆头事件激活
        var plugin = MyrtleSkill.Instance;
        if (plugin?.CurrentEvent?.Name != "InverseHeadshot")
            return null;

        // 获取命中部位（使用Schema访问）
        var hitgroupValue = Schema.GetSchemaValue<int>(info.Handle, "CTakeDamageInfo", "m_nHitgroup");
        var hitgroup = (HitGroup_t)hitgroupValue;

        // 根据命中部位返回伤害倍数
        float damageMultiplier;
        if (hitgroup == HITGROUP_HEAD)
        {
            // 头部伤害：1/4 倍
            damageMultiplier = 0.25f;
            Console.WriteLine($"[反向爆头] 命中头部，伤害倍数: {damageMultiplier}");
        }
        else
        {
            // 身体伤害：4 倍
            damageMultiplier = 4.0f;
            Console.WriteLine($"[反向爆头] 命中身体，伤害倍数: {damageMultiplier}");
        }

        // 返回伤害倍数
        return damageMultiplier;
    }
}
