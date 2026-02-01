using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using System.Linq;

namespace MyrtleSkill;

/// <summary>
/// 全员透视事件 - 所有玩家可以透过墙壁看到彼此
/// </summary>
public class XrayEvent : EntertainmentEvent
{
    public override string Name => "Xray";
    public override string DisplayName => "👁️ 全员透视";
    public override string Description => "所有玩家可以透过墙壁看到彼此！敌我位置一览无余！";

    private readonly Dictionary<int, (int relayIndex, int glowIndex)> _glowingPlayers = new();

    public override void OnApply()
    {
        Console.WriteLine("[全员透视] 事件已激活");

        // 给所有玩家添加发光效果
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive) continue;

            ApplyGlowToPlayer(player);
        }

        // 注册监听器
        if (Plugin != null)
        {
            Plugin.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
            Plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
            Plugin.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Post);
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[全员透视] 事件已恢复");

        // 移除监听器
        if (Plugin != null)
        {
            Plugin.RemoveListener<Listeners.CheckTransmit>(OnCheckTransmit);
            Plugin.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
            Plugin.DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Post);
        }

        // 移除所有发光效果（直接遍历字典，避免遗漏）
        var slotsToRemove = _glowingPlayers.Keys.ToList();
        foreach (var slot in slotsToRemove)
        {
            var (relayIndex, glowIndex) = _glowingPlayers[slot];

            var relay = Utilities.GetEntityFromIndex<CDynamicProp>(relayIndex);
            var glow = Utilities.GetEntityFromIndex<CDynamicProp>(glowIndex);

            if (relay != null && relay.IsValid)
            {
                relay.AcceptInput("Kill");
            }

            if (glow != null && glow.IsValid)
            {
                glow.AcceptInput("Kill");
            }
        }
        _glowingPlayers.Clear();
    }

    /// <summary>
    /// 给玩家添加发光效果
    /// </summary>
    private void ApplyGlowToPlayer(CCSPlayerController player)
    {
        if (player == null || !player.IsValid) return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return;

        // 创建发光效果
        bool success = ApplyEntityGlowEffect(pawn, player.Team, out var relayIndex, out var glowIndex);
        if (success)
        {
            _glowingPlayers[player.Slot] = (relayIndex, glowIndex);
            Console.WriteLine($"[全员透视] 已为 {player.PlayerName} 添加发光效果");
        }
    }

    /// <summary>
    /// 从玩家移除发光效果
    /// </summary>
    private void RemoveGlowFromPlayer(CCSPlayerController player)
    {
        if (player == null || !_glowingPlayers.ContainsKey(player.Slot))
            return;

        var (relayIndex, glowIndex) = _glowingPlayers[player.Slot];

        var relay = Utilities.GetEntityFromIndex<CDynamicProp>(relayIndex);
        var glow = Utilities.GetEntityFromIndex<CDynamicProp>(glowIndex);

        if (relay != null && relay.IsValid)
        {
            relay.AcceptInput("Kill");
        }

        if (glow != null && glow.IsValid)
        {
            glow.AcceptInput("Kill");
        }

        _glowingPlayers.Remove(player.Slot);
    }

    /// <summary>
    /// 应用实体发光效果（参考 CS2-GameModifiers-Plugin）
    /// </summary>
    private bool ApplyEntityGlowEffect(CBaseEntity entity, CsTeam team, out int relayIndex, out int glowIndex)
    {
        relayIndex = -1;
        glowIndex = -1;

        if (entity == null || !entity.IsValid)
            return false;

        var sceneNode = entity.CBodyComponent?.SceneNode;
        if (sceneNode == null)
            return false;

        var skeletonInstance = sceneNode.GetSkeletonInstance();
        if (skeletonInstance == null)
            return false;

        var modelName = skeletonInstance.ModelState.ModelName;
        if (string.IsNullOrEmpty(modelName))
            return false;

        var modelRelay = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        var modelGlow = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");

        if (modelRelay == null || !modelRelay.IsValid || modelGlow == null || !modelGlow.IsValid)
            return false;

        // 设置modelRelay
        modelRelay.Spawnflags = 256u;
        modelRelay.RenderMode = RenderMode_t.kRenderNone;

        if (modelRelay.CBodyComponent != null && modelRelay.CBodyComponent.SceneNode != null)
        {
            var owner = modelRelay.CBodyComponent.SceneNode.Owner;
            if (owner != null && owner.Entity != null)
            {
                owner.Entity.Flags &= ~(uint)(1 << 2);
            }
        }

        modelRelay.SetModel(modelName);
        modelRelay.DispatchSpawn();
        modelRelay.AcceptInput("FollowEntity", entity, modelRelay, "!activator");

        // 设置modelGlow
        if (modelGlow.CBodyComponent != null && modelGlow.CBodyComponent.SceneNode != null)
        {
            var owner = modelGlow.CBodyComponent.SceneNode.Owner;
            if (owner != null && owner.Entity != null)
            {
                owner.Entity.Flags &= ~(uint)(1 << 2);
            }
        }

        modelGlow.SetModel(modelName);
        modelGlow.DispatchSpawn();
        modelGlow.AcceptInput("FollowEntity", modelRelay, modelGlow, "!activator");

        // 根据队伍设置发光颜色
        switch (team)
        {
            case CsTeam.Terrorist:
                modelGlow.Glow.GlowColorOverride = Color.FromArgb(255, 165, 0); // 橙色
                break;
            case CsTeam.CounterTerrorist:
                modelGlow.Glow.GlowColorOverride = Color.FromArgb(135, 206, 235); // 天蓝色
                break;
            default:
                modelGlow.Glow.GlowColorOverride = Color.FromArgb(255, 255, 255); // 白色
                break;
        }

        modelGlow.Spawnflags = 256u;
        modelGlow.RenderMode = RenderMode_t.kRenderTransAlpha;
        modelGlow.Glow.GlowRange = 5000;
        modelGlow.Glow.GlowTeam = -1;
        modelGlow.Glow.GlowType = 3;
        modelGlow.Glow.GlowRangeMin = 20;

        relayIndex = (int)modelRelay.Index;
        glowIndex = (int)modelGlow.Index;

        return true;
    }

    /// <summary>
    /// 检查传输时控制发光效果的可见性
    /// </summary>
    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (_glowingPlayers.Count == 0)
            return;

        foreach (var (info, player) in infoList)
        {
            if (player == null || !player.IsValid)
                continue;

            // 所有玩家都可以看到所有发光效果（全员透视）
            // 不需要移除任何实体
        }
    }

    /// <summary>
    /// 玩家生成时添加发光效果
    /// </summary>
    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        ApplyGlowToPlayer(player);
        return HookResult.Continue;
    }

    /// <summary>
    /// 玩家死亡时移除发光效果
    /// </summary>
    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        RemoveGlowFromPlayer(player);
        return HookResult.Continue;
    }
}
