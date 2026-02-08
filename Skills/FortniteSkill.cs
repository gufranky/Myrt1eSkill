// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using CS2TraceRay.Class;
using CS2TraceRay.Struct;
using System.Collections.Concurrent;

namespace MyrtleSkill.Skills;

/// <summary>
/// 堡垒之夜技能 - 主动技能
/// 点击 [css_useSkill] 创建一个可破坏的路障
/// </summary>
public class FortniteSkill : PlayerSkill
{
    public override string Name => "Fortnite";
    public override string DisplayName => "🏗️ 堡垒之夜";
    public override string Description => "点击 [css_useSkill] 创建可破坏的路障！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 2.0f; // 2秒冷却

    // 路障生命值
    private const int BARRICADE_HEALTH = 200;

    // 路障持续时间（秒，0 表示永久）
    private const float BARRICADE_DURATION = 30.0f;

    // 创建距离（玩家前方）
    private const float SPAWN_DISTANCE = 80.0f;

    // 路障模型（使用普通的木箱模型）
    private const string BARRICADE_MODEL = "models/props/de_dust/du_metal_chest_front.vmdl";

    // 跟踪所有创建的路障
    private static readonly ConcurrentDictionary<ulong, List<BarricadeInfo>> _playerBarricades = new();

    // 跟踪每个玩家的路障数量
    private readonly Dictionary<ulong, int> _barricadeCount = new();

    // 最大同时存在的路障数量
    private const int MAX_BARRICADES = 5;

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        _barricadeCount[player.SteamID] = 0;
        _playerBarricades.TryAdd(player.SteamID, new List<BarricadeInfo>());

        Console.WriteLine($"[堡垒之夜] {player.PlayerName} 获得了堡垒之夜技能");
        player.PrintToChat("🏗️ 你获得了堡垒之夜技能！");
        player.PrintToChat("💡 输入 !useskill 或按键激活！");
        player.PrintToChat($"📦 路障生命值：{BARRICADE_HEALTH}，持续时间：{BARRICADE_DURATION}秒");
        player.PrintToChat($"🚫 最多同时存在 {MAX_BARRICADES} 个路障");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 清除玩家的所有路障
        if (_playerBarricades.TryRemove(player.SteamID, out var barricades))
        {
            foreach (var barricade in barricades)
            {
                if (barricade.Prop != null && barricade.Prop.IsValid)
                {
                    barricade.Prop.AcceptInput("Kill");
                }
            }
        }

        _barricadeCount.Remove(player.SteamID);

        Console.WriteLine($"[堡垒之夜] {player.PlayerName} 失去了堡垒之夜技能");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid)
            return;

        // 获取当前路障数量
        if (_barricadeCount.TryGetValue(player.SteamID, out var count) && count >= MAX_BARRICADES)
        {
            player.PrintToCenter($"❌ 已达到最大路障数量 ({MAX_BARRICADES})！");
            player.PrintToChat($"❌ 已达到最大路障数量 ({MAX_BARRICADES})！等待现有路障消失");
            return;
        }

        // 计算创建位置
        Vector spawnPos = GetSpawnPosition(playerPawn);
        if (spawnPos == null)
        {
            player.PrintToCenter("❌ 无法在此处创建路障！");
            return;
        }

        // 创建路障
        CreateBarricade(player, spawnPos);
    }

    /// <summary>
    /// 获取路障创建位置
    /// </summary>
    private Vector GetSpawnPosition(CCSPlayerPawn playerPawn)
    {
        if (playerPawn.AbsOrigin == null || playerPawn.AbsRotation == null)
            return null;

        // 计算玩家前方的位置
        Vector forward = GetForwardVector(playerPawn.AbsRotation);
        Vector spawnPos = playerPawn.AbsOrigin + forward * SPAWN_DISTANCE;
        spawnPos.Z += 10.0f; // 稍微抬高一点，确保在地面上

        return spawnPos;
    }

    /// <summary>
    /// 创建路障
    /// </summary>
    private void CreateBarricade(CCSPlayerController player, Vector position)
    {
        // 创建动态实体（参考 jRandomSkills）
        var barricade = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
        if (barricade == null || !barricade.IsValid)
        {
            player.PrintToCenter("❌ 创建路障失败！");
            return;
        }

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid || playerPawn.AbsRotation == null)
            return;

        // 设置路障属性
        barricade.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;

        // 关键：清除 EF_NODRAW 标志位（参考 jRandomSkills）
        if (barricade.CBodyComponent?.SceneNode?.Owner?.Entity != null)
        {
            barricade.CBodyComponent.SceneNode.Owner.Entity.Flags = (uint)(barricade.CBodyComponent.SceneNode.Owner.Entity.Flags & ~(1 << 2));
        }

        // 设置路障名称（使用 Globalname）
        string barricadeName = $"FortniteWall_{Server.TickCount}";
        barricade.Entity!.Name = barricadeName;
        barricade.Globalname = barricadeName;

        // 生成实体
        barricade.DispatchSpawn();

        Server.NextFrame(() =>
        {
            if (!barricade.IsValid)
                return;

            try
            {
                // 设置模型
                barricade.SetModel(BARRICADE_MODEL);

                // 计算角度（参考 jRandomSkills）
                QAngle angles = new QAngle(
                    playerPawn.AbsRotation.X,
                    playerPawn.V_angle.Y + 90,
                    playerPawn.AbsRotation.Z
                );

                // 设置位置和旋转（3个参数）
                barricade.Teleport(position, angles, new Vector(0, 0, 0));

                // 添加到玩家的路障列表（使用 Index 作为键）
                if (_playerBarricades.TryGetValue(player.SteamID, out var barricades))
                {
                    var info = new BarricadeInfo
                    {
                        Prop = barricade,
                        Index = barricade.Index,
                        CreateTime = Server.CurrentTime,
                        Health = BARRICADE_HEALTH
                    };
                    barricades.Add(info);

                    // 更新计数
                    _barricadeCount[player.SteamID] = barricades.Count;
                }

                Console.WriteLine($"[堡垒之夜] {player.PlayerName} 创建了路障，位置：({position.X}, {position.Y}, {position.Z})");

                player.PrintToChat("🏗️ 路障已创建！");
                player.PrintToCenter($"🏗️ 路障已创建！生命值：{BARRICADE_HEALTH}");

                // 播放音效（使用木头音效）
                player.EmitSound("Wood_Plank.BulletImpact");

                // 设置持续时间后自动销毁
                Plugin?.AddTimer(BARRICADE_DURATION, () =>
                {
                    if (barricade.IsValid)
                    {
                        RemoveBarricade(player, barricade);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[堡垒之夜] 创建路障时出错: {ex.Message}");
                barricade.AcceptInput("Kill");
            }
        });
    }

    /// <summary>
    /// 移除路障
    /// </summary>
    private void RemoveBarricade(CCSPlayerController player, CDynamicProp barricade)
    {
        if (!_playerBarricades.TryGetValue(player.SteamID, out var barricades))
            return;

        var info = barricades.FirstOrDefault(b => b.Prop == barricade);
        if (info != null)
        {
            barricades.Remove(info);
            _barricadeCount[player.SteamID] = barricades.Count;
        }

        if (barricade.IsValid)
        {
            barricade.AcceptInput("Kill");
        }

        Console.WriteLine($"[堡垒之夜] {player.PlayerName} 的路障已被移除");
    }

    /// <summary>
    /// 处理路障受到伤害
    /// </summary>
    public static void HandleBarricadeDamage(CEntityInstance entity, CTakeDamageInfo damageInfo)
    {
        if (entity?.Entity == null || entity.Entity.Name == null)
            return;

        // 检查是否是 FortniteWall（使用 jRandomSkills 的命名）
        if (!entity.Entity.Name.StartsWith("FortniteWall"))
            return;

        var barricade = entity.As<CDynamicProp>();
        if (barricade == null || !barricade.IsValid)
            return;

        // 播放木头音效
        barricade.EmitSound("Wood_Plank.BulletImpact", volume: 1.0f);

        // 查找路障（遍历所有玩家的路障列表）
        BarricadeInfo? targetBarricade = null;
        CCSPlayerController? targetPlayer = null;

        foreach (var kvp in _playerBarricades)
        {
            var info = kvp.Value.FirstOrDefault(b => b.Index == barricade.Index);
            if (info != null)
            {
                targetBarricade = info;
                targetPlayer = Utilities.GetPlayerFromSteamId(kvp.Key);
                break;
            }
        }

        if (targetBarricade == null)
        {
            // 找不到记录，直接销毁
            barricade.AcceptInput("Kill");
            return;
        }

        // 计算伤害
        float damage = damageInfo.Damage;
        targetBarricade.Health -= (int)damage;

        Console.WriteLine($"[堡垒之夜] 路障受到 {damage} 点伤害，剩余生命值：{targetBarricade.Health}");

        // 检查是否销毁
        if (targetBarricade.Health <= 0)
        {
            barricade.AcceptInput("Kill");

            // 从列表中移除
            if (targetPlayer != null && _playerBarricades.TryGetValue(targetPlayer.SteamID, out var barricades))
            {
                barricades.Remove(targetBarricade);
            }

            // 通知玩家
            if (targetPlayer != null && targetPlayer.IsValid)
            {
                targetPlayer.PrintToChat("💥 你的路障被摧毁了！");
            }

            Console.WriteLine($"[堡垒之夜] 路障被摧毁");
        }
    }

    /// <summary>
    /// 计算前方向量
    /// </summary>
    private Vector GetForwardVector(QAngle angles)
    {
        float radiansY = angles.Y * (float)Math.PI / 180.0f;

        return new Vector(
            (float)Math.Cos(radiansY),
            (float)Math.Sin(radiansY),
            0
        );
    }

    /// <summary>
    /// 清理所有路障（回合结束时调用）
    /// </summary>
    public static void ClearAllBarricades()
    {
        foreach (var barricades in _playerBarricades.Values)
        {
            foreach (var barricade in barricades)
            {
                if (barricade.Prop != null && barricade.Prop.IsValid)
                {
                    barricade.Prop.AcceptInput("Kill");
                }
            }
        }

        _playerBarricades.Clear();
        Console.WriteLine("[堡垒之夜] 已清理所有路障");
    }

    /// <summary>
    /// 路障信息
    /// </summary>
    private class BarricadeInfo
    {
        public CDynamicProp? Prop { get; set; }
        public uint Index { get; set; }
        public float CreateTime { get; set; }
        public int Health { get; set; }
    }
}
