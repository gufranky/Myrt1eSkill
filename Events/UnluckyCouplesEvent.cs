using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;

namespace HelloWorldPlugin;

/// <summary>
/// 苦命鸳鸯事件 - 玩家两两配对，配对玩家互相可见且伤害增加
/// </summary>
public class UnluckyCouplesEvent : EntertainmentEvent
{
    public override string Name => "UnluckyCouples";
    public override string DisplayName => "💑 苦命鸳鸯";
    public override string Description => "玩家两两配对！配对玩家互相可见且伤害增加！单数玩家将被忽略！";

    // 存储配对关系：playerSlot -> partnerSlot
    private readonly Dictionary<int, int> _pairs = new();

    // 存储发光效果：playerSlot -> (relayIndex, glowIndex)
    private readonly Dictionary<int, (int relayIndex, int glowIndex)> _glowingPlayers = new();

    // 伤害倍数
    private const float DAMAGE_MULTIPLIER = 2.0f;

    public override void OnApply()
    {
        Console.WriteLine("[苦命鸳鸯] 事件已激活");

        // 配对玩家并应用效果
        MatchPlayersAndApplyEffects();

        // 注册监听器
        if (Plugin != null)
        {
            Plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
            Plugin.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Post);
        }

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                if (_pairs.ContainsKey(player.Slot))
                {
                    var partner = Utilities.GetPlayerFromSlot(_pairs[player.Slot]);
                    if (partner != null && partner.IsValid)
                    {
                        player.PrintToChat($"💑 苦命鸳鸯模式已启用！");
                        player.PrintToCenter($"💑 苦命鸳鸯！\n你的配对对象是：{partner.PlayerName}\n互相可见 + 2倍伤害！");
                    }
                }
                else
                {
                    player.PrintToChat("💑 苦命鸳鸯模式已启用！你是单数玩家，未被配对。");
                }
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[苦命鸳鸯] 事件已恢复");

        // 移除监听器
        if (Plugin != null)
        {
            Plugin.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
            Plugin.DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Post);
        }

        // 移除所有发光效果
        foreach (var player in Utilities.GetPlayers())
        {
            RemoveGlowFromPlayer(player);
        }
        _glowingPlayers.Clear();

        // 清空配对
        _pairs.Clear();

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("💑 苦命鸳鸯模式已禁用");
            }
        }
    }

    /// <summary>
    /// 配对玩家并应用发光效果
    /// </summary>
    private void MatchPlayersAndApplyEffects()
    {
        // 获取所有存活的玩家
        var alivePlayers = Utilities.GetPlayers()
            .Where(p => p.IsValid && p.PawnIsAlive)
            .ToList();

        // 如果是单数，忽略最后一名玩家
        if (alivePlayers.Count % 2 != 0)
        {
            var ignoredPlayer = alivePlayers.Last();
            Console.WriteLine($"[苦命鸳鸯] 玩家数量为单数 ({alivePlayers.Count})，忽略玩家: {ignoredPlayer.PlayerName}");
            alivePlayers.RemoveAt(alivePlayers.Count - 1);
        }

        // 随机打乱玩家顺序
        var random = new Random();
        for (int i = alivePlayers.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (alivePlayers[i], alivePlayers[j]) = (alivePlayers[j], alivePlayers[i]);
        }

        // 两两配对
        for (int i = 0; i < alivePlayers.Count; i += 2)
        {
            var player1 = alivePlayers[i];
            var player2 = alivePlayers[i + 1];

            _pairs[player1.Slot] = player2.Slot;
            _pairs[player2.Slot] = player1.Slot;

            Console.WriteLine($"[苦命鸳鸯] 配对: {player1.PlayerName} <-> {player2.PlayerName}");

            // 为双方添加发光效果（只对配对玩家可见）
            ApplyGlowToPlayer(player1);
            ApplyGlowToPlayer(player2);
        }
    }

    /// <summary>
    /// 处理伤害前事件（在主文件的 OnPlayerTakeDamagePre 中调用）
    /// 返回伤害倍数，由调用方统一应用
    /// </summary>
    public float? HandleDamagePre(CCSPlayerPawn victimPawn, CTakeDamageInfo info)
    {
        if (victimPawn == null || !victimPawn.IsValid) return null;

        // 获取攻击者
        var attackerEntity = info.Attacker?.Value;
        if (attackerEntity == null) return null;

        // 转换为CCSPlayerPawn
        var attackerPawn = attackerEntity as CCSPlayerPawn;
        if (attackerPawn == null) return null;

        // 获取玩家控制器
        var victim = victimPawn.Controller.Value as CCSPlayerController;
        var attacker = attackerPawn.Controller.Value as CCSPlayerController;

        if (victim == null || !victim.IsValid || attacker == null || !attacker.IsValid)
            return null;

        // 检查是否是配对关系
        if (_pairs.ContainsKey(attacker.Slot) && _pairs[attacker.Slot] == victim.Slot)
        {
            // 是配对关系，返回伤害倍数
            Console.WriteLine($"[苦命鸳鸯] 配对伤害：{attacker.PlayerName} -> {victim.PlayerName}: {DAMAGE_MULTIPLIER}倍");
            return DAMAGE_MULTIPLIER;
        }

        return null;
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
            Console.WriteLine($"[苦命鸳鸯] 已为 {player.PlayerName} 添加发光效果");
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

        // 设置为粉红色（爱情色 ❤️）
        modelGlow.Render = Color.FromArgb(1, 255, 105, 180); // 粉红色

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

        // 如果玩家在配对中，重新添加发光效果（但不重新配对）
        if (_pairs.ContainsKey(player.Slot))
        {
            Server.NextFrame(() =>
            {
                RemoveGlowFromPlayer(player);
                ApplyGlowToPlayer(player);
            });
        }

        return HookResult.Continue;
    }

    /// <summary>
    /// 玩家死亡时移除发光效果（但保留配对关系）
    /// </summary>
    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        // 只移除发光效果，不移除配对关系
        RemoveGlowFromPlayer(player);

        return HookResult.Continue;
    }
}
