// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills by Juzlus (Jackal skill)

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 豺狼/追踪技能 - 主动技能
/// 激活后所有敌人身后会留下粉紫色轨迹，方便追踪他们的位置
/// 完全复制自 jRandomSkills Jackal 技能
/// </summary>
public class JackalSkill : PlayerSkill
{
    public override string Name => "Jackal";
    public override string DisplayName => "🦊 豺狼";
    public override string Description => "激活后所有敌人身后留下轨迹，持续追踪他们的位置！持续10秒！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 60.0f; // 60秒冷却

    // 粒子效果路径（与 jRandomSkills 一致）
    private const string PARTICLE_NAME = "particles/ui/hud/ui_map_def_utility_trail.vpcf";

    // 轨迹刷新间隔（秒）
    private const float TRAIL_REFRESH_INTERVAL = 2.5f;

    // 技能持续时间（秒）
    private const float SKILL_DURATION = 10.0f;

    // 跟踪每个玩家的粒子系统
    private readonly Dictionary<CCSPlayerController, CParticleSystem> _playerTrails = new();

    // 跟踪激活此技能的玩家
    private readonly Dictionary<ulong, bool> _activePlayers = new();

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[豺狼] {player.PlayerName} 获得了豺狼技能");
        player.PrintToChat("🦊 你获得了豺狼技能！");
        player.PrintToChat("💡 输入 !useskill 或按键激活！");
        player.PrintToChat($"⏱️ 冷却时间：{Cooldown}秒，持续时间：{SKILL_DURATION}秒");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        // 移除该玩家激活的技能
        DisableSkill(player);

        Console.WriteLine($"[豺狼] {player.PlayerName} 失去了豺狼技能");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        Console.WriteLine($"[豺狼] {player.PlayerName} 激活了豺狼技能");

        // 如果已经激活，则不重复激活
        if (_activePlayers.ContainsKey(player.SteamID))
        {
            player.PrintToChat("🦊 豺狼技能已经在运行中！");
            return;
        }

        // 激活技能
        EnableSkill(player);

        player.PrintToChat($"🦊 豺狼技能已激活！所有敌人身后留下轨迹！持续{SKILL_DURATION}秒！");
    }

    /// <summary>
    /// 激活技能 - 为所有敌人创建轨迹
    /// 完全复制自 jRandomSkills Jackal.EnableSkill
    /// </summary>
    private void EnableSkill(CCSPlayerController player)
    {
        // 注册 CheckTransmit 监听（如果还没有注册）
        if (_activePlayers.Count == 0 && Plugin != null)
        {
            Plugin.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
        }

        // 标记玩家为激活状态
        _activePlayers[player.SteamID] = true;

        // 为所有敌方玩家创建轨迹
        foreach (var enemy in Utilities.GetPlayers()
            .Where(p => p.Team != player.Team && p.IsValid && !p.IsBot && !p.IsHLTV && p.PawnIsAlive))
        {
            if (!_playerTrails.ContainsKey(enemy))
            {
                _playerTrails[enemy] = null!;
                CreatePlayerTrail(enemy);
            }
        }

        Console.WriteLine($"[豺狼] 已为 {player.PlayerName} 激活追踪，{_playerTrails.Count} 个敌人被标记");

        // 10秒后自动禁用技能
        Plugin?.AddTimer(SKILL_DURATION, () =>
        {
            if (player != null && player.IsValid && _activePlayers.ContainsKey(player.SteamID))
            {
                player.PrintToChat("🦊 豺狼技能已结束！");
                DisableSkill(player);
            }
        });
    }

    /// <summary>
    /// 禁用技能 - 移除该玩家的所有轨迹
    /// 完全复制自 jRandomSkills Jackal.DisableSkill
    /// </summary>
    private void DisableSkill(CCSPlayerController player)
    {
        // 移除玩家激活状态
        _activePlayers.Remove(player.SteamID);

        // 如果没有激活的玩家了，清理所有轨迹
        if (_activePlayers.Count == 0)
        {
            NewRound();
        }

        Console.WriteLine($"[豺狼] 已移除 {player.PlayerName} 的追踪");
    }

    /// <summary>
    /// 创建玩家轨迹
    /// 完全复制自 jRandomSkills Jackal.CreatePlayerTrail
    /// </summary>
    private void CreatePlayerTrail(CCSPlayerController? player)
    {
        if (player == null)
            return;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null)
            return;

        if (playerPawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            return;

        if (!_playerTrails.ContainsKey(player))
            return;

        // 创建粒子系统实体
        CParticleSystem particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system")!;
        if (particle == null)
            return;

        // 设置粒子效果
        particle.EffectName = PARTICLE_NAME;
        particle.StartActive = true;

        // 传送到玩家位置
        particle.Teleport(playerPawn.AbsOrigin);
        particle.DispatchSpawn();

        // 附加到玩家身上（跟随玩家移动）
        particle.AcceptInput("SetParent", playerPawn, particle, "!activator");
        particle.AcceptInput("Start");

        // 保存粒子系统引用
        _playerTrails[player] = particle;

        Console.WriteLine($"[豺狼] 为 {player.PlayerName} 创建了轨迹粒子");

        // 2.5秒后刷新轨迹
        if (Plugin != null)
        {
            Plugin.AddTimer(TRAIL_REFRESH_INTERVAL, () =>
            {
                if (particle != null && particle.IsValid)
                {
                    particle.AcceptInput("Kill");
                }
                CreatePlayerTrail(player);
            });
        }
    }

    /// <summary>
    /// 清理所有轨迹（回合结束或技能失效时）
    /// 完全复制自 jRandomSkills Jackal.NewRound
    /// </summary>
    private void NewRound()
    {
        // 销毁所有粒子系统
        foreach (var trail in _playerTrails.Values)
        {
            if (trail != null && trail.IsValid)
            {
                trail.AcceptInput("Kill");
            }
        }

        _playerTrails.Clear();
        _activePlayers.Clear();

        // 移除 CheckTransmit 监听
        if (Plugin != null)
        {
            Plugin.RemoveListener<Listeners.CheckTransmit>(OnCheckTransmit);
        }

        Console.WriteLine("[豺狼] 已清理所有轨迹");
    }

    /// <summary>
    /// 控制轨迹可见性
    /// 完全复制自 jRandomSkills Jackal.CheckTransmit
    /// 只有拥有豺狼技能的玩家能看到轨迹，其他人看不到
    /// </summary>
    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        foreach (var (info, player) in infoList)
        {
            if (player == null || !player.IsValid)
                continue;

            // 检查玩家是否有豺狼技能或在观察拥有技能的玩家
            bool hasSkill = _activePlayers.ContainsKey(player.SteamID);

            // 如果玩家正在观察其他人，检查被观察者是否有豺狼技能
            if (!hasSkill)
            {
                var targetHandle = player.Pawn.Value?.ObserverServices?.ObserverTarget.Value?.Handle ?? nint.Zero;
                if (targetHandle != nint.Zero)
                {
                    var target = Utilities.GetPlayers().FirstOrDefault(p => p?.Pawn?.Value?.Handle == targetHandle);
                    if (target != null && _activePlayers.ContainsKey(target.SteamID))
                    {
                        hasSkill = true;
                    }
                }
            }

            // 控制每个轨迹粒子的可见性
            foreach (var kvp in _playerTrails)
            {
                var enemy = kvp.Key;
                var trail = kvp.Value;

                if (trail == null || !trail.IsValid)
                    continue;

                var entity = Utilities.GetEntityFromIndex<CBaseEntity>((int)trail.Index);
                if (entity == null || !entity.IsValid)
                    continue;

                // 如果玩家没有豺狼技能，或者轨迹属于队友，则隐藏轨迹
                if (!hasSkill || enemy.Team == player.Team)
                {
                    info.TransmitEntities.Remove(entity.Index);
                }
            }
        }
    }
}
