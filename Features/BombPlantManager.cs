using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Features;

/// <summary>
/// 炸弹相关功能管理器
/// 负责任意下包和炸弹计时器功能
/// </summary>
public class BombPlantManager
{
    public bool AllowAnywherePlant { get; set; } = false;
    public float BombTimer { get; set; } = 65.0f;

    /// <summary>
    /// 处理玩家按键变化（用于任意下包）
    /// </summary>
    public void HandlePlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed)
    {
        if (!AllowAnywherePlant)
            return;

        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Get();
        if (pawn == null || !pawn.IsValid)
            return;

        if ((pressed & PlayerButtons.Use) != 0)
        {
            var weaponServices = pawn.WeaponServices;
            if (weaponServices == null)
                return;

            var activeWeapon = weaponServices.ActiveWeapon.Get();
            if (activeWeapon == null || !activeWeapon.IsValid)
                return;

            var weaponBase = activeWeapon.As<CCSWeaponBase>();
            if (weaponBase == null || weaponBase.VData == null)
                return;

            if (weaponBase.VData.WeaponType == CSWeaponType.WEAPONTYPE_C4)
            {
                pawn.InBombZone = true;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_bInBombZone");

                Console.WriteLine("[任意下包] 玩家 " + player.PlayerName + " 按下Use键，已临时设置InBombZone为true");
            }
        }
    }

    /// <summary>
    /// 处理取消下包事件
    /// 返回 true 表示阻止取消下包
    /// </summary>
    public bool HandleBombAbortPlant(CCSPlayerController player)
    {
        if (!AllowAnywherePlant)
            return false;

        if (player == null || !player.IsValid)
            return false;

        var pawn = player.PlayerPawn.Get();
        if (pawn == null || !pawn.IsValid)
            return false;

        if (!pawn.InBombZone)
        {
            pawn.InBombZone = true;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_bInBombZone");

            Console.WriteLine("[任意下包] 阻止玩家 " + player.PlayerName + " 的下包被取消");
            return true; // 阻止取消下包
        }

        return false;
    }

    /// <summary>
    /// 持续检查手持 C4 的玩家
    /// </summary>
    public void HandleServerPostEntityThink()
    {
        if (!AllowAnywherePlant)
            return;

        var players = Utilities.GetPlayers();
        foreach (var player in players)
        {
            if (player == null || !player.IsValid)
                continue;

            var pawn = player.PlayerPawn.Get();
            if (pawn == null || !pawn.IsValid)
                continue;

            var weaponServices = pawn.WeaponServices;
            if (weaponServices == null)
                continue;

            var activeWeapon = weaponServices.ActiveWeapon.Get();
            if (activeWeapon == null || !activeWeapon.IsValid)
                continue;

            var weaponBase = activeWeapon.As<CCSWeaponBase>();
            if (weaponBase == null || weaponBase.VData == null)
                continue;

            if (weaponBase.VData.WeaponType == CSWeaponType.WEAPONTYPE_C4)
            {
                if (!pawn.InBombZone)
                {
                    pawn.InBombZone = true;
                    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_bInBombZone");
                }
            }
        }
    }
}
