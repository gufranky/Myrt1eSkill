using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Timers;

namespace MyrtleSkill;

/// <summary>
/// 更致命的手雷事件 - 无限高爆手雷、手雷伤害和范围增加
/// 采用不删除武器的安全方案，避免 PVS 崩溃
/// </summary>
public class DeadlyGrenadesEvent : EntertainmentEvent
{
    public override string Name => "DeadlyGrenades";
    public override string DisplayName => "💣 更致命的手雷";
    public override string Description => "无限高爆手雷！手雷伤害和范围大幅增加！";

    // 标志：事件是否激活
    private bool _isActive = false;

    private ConVar? _buyAllowGunsConVar;
    private ConVar? _heDamageConVar;
    private ConVar? _heRadiusConVar;
    private ConVar? _infiniteAmmoConVar;
    private int _originalBuyAllowGuns = 1;
    private float _originalHeDamage = 1.0f;
    private float _originalHeRadius = 1.0f;
    private int _originalInfiniteAmmo = 0;

    public override void OnApply()
    {
        Console.WriteLine("[更致命的手雷] 事件已激活");

        // 设置激活标志
        _isActive = true;

        // 1. 禁用商店
        _buyAllowGunsConVar = ConVar.Find("mp_buy_allow_guns");
        if (_buyAllowGunsConVar != null)
        {
            _originalBuyAllowGuns = _buyAllowGunsConVar.GetPrimitiveValue<int>();
            _buyAllowGunsConVar.SetValue(0);
            Console.WriteLine($"[更致命的手雷] mp_buy_allow_guns 已设置为 0 (原值: {_originalBuyAllowGuns})");
        }

        // 2. 增加手雷伤害和范围
        _heDamageConVar = ConVar.Find("sv_hegrenade_damage_multiplier");
        if (_heDamageConVar != null)
        {
            _originalHeDamage = _heDamageConVar.GetPrimitiveValue<float>();
            _heDamageConVar.SetValue(3.0f); // 3倍伤害
            Console.WriteLine($"[更致命的手雷] sv_hegrenade_damage_multiplier 已设置为 3.0 (原值: {_originalHeDamage})");
        }

        _heRadiusConVar = ConVar.Find("sv_hegrenade_radius_multiplier");
        if (_heRadiusConVar != null)
        {
            _originalHeRadius = _heRadiusConVar.GetPrimitiveValue<float>();
            _heRadiusConVar.SetValue(5.0f); // 5倍范围
            Console.WriteLine($"[更致命的手雷] sv_hegrenade_radius_multiplier 已设置为 5.0 (原值: {_originalHeRadius})");
        }

        // 3. 启用无限弹药（包括手雷）
        _infiniteAmmoConVar = ConVar.Find("sv_infinite_ammo");
        if (_infiniteAmmoConVar != null)
        {
            _originalInfiniteAmmo = _infiniteAmmoConVar.GetPrimitiveValue<int>();
            _infiniteAmmoConVar.SetValue(1);
            Console.WriteLine($"[更致命的手雷] sv_infinite_ammo 已设置为 1 (原值: {_originalInfiniteAmmo})");
        }

        // 4. 给予所有玩家手雷并移除主副武器
        GiveGrenadesToAllPlayers();

        // 5. 注册事件监听
        if (Plugin != null)
        {
            Plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
            Plugin.RegisterEventHandler<EventWeaponFire>(OnWeaponFire, HookMode.Post);
        }

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToCenter("💣 更致命的手雷！\n无限高爆手雷 + 3倍伤害 + 5倍范围！");
                player.PrintToChat("💣 更致命的手雷模式已启用！");
                player.PrintToChat("🚫 商店已禁用！主副武器已移除！");
                player.PrintToChat("💡 投掷手雷会自动补充！");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[更致命的手雷] 事件已恢复");

        // 首先取消激活标志
        _isActive = false;

        // 移除事件监听
        if (Plugin != null)
        {
            Plugin.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
            Plugin.DeregisterEventHandler<EventWeaponFire>(OnWeaponFire, HookMode.Post);
        }

        // 恢复商店
        if (_buyAllowGunsConVar != null)
        {
            _buyAllowGunsConVar.SetValue(_originalBuyAllowGuns);
            Console.WriteLine($"[更致命的手雷] mp_buy_allow_guns 已恢复为 {_originalBuyAllowGuns}");
        }

        // 恢复手雷伤害和范围
        if (_heDamageConVar != null)
        {
            _heDamageConVar.SetValue(_originalHeDamage);
            Console.WriteLine($"[更致命的手雷] sv_hegrenade_damage_multiplier 已恢复为 {_originalHeDamage}");
        }

        if (_heRadiusConVar != null)
        {
            _heRadiusConVar.SetValue(_originalHeRadius);
            Console.WriteLine($"[更致命的手雷] sv_hegrenade_radius_multiplier 已恢复为 {_originalHeRadius}");
        }

        // 恢复无限弹药
        if (_infiniteAmmoConVar != null)
        {
            _infiniteAmmoConVar.SetValue(_originalInfiniteAmmo);
            Console.WriteLine($"[更致命的手雷] sv_infinite_ammo 已恢复为 {_originalInfiniteAmmo}");
        }

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("💣 更致命的手雷模式已禁用");
            }
        }
    }

    /// <summary>
    /// 给予所有玩家手雷并让他们丢弃主副武器
    /// </summary>
    private void GiveGrenadesToAllPlayers()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            try
            {
                if (!player.IsValid || !player.PawnIsAlive) continue;

                // 先让玩家丢弃主副武器
                RemovePrimaryAndSecondaryWeapons(player);

                // 给予3颗手雷
                for (int i = 0; i < 3; i++)
                {
                    player.GiveNamedItem("weapon_hegrenade");
                }

                Console.WriteLine($"[更致命的手雷] {player.PlayerName} 已丢弃主副武器并给予3颗高爆手雷");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[更致命的手雷] 处理玩家时出错: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 移除玩家的主武器和副武器（直接删除）
    /// </summary>
    private void RemovePrimaryAndSecondaryWeapons(CCSPlayerController player)
    {
        if (player == null || !player.IsValid) return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return;

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null) return;

        // 直接移除主副武器（不使用延迟，参考裁军技能）
        foreach (var weaponHandle in weaponServices.MyWeapons)
        {
            if (!weaponHandle.IsValid) continue;

            var weapon = weaponHandle.Get();
            if (weapon == null || !weapon.IsValid) continue;

            var weaponBase = weapon.As<CCSWeaponBase>();
            if (weaponBase == null || weaponBase.VData == null) continue;

            var weaponType = weaponBase.VData.WeaponType;

            // 只移除主武器和副武器
            if (weaponType == CSWeaponType.WEAPONTYPE_PISTOL ||
                weaponType == CSWeaponType.WEAPONTYPE_SUBMACHINEGUN ||
                weaponType == CSWeaponType.WEAPONTYPE_RIFLE ||
                weaponType == CSWeaponType.WEAPONTYPE_SHOTGUN ||
                weaponType == CSWeaponType.WEAPONTYPE_SNIPER_RIFLE ||
                weaponType == CSWeaponType.WEAPONTYPE_MACHINEGUN)
            {
                weapon.Remove();
                Console.WriteLine($"[更致命的手雷] 移除了 {player.PlayerName} 的 {weaponBase.DesignerName}");
            }
        }
    }

    /// <summary>
    /// 玩家生成时给予手雷并移除主副武器
    /// </summary>
    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        // 如果事件不激活，不处理
        if (!_isActive) return HookResult.Continue;

        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        // 延迟处理，等待玩家完全生成
        Plugin?.AddTimer(0.5f, () =>
        {
            if (_isActive && player.IsValid && player.PawnIsAlive)
            {
                try
                {
                    // 移除主副武器
                    RemovePrimaryAndSecondaryWeapons(player);

                    // 给予3颗手雷
                    for (int i = 0; i < 3; i++)
                    {
                        player.GiveNamedItem("weapon_hegrenade");
                    }

                    player.PrintToCenter("💣 更致命的手雷！");
                    Console.WriteLine($"[更致命的手雷] {player.PlayerName} 生成，已移除主副武器并给予手雷");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[更致命的手雷] 处理玩家生成时出错: {ex.Message}");
                }
            }
        });

        return HookResult.Continue;
    }

    /// <summary>
    /// 处理武器投掷事件 - 自动补充手雷
    /// </summary>
    private HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        // 如果事件不激活，不处理
        if (!_isActive) return HookResult.Continue;

        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return HookResult.Continue;

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null) return HookResult.Continue;

        var activeWeapon = weaponServices.ActiveWeapon.Value;
        if (activeWeapon == null || !activeWeapon.IsValid) return HookResult.Continue;

        // 检查是否是手雷
        var weaponBase = activeWeapon.As<CCSWeaponBase>();
        if (weaponBase == null || weaponBase.VData == null) return HookResult.Continue;

        string weaponName = weaponBase.DesignerName.ToLower();
        if (!weaponName.Contains("hegrenade"))
            return HookResult.Continue;

        // 延迟补充手雷（等待投掷动画完成）
        Plugin?.AddTimer(0.3f, () =>
        {
            if (_isActive && player.IsValid && player.PawnIsAlive)
            {
                try
                {
                    // 给予1颗新手雷
                    player.GiveNamedItem("weapon_hegrenade");
                    Console.WriteLine($"[更致命的手雷] 自动补充了 {player.PlayerName} 的手雷");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[更致命的手雷] 补充手雷时出错: {ex.Message}");
                }
            }
        });

        return HookResult.Continue;
    }
}
