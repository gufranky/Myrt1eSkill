using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace HelloWorldPlugin;

/// <summary>
/// 我是小鸡事件 - 所有玩家变成鸡，移速1.1倍，血量50
/// </summary>
public class ChickenModeEvent : EntertainmentEvent
{
    public override string Name => "ChickenMode";
    public override string DisplayName => "🐔 我是小鸡";
    public override string Description => "所有玩家都变成了小鸡！移速1.1倍，血量50%！";

    private const float ChickenSpeedMultiplier = 1.1f;
    private const int ChickenHealth = 50;

    private readonly Dictionary<int, string> _originalModels = new();
    private readonly Dictionary<int, float> _originalSpeed = new();
    private readonly Dictionary<int, int> _originalHealth = new();

    public override void OnApply()
    {
        Console.WriteLine("[我是小鸡] 事件已激活");

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive) continue;

            ApplyChickenEffects(player);
        }

        // 注册玩家生成事件
        if (Plugin != null)
        {
            Plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[我是小鸡] 事件已恢复");

        // 移除监听器
        if (Plugin != null)
        {
            Plugin.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
        }

        // 恢复所有玩家的原始状态
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            RemoveChickenEffects(player);
        }

        _originalModels.Clear();
        _originalSpeed.Clear();
        _originalHealth.Clear();
    }

    /// <summary>
    /// 应用小鸡效果
    /// </summary>
    private void ApplyChickenEffects(CCSPlayerController player)
    {
        if (player == null || !player.IsValid) return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return;

        // 保存原始模型、速度和血量
        string originalModel = pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState?.ModelName ?? "";
        _originalModels[player.Slot] = originalModel;
        _originalSpeed[player.Slot] = pawn.VelocityModifier;
        _originalHealth[player.Slot] = pawn.Health;

        // 设置鸡的模型
        try
        {
            pawn.SetModel("characters/models/chicken/chicken.vmdl");
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
            Console.WriteLine($"[我是小鸡] {player.PlayerName} 已变成小鸡");
        }
        catch
        {
            Console.WriteLine($"[我是小鸡] 警告：无法为 {player.PlayerName} 设置鸡模型");
        }

        // 设置移速为当前值的1.1倍
        _originalSpeed[player.Slot] = pawn.VelocityModifier;
        pawn.VelocityModifier *= ChickenSpeedMultiplier;
        var movementServices = pawn.MovementServices;
        if (movementServices != null)
        {
            movementServices.Maxspeed = pawn.VelocityModifier * 240.0f;
        }
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");

        // 设置血量为50
        pawn.Health = ChickenHealth;
        pawn.MaxHealth = ChickenHealth;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");

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
                    Console.WriteLine($"[我是小鸡] {player.PlayerName} 已恢复原始模型");
                }
                catch
                {
                    Console.WriteLine($"[我是小鸡] 警告：无法恢复 {player.PlayerName} 的模型");
                }
            }
        }

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
    }

    /// <summary>
    /// 玩家生成时应用小鸡效果
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
                ApplyChickenEffects(player);
            }
        });

        return HookResult.Continue;
    }
}
