// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills Fortnite skill

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;

namespace MyrtleSkill.Skills;

/// <summary>
/// 堡垒之夜技能 - 主动技能
/// 点击 [css_useSkill] 创建一个可破坏的路障
/// 完全复制自 jRandomSkills Fortnite
/// </summary>
public class FortniteSkill : PlayerSkill
{
    public override string Name => "Fortnite";
    public override string DisplayName => "🏗️ 堡垒之夜";
    public override string Description => "点击 [css_useSkill] 创建可破坏的路障！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 2.0f; // 2秒冷却（与 jRandomSkills 一致）

    // 路障生命值（与 jRandomSkills 一致）
    private const int BARRICADE_HEALTH = 115;

    // 路障模型（与 jRandomSkills 完全一致）
    private const string BARRICADE_MODEL = "models/props/de_aztec/hr_aztec/aztec_scaffolding/aztec_scaffold_wall_support_128.vmdl";

    // 创建距离（与 jRandomSkills 一致）
    private const float SPAWN_DISTANCE = 50.0f;

    // 跟踪所有路障的生命值
    private static readonly ConcurrentDictionary<uint, int> _barricades = new();

    // 静态构造函数
    static FortniteSkill()
    {
        Console.WriteLine("[堡垒之夜] 初始化技能");
    }

    /// <summary>
    /// 注册模型到资源清单（在插件 Load 时调用）
    /// </summary>
    public static void RegisterModel()
    {
        // 将模型添加到资源清单，确保在服务器启动时预加载
        MyrtleSkill.Instance?.AddToManifest(BARRICADE_MODEL);
        Console.WriteLine("[堡垒之夜] 已添加模型到资源清单: " + BARRICADE_MODEL);
    }

    /// <summary>
    /// 预加载模型资源（已弃用 - 使用 ResourceManifest 系统）
    /// </summary>
    [Obsolete("使用 ResourceManifest 系统代替")]
    public static void PrecacheModel()
    {
        Server.PrecacheModel(BARRICADE_MODEL);
        Console.WriteLine("[堡垒之夜] 模型已预加载: " + BARRICADE_MODEL);
    }

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[堡垒之夜] {player.PlayerName} 获得了堡垒之夜技能");

        player.PrintToChat("🏗️ 你获得了堡垒之夜技能！");
        player.PrintToChat("💡 输入 !useskill 或按键激活！");
        player.PrintToChat($"📦 路障生命值：{BARRICADE_HEALTH}");
        player.PrintToChat($"⏱️ 冷却时间：{Cooldown}秒");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[堡垒之夜] {player.PlayerName} 失去了堡垒之夜技能");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid)
            return;

        Console.WriteLine($"[堡垒之夜] {player.PlayerName} 激活了堡垒之夜技能");

        // 创建路障
        CreateBox(player);
    }

    /// <summary>
    /// 创建路障（完全复制 jRandomSkills Fortnite.CreateBox）
    /// </summary>
    private void CreateBox(CCSPlayerController player)
    {
        var playerPawn = player.PlayerPawn.Value;
        var box = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
        if (box == null || playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null || playerPawn.AbsRotation == null)
            return;

        // 计算位置和角度（完全复制 jRandomSkills）
        float distance = 50;
        Vector pos = playerPawn.AbsOrigin + GetForwardVector(playerPawn.AbsRotation) * distance;
        QAngle angle = new QAngle(playerPawn.AbsRotation.X, playerPawn.V_angle.Y + 90, playerPawn.AbsRotation.Z);

        // 设置路障属性（完全复制 jRandomSkills）
        box.Entity!.Name = box.Globalname = $"FortniteWall_{Server.TickCount}";
        box.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
        box.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(box.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));

        // 生成实体
        box.DispatchSpawn();

        // 记录路障生命值
        _barricades.TryAdd(box.Index, BARRICADE_HEALTH);

        // 下一帧设置模型和位置（完全复制 jRandomSkills）
        Server.NextFrame(() =>
        {
            if (!box.IsValid)
                return;

            box.SetModel(BARRICADE_MODEL);
            box.Teleport(pos, angle, null);

            Console.WriteLine($"[堡垒之夜] {player.PlayerName} 创建了路障，位置：({pos.X}, {pos.Y}, {pos.Z})");

            player.PrintToChat("🏗️ 路障已创建！");
            player.PrintToCenter($"🏗️ 路障已创建！生命值：{BARRICADE_HEALTH}");

            // 播放音效
            player.EmitSound("Wood_Plank.BulletImpact");
        });
    }

    /// <summary>
    /// 处理路障受到伤害（完全复制 jRandomSkills Fortnite.OnTakeDamage）
    /// </summary>
    public static void HandleBarricadeDamage(CEntityInstance entity, CTakeDamageInfo damageInfo)
    {
        if (entity == null || entity.Entity == null || string.IsNullOrEmpty(entity.Entity.Name))
            return;

        if (!entity.Entity.Name.StartsWith("FortniteWall"))
            return;

        var box = entity.As<CDynamicProp>();
        if (box == null || !box.IsValid)
            return;

        // 播放木头音效
        box.EmitSound("Wood_Plank.BulletImpact", volume: 1.0f);

        // 计算伤害
        if (_barricades.TryGetValue(box.Index, out int health))
        {
            health -= (int)damageInfo.Damage;
            _barricades.AddOrUpdate(box.Index, health, (k, v) => health);

            Console.WriteLine($"[堡垒之夜] 路障受到 {damageInfo.Damage} 点伤害，剩余生命值：{health}");

            if (health <= 0)
            {
                box.AcceptInput("Kill");
                _barricades.TryRemove(box.Index, out _);
                Console.WriteLine($"[堡垒之夜] 路障被摧毁");
            }
        }
        else
        {
            box.AcceptInput("Kill");
        }
    }

    /// <summary>
    /// 清理所有路障（回合结束时调用）
    /// </summary>
    public static void ClearAllBarricades()
    {
        foreach (var kvp in _barricades)
        {
            var box = Utilities.GetEntityFromIndex<CDynamicProp>((int)kvp.Key);
            if (box != null && box.IsValid)
            {
                box.AcceptInput("Kill");
            }
        }

        _barricades.Clear();
        Console.WriteLine("[堡垒之夜] 已清理所有路障");
    }

    /// <summary>
    /// 计算前方向量（参考 jRandomSkills SkillUtils.GetForwardVector）
    /// </summary>
    private static Vector GetForwardVector(QAngle angles)
    {
        float radiansY = angles.Y * (float)Math.PI / 180.0f;

        return new Vector(
            (float)Math.Cos(radiansY),
            (float)Math.Sin(radiansY),
            0
        );
    }
}
