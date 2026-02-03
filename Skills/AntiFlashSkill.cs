using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 防闪光技能 - 被动技能
/// 免疫闪光弹，你的闪光弹持续7秒，获得3颗闪光弹（投掷后自动补充）
/// </summary>
public class AntiFlashSkill : PlayerSkill
{
    public override string Name => "AntiFlash";
    public override string DisplayName => "✨ 防闪光";
    public override string Description => "免疫闪光弹！你的闪光弹持续7秒！获得3颗闪光弹（投掷后自动补充）！";
    public override bool IsActive => false; // 被动技能

    // 闪光弹持续时间和数量
    private const float FLASH_DURATION = 7.0f;
    private const int FLASHBANG_COUNT = 3;

    // 计数器：跟踪每个玩家的闪光弹数量
    private static readonly Dictionary<ulong, int> _flashbangCounters = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[防闪光] {player.PlayerName} 获得了防闪光技能");

        // 设置计数器为3
        _flashbangCounters[player.SteamID] = FLASHBANG_COUNT;

        // 给予3个闪光弹
        GiveFlashbangs(player, FLASHBANG_COUNT);

        player.PrintToChat("✨ 你获得了防闪光技能！");
        player.PrintToChat($"💣 获得了 {FLASHBANG_COUNT} 颗闪光弹（投掷后自动补充）！");
        player.PrintToChat($"💡 你的闪光弹持续 {FLASH_DURATION} 秒！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 清除计数器
        _flashbangCounters.Remove(player.SteamID);

        Console.WriteLine($"[防闪光] {player.PlayerName} 失去了防闪光技能");
    }

    /// <summary>
    /// 监听闪光弹投掷事件 - 自动补充
    /// </summary>
    public void OnFlashbangDetonate(EventFlashbangDetonate @event)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return;

        // 检查玩家是否有防闪光技能
        var skill = Plugin?.SkillManager.GetPlayerSkill(player);
        if (skill?.Name != "AntiFlash")
            return;

        // 检查计数器是否存在
        if (!_flashbangCounters.ContainsKey(player.SteamID))
            return;

        // 立即补充1个闪光弹
        Server.NextFrame(() =>
        {
            if (player.IsValid && player.PawnIsAlive)
            {
                GiveFlashbangs(player, 1);
                // 计数器始终保持为 FLASHBANG_COUNT（因为我们总是补充到满）
                _flashbangCounters[player.SteamID] = FLASHBANG_COUNT;
            }
        });
    }

    /// <summary>
    /// 给予玩家指定数量的闪光弹
    /// </summary>
    private void GiveFlashbangs(CCSPlayerController player, int count)
    {
        if (player == null || !player.IsValid)
            return;

        try
        {
            for (int i = 0; i < count; i++)
            {
                player.GiveNamedItem("weapon_flashbang");
            }

            Console.WriteLine($"[防闪光] 给予 {player.PlayerName} {count} 个闪光弹");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[防闪光] 给予闪光弹时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理玩家致盲事件 - 免疫或增强闪光弹
    /// </summary>
    public static void HandlePlayerBlind(EventPlayerBlind @event, PlayerSkillManager skillManager)
    {
        var player = @event.Userid;          // 被闪到的玩家
        var attacker = @event.Attacker;      // 投掷者

        if (player == null || !player.IsValid)
            return;

        if (attacker == null || !attacker.IsValid)
            return;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid)
            return;

        // 检查被闪者是否有防闪光技能
        var playerSkill = skillManager.GetPlayerSkill(player);
        if (playerSkill?.Name == "AntiFlash")
        {
            // 免疫闪光弹
            playerPawn.FlashDuration = 0.0f;
            Console.WriteLine($"[防闪光] {player.PlayerName} 的防闪光技能免疫了闪光");
            return;
        }

        // 检查投掷者是否有防闪光技能
        var attackerSkill = skillManager.GetPlayerSkill(attacker);
        if (attackerSkill?.Name == "AntiFlash")
        {
            // 如果是自己投掷的，不增强（只补充，已在 OnFlashbangDetonate 中处理）
            if (player == attacker)
            {
                // 不做处理
            }
            else
            {
                // 是别人，增强闪光弹效果
                playerPawn.FlashDuration = FLASH_DURATION;
                Console.WriteLine($"[防闪光] {attacker.PlayerName} 的强力闪光弹致盲了 {player.PlayerName}，持续时间 {FLASH_DURATION} 秒");
            }
        }
    }
}
