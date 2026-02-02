using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 闪光跳跃技能 - 被动技能
/// 跳跃时随机获得移速加成（1.2-3.0倍），拥有闪光弹（投掷后自动补充）
/// </summary>
public class FlashJumpSkill : PlayerSkill
{
    public override string Name => "FlashJump";
    public override string DisplayName => "✈️ 闪光跳跃";
    public override string Description => "跳跃时获得随机移速加成！拥有闪光弹（投掷后自动补充）！";
    public override bool IsActive => false; // 被动技能

    // 移速范围
    private const float MIN_SPEED_MULTIPLIER = 1.2f;
    private const float MAX_SPEED_MULTIPLIER = 3.0f;

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

    // 存储玩家的移速倍数
    private readonly Dictionary<int, float> _playerSpeedMultipliers = new();

    // 存储玩家是否可以跳跃（冷却）
    private readonly Dictionary<int, int> _jumpCooldowns = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[闪光跳跃] {player.PlayerName} 获得了闪光跳跃技能");

        // 随机移速倍数
        float speedMultiplier = (float)(new Random().NextDouble() * (MAX_SPEED_MULTIPLIER - MIN_SPEED_MULTIPLIER) + MIN_SPEED_MULTIPLIER);
        speedMultiplier = (float)Math.Round(speedMultiplier, 2);

        _playerSpeedMultipliers[player.Slot] = speedMultiplier;
        _jumpCooldowns[player.Slot] = 0;

        // 设置移速
        var pawn = player.PlayerPawn.Value;
        if (pawn != null && pawn.IsValid)
        {
            pawn.VelocityModifier = speedMultiplier;
        }

        // 给予3颗闪光弹
        GiveFlashbangs(player, FLASHBANG_COUNT);

        player.PrintToChat("✈️ 你获得了闪光跳跃技能！");
        player.PrintToChat($"💨 跳跃时获得 {speedMultiplier:F2} 倍移速！");
        player.PrintToChat($"💣 获得了 {FLASHBANG_COUNT} 颗闪光弹（投掷后自动补充）！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[闪光跳跃] {player.PlayerName} 失去了闪光跳跃技能");

        // 恢复移速
        var pawn = player.PlayerPawn.Value;
        if (pawn != null && pawn.IsValid && _playerSpeedMultipliers.ContainsKey(player.Slot))
        {
            pawn.VelocityModifier = 1.0f;
        }

        _playerSpeedMultipliers.Remove(player.Slot);
        _jumpCooldowns.Remove(player.Slot);
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

            Console.WriteLine($"[闪光跳跃] 给予 {player.PlayerName} {count} 个闪光弹");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[闪光跳跃] 给予闪光弹时出错: {ex.Message}");
        }
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

                Console.WriteLine($"[闪光跳跃] {player.PlayerName} - {weapon.DesignerName} 武器已{(disableWeapon ? "禁用" : "启用")}");
            }
        }
    }

    /// <summary>
    /// 玩家拾取武器时禁用攻击（参考有毒烟雾弹/鸡模式）
    /// </summary>
    public static void HandleItemPickup(EventItemPickup @event, PlayerSkillManager skillManager)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive) return;

        // 检查是否有闪光跳跃技能
        var skill = skillManager.GetPlayerSkill(player);
        if (skill?.Name != "FlashJump")
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
    /// 处理玩家致盲事件 - 投掷闪光弹后自动补充
    /// </summary>
    public static void HandlePlayerBlind(EventPlayerBlind @event, PlayerSkillManager skillManager)
    {
        var player = @event.Attacker;
        if (player == null || !player.IsValid) return;

        // 检查投掷者是否有闪光跳跃技能
        var skill = skillManager.GetPlayerSkill(player);
        if (skill?.Name != "FlashJump")
            return;

        // 自动补充闪光弹
        Server.NextFrame(() =>
        {
            var flashJumpSkill = (FlashJumpSkill)skill;
            flashJumpSkill.GiveFlashbangs(player, 1);
            player.PrintToChat("✈️ 闪光弹已自动补充！");
        });

        Console.WriteLine($"[闪光跳跃] {player.PlayerName} 投掷闪光弹，自动补充");
    }

    /// <summary>
    /// 每帧更新移速（跳跃时获得加成）
    /// </summary>
    public static void OnTick(PlayerSkillManager skillManager)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid)
                continue;

            var skill = skillManager.GetPlayerSkill(player);
            if (skill?.Name != "FlashJump")
                continue;

            var flashJumpSkill = (FlashJumpSkill)skill;

            var pawn = player.PlayerPawn?.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            // 检查移速倍数
            if (!flashJumpSkill._playerSpeedMultipliers.ContainsKey(player.Slot))
                continue;

            float speedMultiplier = flashJumpSkill._playerSpeedMultipliers[player.Slot];

            // 检查跳跃冷却
            if (flashJumpSkill._jumpCooldowns.TryGetValue(player.Slot, out var cooldown))
            {
                if (cooldown > Server.TickCount)
                {
                    // 冷却中，限制跳跃高度
                    if (!((PlayerFlags)player.Flags).HasFlag(PlayerFlags.FL_ONGROUND))
                    {
                        pawn.AbsVelocity.Z = Math.Min(pawn.AbsVelocity.Z, 10);
                    }
                    continue;
;
                }
            }

            // 检查玩家是否在移动（前进、后退、左、右）
            var buttons = player.Buttons;
            bool isMoving = buttons.HasFlag(PlayerButtons.Moveleft) ||
                          buttons.HasFlag(PlayerButtons.Moveright) ||
                          buttons.HasFlag(PlayerButtons.Forward) ||
                          buttons.HasFlag(PlayerButtons.Back);

            // 移动时应用移速倍数
            if (isMoving)
            {
                pawn.VelocityModifier = speedMultiplier;
            }
            else
            {
                // 静止时恢复基础移速
                pawn.VelocityModifier = 1.0f;
            }
        }
    }

    /// <summary>
    /// 玩家跳跃时设置冷却（20 ticks）
    /// </summary>
    public static void HandlePlayerJump(EventPlayerJump @event, PlayerSkillManager skillManager)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid) return;

        var skill = skillManager.GetPlayerSkill(player);
        if (skill?.Name != "FlashJump")
            return;

        var flashJumpSkill = (FlashJumpSkill)skill;

        // 设置跳跃冷却（当前时间 + 20 ticks）
        flashJumpSkill._jumpCooldowns[player.Slot] = Server.TickCount + 20;
    }
}
