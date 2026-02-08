using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;

namespace MyrtleSkill.Events;

/// <summary>
/// 反向爆头事件 - 射到四肢有爆头效果！
/// 头部伤害：1/4 倍
/// 四肢伤害：4 倍（爆头效果）
/// 胸部/腹部伤害：1 倍
/// </summary>
public class InverseHeadshotEvent : EntertainmentEvent
{
    public override string Name => "InverseHeadshot";
    public override string DisplayName => "🎯 反向爆头";
    public override string Description => "射到四肢有爆头效果！四肢伤害 4 倍！";

    // 命中部位（使用 HitGroup_t 枚举）
    private const HitGroup_t HITGROUP_HEAD = HitGroup_t.HITGROUP_HEAD;
    private const HitGroup_t HITGROUP_CHEST = HitGroup_t.HITGROUP_CHEST;
    private const HitGroup_t HITGROUP_STOMACH = HitGroup_t.HITGROUP_STOMACH;
    private const HitGroup_t HITGROUP_LEFTARM = HitGroup_t.HITGROUP_LEFTARM;
    private const HitGroup_t HITGROUP_RIGHTARM = HitGroup_t.HITGROUP_RIGHTARM;
    private const HitGroup_t HITGROUP_LEFTLEG = HitGroup_t.HITGROUP_LEFTLEG;
    private const HitGroup_t HITGROUP_RIGHTLEG = HitGroup_t.HITGROUP_RIGHTLEG;

    public override void OnApply()
    {
        Console.WriteLine("[反向爆头] 事件已激活");

        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("🎯 反向爆头事件已激活！");
                player.PrintToChat("💡 射到四肢有爆头效果（4倍伤害）！");
                player.PrintToChat("💡 头部伤害降为 1/4 倍！");
                player.PrintToChat("💡 胸部和腹部伤害正常！");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[反向爆头] 事件已恢复");
    }

    /// <summary>
    /// 处理伤害倍数 - 检查命中部位并返回伤害倍数
    /// 四肢有爆头效果（4倍伤害），头部伤害降低
    /// 注意：调用者应该先通过 FindEventsOfType<InverseHeadshotEvent>() 确认事件存在
    /// </summary>
    public static float? HandleDamagePre(CCSPlayerPawn victim, CTakeDamageInfo info)
    {
        // 获取命中部位（使用Schema访问）
        var hitgroupValue = Schema.GetSchemaValue<int>(info.Handle, "CTakeDamageInfo", "m_nHitgroup");
        var hitgroup = (HitGroup_t)hitgroupValue;

        // 根据命中部位返回伤害倍数
        float damageMultiplier;
        string hitLocation;

        switch (hitgroup)
        {
            case HITGROUP_LEFTARM:
            case HITGROUP_RIGHTARM:
            case HITGROUP_LEFTLEG:
            case HITGROUP_RIGHTLEG:
                // 四肢：爆头效果（4 倍伤害）
                damageMultiplier = 4.0f;
                hitLocation = "四肢";
                break;

            case HITGROUP_HEAD:
                // 头部：伤害降低（1/4 倍）
                damageMultiplier = 0.25f;
                hitLocation = "头部";
                break;

            case HITGROUP_CHEST:
            case HITGROUP_STOMACH:
            default:
                // 胸部/腹部/其他：正常伤害（1 倍）
                damageMultiplier = 1.0f;
                hitLocation = "身体";
                break;
        }

        Console.WriteLine($"[反向爆头] 命中{hitLocation}，伤害倍数: {damageMultiplier}");

        // 返回伤害倍数
        return damageMultiplier;
    }
}
