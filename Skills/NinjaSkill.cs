using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 明刀技能 - 被动技能
/// 首次受到致命伤害时触发，无敌0.5秒
/// </summary>
public class NinjaSkill : PlayerSkill
{
    public override string Name => "Ninja";
    public override string DisplayName => "🗡️ 明刀";
    public override string Description => "首伤触发无敌，落地留烟雾！";
    public override bool IsActive => false;
    public override float Cooldown => 0f;
    public override List<string> ExcludedEvents => new() { };

    // 追踪玩家状态
    private readonly Dictionary<uint, bool> _hasTriggered = new();
    private readonly Dictionary<uint, DateTime> _invincibleEndTime = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;
        _hasTriggered[slot] = false;
        _invincibleEndTime[slot] = DateTime.MinValue;

        Console.WriteLine($"[明刀] {player.PlayerName} 获得了明刀能力");
        player.PrintToChat("🗡️ 你获得了明刀技能！首伤触发无敌！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;
        _hasTriggered.Remove(slot);
        _invincibleEndTime.Remove(slot);

        Console.WriteLine($"[明刀] {player.PlayerName} 失去了明刀能力");
    }

    /// <summary>
    /// 检查玩家是否在无敌状态
    /// </summary>
    public bool IsInvincible(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return false;

        var slot = player.Index;

        if (!_hasTriggered.TryGetValue(slot, out var hasTriggered) || !hasTriggered)
            return false;

        if (!_invincibleEndTime.TryGetValue(slot, out var endTime))
            return false;

        return DateTime.Now <= endTime;
    }

    /// <summary>
    /// 处理玩家受伤事件
    /// </summary>
    public void OnPlayerHurtSkill(CCSPlayerController player, EventPlayerHurt @event)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        var slot = player.Index;

        if (_hasTriggered.TryGetValue(slot, out var hasTriggered) && hasTriggered)
            return;

        var health = pawn.Health;
        var damageHealth = @event.DmgHealth;
        bool isFatal = damageHealth >= health;

        if (isFatal)
        {
            Console.WriteLine($"[明刀] {player.PlayerName} 受到致命伤害，触发明刀效果！");
            _hasTriggered[slot] = true;
            _invincibleEndTime[slot] = DateTime.Now.AddSeconds(0.5);
            player.PrintToCenter("🗡️ 明刀护体！");

            // 在脚下生成烟雾弹
            CreateSmokeGrenadeAtPlayer(player);
        }
    }

    /// <summary>
    /// 检查伤害修改（无敌保护）
    /// </summary>
    public float? HandleDamagePre(CCSPlayerPawn player, CTakeDamageInfo info)
    {
        if (player == null)
            return null;

        var controller = player.Controller.Value;
        if (controller == null || !controller.IsValid)
            return null;

        var slot = controller.Index;

        if (!_hasTriggered.TryGetValue(slot, out var hasTriggered) || !hasTriggered)
            return null;

        if (_invincibleEndTime.TryGetValue(slot, out var endTime))
        {
            if (DateTime.Now <= endTime)
            {
                Console.WriteLine($"[明刀] {controller.PlayerName} 受到伤害但无敌，伤害归零");
                return 0f;
            }
        }

        return null;
    }

    /// <summary>
    /// 在玩家脚下生成烟雾弹
    /// </summary>
    private void CreateSmokeGrenadeAtPlayer(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        try
        {
            // 获取玩家位置
            var playerPos = pawn.AbsOrigin;
            if (playerPos == null)
                return;

            // 创建烟雾弹
            var smoke = Utilities.CreateEntityByName<CSmokeGrenade>("smokegrenade_projectile");
            if (smoke == null)
            {
                Console.WriteLine($"[明刀] 创建烟雾弹失败");
                return;
            }

            // 使用 Teleport 设置位置（稍微偏移到地面）
            var smokePos = new Vector(playerPos.X, playerPos.Y, playerPos.Z + 5.0f);
            smoke.Teleport(smokePos, new QAngle(0, 0, 0), new Vector(0, 0, 0));

            // 激活烟雾弹
            smoke.DispatchSpawn();

            Console.WriteLine($"[明刀] 在 {player.PlayerName} 脚下生成烟雾弹");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[明刀] 生成烟雾弹时出错: {ex.Message}");
        }
    }
}
