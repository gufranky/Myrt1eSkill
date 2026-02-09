using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using System.Linq;

namespace MyrtleSkill;

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

    // ✅ 修改：存储实体索引而不是实体引用（与 WoodManSkill 一致）
    private readonly Dictionary<int, (int relayIndex, int glowIndex)> _glowingPlayers = new();

    // 伤害倍数
    private const float DAMAGE_MULTIPLIER = 2.0f;

    public override void OnApply()
    {
        Console.WriteLine("[苦命鸳鸯] 事件已激活");

        // ✅ 强制清理旧状态（防止跨回合透视效果）
        // 即使 OnRevert() 没有被调用，也要确保清理旧监听器和实体
        if (Plugin != null)
        {
            Plugin.RemoveListener<Listeners.CheckTransmit>(OnCheckTransmit);
            Plugin.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
            Plugin.DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Post);
        }

        // ✅ 移除所有旧的发光效果（通过索引重新获取实体，与 WoodManSkill 一致）
        int removedCount = 0;
        foreach (var (slot, (relayIndex, glowIndex)) in _glowingPlayers)
        {
            var relay = Utilities.GetEntityFromIndex<CDynamicProp>(relayIndex);
            var glow = Utilities.GetEntityFromIndex<CDynamicProp>(glowIndex);

            if (relay != null && relay.IsValid)
            {
                relay.AcceptInput("Kill");
                removedCount++;
            }
            if (glow != null && glow.IsValid)
            {
                glow.AcceptInput("Kill");
                removedCount++;
            }
        }
        _glowingPlayers.Clear();
        if (removedCount > 0)
        {
            Console.WriteLine($"[苦命鸳鸯] OnApply: 清理了 {removedCount} 个旧发光实体");
        }

        // 每次都重新配对（不保持跨回合配对）
        Console.WriteLine("[苦命鸳鸯] 进行新配对");

        // 配对玩家并应用效果
        MatchPlayersAndApplyEffects();

        // 注册监听器
        if (Plugin != null)
        {
            Plugin.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
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
                        player.PrintToChat($"💑 苦命鸳鸯模式已启用！你的配对对象是：{partner.PlayerName}");
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
        Console.WriteLine("[苦命鸳鸯] 事件已恢复，开始清理");

        // 1. 首先取消激活标志，阻止所有监听器继续工作
        // 这样即使OnPlayerSpawn的NextFrame回调被调用，也不会创建新实体
        _pairs.Clear();

        // 2. 先移除监听器（防止继续应用效果）
        if (Plugin != null)
        {
            Plugin.RemoveListener<Listeners.CheckTransmit>(OnCheckTransmit);
            Plugin.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
            Plugin.DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Post);
            Console.WriteLine("[苦命鸳鸯] 已移除所有事件监听器");
        }

        // 3. 移除所有发光效果（通过索引重新获取实体，与 WoodManSkill 一致）
        int removedCount = 0;
        foreach (var (slot, (relayIndex, glowIndex)) in _glowingPlayers)
        {
            var relay = Utilities.GetEntityFromIndex<CDynamicProp>(relayIndex);
            var glow = Utilities.GetEntityFromIndex<CDynamicProp>(glowIndex);

            if (relay != null && relay.IsValid)
            {
                relay.AcceptInput("Kill");
                removedCount++;
                Console.WriteLine($"[苦命鸳鸯] 已移除 relay 实体 (index: {relayIndex})");
            }

            if (glow != null && glow.IsValid)
            {
                glow.AcceptInput("Kill");
                removedCount++;
                Console.WriteLine($"[苦命鸳鸯] 已移除 glow 实体 (index: {glowIndex})");
            }
        }
        _glowingPlayers.Clear();
        Console.WriteLine($"[苦命鸳鸯] 已清理所有发光效果，共移除 {removedCount} 个实体");

        // 4. 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("💑 苦命鸳鸯模式已禁用，配对关系已解除");
            }
        }

        Console.WriteLine("[苦命鸳鸯] 事件恢复完成");
    }

    /// <summary>
    /// 配对玩家并应用发光效果
    /// </summary>
    private void MatchPlayersAndApplyEffects()
    {
        // 获取所有存活的玩家（分别按队伍）
        var terroristPlayers = Utilities.GetPlayers()
            .Where(p => p.IsValid && p.PawnIsAlive && p.Team == CsTeam.Terrorist)
            .ToList();

        var ctPlayers = Utilities.GetPlayers()
            .Where(p => p.IsValid && p.PawnIsAlive && p.Team == CsTeam.CounterTerrorist)
            .ToList();

        // 如果是单数，忽略最后一名玩家
        CCSPlayerController? ignoredPlayer = null;
        if (terroristPlayers.Count % 2 != 0)
        {
            ignoredPlayer = terroristPlayers.Last();
            Console.WriteLine($"[苦命鸳鸯] T队玩家数量为单数 ({terroristPlayers.Count})，忽略玩家: {ignoredPlayer.PlayerName}");
            terroristPlayers.RemoveAt(terroristPlayers.Count - 1);
        }
        if (ctPlayers.Count % 2 != 0)
        {
            ignoredPlayer = ctPlayers.Last();
            Console.WriteLine($"[苦命鸳鸯] CT队玩家数量为单数 ({ctPlayers.Count})，忽略玩家: {ignoredPlayer.PlayerName}");
            ctPlayers.RemoveAt(ctPlayers.Count - 1);
        }

        // 合并两个队伍的玩家
        var alivePlayers = terroristPlayers.Concat(ctPlayers).ToList();

        // 随机打乱玩家顺序
        var random = new Random();
        for (int i = alivePlayers.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (alivePlayers[i], alivePlayers[j]) = (alivePlayers[j], alivePlayers[i]);
        }

        // 两两配对（确保配对的是不同队伍的敌人）
        for (int i = 0; i < alivePlayers.Count; i += 2)
        {
            var player1 = alivePlayers[i];
            var player2 = alivePlayers[i + 1];

            // 确保配对的是敌人（不同队伍）
            if (player1.Team == player2.Team)
            {
                Console.WriteLine($"[苦命鸳鸯] 警告：尝试配对同队玩家 {player1.PlayerName} <-> {player2.PlayerName}，跳过此次配对");
                continue;
            }

            _pairs[player1.Slot] = player2.Slot;
            _pairs[player2.Slot] = player1.Slot;

            Console.WriteLine($"[苦命鸳鸯] 配对: {player1.PlayerName} ({player1.Team}) <-> {player2.PlayerName} ({player2.Team})");

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

        // 创建发光效果并返回实体引用
        bool success = ApplyEntityGlowEffect(pawn, player.Team, out var relay, out var glow);
        if (success && relay != null && glow != null)
        {
            // ✅ 存储实体索引而不是引用（与 WoodManSkill 一致）
            _glowingPlayers[player.Slot] = ((int)relay.Index, (int)glow.Index);
            Console.WriteLine($"[苦命鸳鸯] 已为 {player.PlayerName} 添加发光效果 (relay: {relay.Index}, glow: {glow.Index})");
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

        // ✅ 通过索引重新获取实体（与 WoodManSkill 一致）
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
    /// 应用实体发光效果（参考 CS2-GameModifiers-Plugin 和 WallhackSkill）
    /// </summary>
    private bool ApplyEntityGlowEffect(CBaseEntity entity, CsTeam team, out CBaseEntity? relay, out CBaseEntity? glow)
    {
        relay = null;
        glow = null;

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
        modelGlow.Glow.GlowColorOverride = Color.FromArgb(255, 105, 180); // 粉红色

        modelGlow.Spawnflags = 256u;
        modelGlow.RenderMode = RenderMode_t.kRenderTransAlpha;
        modelGlow.Glow.GlowRange = 5000;
        modelGlow.Glow.GlowTeam = -1;
        modelGlow.Glow.GlowType = 3;
        modelGlow.Glow.GlowRangeMin = 20;

        relay = modelRelay;
        glow = modelGlow;

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

            // 检查玩家是否在配对中
            if (_pairs.ContainsKey(player.Slot))
            {
                var partnerSlot = _pairs[player.Slot];

                // 只显示配对对象的发光效果
                foreach (var kvp in _glowingPlayers)
                {
                    // 如果是配对对象，保留其发光效果
                    if (kvp.Key == partnerSlot)
                    {
                        continue;
                    }
                    // 否则移除其他人的发光效果
                    else
                    {
                        var (relayIndex, glowIndex) = kvp.Value;
                        var relay = Utilities.GetEntityFromIndex<CDynamicProp>(relayIndex);
                        var glow = Utilities.GetEntityFromIndex<CDynamicProp>(glowIndex);

                        if (relay != null && relay.IsValid)
                        {
                            info.TransmitEntities.Remove(relay.Index);
                        }
                        if (glow != null && glow.IsValid)
                        {
                            info.TransmitEntities.Remove(glow.Index);
                        }
                    }
                }
            }
            else
            {
                // 未配对的玩家看不到任何发光效果
                foreach (var (relayIndex, glowIndex) in _glowingPlayers.Values)
                {
                    var relay = Utilities.GetEntityFromIndex<CDynamicProp>(relayIndex);
                    var glow = Utilities.GetEntityFromIndex<CDynamicProp>(glowIndex);

                    if (relay != null && relay.IsValid)
                    {
                        info.TransmitEntities.Remove(relay.Index);
                    }
                    if (glow != null && glow.IsValid)
                    {
                        info.TransmitEntities.Remove(glow.Index);
                    }
                }
            }
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

        // ✅ 增加检查：玩家必须在配对中
        // 如果事件已经被恢复（_pairs已清空），这里会返回false
        if (!_pairs.ContainsKey(player.Slot))
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            // ✅ 再次检查：确保玩家仍在配对中且事件仍然激活
            // 防止在OnRevert之后才执行，导致创建新的发光实体
            if (_pairs.ContainsKey(player.Slot))
            {
                RemoveGlowFromPlayer(player);
                ApplyGlowToPlayer(player);
            }
        });

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
