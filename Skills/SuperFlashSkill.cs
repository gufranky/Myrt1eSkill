using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 超级闪光技能 - 被动技能
/// 你的闪光弹总会让所有敌人屏幕变黑3秒（使用UserMessage Fade实现）
/// </summary>
public class SuperFlashSkill : PlayerSkill
{
    public override string Name => "SuperFlash";
    public override string DisplayName => "💥 超级闪光";
    public override string Description => "你的闪光弹会让所有敌人屏幕变黑3秒！无视距离和遮挡！";
    public override bool IsActive => false; // 被动技能

    // 与其他闪光弹技能互斥
    public override List<string> ExcludedSkills => new() { "AntiFlash", "FlashJump", "KillerFlash" };

    // 黑暗效果持续时间（秒）
    private const float DARKNESS_DURATION = 3.0f;

    // 黑暗参数（接近完全黑屏）
    private const int DARKNESS_R = 0;
    private const int DARKNESS_G = 0;
    private const int DARKNESS_B = 0;
    private const int DARKNESS_A = 255; // 完全不透明

    // UserMessage 参数
    private const int FADE_DURATION = 100; // 0.1秒渐变
    private const int FADE_HOLD_TIME = 3000; // 3秒保持（100ms单位）

    // 跟踪被施加黑暗效果的玩家
    private readonly Dictionary<int, DarknessState> _darknessStates = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[超级闪光] {player.PlayerName} 获得了超级闪光技能");

        // 给予1个闪光弹
        player.GiveNamedItem("weapon_flashbang");

        player.PrintToChat("💥 你获得了超级闪光技能！");
        player.PrintToChat("💡 你的闪光弹会让所有敌人屏幕变黑3秒！");
        player.PrintToChat("⚠️ 无视距离和遮挡！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[超级闪光] {player.PlayerName} 失去了超级闪光技能");
    }

    /// <summary>
    /// 处理闪光弹爆炸事件 - 让所有敌人屏幕变黑3秒（无视距离和遮挡）
    /// 参考 jRandomSkills Darkness 技能实现
    /// </summary>
    public void OnFlashbangDetonate(EventFlashbangDetonate @event)
    {
        var attacker = @event.Userid;
        if (attacker == null || !attacker.IsValid)
            return;

        Console.WriteLine($"[超级闪光] {attacker.PlayerName} 的闪光弹爆炸了！");

        // 延迟执行，确保在游戏引擎处理完闪光弹后再施加黑暗效果
        Server.NextFrame(() =>
        {
            int blindedCount = 0;

            // 让所有敌方玩家屏幕变黑（无视距离和遮挡）
            foreach (var player in Utilities.GetPlayers())
            {
                if (player == null || !player.IsValid || !player.PawnIsAlive)
                    continue;

                // 不闪自己
                if (player == attacker)
                    continue;

                // 只闪敌方玩家
                if (player.Team == attacker.Team)
                    continue;

                // 施加黑暗效果
                ApplyDarkness(player, DARKNESS_DURATION);
                blindedCount++;

                Console.WriteLine($"[超级闪光] {player.PlayerName} 被变黑 {DARKNESS_DURATION} 秒");
                player.PrintToCenter($"💥 被超级闪光弹致盲 {DARKNESS_DURATION} 秒！");
            }

            if (blindedCount > 0)
            {
                attacker.PrintToChat($"💥 超级闪光弹！{blindedCount} 个敌人屏幕变黑 {DARKNESS_DURATION} 秒！");
                Console.WriteLine($"[超级闪光] {attacker.PlayerName} 的闪光弹让 {blindedCount} 个敌人屏幕变黑");
            }
        });
    }

    /// <summary>
    /// 对玩家施加黑暗效果
    /// 使用 UserMessage Fade 效果（与 jRandomSkills Darkness 一致）
    /// </summary>
    private void ApplyDarkness(CCSPlayerController target, float duration)
    {
        if (target == null || !target.IsValid)
            return;

        // 移除旧的黑暗效果
        RemoveDarkness(target);

        // 使用 UserMessage Fade 施加黑色屏幕效果
        ApplyScreenColor(target, DARKNESS_R, DARKNESS_G, DARKNESS_B, DARKNESS_A, FADE_DURATION, FADE_HOLD_TIME);

        // 保存状态
        _darknessStates[target.Slot] = new DarknessState
        {
            TargetPlayer = target,
            EndTime = Server.CurrentTime + duration
        };

        Console.WriteLine($"[超级闪光] 对 {target.PlayerName} 施加黑暗，持续 {duration} 秒");
    }

    /// <summary>
    /// 移除玩家的黑暗效果
    /// </summary>
    private void RemoveDarkness(CCSPlayerController target)
    {
        if (target == null || !target.IsValid)
            return;

        if (!_darknessStates.TryGetValue(target.Slot, out var state))
            return;

        // 使用 UserMessage Fade 移除黑色屏幕效果
        ApplyScreenColor(target, 0, 0, 0, 0, 200, 0);

        _darknessStates.Remove(target.Slot);

        Console.WriteLine($"[超级闪光] 已移除 {target.PlayerName} 的黑暗效果");
    }

    /// <summary>
    /// 移除所有黑暗效果
    /// </summary>
    private void RemoveAllDarkness()
    {
        var toRemove = _darknessStates.Keys.ToList();

        foreach (var slot in toRemove)
        {
            if (_darknessStates.TryGetValue(slot, out var state))
            {
                RemoveDarkness(state.TargetPlayer);
            }
        }
    }

    /// <summary>
    /// 每帧更新（检查黑暗效果持续时间）
    /// </summary>
    public void OnTick()
    {
        var currentTime = Server.CurrentTime;
        var expiredSlots = new List<int>();

        foreach (var kvp in _darknessStates)
        {
            if (currentTime >= kvp.Value.EndTime)
            {
                expiredSlots.Add(kvp.Key);
            }
        }

        foreach (var slot in expiredSlots)
        {
            if (_darknessStates.TryGetValue(slot, out var state))
            {
                RemoveDarkness(state.TargetPlayer);
                state.TargetPlayer?.PrintToChat("💥 超级闪光效果已消退");
            }
        }
    }

    /// <summary>
    /// 应用屏幕颜色效果（使用 UserMessage Fade）
    /// 参考 jRandomSkills SkillUtils.ApplyScreenColor
    /// </summary>
    private void ApplyScreenColor(CCSPlayerController player, int r, int g, int b, int a, int duration, int holdTime)
    {
        if (player == null || !player.IsValid)
            return;

        using var msg = UserMessage.FromPartialName("Fade");
        if (msg == null)
            return;

        // 组装颜色值：A B G R (小端序)
        int packageColor = (a << 24) | (b << 16) | (g << 8) | r;

        msg.SetInt("duration", duration);
        msg.SetInt("hold_time", holdTime);
        msg.SetInt("flags", 1); // FFADE_IN
        msg.SetInt("color", packageColor);

        msg.Send(player);
    }

    /// <summary>
    /// 黑暗效果状态
    /// </summary>
    private class DarknessState
    {
        public CCSPlayerController TargetPlayer { get; set; } = null!;
        public float EndTime { get; set; }
    }
}
