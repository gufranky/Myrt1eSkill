// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills by Juzlus (SuperpowerXray event)

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;

namespace MyrtleSkill;

/// <summary>
/// 超能力者事件 - 双方各只有一名玩家拥有透视能力
/// </summary>
public class SuperpowerXrayEvent : EntertainmentEvent
{
    public override string Name => "SuperpowerXray";
    public override string DisplayName => "🦸 超能力者";
    public override string Description => "双方各有一名玩家获得透视能力！只有超能力者能看到敌人位置！";

    private readonly Random _random = new();
    private CCSPlayerController? _tSuperpower;
    private CCSPlayerController? _ctSuperpower;
    private readonly Dictionary<int, (int relayIndex, int glowIndex)> _glowingEnemies = new();

    public override void OnApply()
    {
        Console.WriteLine("[超能力者] 事件已激活");

        // 随机选择双方的超能力者
        SelectSuperpowerPlayers();

        // 给所有敌人添加发光效果
        ApplyGlowToAllEnemies();

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
        Console.WriteLine("[超能力者] 事件已恢复");

        // 移除监听器
        if (Plugin != null)
        {
            Plugin.RemoveListener<Listeners.CheckTransmit>(OnCheckTransmit);
            Plugin.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
            Plugin.DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Post);
        }

        // 移除所有发光效果
        RemoveAllGlowEffects();
        _glowingEnemies.Clear();

        // 通知超能力者
        if (_tSuperpower != null && _tSuperpower.IsValid)
            _tSuperpower.PrintToChat("🦸 你的透视能力已消失");

        if (_ctSuperpower != null && _ctSuperpower.IsValid)
            _ctSuperpower.PrintToChat("🦸 你的透视能力已消失");
    }

    /// <summary>
    /// 随机选择双方的超能力者
    /// </summary>
    private void SelectSuperpowerPlayers()
    {
        var tPlayers = Utilities.GetPlayers().Where(p => p.IsValid && p.PawnIsAlive && p.Team == CsTeam.Terrorist).ToList();
        var ctPlayers = Utilities.GetPlayers().Where(p => p.IsValid && p.PawnIsAlive && p.Team == CsTeam.CounterTerrorist).ToList();

        if (tPlayers.Count > 0)
        {
            _tSuperpower = tPlayers[_random.Next(tPlayers.Count)];
            _tSuperpower.PrintToChat("🦸 你是T队的超能力者！你可以看到所有CT队员的位置！");
            _tSuperpower.PrintToCenter("🦸 你获得了透视能力！");
            Console.WriteLine($"[超能力者] T队超能力者: {_tSuperpower.PlayerName}");
        }

        if (ctPlayers.Count > 0)
        {
            _ctSuperpower = ctPlayers[_random.Next(ctPlayers.Count)];
            _ctSuperpower.PrintToChat("🦸 你是CT队的超能力者！你可以看到所有T队员的位置！");
            _ctSuperpower.PrintToCenter("🦸 你获得了透视能力！");
            Console.WriteLine($"[超能力者] CT队超能力者: {_ctSuperpower.PlayerName}");
        }
    }

    /// <summary>
    /// 给所有敌人添加发光效果
    /// </summary>
    private void ApplyGlowToAllEnemies()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive) continue;
            if (player.Team != CsTeam.Terrorist && player.Team != CsTeam.CounterTerrorist) continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            // 为所有玩家创建发光效果
            bool success = ApplyEntityGlowEffect(pawn, player.Team, out var relayIndex, out var glowIndex);
            if (success)
            {
                _glowingEnemies[player.Slot] = (relayIndex, glowIndex);
                Console.WriteLine($"[超能力者] 已为 {player.PlayerName} ({player.Team}) 添加发光效果");
            }
        }
    }

    /// <summary>
    /// 移除所有发光效果
    /// </summary>
    private void RemoveAllGlowEffects()
    {
        foreach (var slot in _glowingEnemies.Keys)
        {
            var (relayIndex, glowIndex) = _glowingEnemies[slot];

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
    }

    /// <summary>
    /// 检查传输时控制发光效果的可见性（核心逻辑）
    /// </summary>
    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (_glowingEnemies.Count == 0)
            return;

        foreach (var (info, player) in infoList)
        {
            if (player == null || !player.IsValid)
                continue;

            // 检查是否是超能力者或正在观察超能力者
            bool isSuperpower = (player == _tSuperpower && _tSuperpower != null && _tSuperpower.IsValid) ||
                               (player == _ctSuperpower && _ctSuperpower != null && _ctSuperpower.IsValid);

            // 检查是否在观察超能力者
            bool isObservingSuperpower = false;
            var pawn = player.PlayerPawn.Value;
            if (pawn != null && pawn.IsValid && pawn.ObserverServices != null)
            {
                var observerTarget = pawn.ObserverServices.ObserverTarget.Value;
                if (observerTarget != null && observerTarget.IsValid)
                {
                    // 比较实体句柄来判断是否在观察超能力者
                    if (_tSuperpower != null && _tSuperpower.IsValid && _tSuperpower.PlayerPawn.Value != null)
                    {
                        isObservingSuperpower = observerTarget.Handle == _tSuperpower.PlayerPawn.Value.Handle;
                    }

                    if (!isObservingSuperpower && _ctSuperpower != null && _ctSuperpower.IsValid && _ctSuperpower.PlayerPawn.Value != null)
                    {
                        isObservingSuperpower = observerTarget.Handle == _ctSuperpower.PlayerPawn.Value.Handle;
                    }
                }
            }

            // 如果不是超能力者且没在观察超能力者，移除所有发光效果
            if (!isSuperpower && !isObservingSuperpower)
            {
                foreach (var (relayIndex, glowIndex) in _glowingEnemies.Values)
                {
                    info.TransmitEntities.Remove(relayIndex);
                    info.TransmitEntities.Remove(glowIndex);
                }
            }
            else
            {
                // 超能力者可以看到敌人的发光效果
                CsTeam superpowerTeam = player.Team;
                foreach (var kvp in _glowingEnemies)
                {
                    var targetPlayer = Utilities.GetPlayerFromSlot(kvp.Key);
                    if (targetPlayer != null && targetPlayer.IsValid && targetPlayer.Team == superpowerTeam)
                    {
                        // 隐藏己方队员的发光效果
                        info.TransmitEntities.Remove(kvp.Value.relayIndex);
                        info.TransmitEntities.Remove(kvp.Value.glowIndex);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 应用实体发光效果
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
                modelGlow.Glow.GlowColorOverride = Color.FromArgb(173, 216, 230); // 天蓝色
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
    /// 玩家生成时添加发光效果
    /// </summary>
    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        if (player.Team != CsTeam.Terrorist && player.Team != CsTeam.CounterTerrorist)
            return HookResult.Continue;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return HookResult.Continue;

        bool success = ApplyEntityGlowEffect(pawn, player.Team, out var relayIndex, out var glowIndex);
        if (success)
        {
            _glowingEnemies[player.Slot] = (relayIndex, glowIndex);
        }

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

        if (_glowingEnemies.ContainsKey(player.Slot))
        {
            var (relayIndex, glowIndex) = _glowingEnemies[player.Slot];

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

            _glowingEnemies.Remove(player.Slot);
        }

        return HookResult.Continue;
    }
}
