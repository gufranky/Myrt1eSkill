using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace HelloWorldPlugin.Core;

/// <summary>
/// 事件权重配置
/// </summary>
public class EventWeightsConfig : BasePluginConfig
{
    [JsonPropertyName("EventWeights")]
    public Dictionary<string, int> EventWeights { get; set; } = new Dictionary<string, int>
    {
        ["NoEvent"] = 40,
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
        ["SmallAndDeadly"] = 10
    };

    [JsonPropertyName("Notes")]
    public string Notes { get; set; } = "权重越高，事件被选中的概率越大。设置为0可禁用某个事件。";
}
