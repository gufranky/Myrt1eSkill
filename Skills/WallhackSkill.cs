// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills by Juzlus (Wallhack/Xray skills)

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;
using System.Drawing;

namespace MyrtleSkill.Skills;

/// <summary>
/// 透视技能 - 可以看到敌人位置
/// </summary>
public class WallhackSkill : PlayerSkill
{
    public override string Name => "Wallhack";
    public override string DisplayName => "👁️ 透视";
    public override string Description => "你可以透过墙壁看到所有敌人的位置！";
    public override bool IsActive => false; // 被动技能

    // 与其他视野技能互斥
    public override List<string> ExcludedSkills => new() { "RadarHack", "DecoyXRay" };

    // 与透视事件和隐身事件互斥
    public override List<string> ExcludedEvents => new() { "Xray", "SuperpowerXray", "StayQuiet", "RainyDay" };

    // 存储发光实体: (modelRelay, modelGlow, enemyTeam)
    private static readonly ConcurrentBag<(CDynamicProp, CDynamicProp, CsTeam)> _glows = new();

    // 跟踪拥有该技能的玩家
    private static readonly ConcurrentDictionary<int, byte> _playersWithSkill = new();

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[透视] {player.PlayerName} 获得了透视技能");

        // 添加到技能玩家列表
        _playersWithSkill.TryAdd(player.Slot, 0);

        // 如果是第一个拥有该技能的玩家，创建所有发光效果
        if (_playersWithSkill.Count == 1)
        {
            SetGlowEffectForAll();
        }

        // 启用 CheckTransmit 监听
        if (MyrtleSkill.Instance != null)
        {
            MyrtleSkill.Instance.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
        }

        player.PrintToChat("👁️ 你获得了透视技能！");
        player.PrintToChat("💡 你可以透过墙壁看到所有敌人的位置！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        Console.WriteLine($"[透视] {player.PlayerName} 失去了透视技能");

        // 从技能玩家列表移除
        _playersWithSkill.TryRemove(player.Slot, out _);

        // 如果没有玩家拥有该技能了，清理所有发光效果
        if (_playersWithSkill.IsEmpty)
        {
            RemoveAllGlowEffects();

            // 移除 CheckTransmit 监听
            if (MyrtleSkill.Instance != null)
            {
                MyrtleSkill.Instance.RemoveListener<Listeners.CheckTransmit>(OnCheckTransmit);
            }
        }
    }

    /// <summary>
    /// CheckTransmit 回调 - 控制发光效果的可见性
    /// </summary>
    private static void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (_glows.IsEmpty)
            return;

        foreach (var (info, player) in infoList)
        {
            if (player == null || !player.IsValid)
                continue;

            // 检查玩家是否有透视技能
            bool hasSkill = _playersWithSkill.ContainsKey(player.Slot);

            // 检查是否在观察有透视技能的玩家
            bool isObserving = false;
            var pawn = player.PlayerPawn.Value;
            if (pawn != null && pawn.IsValid && pawn.ObserverServices != null)
            {
                var observerTarget = pawn.ObserverServices.ObserverTarget.Value;
                if (observerTarget != null && observerTarget.IsValid)
                {
                    // 检查观察目标是否有透视技能
                    foreach (var slot in _playersWithSkill.Keys)
                    {
                        var targetPlayer = Utilities.GetPlayerFromSlot(slot);
                        if (targetPlayer != null && targetPlayer.IsValid &&
                            targetPlayer.PlayerPawn.Value != null &&
                            observerTarget.Handle == targetPlayer.PlayerPawn.Value.Handle)
                        {
                            isObserving = true;
                            break;
                        }
                    }
                }
            }

            // 如果没有技能且没有观察有技能的玩家，隐藏所有发光效果
            if (!hasSkill && !isObserving)
            {
                foreach (var glow in _glows)
                {
                    info.TransmitEntities.Remove(glow.Item1.Index); // 移除 modelRelay
                    info.TransmitEntities.Remove(glow.Item2.Index); // 移除 modelGlow
                }
            }
            else
            {
                // 有技能的玩家可以看到敌人的发光效果，但看不到队友的
                CsTeam playerTeam = player.Team;
                foreach (var glow in _glows)
                {
                    if (glow.Item3 == playerTeam)
                    {
                        // 隐藏队友的发光效果
                        info.TransmitEntities.Remove(glow.Item1.Index);
                        info.TransmitEntities.Remove(glow.Item2.Index);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 为所有敌人创建发光效果
    /// </summary>
    private static void SetGlowEffectForAll()
    {
        foreach (var enemy in Utilities.GetPlayers())
        {
            if (!enemy.IsValid || !enemy.PawnIsAlive)
                continue;

            if (enemy.Team != CsTeam.Terrorist && enemy.Team != CsTeam.CounterTerrorist)
                continue;

            var enemyPawn = enemy.PlayerPawn.Value;
            if (enemyPawn == null || !enemyPawn.IsValid)
                continue;

            // 创建发光效果
            if (CreateGlowEffect(enemyPawn, enemy.Team, out var modelRelay, out var modelGlow))
            {
                _glows.Add((modelRelay, modelGlow, enemy.Team));
                Console.WriteLine($"[透视] 已为 {enemy.PlayerName} ({enemy.Team}) 添加发光效果");
            }
        }
    }

    /// <summary>
    /// 创建单个发光效果
    /// </summary>
    private static bool CreateGlowEffect(CCSPlayerPawn playerPawn, CsTeam team, out CDynamicProp modelRelay, out CDynamicProp modelGlow)
    {
        modelRelay = null!;
        modelGlow = null!;

        var skeletonInstance = playerPawn.CBodyComponent?.SceneNode?.GetSkeletonInstance();
        if (skeletonInstance == null)
            return false;

        var modelName = skeletonInstance.ModelState.ModelName;
        if (string.IsNullOrEmpty(modelName))
            return false;

        modelRelay = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        modelGlow = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");

        if (modelRelay == null || !modelRelay.IsValid || modelGlow == null || !modelGlow.IsValid)
            return false;

        // 设置 modelRelay（不可见的中继实体）
        if (modelRelay.CBodyComponent?.SceneNode?.Owner?.Entity != null)
        {
            modelRelay.CBodyComponent.SceneNode.Owner.Entity.Flags =
                (uint)(modelRelay.CBodyComponent.SceneNode.Owner.Entity.Flags & ~(1 << 2));
        }

        modelRelay.SetModel(modelName);
        modelRelay.Spawnflags = 256u;
        modelRelay.RenderMode = RenderMode_t.kRenderNone;
        modelRelay.DispatchSpawn();
        modelRelay.AcceptInput("FollowEntity", playerPawn, modelRelay, "!activator");

        // 设置 modelGlow（发光实体）
        if (modelGlow.CBodyComponent?.SceneNode?.Owner?.Entity != null)
        {
            modelGlow.CBodyComponent.SceneNode.Owner.Entity.Flags =
                (uint)(modelGlow.CBodyComponent.SceneNode.Owner.Entity.Flags & ~(1 << 2));
        }

        modelGlow.SetModel(modelName);
        modelGlow.Spawnflags = 256u;
        modelGlow.Render = Color.FromArgb(1, 255, 255, 255);
        modelGlow.DispatchSpawn();

        // 根据队伍设置发光颜色
        modelGlow.Glow.GlowColorOverride = team == CsTeam.Terrorist
            ? Color.FromArgb(255, 255, 165, 0)   // T: 橙色
            : Color.FromArgb(255, 173, 216, 230); // CT: 天蓝色

        modelGlow.Glow.GlowRange = 5000;
        modelGlow.Glow.GlowTeam = -1;
        modelGlow.Glow.GlowType = 3;
        modelGlow.Glow.GlowRangeMin = 100;

        modelGlow.AcceptInput("FollowEntity", modelRelay, modelGlow, "!activator");

        return true;
    }

    /// <summary>
    /// 移除所有发光效果
    /// </summary>
    private static void RemoveAllGlowEffects()
    {
        foreach (var (modelRelay, modelGlow, _) in _glows)
        {
            if (modelRelay != null && modelRelay.IsValid)
            {
                modelRelay.AcceptInput("Kill");
            }

            if (modelGlow != null && modelGlow.IsValid)
            {
                modelGlow.AcceptInput("Kill");
            }
        }

        _glows.Clear();
        Console.WriteLine("[透视] 已移除所有发光效果");
    }
}
