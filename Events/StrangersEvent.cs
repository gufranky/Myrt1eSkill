using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using MyrtleSkill.ThirdParty;

namespace MyrtleSkill;

/// <summary>
/// 不认识的人事件 - 所有人模型统一、可对友军造成伤害、不显示小地图、取消瞄准时显示名字
/// </summary>
public class StrangersEvent : EntertainmentEvent
{
    public override string Name => "Strangers";
    public override string DisplayName => "👥 不认识的人";
    public override string Description => "所有人的模型都一样！可以对友军造成伤害！不显示小地图！";

    // ConVars
    private ConVar? _radarEnableConVar;
    private ConVar? _friendlyFireConVar;
    private ConVar? _teammatesAreEnemiesConVar;
    private float _originalRadarEnable = 1.0f;
    private bool _originalFriendlyFire = false;
    private int _originalTeammatesAreEnemies = 0;

    // 统一模型路径（所有玩家都使用这个模型）
    private const string UNIFIED_MODEL = "characters/models/ctm_swat/ctm_swat.vmdl";

    // 存储原始模型
    private readonly Dictionary<int, string> _originalModels = new();

    public override void OnApply()
    {
        Console.WriteLine("[不认识的人] 事件已激活");

        // 1. 启用友军伤害
        _friendlyFireConVar = ConVar.Find("mp_friendlyfire");
        if (_friendlyFireConVar != null)
        {
            _originalFriendlyFire = _friendlyFireConVar.GetPrimitiveValue<bool>();
            _friendlyFireConVar.SetValue(true);
            Console.WriteLine($"[不认识的人] mp_friendlyfire 已设置为 true (原值: {_originalFriendlyFire})");
        }
        else
        {
            Console.WriteLine("[不认识的人] 警告：无法找到 mp_friendlyfire ConVar");
        }

        // 2. 启用"队友是敌人"模式（减少友军伤害惩罚和提示）
        _teammatesAreEnemiesConVar = ConVar.Find("mp_teammates_are_enemies");
        if (_teammatesAreEnemiesConVar != null)
        {
            _originalTeammatesAreEnemies = _teammatesAreEnemiesConVar.GetPrimitiveValue<int>();
            _teammatesAreEnemiesConVar.SetValue(1);
            Console.WriteLine($"[不认识的人] mp_teammates_are_enemies 已设置为 1 (原值: {_originalTeammatesAreEnemies})");
        }
        else
        {
            Console.WriteLine("[不认识的人] 警告：无法找到 mp_teammates_are_enemies ConVar");
        }

        // 3. 禁用小地图
        _radarEnableConVar = ConVar.Find("sv_radar_enable");
        if (_radarEnableConVar != null)
        {
            _originalRadarEnable = _radarEnableConVar.GetPrimitiveValue<float>();
            _radarEnableConVar.SetValue(0.0f);
            Console.WriteLine($"[不认识的人] sv_radar_enable 已设置为 0 (原值: {_originalRadarEnable})");
        }
        else
        {
            Console.WriteLine("[不认识的人] 警告：无法找到 sv_radar_enable ConVar");
        }

        // 4. 给所有玩家设置统一模型
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive) continue;

            ApplyStrangerEffects(player);
        }

        // 5. 随机传送所有玩家到不同位置
        RandomTeleportAllPlayers();

        // 6. 注册玩家生成事件
        if (Plugin != null)
        {
            Plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
        }

        // 7. 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("👥 不认识的人模式已启用！小心，所有人看起来都一样！");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[不认识的人] 事件已恢复");

        // 移除监听器
        if (Plugin != null)
        {
            Plugin.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
        }

        // 恢复友军伤害
        if (_friendlyFireConVar != null)
        {
            _friendlyFireConVar.SetValue(_originalFriendlyFire);
            Console.WriteLine($"[不认识的人] mp_friendlyfire 已恢复为 {_originalFriendlyFire}");
        }

        // 恢复"队友是敌人"模式
        if (_teammatesAreEnemiesConVar != null)
        {
            _teammatesAreEnemiesConVar.SetValue(_originalTeammatesAreEnemies);
            Console.WriteLine($"[不认识的人] mp_teammates_are_enemies 已恢复为 {_originalTeammatesAreEnemies}");
        }

        // 恢复小地图
        if (_radarEnableConVar != null)
        {
            _radarEnableConVar.SetValue(_originalRadarEnable);
            Console.WriteLine($"[不认识的人] sv_radar_enable 已恢复为 {_originalRadarEnable}");
        }

        // 恢复所有玩家的原始模型
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            RemoveStrangerEffects(player);
        }

        _originalModels.Clear();

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("👥 不认识的人模式已禁用");
            }
        }
    }

    /// <summary>
    /// 应用陌生人效果
    /// </summary>
    private void ApplyStrangerEffects(CCSPlayerController player)
    {
        if (player == null || !player.IsValid) return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return;

        // 保存原始模型
        string originalModel = pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState?.ModelName ?? "";
        _originalModels[player.Slot] = originalModel;

        // 所有人使用统一模型（不再区分CT和T）
        try
        {
            pawn.SetModel(UNIFIED_MODEL);
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
            Console.WriteLine($"[不认识的人] {player.PlayerName} 的模型已统一");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[不认识的人] 警告：无法为 {player.PlayerName} 设置统一模型: {ex.Message}");
        }
    }

    /// <summary>
    /// 移除陌生人效果
    /// </summary>
    private void RemoveStrangerEffects(CCSPlayerController player)
    {
        if (player == null || !player.IsValid) return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return;

        // 恢复原始模型
        if (_originalModels.ContainsKey(player.Slot))
        {
            string originalModel = _originalModels[player.Slot];
            if (!string.IsNullOrEmpty(originalModel))
            {
                try
                {
                    pawn.SetModel(originalModel);
                    Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
                    Console.WriteLine($"[不认识的人] {player.PlayerName} 已恢复原始模型");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[不认识的人] 警告：无法恢复 {player.PlayerName} 的原始模型: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// 玩家生成时应用陌生人效果
    /// </summary>
    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        // 延迟一帧应用效果，确保模型已加载
        Server.NextFrame(() =>
        {
            if (player.IsValid && player.PawnIsAlive)
            {
                ApplyStrangerEffects(player);
            }
        });

        return HookResult.Continue;
    }

    /// <summary>
    /// 随机传送所有玩家到不同位置
    /// </summary>
    private void RandomTeleportAllPlayers()
    {
        var players = Utilities.GetPlayers().Where(p => p.IsValid && p.PawnIsAlive).ToList();

        Console.WriteLine($"[不认识的人] 开始随机传送 {players.Count} 名玩家");

        foreach (var player in players)
        {
            // 为每个玩家获取一个随机位置
            Vector? randomPosition = NavMesh.GetRandomPosition(maxAttempts: 20);
            if (randomPosition == null)
            {
                Console.WriteLine($"[不认识的人] 警告：无法为 {player.PlayerName} 找到随机位置！");
                continue;
            }

            // 传送玩家
            TeleportPlayer(player, randomPosition);
            Console.WriteLine($"[不认识的人] {player.PlayerName} 已传送到随机位置");
        }
    }

    /// <summary>
    /// 传送玩家到指定位置，并处理碰撞组防止卡墙
    /// </summary>
    private void TeleportPlayer(CCSPlayerController player, Vector position)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 执行传送
        pawn.Teleport(position, pawn.AbsRotation, new Vector(0, 0, 0));

        // 临时设置为穿透模式，防止卡在墙里或其他玩家身上
        pawn.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
        pawn.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
        Utilities.SetStateChanged(pawn, "CCollisionProperty", "m_CollisionGroup");
        Utilities.SetStateChanged(pawn, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");

        // 下一帧恢复正常碰撞
        Server.NextFrame(() =>
        {
            if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                return;

            pawn.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
            pawn.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
            Utilities.SetStateChanged(pawn, "CCollisionProperty", "m_CollisionGroup");
            Utilities.SetStateChanged(pawn, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
        });
    }
}
