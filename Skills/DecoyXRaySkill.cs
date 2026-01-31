using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;

namespace MyrtleSkill.Skills;

/// <summary>
/// 透视诱饵弹技能 - 主动技能
/// 开局获得3个诱饵弹，诱饵弹落地立即爆炸并显示范围内所有敌人
/// </summary>
public class DecoyXRaySkill : PlayerSkill
{
    public override string Name => "DecoyXRay";
    public override string DisplayName => "💣 透视诱饵弹";
    public override string Description => "开局3个诱饵弹，爆炸显示敌人位置！";
    public override bool IsActive => true;
    public override float Cooldown => 9999f; // 一局只能用一次
    public override List<string> ExcludedEvents => new() { "Xray", "SuperpowerXray" }; // 与全员透视事件互斥

    // 追踪每回合是否已使用
    private readonly Dictionary<uint, bool> _usedThisRound = new();

    // 透视范围半径
    private const float XRAY_RANGE = 500.0f;

    // 透视持续时间（秒）
    private const float XRAY_DURATION = 10.0f;

    // 追踪活跃的诱饵弹
    private readonly Dictionary<int, CDecoyGrenade> _activeDecoys = new();

    // 追踪发光效果的敌人
    private readonly Dictionary<int, (int relayIndex, int glowIndex)> _glowingEnemies = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;
        _usedThisRound[slot] = false;

        // 给予3个诱饵弹
        GiveDecoyGrenades(player, 3);

        Console.WriteLine($"[透视诱饵弹] {player.PlayerName} 获得了透视诱饵弹能力");
        player.PrintToChat("💣 你获得了3个透视诱饵弹！投掷后显示范围敌人！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;
        _usedThisRound.Remove(slot);

        Console.WriteLine($"[透视诱饵弹] {player.PlayerName} 失去了透视诱饵弹能力");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;

        // 检查本回合是否已使用
        if (_usedThisRound.TryGetValue(slot, out var used) && used)
        {
            player.PrintToCenter("❌ 本回合已使用过透视诱饵弹！");
            player.PrintToChat("❌ 本回合已使用过透视诱饵弹技能！");
            return;
        }

        // 给予3个诱饵弹
        GiveDecoyGrenades(player, 3);

        // 标记为已使用
        _usedThisRound[slot] = true;

        player.PrintToCenter("💣 获得了3个透视诱饵弹！");
        player.PrintToChat("💣 投掷诱饵弹，爆炸后显示范围内敌人位置！");

        Console.WriteLine($"[透视诱饵弹] {player.PlayerName} 使用了技能，获得3个诱饵弹");
    }

    /// <summary>
    /// 给予玩家指定数量的诱饵弹
    /// </summary>
    private void GiveDecoyGrenades(CCSPlayerController player, int count)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null)
            return;

        try
        {
            // 给予诱饵弹
            for (int i = 0; i < count; i++)
            {
                player.GiveNamedItem("weapon_decoy");
            }

            Console.WriteLine($"[透视诱饵弹] 给予 {player.PlayerName} {count} 个诱饵弹");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[透视诱饵弹] 给予诱饵弹时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理诱饵弹投掷事件
    /// </summary>
    public void OnDecoyThrown(CCSPlayerController player, CDecoyGrenade decoy)
    {
        if (player == null || !decoy.IsValid)
            return;

        Console.WriteLine($"[透视诱饵弹] {player.PlayerName} 投掷了透视诱饵弹");

        // 记录诱饵弹
        _activeDecoys[(int)decoy.Index] = decoy;

        // 设置诱饵弹立即爆炸（落地就爆炸）
        Plugin?.AddTimer(0.1f, () =>
        {
            TriggerDecoyExplosion(player, decoy);
        });
    }

    /// <summary>
    /// 触发诱饵弹爆炸并应用透视效果
    /// </summary>
    private void TriggerDecoyExplosion(CCSPlayerController owner, CDecoyGrenade decoy)
    {
        if (!decoy.IsValid)
            return;

        var decoyPos = decoy.AbsOrigin;
        if (decoyPos == null)
            return;

        Console.WriteLine($"[透视诱饵弹] 诱饵弹在位置 {decoyPos} 爆炸");

        // 移除诱饵弹实体
        decoy.Remove();
        _activeDecoys.Remove((int)decoy.Index);

        // 找到范围内的所有敌人
        var enemiesInRange = FindEnemiesInRange(owner, decoyPos, XRAY_RANGE);

        // 为范围内的敌人添加发光效果
        foreach (var enemy in enemiesInRange)
        {
            ApplyGlowToEnemy(enemy, owner);
        }

        // 显示爆炸效果
        ShowExplosionEffect(decoyPos);

        // 通知所有人
        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsValid)
            {
                p.PrintToChat($"💣 {owner.PlayerName} 的透视诱饵弹爆炸！{enemiesInRange.Count} 个敌人被标记！");
            }
        }

        // 持续一段时间后移除发光效果
        Plugin?.AddTimer(XRAY_DURATION, () =>
        {
            RemoveGlowEffects();
        });
    }

    /// <summary>
    /// 找到指定范围内的所有敌人
    /// </summary>
    private List<CCSPlayerController> FindEnemiesInRange(CCSPlayerController owner, Vector position, float range)
    {
        var enemies = new List<CCSPlayerController>();

        if (owner == null || !owner.IsValid)
            return enemies;

        var ownerTeam = owner.Team;

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive)
                continue;

            // 跳过同队玩家
            if (player.Team == ownerTeam)
                continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            var playerPos = pawn.AbsOrigin;
            if (playerPos == null)
                continue;

            // 计算距离
            float distance = (float)Math.Sqrt(
                Math.Pow(position.X - playerPos.X, 2) +
                Math.Pow(position.Y - playerPos.Y, 2) +
                Math.Pow(position.Z - playerPos.Z, 2)
            );

            if (distance <= range)
            {
                enemies.Add(player);
                Console.WriteLine($"[透视诱饵弹] 发现敌人 {player.PlayerName}，距离: {distance:F2}");
            }
        }

        return enemies;
    }

    /// <summary>
    /// 为敌人添加发光效果
    /// </summary>
    private void ApplyGlowToEnemy(CCSPlayerController enemy, CCSPlayerController owner)
    {
        if (enemy == null || !enemy.IsValid)
            return;

        var pawn = enemy.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        try
        {
            // 使用CreateGlowEffect添加发光
            bool success = ApplyEntityGlowEffect(pawn, enemy.Team, out var relayIndex, out var glowIndex);
            if (success)
            {
                _glowingEnemies[enemy.Slot] = (relayIndex, glowIndex);
                Console.WriteLine($"[透视诱饵弹] 为 {enemy.PlayerName} 添加发光效果");

                // 注册CheckTransmit监听器
                if (Plugin != null && _glowingEnemies.Count == 1)
                {
                    Plugin.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[透视诱饵弹] 添加发光效果时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 移除所有发光效果
    /// </summary>
    private void RemoveGlowEffects()
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

        _glowingEnemies.Clear();
        Console.WriteLine($"[透视诱饵弹] 已移除所有发光效果");

        // 移除CheckTransmit监听器
        Plugin?.RemoveListener<Listeners.CheckTransmit>(OnCheckTransmit);
    }

    /// <summary>
    /// 检查传输时控制发光效果的可见性
    /// </summary>
    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (_glowingEnemies.Count == 0)
            return;

        foreach (var (info, receiver) in infoList)
        {
            if (receiver == null || !receiver.IsValid)
                continue;

            // 让所有队友都能看到发光的敌人
            foreach (var slot in _glowingEnemies.Keys)
            {
                var (relayIndex, glowIndex) = _glowingEnemies[slot];

                var relay = Utilities.GetEntityFromIndex<CDynamicProp>(relayIndex);
                var glow = Utilities.GetEntityFromIndex<CDynamicProp>(glowIndex);

                if (relay != null && relay.IsValid)
                {
                    info.TransmitEntities.Add(relay.Index);
                }

                if (glow != null && glow.IsValid)
                {
                    info.TransmitEntities.Add(glow.Index);
                }
            }
        }
    }

    /// <summary>
    /// 显示爆炸效果
    /// </summary>
    private void ShowExplosionEffect(Vector position)
    {
        try
        {
            // 创建粒子效果
            var particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
            if (particle != null && particle.IsValid)
            {
                particle.Teleport(position, new QAngle(0, 0, 0), new Vector(0, 0, 0));
                particle.EffectName = "explosion_c4_500"; // 使用C4爆炸效果
                particle.DispatchSpawn();
                particle.AcceptInput("Start");

                // 5秒后移除
                Plugin?.AddTimer(5.0f, () =>
                {
                    particle.Remove();
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[透视诱饵弹] 显示爆炸效果时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 复用发光效果方法（从SuperpowerXrayEvent复制）
    /// </summary>
    private bool ApplyEntityGlowEffect(CBaseEntity entity, CsTeam team, out int relayIndex, out int glowIndex)
    {
        relayIndex = -1;
        glowIndex = -1;

        try
        {
            // 创建Relay实体
            var relay = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
            if (relay == null || !relay.IsValid)
                return false;

            var origin = entity.AbsOrigin ?? new Vector(0, 0, 0);
            relay.Teleport(origin, new QAngle(0, 0, 0), new Vector(0, 0, 0));
            relay.DispatchSpawn();
            relay.AcceptInput("Enable");

            // 创建Glow实体
            var glow = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
            if (glow == null || !glow.IsValid)
            {
                relay.Remove();
                return false;
            }

            var glowOrigin = entity.AbsOrigin ?? new Vector(0, 0, 0);
            glow.Teleport(glowOrigin, new QAngle(0, 0, 0), new Vector(0, 0, 0));

            // 设置发光颜色
            Color glowColor = team == CsTeam.Terrorist ? Color.FromArgb(255, 255, 0, 0) : Color.FromArgb(255, 0, 0, 255);
            glow.Render = Color.FromArgb(255, glowColor.R, glowColor.G, glowColor.B);

            glow.DispatchSpawn();
            glow.AcceptInput("Enable");

            relayIndex = (int)relay.Index;
            glowIndex = (int)glow.Index;

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[透视诱饵弹] 应用发光效果时出错: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 清理所有记录（回合结束时调用）
    /// </summary>
    public static void ClearAllDecoys()
    {
        Console.WriteLine("[透视诱饵弹] 已清理所有透视诱饵弹记录");
    }
}
