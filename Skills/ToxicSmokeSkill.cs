using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;

namespace MyrtleSkill.Skills;

/// <summary>
/// 有毒烟雾弹技能 - 主动技能
/// 开局获得3个有毒烟雾弹，烟雾范围内的敌人持续受到伤害
/// 参考实现：jRandomSkills ToxicSmoke
/// </summary>
public class ToxicSmokeSkill : PlayerSkill
{
    public override string Name => "ToxicSmoke";
    public override string DisplayName => "☠️ 有毒烟雾弹";
    public override string Description => "开局3个有毒烟雾弹，持续伤害敌人！";
    public override bool IsActive => true;
    public override float Cooldown => 9999f; // 一局只能用一次
    public override List<string> ExcludedEvents => new() { };

    // 追踪每回合是否已使用
    private readonly Dictionary<uint, bool> _usedThisRound = new();

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
        _usedThisRound[slot] = false;

        // 给予3个烟雾弹
        GiveSmokeGrenades(player, 3);

        Console.WriteLine($"[有毒烟雾弹] {player.PlayerName} 获得了有毒烟雾弹能力");
        player.PrintToChat("☠️ 你获得了3个有毒烟雾弹！烟雾持续伤害敌人！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;
        _usedThisRound.Remove(slot);

        Console.WriteLine($"[有毒烟雾弹] {player.PlayerName} 失去了有毒烟雾弹能力");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;

        // 检查本回合是否已使用
        if (_usedThisRound.TryGetValue(slot, out var used) && used)
        {
            player.PrintToCenter("❌ 本回合已使用过有毒烟雾弹！");
            player.PrintToChat("❌ 本回合已使用过有毒烟雾弹技能！");
            return;
        }

        // 给予3个烟雾弹
        GiveSmokeGrenades(player, 3);

        // 标记为已使用
        _usedThisRound[slot] = true;

        player.PrintToCenter("☠️ 获得了3个有毒烟雾弹！");
        player.PrintToChat("☠️ 投掷烟雾弹，范围内敌人持续受到伤害！");

        Console.WriteLine($"[有毒烟雾弹] {player.PlayerName} 使用了技能，获得3个烟雾弹");
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
    /// </summary>
    public void OnEntitySpawned(CEntityInstance entity)
    {
        var name = entity.DesignerName;
        if (name != "smokegrenade_projectile")
            return;

        var grenade = entity.As<CBaseCSGrenadeProjectile>();
        if (grenade == null || !grenade.IsValid)
            return;

        if (grenade.OwnerEntity == null || !grenade.OwnerEntity.IsValid || grenade.OwnerEntity.Value == null || !grenade.OwnerEntity.Value.IsValid)
            return;

        var pawn = grenade.OwnerEntity.Value.As<CCSPlayerPawn>();
        if (pawn == null || !pawn.IsValid)
            return;

        var controller = pawn.Controller.Value;
        if (controller == null || !controller.IsValid || !(controller is CCSPlayerController player))
            return;

        // 检查是否有有毒烟雾弹技能
        var skill = Plugin?.SkillManager.GetPlayerSkill(player);
        if (skill?.Name != "ToxicSmoke")
            return;

        // 修改烟雾颜色为紫色（255, 0, 255）
        Server.NextFrame(() =>
        {
            var smoke = entity.As<CSmokeGrenadeProjectile>();
            if (smoke != null && smoke.IsValid)
            {
                smoke.SmokeColor.X = 255; // R
                smoke.SmokeColor.Y = 0;   // G
                smoke.SmokeColor.Z = 255; // B
                Console.WriteLine($"[有毒烟雾弹] {player.PlayerName} 的烟雾弹已设置为紫色");
            }
        });
    }

    /// <summary>
    /// 处理烟雾弹爆炸事件
    /// </summary>
    public void OnSmokegrenadeDetonate(EventSmokegrenadeDetonate @event)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return;

        // 检查是否有有毒烟雾弹技能
        var skill = Plugin?.SkillManager.GetPlayerSkill(player);
        if (skill?.Name != "ToxicSmoke")
            return;

        var smokePos = new Vector(@event.X, @event.Y, @event.Z);
        _toxicSmokes.TryAdd(smokePos, 0);

        Console.WriteLine($"[有毒烟雾弹] {player.PlayerName} 的有毒烟雾在 ({@event.X}, {@event.Y}, {@event.Z}) 爆炸");
        player.PrintToChat("☠️ 有毒烟雾已扩散！");
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
