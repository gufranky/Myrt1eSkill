using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 格拉兹技能 - 透过烟雾弹看到东西
/// </summary>
public class GlazSkill : PlayerSkill
{
    public override string Name => "Glaz";
    public override string DisplayName => "🌫 格拉兹";
    public override string Description => "你可以透过烟雾弹看到东西！格拉兹队友共享视野！";
    public override bool IsActive => false; // 被动技能

    // 追踪所有存活烟雾弹的entityid
    private static readonly Dictionary<int, byte> _smokes = new();

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[格拉兹] {player.PlayerName} 获得了格拉兹技能");
        player.PrintToChat("🌫 你获得了格拉兹技能！");
        player.PrintToChat("💡 你可以透过烟雾弹看到东西！");
        player.PrintToChat("💡 拥有格拉兹技能的队友共享烟雾视野！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        Console.WriteLine($"[格拉兹] {player.PlayerName} 失去了格拉兹技能");
    }

    /// <summary>
    /// 处理烟雾弹爆炸事件
    /// </summary>
    public static void OnSmokegrenadeDetonate(EventSmokegrenadeDetonate @event)
    {
        _smokes[@event.Entityid] = 0;
        Console.WriteLine($"[格拉兹] 烟雾弹 #{@event.Entityid} 已爆炸，添加到追踪列表");
    }

    /// <summary>
    /// 处理烟雾弹过期事件
    /// </summary>
    public static void OnSmokegrenadeExpired(EventSmokegrenadeExpired @event)
    {
        _smokes.Remove(@event.Entityid);
        Console.WriteLine($"[格拉兹] 烟雾弹 #{@event.Entityid} 已过期，从追踪列表移除");
    }

    /// <summary>
    /// 回合开始时清空烟雾弹追踪
    /// </summary>
    public static void OnRoundStart()
    {
        _smokes.Clear();
        Console.WriteLine("[格拉兹] 新回合开始，清空烟雾弹追踪列表");
    }

    /// <summary>
    /// 检查传输时控制烟雾弹的可见性（核心逻辑）
    /// </summary>
    public static void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        // 如果没有追踪的烟雾弹，直接返回
        if (_smokes.Count == 0)
            return;

        // 获取所有有格拉兹技能的玩家
        var glazPlayers = GetPlayersWithGlazSkill();

        foreach (var (info, player) in infoList)
        {
            if (player == null || !player.IsValid)
                continue;

            // 获取被观察的玩家
            CCSPlayerController? observedPlayer = null;
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn?.ObserverServices != null)
            {
                var observerTarget = playerPawn.ObserverServices.ObserverTarget.Value;
                if (observerTarget != null && observerTarget.IsValid)
                {
                    // 通过观察目标的实体句柄找到对应的玩家
                    observedPlayer = Utilities.GetPlayers()
                        .FirstOrDefault(p => p?.PlayerPawn?.Value?.Handle == observerTarget.Handle);
                }
            }

            // 检查观察者和被观察者是否都有格拉兹技能
            bool playerHasGlaz = glazPlayers.Contains(player);
            bool observedHasGlaz = observedPlayer != null && glazPlayers.Contains(observedPlayer);

            // 如果观察者和被观察者都有格拉兹技能，移除所有烟雾弹
            if (playerHasGlaz && observedHasGlaz)
            {
                Console.WriteLine($"[格拉兹] {player.PlayerName} 观察 {observedPlayer.PlayerName}，双方都有格拉兹技能，移除烟雾弹");
                foreach (var smokeEntityId in _smokes.Keys)
                {
                    info.TransmitEntities.Remove(smokeEntityId);
                }
            }
        }
    }

    /// <summary>
    /// 获取所有拥有格拉兹技能的玩家
    /// </summary>
    private static HashSet<CCSPlayerController> GetPlayersWithGlazSkill()
    {
        var players = new HashSet<CCSPlayerController>();
        var skillManager = MyrtleSkill.Instance?.SkillManager;
        if (skillManager == null)
            return players;

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive)
                continue;

            var skill = skillManager.GetPlayerSkill(player);
            if (skill?.Name == "Glaz")
            {
                players.Add(player);
            }
        }

        return players;
    }
}
