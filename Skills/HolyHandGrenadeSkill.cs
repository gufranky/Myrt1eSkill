// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills by Juzlus (Holy Hand Grenade skill)

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 圣手榴弹技能 - 被动技能
/// 你的高爆手雷造成双倍伤害并有双倍范围
/// 开局获得1颗HE手雷，投掷后自动补充1次
/// 完全复制自 jRandomSkills Holy Hand Grenade
/// </summary>
public class HolyHandGrenadeSkill : PlayerSkill
{
    public override string Name => "HolyHandGrenade";
    public override string DisplayName => "✝️ 圣手榴弹";
    public override string Description => "你的HE手雷造成双倍伤害并有双倍范围！开局获得1颗（投掷后自动补充1次）！";
    public override bool IsActive => false; // 被动技能

    // 伤害和范围倍数（与 jRandomSkills 一致）
    private const float DAMAGE_MULTIPLIER = 2.0f;
    private const float DAMAGE_RADIUS_MULTIPLIER = 2.0f;

    // 手雷数量和补充次数
    private const int GRENADE_COUNT = 1;
    private const int MAX_REPLENISH_COUNT = 1; // 最多补充1次

    // 计数器：跟踪每个玩家的手雷数量
    private readonly Dictionary<ulong, int> _grenadeCounters = new();

    // 跟踪每回合已补充次数
    private readonly Dictionary<ulong, int> _replenishedCount = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[圣手榴弹] {player.PlayerName} 获得了圣手榴弹技能");

        // 设置计数器为1，补充次数为0
        _grenadeCounters[player.SteamID] = GRENADE_COUNT;
        _replenishedCount[player.SteamID] = 0;

        // 给予1个HE手雷
        GiveGrenades(player, GRENADE_COUNT);

        player.PrintToChat("✝️ 你获得了圣手榴弹技能！");
        player.PrintToChat($"💣 获得了 {GRENADE_COUNT} 颗HE手雷（投掷后自动补充{MAX_REPLENISH_COUNT}次）！");
        player.PrintToChat($"💡 你的HE手雷造成{DAMAGE_MULTIPLIER}倍伤害和{DAMAGE_RADIUS_MULTIPLIER}倍范围！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 清除计数器
        _grenadeCounters.Remove(player.SteamID);
        _replenishedCount.Remove(player.SteamID);

        Console.WriteLine($"[圣手榴弹] {player.PlayerName} 失去了圣手榴弹技能");
    }

    /// <summary>
    /// 给予玩家指定数量的HE手雷
    /// </summary>
    private void GiveGrenades(CCSPlayerController player, int count)
    {
        if (player == null || !player.IsValid)
            return;

        try
        {
            for (int i = 0; i < count; i++)
            {
                player.GiveNamedItem("weapon_hegrenade");
            }

            Console.WriteLine($"[圣手榴弹] 给予 {player.PlayerName} {count} 个HE手雷");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[圣手榴弹] 给予HE手雷时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理实体生成事件 - 增强HE手雷的伤害和范围，并自动补充
    /// 完全复制自 jRandomSkills Holy Hand Grenade.OnEntitySpawned
    /// </summary>
    public void OnEntitySpawned(CEntityInstance entity)
    {
        var name = entity.DesignerName;
        if (!name.EndsWith("hegrenade_projectile"))
            return;

        Server.NextFrame(() =>
        {
            var hegrenade = entity.As<CHEGrenadeProjectile>();
            if (hegrenade == null || !hegrenade.IsValid)
                return;

            var playerPawn = hegrenade.Thrower.Value;
            if (playerPawn == null || !playerPawn.IsValid)
                return;

            var player = Utilities.GetPlayers().FirstOrDefault(p => p.PlayerPawn?.Value?.Index == playerPawn.Index);
            if (player == null || !player.IsValid)
                return;

            // 检查玩家是否有圣手榴弹技能
            var skills = Plugin?.SkillManager.GetPlayerSkills(player);
            if (skills == null || skills.Count == 0)
                return;

            var holyHandGrenadeSkill = skills.FirstOrDefault(s => s.Name == "HolyHandGrenade");
            if (holyHandGrenadeSkill == null)
                return;

            // 增强手雷伤害和范围
            hegrenade.Damage *= DAMAGE_MULTIPLIER;
            hegrenade.DmgRadius *= DAMAGE_RADIUS_MULTIPLIER;

            Console.WriteLine($"[圣手榴弹] {player.PlayerName} 的HE手雷已增强：伤害×{DAMAGE_MULTIPLIER}，范围×{DAMAGE_RADIUS_MULTIPLIER}");

            // 自动补充1次（最多1次）
            if (!_grenadeCounters.ContainsKey(player.SteamID))
                return;

            if (_replenishedCount.TryGetValue(player.SteamID, out var count) && count >= MAX_REPLENISH_COUNT)
            {
                Console.WriteLine($"[圣手榴弹] {player.PlayerName} 本回合已补充{count}次，达到上限({MAX_REPLENISH_COUNT}次)，不再补充");
                return;
            }

            // 延迟补充（等待手雷投掷完成）
            Server.NextFrame(() =>
            {
                if (player.IsValid && player.PawnIsAlive)
                {
                    GiveGrenades(player, 1);
                    _replenishedCount[player.SteamID] = count + 1;

                    player.PrintToChat($"✝️ HE手雷已补充！({_replenishedCount[player.SteamID]}/{MAX_REPLENISH_COUNT})");
                    Console.WriteLine($"[圣手榴弹] {player.PlayerName} 的HE手雷已补充 ({_replenishedCount[player.SteamID]}/{MAX_REPLENISH_COUNT})");
                }
            });
        });
    }
}
