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

    // 禁用的武器列表（除了闪光弹和刀）
    private static readonly string[] DisabledWeapons =
    [
        "weapon_ak47", "weapon_m4a4", "weapon_m4a1", "weapon_m4a1_silencer",
        "weapon_famas", "weapon_galilar", "weapon_aug", "weapon_sg553",
        "weapon_mp9", "weapon_mac10", "weapon_bizon", "weapon_mp7",
        "weapon_ump45", "weapon_p90", "weapon_mp5sd", "weapon_ssg08",
        "weapon_awp", "weapon_scar20", "weapon_g3sg1", "weapon_nova",
        "weapon_xm1014", "weapon_mag7", "weapon_sawedoff", "weapon_m249",
        "weapon_negev", "weapon_deagle", "weapon_fiveseven", "weapon_glock",
        "weapon_p250", "weapon_p2000", "weapon_usp_silencer", "weapon_hkp2000",
        "weapon_elite", "weapon_fiveseven", "weapon_tec9", "weapon_mp9",
        "weapon_mac10", "weapon_bizon", "weapon_tec9", "weapon_mp7",
        "weapon_scout", "weapon_mp5sd", "weapon_ump45", "weapon_p90",
        "weapon_galilar", "weapon_famas", "weapon_aug", "weapon_sg553"
    ];

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
    /// 玩家拾取武器时禁用攻击（参考有毒烟雾弹/鸡模式）
    /// </summary>
    public static void HandleItemPickup(EventItemPickup @event, PlayerSkillManager skillManager)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive) return;

        // 检查是否有防闪光技能
        var skill = skillManager.GetPlayerSkill(player);
        if (skill?.Name != "AntiFlash")
            return;

        // 延迟一帧禁用武器（确保武器已添加到背包）
        Server.NextFrame(() =>
        {
            if (player.IsValid && player.PawnIsAlive)
            {
                SetWeaponAttack(player, true);
            }
        });
    }

    /// <summary>
    /// 设置武器攻击状态
    /// </summary>
    private static void SetWeaponAttack(CCSPlayerController player, bool disableWeapon)
    {
        if (player == null || !player.IsValid) return;
        var pawn = player.PlayerPawn?.Value;
        if (pawn == null || !pawn.IsValid || pawn.WeaponServices == null || pawn.WeaponServices.MyWeapons == null) return;

        foreach (var weaponHandle in pawn.WeaponServices.MyWeapons)
        {
            if (weaponHandle.Value == null || !weaponHandle.Value.IsValid) continue;

            var weapon = weaponHandle.Value;
            if (DisabledWeapons.Contains(weapon.DesignerName))
            {
                weapon.NextPrimaryAttackTick = disableWeapon ? int.MaxValue : Server.TickCount;
                weapon.NextSecondaryAttackTick = disableWeapon ? int.MaxValue : Server.TickCount;

                Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
                Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextSecondaryAttackTick");

                Console.WriteLine($"[防闪光] {player.PlayerName} - {weapon.DesignerName} 武器已{(disableWeapon ? "禁用" : "启用")}");
            }
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
            Console.WriteLine($"[防闪光] {player.PlayerName} 兿疫了闪光弹");

            // 显示提示
            player.PrintToChat("✨ 你免疫了闪光弹！");
        }

        // 检查投掷者是否有防闪光技能
        var attackerSkill = skillManager.GetPlayerSkill(attacker);
        if (attackerSkill?.Name == "AntiFlash")
        {
            // 增强闪光 - 设置致盲时间为7秒
            playerPawn.FlashDuration = FLASH_DURATION;
            Console.WriteLine($"[防闪光] {attacker.PlayerName} 的强力闪光弹致盲了 {player.PlayerName}，持续时间 {FLASH_DURATION} 秒");

            // 自动补充闪光弹
            Server.NextFrame(() =>
            {
                var flashSkill = (AntiFlashSkill)attackerSkill;
                flashSkill.GiveFlashbangs(attacker, 1);
                attacker.PrintToChat("✨ 闪光弹已自动补充！");
            });
        }
    }
}
