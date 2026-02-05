using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 超级闪光技能 - 被动技能
/// 你的闪光弹总会让所有敌人闪白3秒
/// </summary>
public class SuperFlashSkill : PlayerSkill
{
    public override string Name => "SuperFlash";
    public override string DisplayName => "💥 超级闪光";
    public override string Description => "你的闪光弹会让所有敌人闪白3秒！无视距离和遮挡！";
    public override bool IsActive => false; // 被动技能

    // 与其他闪光弹技能互斥
    public override List<string> ExcludedSkills => new() { "AntiFlash", "FlashJump", "KillerFlash" };

    // 闪白持续时间
    private const float FLASH_DURATION = 3.0f;

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[超级闪光] {player.PlayerName} 获得了超级闪光技能");

        // 给予1个闪光弹
        player.GiveNamedItem("weapon_flashbang");

        player.PrintToChat("💥 你获得了超级闪光技能！");
        player.PrintToChat("💡 你的闪光弹会让所有敌人闪白3秒！");
        player.PrintToChat("⚠️ 无视距离和遮挡！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[超级闪光] {player.PlayerName} 失去了超级闪光技能");
    }

    /// <summary>
    /// 监听闪光弹爆炸事件 - 让所有敌人闪白
    /// </summary>
    public void OnFlashbangDetonate(EventFlashbangDetonate @event)
    {
        var attacker = @event.Userid;
        if (attacker == null || !attacker.IsValid)
            return;

        Console.WriteLine($"[超级闪光] {attacker.PlayerName} 的闪光弹爆炸了！");

        // 计数被闪白的敌人数量
        int blindedCount = 0;

        // 让所有敌方玩家被闪白
        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive)
                continue;

            // 不闪自己
            if (player == attacker)
                continue;

            // 只闪敌方玩家
            if (player.Team == attacker.Team)
                continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            // 设置闪白时长
            pawn.FlashDuration = FLASH_DURATION;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_flFlashDuration");

            blindedCount++;

            Console.WriteLine($"[超级闪光] {player.PlayerName} 被闪白 {FLASH_DURATION} 秒");
            player.PrintToCenter($"💥 被超级闪光弹闪到 {FLASH_DURATION} 秒！");
        }

        attacker.PrintToChat($"💥 超级闪光弹！{blindedCount} 个敌人被闪白 {FLASH_DURATION} 秒！");
        Console.WriteLine($"[超级闪光] {attacker.PlayerName} 的闪光弹让 {blindedCount} 个敌人闪白");
    }
}
