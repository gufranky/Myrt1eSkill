// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills by Juzlus (ToxicSmoke skill)

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;

namespace MyrtleSkill.Skills;

/// <summary>
/// 有毒烟雾弹技能 - 被动技能
/// 开局获得3个有毒烟雾弹，烟雾范围内的敌人持续受到伤害
/// 参考实现：jRandomSkills ToxicSmoke
/// </summary>
public class ToxicSmokeSkill : PlayerSkill
{
    public override string Name => "ToxicSmoke";
    public override string DisplayName => "☠️ 有毒烟雾弹";
    public override string Description => "开局1个有毒烟雾弹，持续伤害敌人！投掷后补充1次！";
    public override bool IsActive => false; // 被动技能
    public override float Cooldown => 0f; // 被动技能无冷却
    public override List<string> ExcludedEvents => new() { };

    // 与格拉兹互斥
    public override List<string> ExcludedSkills => new() { "Glaz" };

    // 追踪每回合是否已补充过（只补充1次）
    private readonly Dictionary<uint, bool> _replenishedThisRound = new();

    // 追踪有毒烟雾弹位置（使用ConcurrentDictionary保证线程安全）
    private static readonly ConcurrentDictionary<Vector, byte> _toxicSmokes = new();

    // 有毒烟雾弹参数
    private const int SMOKE_DAMAGE = 5;       // 每次伤害
    private const float SMOKE_RADIUS = 180.0f; // 烟雾半径

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;
        _replenishedThisRound[slot] = false;

        // 给予1个烟雾弹
        GiveSmokeGrenades(player, 1);

        Console.WriteLine($"[有毒烟雾弹] {player.PlayerName} 获得了有毒烟雾弹能力");
        player.PrintToChat("☠️ 你获得了1个有毒烟雾弹！烟雾持续伤害敌人！");
        player.PrintToChat("💡 投掷后可补充1次！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;
        _replenishedThisRound.Remove(slot);

        // 清理该玩家可能残留的有毒烟雾记录
        // 注意：由于_toxicSmokes只记录位置，无法直接按玩家清理
        // 这里不做清理，依靠回合结束时的ClearAllToxicSmokes()

        Console.WriteLine($"[有毒烟雾弹] {player.PlayerName} 失去了有毒烟雾弹能力");
    }

    /// <summary>
    /// 监听烟雾弹投掷事件 - 自动补充1次并记录烟雾位置
    /// </summary>
    public void OnSmokegrenadeDetonate(EventSmokegrenadeDetonate @event)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return;

        // 检查玩家是否有有毒烟雾弹技能
        var skill = Plugin?.SkillManager.GetPlayerSkill(player);
        if (skill?.Name != "ToxicSmoke")
            return;

        var slot = player.Index;

        // 检查是否已经补充过
        if (_replenishedThisRound.TryGetValue(slot, out var replenished) && replenished)
        {
            Console.WriteLine($"[有毒烟雾弹] {player.PlayerName} 本回合已补充过，不再补充");
        }
        else
        {
            // 自动补充1个烟雾弹
            Server.NextFrame(() =>
            {
                if (player.IsValid && player.PawnIsAlive)
                {
                    GiveSmokeGrenades(player, 1);
                    _replenishedThisRound[slot] = true;

                    player.PrintToChat("☠️ 烟雾弹已补充！(1/1)");
                    Console.WriteLine($"[有毒烟雾弹] {player.PlayerName} 的烟雾弹已补充");
                }
            });
        }

        // 记录烟雾位置
        var smokePos = new Vector(@event.X, @event.Y, @event.Z);
        _toxicSmokes.TryAdd(smokePos, 0);

        Console.WriteLine($"[有毒烟雾弹] {player.PlayerName} 的有毒烟雾在 ({@event.X}, {@event.Y}, {@event.Z}) 爆炸");
        player.PrintToChat("☠️ 有毒烟雾已扩散！");
    }

    /// <summary>
    /// 给予玩家指定数量的烟雾弹
    /// </summary>
    private void GiveSmokeGrenades(CCSPlayerController player, int count)
    {
        if (player == null || !player.IsValid)
            return;

        try
        {
            // 给予烟雾弹
            for (int i = 0; i < count; i++)
            {
                player.GiveNamedItem("weapon_smokegrenade");
            }

            Console.WriteLine($"[有毒烟雾弹] 给予 {player.PlayerName} {count} 个烟雾弹");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[有毒烟雾弹] 给予烟雾弹时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理烟雾弹实体生成（修改烟雾颜色为紫色）
    /// 参考 jRandomSkills 实现
    /// 注意：MyrtleSkill.cs 已经做了所有检查，这里只需要设置颜色
    /// </summary>
    public void OnEntitySpawned(CEntityInstance entity)
    {
        try
        {
            // 使用 NextFrame 延迟设置颜色（参考 jRandomSkills）
            Server.NextFrame(() =>
            {
                var smoke = entity.As<CSmokeGrenadeProjectile>();
                if (smoke == null || !smoke.IsValid)
                    return;

                // 修改烟雾颜色为紫色（255, 0, 255）
                smoke.SmokeColor.X = 255; // R
                smoke.SmokeColor.Y = 0;   // G
                smoke.SmokeColor.Z = 255; // B

                Console.WriteLine($"[有毒烟雾弹] 烟雾颜色已设置为紫色");
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[有毒烟雾弹] OnEntitySpawned出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理烟雾弹消失事件
    /// </summary>
    public void OnSmokegrenadeExpired(EventSmokegrenadeExpired @event)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return;

        // 检查是否有有毒烟雾弹技能
        var skill = Plugin?.SkillManager.GetPlayerSkill(player);
        if (skill?.Name != "ToxicSmoke")
            return;

        // 移除对应的烟雾弹记录
        foreach (var smoke in _toxicSmokes.Keys.Where(v => v.X == @event.X && v.Y == @event.Y && v.Z == @event.Z))
        {
            _toxicSmokes.TryRemove(smoke, out _);
            Console.WriteLine($"[有毒烟雾弹] 有毒烟雾在 ({@event.X}, {@event.Y}, {@event.Z}) 消散");
        }
    }

    /// <summary>
    /// 每帧检查并造成伤害
    /// </summary>
    public void OnTick()
    {
        foreach (Vector smokePos in _toxicSmokes.Keys)
        {
            // 每17 tick造成一次伤害（约0.27秒，64tick服务器）
            if (Server.TickCount % 17 != 0)
                continue;

            foreach (var player in Utilities.GetPlayers())
            {
                if (player == null || !player.IsValid)
                    continue;

                var pawn = player.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid)
                    continue;

                if (pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                    continue;

                var playerPos = pawn.AbsOrigin;
                if (playerPos == null)
                    continue;

                // 计算距离
                float distance = GetDistance(smokePos, playerPos);

                if (distance <= SMOKE_RADIUS)
                {
                    ApplyDamage(pawn, player);
                }
            }
        }
    }

    /// <summary>
    /// 计算两点之间的距离
    /// </summary>
    private float GetDistance(Vector pos1, Vector pos2)
    {
        return (float)Math.Sqrt(
            Math.Pow(pos1.X - pos2.X, 2) +
            Math.Pow(pos1.Y - pos2.Y, 2) +
            Math.Pow(pos1.Z - pos2.Z, 2)
        );
    }

    /// <summary>
    /// 对玩家造成伤害
    /// </summary>
    private void ApplyDamage(CCSPlayerPawn pawn, CCSPlayerController player)
    {
        if (pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            return;

        // 造成伤害
        pawn.Health -= SMOKE_DAMAGE;

        // 通知状态改变
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        // 播放受伤音效
        pawn.EmitSound("Player.DamageBody.Onlooker");

        // 如果死亡
        if (pawn.Health <= 0)
        {
            Console.WriteLine($"[有毒烟雾弹] {player.PlayerName} 被毒死");
            pawn.CommitSuicide(false, true);
        }
    }

    /// <summary>
    /// 清理所有记录（回合结束时调用）
    /// </summary>
    public static void ClearAllToxicSmokes()
    {
        _toxicSmokes.Clear();
        Console.WriteLine("[有毒烟雾弹] 已清理所有有毒烟雾弹记录");
    }
}
