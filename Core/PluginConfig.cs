using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace MyrtleSkill.Core;

/// <summary>
/// 插件配置（包含事件权重和技能权重）
/// </summary>
public class EventWeightsConfig : BasePluginConfig
{
    [JsonPropertyName("EventWeights")]
    public Dictionary<string, int> EventWeights { get; set; } = new Dictionary<string, int>
    {
        ["NoEvent"] = 100,
        ["LowGravity"] = 10,
        ["LowGravityPlusPlus"] = 10,
        ["HighSpeed"] = 10,
        ["Vampire"] = 10,
        ["TeleportOnDamage"] = 10,
        ["JumpOnShoot"] = 10,
        ["JumpPlusPlus"] = 10,
        ["AnywhereBombPlant"] = 10,
        ["MiniSize"] = 10,
        ["Juggernaut"] = 10,
        ["InfiniteAmmo"] = 10,
        ["SwapOnHit"] = 10,
        ["SmallAndDeadly"] = 10,
        ["Blitzkrieg"] = 10,
        ["DecoyTeleport"] = 10,
        ["Xray"] = 10,
        ["SuperpowerXray"] = 10,
        ["ChickenMode"] = 10,
        ["TopTierParty"] = 5,
        ["TopTierPartyPlusPlus"] = 2,
        ["StayQuiet"] = 10,
        ["RainyDay"] = 10,
        ["ScreamingRabbit"] = 10,
        ["HeadshotOnly"] = 10,
        ["OneShot"] = 10,
        ["DeadlyGrenades"] = 10,
        ["UnluckyCouples"] = 10,
        ["Strangers"] = 10,
        ["AutoBhop"] = 10,
        ["SlowMotion"] = 10,
        ["NoSkill"] = 100
    };

    [JsonPropertyName("SkillWeights")]
    public Dictionary<string, int> SkillWeights { get; set; } = new Dictionary<string, int>
    {
        ["Teleport"] = 10,
        ["SpeedBoost"] = 10,
        ["HighJump"] = 10
    };

    [JsonPropertyName("Notes")]
    public string Notes { get; set; } = "权重越高，事件/技能被选中的概率越大。设置为0可禁用某个事件/技能。";
}
