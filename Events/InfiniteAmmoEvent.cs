using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;

namespace MyrtleSkill;

/// <summary>
/// 无限弹药事件 - 弹药永不耗尽
/// </summary>
public class InfiniteAmmoEvent : EntertainmentEvent
{
    public override string Name => "InfiniteAmmo";
    public override string DisplayName => "无限弹药";
    public override string Description => "弹药永不耗尽！";

    private CounterStrikeSharp.API.Modules.Timers.Timer? _ammoTimer;

    public override void OnApply()
    {
        Console.WriteLine("[无限弹药] 事件已激活");

        // 启动定时器，定期补充弹药
        if (Plugin != null)
        {
            _ammoTimer = Plugin.AddTimer(0.5f, () =>
            {
                RefillAmmo();
            }, TimerFlags.REPEAT);
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[无限弹药] 事件已结束");

        // 停止定时器
        if (_ammoTimer != null)
        {
            _ammoTimer.Kill();
            _ammoTimer = null;
        }
    }

    private void RefillAmmo()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive) continue;

            var pawn = player.PlayerPawn.Get();
            if (pawn == null || !pawn.IsValid) continue;

            var weaponServices = pawn.WeaponServices;
            if (weaponServices == null) continue;

            // 遍历所有武器
            foreach (var weaponHandle in weaponServices.MyWeapons)
            {
                var weapon = weaponHandle.Get();
                if (weapon == null || !weapon.IsValid) continue;

                var weaponBase = weapon.As<CCSWeaponBase>();
                if (weaponBase == null || weaponBase.VData == null) continue;

                // 跳过刀和C4
                var weaponType = weaponBase.VData.WeaponType;
                if (weaponType == CSWeaponType.WEAPONTYPE_KNIFE ||
                    weaponType == CSWeaponType.WEAPONTYPE_C4)
                    continue;

                // 补充弹药
                if (weaponBase.VData.MaxClip1 > 0)
                {
                    weapon.Clip1 = weaponBase.VData.MaxClip1;
                    weapon.ReserveAmmo[0] = 999;
                }
            }
        }
    }
}
