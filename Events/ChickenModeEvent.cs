using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;

namespace MyrtleSkill;

/// <summary>
/// 我是小鸡事件 - 所有玩家变成鸡，移速1.1倍，血量50
/// </summary>
public class ChickenModeEvent : EntertainmentEvent
{
    public override string Name => "ChickenMode";
    public override string DisplayName => "🐔 我是小鸡";
    public override string Description => "所有玩家都变成了小鸡！移速1.1倍，血量50%！禁用大部分武器！";

    private const float ChickenSpeedMultiplier = 1.1f;
    private const int ChickenHealth = 50;

    // 禁用的武器列表
    private static readonly string[] DisabledWeapons =
    [
        "weapon_ak47", "weapon_m4a4", "weapon_m4a1", "weapon_m4a1_silencer",
        "weapon_famas", "weapon_galilar", "weapon_aug", "weapon_sg553",
        "weapon_mp9", "weapon_mac10", "weapon_bizon", "weapon_mp7",
        "weapon_ump45", "weapon_p90", "weapon_mp5sd", "weapon_ssg08",
        "weapon_awp", "weapon_scar20", "weapon_g3sg1", "weapon_nova",
        "weapon_xm1014", "weapon_mag7", "weapon_sawedoff", "weapon_m249",
        "weapon_negev"
    ];

    // 存储玩家与鸡模型的映射
    private readonly Dictionary<int, CBaseModelEntity> _chickens = new();

    // 存储玩家原始属性
    private readonly Dictionary<int, float> _originalSpeed = new();
    private readonly Dictionary<int, int> _originalHealth = new();
    private readonly Dictionary<int, Color> _originalRender = new();
    private readonly Dictionary<int, float> _originalScale = new();

    /// <summary>
    /// 修改玩家缩放（通过 CBodyComponent 修改）
    /// </summary>
    private static void SetPlayerScale(CCSPlayerPawn pawn, float scale)
    {
        if (pawn == null || !pawn.IsValid || pawn.CBodyComponent == null || pawn.CBodyComponent.SceneNode == null)
            return;

        var skeletonInstance = pawn.CBodyComponent.SceneNode.GetSkeletonInstance();
        if (skeletonInstance != null)
        {
            skeletonInstance.Scale = scale;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");
            Server.NextFrame(() => pawn.AcceptInput("SetScale", pawn, pawn, scale.ToString()));
        }
    }

    public override void OnApply()
    {
        Console.WriteLine("[我是小鸡] 事件已激活");

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive) continue;

            ApplyChickenEffects(player);
        }

        // 注册监听器
        if (Plugin != null)
        {
            Plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
            Plugin.RegisterEventHandler<EventItemPickup>(OnItemPickup, HookMode.Post);
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[我是小鸡] 事件已恢复");

        // 移除监听器
        if (Plugin != null)
        {
            Plugin.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
            Plugin.DeregisterEventHandler<EventItemPickup>(OnItemPickup, HookMode.Post);
        }

        // 恢复所有玩家的原始状态
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            RemoveChickenEffects(player);
        }

        // 清理所有鸡模型
        foreach (var chicken in _chickens.Values)
        {
            if (chicken != null && chicken.IsValid)
            {
                chicken.AcceptInput("Kill");
            }
        }
        _chickens.Clear();
        _originalSpeed.Clear();
        _originalHealth.Clear();
        _originalRender.Clear();
        _originalScale.Clear();
    }

    /// <summary>
    /// 应用小鸡效果
    /// </summary>
    private void ApplyChickenEffects(CCSPlayerController player)
    {
        if (player == null || !player.IsValid) return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return;

        // 保存原始属性
        _originalSpeed[player.Slot] = pawn.VelocityModifier;
        _originalHealth[player.Slot] = pawn.Health;
        _originalRender[player.Slot] = pawn.Render;

        // 保存原始缩放
        if (pawn.CBodyComponent != null && pawn.CBodyComponent.SceneNode != null)
        {
            var skeleton = pawn.CBodyComponent.SceneNode.GetSkeletonInstance();
            if (skeleton != null)
            {
                _originalScale[player.Slot] = skeleton.Scale;
            }
        }

        // 创建鸡模型
        CreateChickenModel(player);

        // 设置玩家透明（Alpha=0）
        pawn.Render = Color.FromArgb(0, 255, 255, 255);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");

        // 禁用阴影
        pawn.ShadowStrength = 0f;
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_flShadowStrength");

        // 缩放玩家到0.2倍
        SetPlayerScale(pawn, 0.2f);

        // 设置移速为1.1倍
        pawn.VelocityModifier = ChickenSpeedMultiplier;
        var movementServices = pawn.MovementServices;
        if (movementServices != null)
        {
            movementServices.Maxspeed = ChickenSpeedMultiplier * 240.0f;
        }
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");

        // 设置血量为50
        pawn.Health = ChickenHealth;
        pawn.MaxHealth = ChickenHealth;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");

        // 禁用武器
        SetWeaponAttack(player, true);

        player.PrintToCenter("🐔 咕咕咕！你变成了小鸡！");
    }

    /// <summary>
    /// 移除小鸡效果
    /// </summary>
    private void RemoveChickenEffects(CCSPlayerController player)
    {
        if (player == null || !player.IsValid) return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return;

        // 移除鸡模型
        if (_chickens.TryGetValue(player.Slot, out var chicken))
        {
            if (chicken != null && chicken.IsValid)
            {
                chicken.AcceptInput("Kill");
            }
            _chickens.Remove(player.Slot);
        }

        // 恢复原始透明度
        if (_originalRender.ContainsKey(player.Slot))
        {
            pawn.Render = _originalRender[player.Slot];
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        }

        // 恢复原始缩放
        if (_originalScale.ContainsKey(player.Slot))
        {
            SetPlayerScale(pawn, _originalScale[player.Slot]);
        }

        // 恢复阴影
        pawn.ShadowStrength = 1.0f;
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_flShadowStrength");

        // 恢复原始速度
        if (_originalSpeed.ContainsKey(player.Slot))
        {
            float originalSpeed = _originalSpeed[player.Slot];
            pawn.VelocityModifier = originalSpeed;
            var movementServices = pawn.MovementServices;
            if (movementServices != null)
            {
                movementServices.Maxspeed = originalSpeed * 240.0f;
            }
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
        }

        // 恢复原始血量（如果玩家还活着）
        if (player.PawnIsAlive && _originalHealth.ContainsKey(player.Slot))
        {
            int originalHealth = _originalHealth[player.Slot];
            pawn.Health = originalHealth;
            pawn.MaxHealth = originalHealth;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
        }

        // 恢复武器
        SetWeaponAttack(player, false);
    }

    /// <summary>
    /// 创建鸡模型
    /// </summary>
    private void CreateChickenModel(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return;

        // 创建鸡模型实体
        var chickenModel = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (chickenModel == null)
        {
            Console.WriteLine($"[我是小鸡] 警告：无法为 {player.PlayerName} 创建鸡模型");
            return;
        }

        // 移除 FL_EDICT_DONTSEND 标志，确保模型可被传输
        if (chickenModel.CBodyComponent != null &&
            chickenModel.CBodyComponent.SceneNode != null &&
            chickenModel.CBodyComponent.SceneNode.Owner != null &&
            chickenModel.CBodyComponent.SceneNode.Owner.Entity != null)
        {
            chickenModel.CBodyComponent.SceneNode.Owner.Entity.Flags &= ~(uint)(1 << 2);
        }

        // 设置鸡模型
        chickenModel.SetModel("models/chicken/chicken.vmdl");
        chickenModel.Render = Color.FromArgb(255, 255, 255, 255);
        chickenModel.Teleport(pawn.AbsOrigin, pawn.AbsRotation, null);
        chickenModel.DispatchSpawn();
        chickenModel.AcceptInput("InitializeSpawnFromWorld", pawn, pawn, "");
        Utilities.SetStateChanged(chickenModel, "CBaseEntity", "m_CBodyComponent");

        // 设置鸡模型缩放为1
        if (chickenModel.CBodyComponent != null &&
            chickenModel.CBodyComponent.SceneNode != null)
        {
            var skeleton = chickenModel.CBodyComponent.SceneNode.GetSkeletonInstance();
            if (skeleton != null)
            {
                skeleton.Scale = 1;
                Utilities.SetStateChanged(chickenModel, "CBaseEntity", "m_CBodyComponent");
            }
        }

        // 下一帧再设置缩放（确保实体已初始化）
        Server.NextFrame(() =>
        {
            if (chickenModel != null && chickenModel.IsValid)
            {
                chickenModel.AcceptInput("SetScale", chickenModel, chickenModel, "1");
            }
        });

        // 设置鸡模型跟随玩家
        chickenModel.AcceptInput("SetParent", pawn, pawn, "!activator");

        _chickens[player.Slot] = chickenModel;
        Console.WriteLine($"[我是小鸡] 已为 {player.PlayerName} 创建鸡模型");
    }

    /// <summary>
    /// 设置武器攻击状态
    /// </summary>
    private void SetWeaponAttack(CCSPlayerController player, bool disableWeapon)
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

                Console.WriteLine($"[我是小鸡] {player.PlayerName} - {weapon.DesignerName} 武器已{(disableWeapon ? "禁用" : "启用")}");
            }
        }
    }

    /// <summary>
    /// 玩家生成时应用小鸡效果
    /// </summary>
    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        // 延迟几帧应用效果，确保模型已加载
        Server.NextFrame(() =>
        {
            Server.NextFrame(() =>
            {
                if (player.IsValid && player.PawnIsAlive)
                {
                    ApplyChickenEffects(player);
                }
            });
        });

        return HookResult.Continue;
    }

    /// <summary>
    /// 玩家拾取武器时禁用武器
    /// </summary>
    private HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive) return HookResult.Continue;

        // 延迟一帧禁用武器（确保武器已添加到背包）
        Server.NextFrame(() =>
        {
            if (player.IsValid && player.PawnIsAlive)
            {
                SetWeaponAttack(player, true);
            }
        });

        return HookResult.Continue;
    }
}
