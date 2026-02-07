// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills by Juzlus (Silent skill)

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 沉默技能 - 被动技能
/// 你的脚步声和跳跃声对其他玩家来说是无声的
/// </summary>
public class SilentSkill : PlayerSkill
{
    public override string Name => "Silent";
    public override string DisplayName => "🤫 沉默";
    public override string Description => "你的脚步声和跳跃声对其他玩家来说是无声的！";
    public override bool IsActive => false; // 被动技能

    // 脚步声事件哈希列表（与 jRandomSkills 一致）
    public static readonly uint[] FootstepSoundEvents =
    [
        3109879199, 70939233, 1342713723, 2722081556, 1909915699, 3193435079, 2300993891,
        3847761506, 4084367249, 1342713723, 3847761506, 2026488395, 2745524735, 2684452812,
        2265091453, 1269567645, 520432428, 3266483468, 1346129716, 2061955732, 2240518199,
        2829617974, 1194677450, 1803111098, 3749333696, 29217150, 1692050905, 2207486967,
        2633527058, 3342414459, 988265811, 540697918, 1763490157, 3755338324, 3161194970,
        3753692454, 3166948458, 3997353267, 3161194970, 3753692454, 3166948458, 3997353267,
        809738584, 3368720745, 3295206520, 3184465677, 123085364, 3123711576, 737696412,
        1403457606, 1770765328, 892882552, 3023174225, 4163677892, 3952104171, 4082928848,
        1019414932, 1485322532, 1161855519, 1557420499, 1163426340, 809738584, 3368720745,
        2708661994, 2479376962, 3295206520, 1404198078, 1194093029, 1253503839, 2189706910,
        1218015996, 96240187, 1116700262, 84876002, 1598540856, 2231399653
    ];

    // 沉默声事件哈希列表（与 jRandomSkills 一致）
    public static readonly uint[] SilentSoundEvents =
    [
        2551626319, 765706800, 765706800, 2860219006, 2162652424, 2551626319, 2162652424, 117596568,
        117596568, 740474905, 1661204257, 3009312615, 1506215040, 115843229, 3299941720,
        1016523349, 2684452812, 2067683805, 2067683805, 1016523349, 4160462271, 1543118744,
        585390608, 3802757032, 2302139631, 2546391140, 144629619, 4152012084, 4113422219,
        1627020521, 2899365092, 819435812, 3218103073, 961838155, 1535891875, 1826799645,
        3460445620, 1818046345, 3666896632, 3099536373, 1440734007, 1409986305, 1939055066,
        782454593, 4074593561, 1540837791, 3257325156
    ];

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[沉默] {player.PlayerName} 获得了沉默技能");
        player.PrintToChat("🤫 你获得了沉默技能！");
        player.PrintToChat("💡 你的脚步声和跳跃声对其他玩家是无声的！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[沉默] {player.PlayerName} 失去了沉默技能");
    }

    /// <summary>
    /// 处理玩家发出声音事件（在主文件的 HookUserMessage 中调用）
    /// 拦截脚步声和跳跃声，清空接收者列表
    /// </summary>
    public static void PlayerMakeSound(UserMessage um)
    {
        // 读取声音事件哈希
        uint soundEventHash = um.ReadUInt("soundevent_hash");
        uint userIndex = um.ReadUInt("source_entity_index");

        if (userIndex == 0)
            return;

        // 检查是否是脚步声或跳跃声
        if (!FootstepSoundEvents.Contains(soundEventHash) && !SilentSoundEvents.Contains(soundEventHash))
            return;

        // 查找玩家
        var player = Utilities.GetPlayers().FirstOrDefault(p =>
            p.PlayerPawn?.Value != null &&
            p.PlayerPawn.Value.IsValid &&
            p.PlayerPawn.Value.Index == userIndex);

        if (player == null || !player.IsValid)
            return;

        // 检查玩家是否有沉默技能
        var plugin = MyrtleSkill.Instance;
        if (plugin?.SkillManager == null)
            return;

        var skills = plugin.SkillManager.GetPlayerSkills(player);
        if (skills.Count == 0)
            return;

        var silentSkill = skills.FirstOrDefault(s => s.Name == "Silent");
        if (silentSkill == null)
            return;

        // 清空接收者列表，阻止声音传播
        um.Recipients.Clear();

        Console.WriteLine($"[沉默] {player.PlayerName} 的脚步声/跳跃声被静音");
    }
}
