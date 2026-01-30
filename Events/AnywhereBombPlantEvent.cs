using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace HelloWorldPlugin;

/// <summary>
/// 任意下包事件 - 可以在任意位置下包
/// </summary>
public class AnywhereBombPlantEvent : EntertainmentEvent
{
    public override string Name => "AnywhereBombPlant";
    public override string DisplayName => "任意下包";
    public override string Description => "可以在地图任意位置下包！";

    public override void OnApply()
    {
        Console.WriteLine("[任意下包] 事件已激活");
    }

    /// <summary>
    /// 处理玩家按键变化（在主文件的 OnPlayerButtonsChanged 中调用）
    /// </summary>
    public void HandlePlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Get();
        if (pawn == null || !pawn.IsValid)
            return;

        // 检测是否按下 Use 键
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

            // 如果手持 C4，设置为在炸弹区域内
            if (weaponBase.VData.WeaponType == CSWeaponType.WEAPONTYPE_C4)
            {
                pawn.InBombZone = true;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_bInBombZone");

                Console.WriteLine($"[任意下包] 玩家 {player.PlayerName} 按下Use键，已临时设置InBombZone为true");
            }
        }
    }

    /// <summary>
    /// 处理取消下包事件（在主文件的 OnBombAbortPlant 中调用）
    /// 返回 true 表示阻止取消下包
    /// </summary>
    public bool HandleBombAbortPlant(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return false;

        var pawn = player.PlayerPawn.Get();
        if (pawn == null || !pawn.IsValid)
            return false;

        // 如果不在炸弹区域，强制设置为在区域内，阻止取消下包
        if (!pawn.InBombZone)
        {
            pawn.InBombZone = true;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_bInBombZone");

            Console.WriteLine($"[任意下包] 阻止玩家 {player.PlayerName} 的下包被取消");
            return true; // 阻止取消下包
        }

        return false;
    }

    /// <summary>
    /// 持续检查手持 C4 的玩家（在主文件的 OnServerPostEntityThink 中调用）
    /// </summary>
    public void HandleServerPostEntityThink()
    {
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

            // 如果手持 C4，持续设置为在炸弹区域内
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
