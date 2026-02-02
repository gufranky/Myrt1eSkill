using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 防闪光技能 - 免疫闪光弹，你的闪光弹持续7秒，获得三颗闪光弹（自动补充）
/// </summary>
public class AntiFlashSkill : PlayerSkill
{
    public override string Name => "AntiFlash";
    public override string DisplayName => "✨ 防闪光";
    public override string Description => "免疫闪光弹！你的闪光弹持续7秒！获得3颗闪光弹（投掷后自动补充）！";
    public override bool IsActive => false; // 被动技能

    // 闪光弹持续时间（秒）
    private const float FLASH_DURATION = 7.0f;

    // 给予的闪光弹数量
    private const int FLASHBANG_COUNT = 3;

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[防闪光] {player.PlayerName} 获得了防闪光技能");

        // 给予3颗闪光弹
        GiveFlashbangs(player, FLASHBANG_COUNT);

        player.PrintToChat("✨ 你获得了防闪光技能！");
        player.PrintToChat($"💡 免疫所有闪光弹！");
        player.PrintToChat($"💣 你的闪光弹持续 {FLASH_DURATION} 秒！");
        player.PrintToChat($"💣 获得了 {FLASHBANG_COUNT} 颗闪光弹（投掷后自动补充）！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        Console.WriteLine($"[防闪光] {player.PlayerName} 失去了防闪光技能");
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
            // 给予闪光弹
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
    /// 处理玩家致盲事件
    /// </summary>
    public static void HandlePlayerBlind(EventPlayerBlind @event, PlayerSkillManager skillManager)
    {
        var player = @event.Userid;
        var attacker = @event.Attacker;

        if (player == null || !player.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            return;

        if (attacker == null || !attacker.IsValid)
            return;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid)
            return;

        // 检查被闪的玩家是否有防闪光技能
        var playerSkill = skillManager.GetPlayerSkill(player);
        if (playerSkill?.Name == "AntiFlash")
        {
            // 免疫闪光 - 设置致盲时间为0
            playerPawn.FlashDuration = 0.0f;
            Console.WriteLine($"[防闪光] {player.PlayerName} 免疫了闪光弹");

            // 显示提示
            player.PrintToChat("✨ 你免疫了闪光弹！");
        }

        // 检查投掷者是否有防闪光技能
        var attackerSkill = skillManager.GetPlayerSkill(attacker);
        if (attackerSkill?.Name == "AntiFlash")
        {
            // 如果是自己投掷的，只补充闪光弹，不增强
            if (player == attacker)
            {
                // 自动补充闪光弹（检查数量上限）
                Server.NextFrame(() =>
                {
                    if (attacker.IsValid && attacker.PawnIsAlive && attackerSkill is AntiFlashSkill flashSkill)
                    {
                        int currentCount = flashSkill.GetFlashbangCount(attacker);

                        if (currentCount < FLASHBANG_COUNT)
                        {
                            flashSkill.GiveFlashbangs(attacker, 1);
                            attacker.PrintToChat($"✨ 闪光弹已补充！({currentCount + 1}/{FLASHBANG_COUNT})");
                        }
                        else
                        {
                            Console.WriteLine($"[防闪光] {attacker.PlayerName} 闪光弹已满 ({currentCount}/{FLASHBANG_COUNT})，不补充");
                        }
                    }
                });
            }
            else
            {
                // 是别人投掷的，增强闪光并补充
                playerPawn.FlashDuration = FLASH_DURATION;
                Console.WriteLine($"[防闪光] {attacker.PlayerName} 的强力闪光弹致盲了 {player.PlayerName}，持续时间 {FLASH_DURATION} 秒");

                // 自动补充闪光弹（检查数量上限）
                Server.NextFrame(() =>
                {
                    if (attacker.IsValid && attacker.PawnIsAlive && attackerSkill is AntiFlashSkill flashSkill)
                    {
                        int currentCount = flashSkill.GetFlashbangCount(attacker);

                        if (currentCount < FLASHBANG_COUNT)
                        {
                            flashSkill.GiveFlashbangs(attacker, 1);
                            attacker.PrintToChat($"✨ 闪光弹已补充！({currentCount + 1}/{FLASHBANG_COUNT})");
                        }
                        else
                        {
                            Console.WriteLine($"[防闪光] {attacker.PlayerName} 闪光弹已满 ({currentCount}/{FLASHBANG_COUNT})，不补充");
                        }
                    }
                });
            }
        }
    }

    /// <summary>
    /// 获取玩家当前闪光弹数量
    /// </summary>
    private int GetFlashbangCount(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return 0;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.WeaponServices == null)
            return 0;

        int count = 0;
        foreach (var weapon in pawn.WeaponServices.MyWeapons)
        {
            if (weapon != null && weapon.IsValid)
            {
                var weaponEntity = weapon.Value;
                if (weaponEntity != null && weaponEntity.IsValid &&
                    weaponEntity.DesignerName == "weapon_flashbang")
                {
                    count++;
                }
            }
        }

        return count;
    }
}
