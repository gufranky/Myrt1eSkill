using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MyrtleSkill.Skills;

/// <summary>
/// 明刀技能 - 被动技能
/// 首次受到致命伤害时触发，无敌0.5秒后在地上留烟雾弹
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
    private readonly Dictionary<int, bool> _hasTriggered = new();
    private readonly Dictionary<int, DateTime> _invincibleEndTime = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        _hasTriggered[player.Slot] = false;
        _invincibleEndTime[player.Slot] = DateTime.MinValue;

        Console.WriteLine($"[明刀] {player.PlayerName} 获得了明刀能力");
        player.PrintToChat("🗡️ 你获得了明刀技能！首伤触发无敌！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        _hasTriggered.Remove(player.Slot);
        _invincibleEndTime.Remove(player.Slot);

        Console.WriteLine($"[明刀] {player.PlayerName} 失去了明刀能力");
    }

    /// <summary>
    /// 检查玩家是否在无敌状态
    /// </summary>
    public bool IsInvincible(CCSPlayerController player)
    {
        if (!_hasTriggered.TryGetValue(player.Slot, out var hasTriggered) || !hasTriggered)
            return false;

        if (!_invincibleEndTime.TryGetValue(player.Slot, out var endTime))
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

        if (_hasTriggered.TryGetValue(player.Slot, out var hasTriggered) && hasTriggered)
            return;

        var health = pawn.Health;
        var damageHealth = @event.DmgHealth;
        bool isFatal = damageHealth >= health;

        if (isFatal)
        {
            Console.WriteLine($"[明刀] {player.PlayerName} 受到致命伤害，触发明刀效果！");
            _hasTriggered[player.Slot] = true;
            _invincibleEndTime[player.Slot] = DateTime.Now.AddSeconds(0.5);
            player.PrintToCenter("🗡️ 明刀护体！");
        }
    }

    /// <summary>
    /// 检查伤害修改（无敌保护）
    /// </summary>
    public float? HandleDamagePre(CCSPlayerPawn player)
    {
        if (player == null || !player.Controller.IsValid)
            return null;

        var controller = player.Controller;

        if (!_hasTriggered.TryGetValue(controller.Slot, out var hasTriggered) || !hasTriggered)
            return null;

        if (_invincibleEndTime.TryGetValue(controller.Slot, out var endTime))
        {
            if (DateTime.Now <= endTime)
            {
                Console.WriteLine($"[明刀] {controller.PlayerName} 受到伤害但无敌，伤害归零");
                return 0f;
            }
        }

        return null;
    }
}
