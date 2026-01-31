using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 爆炸射击技能 - 射击时有随机几率发射爆炸子弹
/// </summary>
public class ExplosiveShotSkill : PlayerSkill
{
    public override string Name => "ExplosiveShot";
    public override string DisplayName => "💥 爆炸射击";
    public override string Description => "射击时有20%-30%几率在目标位置引发爆炸！";
    public override bool IsActive => false; // 被动技能

    // 爆炸概率范围
    private const float CHANCE_FROM = 0.2f; // 20%
    private const float CHANCE_TO = 0.3f;   // 30%

    // 爆炸伤害和半径
    private const float EXPLOSION_DAMAGE = 25.0f;
    private const float EXPLOSION_RADIUS = 210.0f;

    // 特殊角度用于识别自己创建的爆炸
    private static readonly QAngle IDENTIFIER_ANGLE = new QAngle(5, 10, -4);

    // 防止同一tick重复触发
    private static int _lastTick = 0;

    // 静态随机数生成器（用于HandlePlayerDamagePre静态方法）
    private static readonly Random _staticRandom = new();

    // 每个玩家的爆炸概率
    private static readonly Dictionary<ulong, float> _playerChances = new();

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[爆炸射击] {player.PlayerName} 获得了爆炸射击技能");

        // 为玩家随机分配一个概率
        float chance = (float)(_staticRandom.NextDouble() * (CHANCE_TO - CHANCE_FROM)) + CHANCE_FROM;
        _playerChances[player.SteamID] = chance;

        player.PrintToChat("💥 你获得了爆炸射击技能！");
        player.PrintToChat($"💡 射击时有{chance * 100:F0}%几率引发爆炸！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        Console.WriteLine($"[爆炸射击] {player.PlayerName} 失去了爆炸射击技能");
        _playerChances.Remove(player.SteamID);
    }

    /// <summary>
    /// 处理玩家伤害前事件
    /// </summary>
    public static void HandlePlayerDamagePre(CCSPlayerPawn player, CTakeDamageInfo info)
    {
        // 防止同一tick重复触发
        if (_lastTick == Server.TickCount)
            return;

        // 检查伤害来源
        if (info == null || info.Attacker == null || info.Attacker.Value == null)
            return;

        var attackerPawn = new CCSPlayerPawn(info.Attacker.Value.Handle);
        if (attackerPawn == null || !attackerPawn.IsValid)
            return;

        // 检查是否是玩家造成的伤害
        if (attackerPawn.DesignerName != "player")
            return;

        if (attackerPawn.Controller == null || attackerPawn.Controller.Value == null)
            return;

        var attacker = attackerPawn.Controller.Value.As<CCSPlayerController>();
        if (attacker == null || !attacker.IsValid)
            return;

        // 检查攻击者是否有爆炸射击技能
        if (!_playerChances.TryGetValue(attacker.SteamID, out float chance))
            return;

        // 20%-30%概率触发爆炸
        if (_staticRandom.NextDouble() > chance)
            return;

        // 获取伤害位置
        var damagePosition = info.DamagePosition;

        Console.WriteLine($"[爆炸射击] {attacker.PlayerName} 的射击触发了爆炸效果");

        // 创建爆炸
        SpawnExplosion(damagePosition);

        attacker.PrintToChat($"💥 你的射击引发了爆炸！");
    }

    /// <summary>
    /// 创建爆炸
    /// </summary>
    private static void SpawnExplosion(Vector position)
    {
        _lastTick = Server.TickCount;
        CreateHEGrenadeProjectile(position, IDENTIFIER_ANGLE, new Vector(0, 0, 0), 0);
        Console.WriteLine($"[爆炸射击] 在位置 ({position.X:F1}, {position.Y:F1}, {position.Z:F1}) 创建了爆炸");
    }

    /// <summary>
    /// 处理实体生成事件
    /// </summary>
    public static void OnEntitySpawned(CEntityInstance entity)
    {
        if (entity.DesignerName != "hegrenade_projectile")
            return;

        var heProjectile = entity.As<CBaseCSGrenadeProjectile>();
        if (heProjectile == null || !heProjectile.IsValid || heProjectile.AbsRotation == null)
            return;

        Server.NextFrame(() =>
        {
            if (heProjectile == null || !heProjectile.IsValid)
                return;

            // 检查是否是我们创建的爆炸（通过特殊角度识别）
            if (!NearlyEquals(IDENTIFIER_ANGLE.X, heProjectile.AbsRotation.X) ||
                !NearlyEquals(IDENTIFIER_ANGLE.Y, heProjectile.AbsRotation.Y) ||
                !NearlyEquals(IDENTIFIER_ANGLE.Z, heProjectile.AbsRotation.Z))
                return;

            // 修改爆炸属性
            heProjectile.TicksAtZeroVelocity = 100;
            heProjectile.TeamNum = (byte)CsTeam.None; // 中立伤害
            heProjectile.Damage = EXPLOSION_DAMAGE;
            heProjectile.DmgRadius = EXPLOSION_RADIUS;
            heProjectile.DetonateTime = 0; // 立即爆炸

            Console.WriteLine($"[爆炸射击] 修改手雷属性：伤害={EXPLOSION_DAMAGE}，半径={EXPLOSION_RADIUS}");
        });
    }

    /// <summary>
    /// 浮点数近似相等判断
    /// </summary>
    private static bool NearlyEquals(float a, float b, float epsilon = 0.001f)
    {
        return Math.Abs(a - b) < epsilon;
    }

    /// <summary>
    /// 创建HE手雷弹道
    /// </summary>
    private static void CreateHEGrenadeProjectile(Vector pos, QAngle angle, Vector vel, int teamNum)
    {
        try
        {
            var function = new MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int>(
                GameData.GetSignature("HEGrenadeProjectile_CreateFunc")
            );
            function.Invoke(pos.Handle, angle.Handle, vel.Handle, vel.Handle, IntPtr.Zero, IntPtr.Zero, teamNum);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[爆炸射击] 创建HE手雷失败: {ex.Message}");
        }
    }
}
