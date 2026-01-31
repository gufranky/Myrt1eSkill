using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Features;

/// <summary>
/// 重甲战士管理器
/// 负责管理重甲战士的选择、属性设置和武器限制
/// </summary>
public class HeavyArmorManager
{
    private readonly MyrtleSkill _plugin;
    private readonly Random _random = new();
    private CCSPlayerController? _currentHeavyArmorPlayer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _weaponCheckTimer;

    public bool IsEnabled { get; set; } = true;
    public CCSPlayerController? CurrentPlayer => _currentHeavyArmorPlayer;

    public HeavyArmorManager(MyrtleSkill plugin)
    {
        _plugin = plugin;
    }

    /// <summary>
    /// 回合开始时选择并设置重甲战士
    /// </summary>
    public void OnRoundStart()
    {
        if (!IsEnabled)
        {
            Console.WriteLine("[重甲战士] 功能已禁用，跳过本回合");
            return;
        }

        // 恢复上一个重甲战士的速度
        if (_currentHeavyArmorPlayer != null && _currentHeavyArmorPlayer.IsValid)
        {
            var oldPawn = _currentHeavyArmorPlayer.PlayerPawn.Get();
            if (oldPawn != null && oldPawn.IsValid)
            {
                SetPlayerSpeed(oldPawn, 1.0f);
            }
        }

        // 选择新的重甲战士
        var luckyPlayer = SelectRandomPlayer();
        if (luckyPlayer != null)
        {
            _currentHeavyArmorPlayer = luckyPlayer;

            luckyPlayer.GiveNamedItem("item_assaultsuit");

            var pawn = luckyPlayer.PlayerPawn.Get();
            if (pawn != null && pawn.IsValid)
            {
                pawn.ArmorValue = 200;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
                SetPlayerSpeed(pawn, 0.6f);
                RestrictToSecondaryWeapons(luckyPlayer);

                luckyPlayer.PrintToChat(" 🛡️ 你被选中为重甲战士！");
                luckyPlayer.PrintToChat(" ⚡ 护甲值: 200 | 速度: 60% | 伤害抗性: +60% | 武器限制: 仅副武器和道具");
                luckyPlayer.PrintToCenter(" 🛡️ 重甲战士模式已激活！");
            }

            StartWeaponCheckTimer();

            foreach (var p in Utilities.GetPlayers())
            {
                if (p.IsValid)
                {
                    p.PrintToChat("🎲 幸运玩家：" + luckyPlayer.PlayerName + " 获得了重甲战士效果！");
                }
            }
        }
    }

    /// <summary>
    /// 回合结束时清理
    /// </summary>
    public void OnRoundEnd()
    {
        if (_currentHeavyArmorPlayer != null && _currentHeavyArmorPlayer.IsValid)
        {
            var pawn = _currentHeavyArmorPlayer.PlayerPawn.Get();
            if (pawn != null && pawn.IsValid)
            {
                SetPlayerSpeed(pawn, 1.0f);
            }
            _currentHeavyArmorPlayer = null;
        }

        StopWeaponCheckTimer();
    }

    /// <summary>
    /// 处理重甲战士受到伤害（减伤）
    /// 返回伤害倍数，由调用方统一应用
    /// </summary>
    public float? HandleDamage(CCSPlayerPawn player, CTakeDamageInfo info)
    {
        var controller = player.Controller.Value;
        if (controller == null || !controller.IsValid)
            return null;

        if (controller != _currentHeavyArmorPlayer)
            return null;

        const float damageReduction = 0.6f;
        float multiplier = 1.0f - damageReduction; // 0.4倍伤害

        Console.WriteLine("[减伤] 玩家: " + controller.PlayerName + " | 伤害倍数: " + multiplier + " (减免" + damageReduction * 100 + "%)");

        return multiplier;
    }

    /// <summary>
    /// 处理武器切换事件
    /// </summary>
    public bool HandleWeaponSelection(CCSPlayerController player, CBasePlayerWeapon? selectedWeapon)
    {
        if (player != _currentHeavyArmorPlayer)
            return false;

        if (selectedWeapon != null && selectedWeapon.IsValid)
        {
            var weaponBase = selectedWeapon.As<CCSWeaponBase>();
            if (weaponBase != null && weaponBase.VData != null)
            {
                var weaponType = weaponBase.VData.WeaponType;
                // 重甲战士可以使用：副武器、刀具、C4、手雷（道具）
                if (weaponType != CSWeaponType.WEAPONTYPE_PISTOL &&
                    weaponType != CSWeaponType.WEAPONTYPE_KNIFE &&
                    weaponType != CSWeaponType.WEAPONTYPE_C4 &&
                    weaponType != CSWeaponType.WEAPONTYPE_GRENADE &&
                    weaponType != CSWeaponType.WEAPONTYPE_TASER)
                {
                    player.PrintToChat(" 🚫 重甲战士只能使用副武器和道具！");
                    Console.WriteLine("[重甲战士] 阻止玩家 " + player.PlayerName + " 使用非副武器/道具 (类型: " + weaponType + ")");
                    ForceSecondaryWeapon(player);
                    return true; // 阻止切换
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 处理拾取武器事件
    /// </summary>
    public bool HandleItemPickup(CCSPlayerController player, string itemName)
    {
        if (player != _currentHeavyArmorPlayer)
            return false;

        if (IsPrimaryWeapon(itemName))
        {
            player.PrintToChat(" 🚫 重甲战士无法拾取主武器！");
            Console.WriteLine("[重甲战士] 阻止玩家 " + player.PlayerName + " 拾取主武器: " + itemName);

            ClearPrimaryWeapons(player);

            return true; // 阻止拾取
        }

        return false;
    }

    #region 私有方法

    private CCSPlayerController? SelectRandomPlayer()
    {
        var players = Utilities.GetPlayers();
        if (players.Count == 0)
            return null;

        var validPlayers = players.Where(p => p.IsValid && p.PlayerPawn.IsValid).ToList();
        if (validPlayers.Count == 0)
            return null;

        return validPlayers[_random.Next(validPlayers.Count)];
    }

    private void SetPlayerSpeed(CCSPlayerPawn pawn, float multiplier)
    {
        pawn.VelocityModifier = multiplier;

        var movementServices = pawn.MovementServices;
        if (movementServices != null)
        {
            movementServices.Maxspeed = multiplier * 240.0f;
        }

        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");

        Console.WriteLine("[速度设置] VelocityModifier=" + pawn.VelocityModifier + ", Maxspeed=" + (movementServices?.Maxspeed ?? 0));
    }

    private void RestrictToSecondaryWeapons(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Get();
        if (pawn == null || !pawn.IsValid)
            return;

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null)
            return;

        foreach (var weaponHandle in weaponServices.MyWeapons)
        {
            var weapon = weaponHandle.Get();
            if (weapon != null && weapon.IsValid)
            {
                var weaponBase = weapon.As<CCSWeaponBase>();
                if (weaponBase != null && weaponBase.VData != null)
                {
                    var weaponType = weaponBase.VData.WeaponType;
                    // 保留：副武器、刀具、C4、手雷、电击枪
                    if (weaponType != CSWeaponType.WEAPONTYPE_PISTOL &&
                        weaponType != CSWeaponType.WEAPONTYPE_KNIFE &&
                        weaponType != CSWeaponType.WEAPONTYPE_C4 &&
                        weaponType != CSWeaponType.WEAPONTYPE_GRENADE &&
                        weaponType != CSWeaponType.WEAPONTYPE_TASER)
                    {
                        weapon.Remove();
                        Console.WriteLine("[重甲战士] 已移除玩家 " + player.PlayerName + " 的武器: (类型: " + weaponType + ")");
                    }
                }
            }
        }

        EnsurePlayerHasSecondaryWeapon(player);
        ForceSecondaryWeapon(player);
    }

    private void ForceSecondaryWeapon(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Get();
        if (pawn == null || !pawn.IsValid)
            return;

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null)
            return;

        CBasePlayerWeapon? secondaryWeapon = null;
        foreach (var weaponHandle in weaponServices.MyWeapons)
        {
            var weapon = weaponHandle.Get();
            if (weapon != null && weapon.IsValid)
            {
                var weaponBase = weapon.As<CCSWeaponBase>();
                if (weaponBase != null && weaponBase.VData != null &&
                    weaponBase.VData.WeaponType == CSWeaponType.WEAPONTYPE_PISTOL)
                {
                    secondaryWeapon = weapon;
                    break;
                }
            }
        }

        if (secondaryWeapon == null)
        {
            foreach (var weaponHandle in weaponServices.MyWeapons)
            {
                var weapon = weaponHandle.Get();
                if (weapon != null && weapon.IsValid)
                {
                    var weaponBase = weapon.As<CCSWeaponBase>();
                    if (weaponBase != null && weaponBase.VData != null &&
                        weaponBase.VData.WeaponType == CSWeaponType.WEAPONTYPE_KNIFE)
                    {
                        secondaryWeapon = weapon;
                        break;
                    }
                }
            }
        }

        if (secondaryWeapon != null && secondaryWeapon.IsValid)
        {
            var activeWeapon = weaponServices.ActiveWeapon.Get();
            if (activeWeapon == null || !activeWeapon.IsValid || activeWeapon.Index != secondaryWeapon.Index)
            {
                player.ExecuteClientCommand("slot2");
                Console.WriteLine("[重甲战士] 已强制玩家 " + player.PlayerName + " 使用副武器");
            }
        }
    }

    private void EnsurePlayerHasSecondaryWeapon(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Get();
        if (pawn == null || !pawn.IsValid)
            return;

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null)
            return;

        bool hasSecondaryWeapon = false;
        foreach (var weaponHandle in weaponServices.MyWeapons)
        {
            var weapon = weaponHandle.Get();
            if (weapon != null && weapon.IsValid)
            {
                var weaponBase = weapon.As<CCSWeaponBase>();
                if (weaponBase != null && weaponBase.VData != null &&
                    weaponBase.VData.WeaponType == CSWeaponType.WEAPONTYPE_PISTOL)
                {
                    hasSecondaryWeapon = true;
                    break;
                }
            }
        }

        if (!hasSecondaryWeapon)
        {
            string[] secondaryWeapons = { "weapon_p2000", "weapon_glock", "weapon_usp_silencer", "weapon_p250" };
            string randomWeapon = secondaryWeapons[_random.Next(secondaryWeapons.Length)];
            player.GiveNamedItem(randomWeapon);
            Console.WriteLine("[重甲战士] 已给予玩家 " + player.PlayerName + " 副武器: " + randomWeapon);
        }
    }

    private void ClearPrimaryWeapons(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Get();
        if (pawn == null || !pawn.IsValid)
            return;

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null)
            return;

        foreach (var weaponHandle in weaponServices.MyWeapons)
        {
            var weapon = weaponHandle.Get();
            if (weapon != null && weapon.IsValid)
            {
                var weaponBase = weapon.As<CCSWeaponBase>();
                if (weaponBase != null && weaponBase.VData != null)
                {
                    var weaponType = weaponBase.VData.WeaponType;
                    if (weaponType != CSWeaponType.WEAPONTYPE_PISTOL &&
                        weaponType != CSWeaponType.WEAPONTYPE_KNIFE &&
                        weaponType != CSWeaponType.WEAPONTYPE_C4 &&
                        weaponType != CSWeaponType.WEAPONTYPE_GRENADE &&
                        weaponType != CSWeaponType.WEAPONTYPE_TASER)
                    {
                        weapon.Remove();
                        Console.WriteLine("[重甲战士] 已移除玩家 " + player.PlayerName + " 的主武器: (类型: " + weaponType + ")");
                    }
                }
            }
        }
    }

    private bool IsPrimaryWeapon(string itemName)
    {
        string[] primaryWeapons =
        {
            "weapon_ak47", "weapon_m4a1", "weapon_m4a1_silencer", "weapon_aug", "weapon_sg556",
            "weapon_famas", "weapon_galilar", "weapon_awp", "weapon_ssg08",
            "weapon_g3sg1", "weapon_scar20", "weapon_m249",
            "weapon_mac10", "weapon_mp5sd", "weapon_mp7", "weapon_mp9", "weapon_p90",
            "weapon_ump45", "weapon_bizon", "weapon_mp5sd",
            "weapon_mag7", "weapon_nova", "weapon_sawedoff", "weapon_xm1014",
            "weapon_ssg08", "weapon_awp", "weapon_g3sg1", "weapon_scar20",
            "weapon_negev", "weapon_m249"
        };

        return primaryWeapons.Contains(itemName.ToLower());
    }

    private void OnPlayerStateChanged()
    {
        if (_currentHeavyArmorPlayer == null || !_currentHeavyArmorPlayer.IsValid)
            return;

        var pawn = _currentHeavyArmorPlayer.PlayerPawn.Get();
        if (pawn == null || !pawn.IsValid)
            return;

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null)
            return;

        var activeWeapon = weaponServices.ActiveWeapon.Get();
        if (activeWeapon != null && activeWeapon.IsValid)
        {
            var weaponBase = activeWeapon.As<CCSWeaponBase>();
            if (weaponBase != null && weaponBase.VData != null)
            {
                var weaponType = weaponBase.VData.WeaponType;
                // 重甲战士可以使用：副武器、刀具、C4、手雷（道具）
                if (weaponType != CSWeaponType.WEAPONTYPE_PISTOL &&
                    weaponType != CSWeaponType.WEAPONTYPE_KNIFE &&
                    weaponType != CSWeaponType.WEAPONTYPE_C4 &&
                    weaponType != CSWeaponType.WEAPONTYPE_GRENADE &&
                    weaponType != CSWeaponType.WEAPONTYPE_TASER)
                {
                    ForceSecondaryWeapon(_currentHeavyArmorPlayer);
                }
            }
        }
    }

    private void StartWeaponCheckTimer()
    {
        if (_weaponCheckTimer != null)
            return;

        _weaponCheckTimer = _plugin.AddTimer(0.5f, () =>
        {
            OnPlayerStateChanged();
        }, TimerFlags.REPEAT);
    }

    private void StopWeaponCheckTimer()
    {
        if (_weaponCheckTimer != null)
        {
            _weaponCheckTimer.Kill();
            _weaponCheckTimer = null;
        }
    }

    #endregion
}
