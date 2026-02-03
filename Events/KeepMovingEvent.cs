using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace MyrtleSkill;

/// <summary>
/// 永动机事件 - 所有玩家必须持续按住 W 键，否则每 0.75 秒扣 10 滴血
/// </summary>
public class KeepMovingEvent : EntertainmentEvent
{
    public override string Name => "KeepMoving";
    public override string DisplayName => "🏃 永动机";
    public override string Description => "所有玩家必须持续按住 W 键！没按住的话每 0.75 秒扣 10 滴血！";

    // 伤害参数
    private const float DAMAGE_INTERVAL = 0.75f; // 伤害间隔（秒）
    private const int DAMAGE_AMOUNT = 10;         // 每次伤害量
    private const float GRACE_PERIOD = 3.0f;      // 宽限期（秒）

    // 每个玩家的状态跟踪
    private readonly Dictionary<int, PlayerKeepMovingState> _playerStates = new();

    public override void OnApply()
    {
        Console.WriteLine("[永动机] 事件已激活");

        // 初始化所有存活玩家的状态
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid && player.PawnIsAlive)
            {
                _playerStates[player.Slot] = new PlayerKeepMovingState
                {
                    GraceTimeRemaining = GRACE_PERIOD,
                    TimeSinceLastDamage = 0f
                };

                player.PrintToChat("🏃 永动机事件已激活！");
                player.PrintToChat("⚠️ 必须持续按住 W 键！");
                player.PrintToChat($"💡 {GRACE_PERIOD:F0} 秒后开始检测！");
            }
        }

        // 全局提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToCenter("🏃 永动机模式！\n按住 W 键或受到持续伤害！");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[永动机] 事件已恢复");

        // 清理所有玩家状态
        _playerStates.Clear();

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("🏃 永动机模式已结束");
            }
        }
    }

    /// <summary>
    /// 每帧更新（在 MyrtleSkill 的 OnServerPostEntityThink 中调用）
    /// </summary>
    public void OnTick()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive)
                continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            // 获取或创建玩家状态
            if (!_playerStates.TryGetValue(player.Slot, out var state))
            {
                state = new PlayerKeepMovingState
                {
                    GraceTimeRemaining = GRACE_PERIOD,
                    TimeSinceLastDamage = 0f
                };
                _playerStates[player.Slot] = state;
            }

            HandleKeepMoving(player, pawn, state);
        }
    }

    /// <summary>
    /// 处理永动机逻辑
    /// </summary>
    private void HandleKeepMoving(CCSPlayerController player, CCSPlayerPawn pawn, PlayerKeepMovingState state)
    {
        // 获取当前按钮状态
        var buttons = player.Buttons;
        bool isHoldingW = (buttons & PlayerButtons.Forward) != 0;

        // 检查是否在宽限期内
        if (state.GraceTimeRemaining > 0)
        {
            state.GraceTimeRemaining -= 0.03f; // 假设每帧约 0.03 秒

            // 宽限期快结束时警告
            if (state.GraceTimeRemaining <= 1.0f && state.GraceTimeRemaining > 0.97f)
            {
                player.PrintToCenter("⚠️ 1 秒后开始检测！");
            }
            else if (state.GraceTimeRemaining <= 0)
            {
                state.GraceTimeRemaining = 0;
                player.PrintToCenter("🏃 开始按住 W 键！");
            }

            return; // 宽限期内不检测
        }

        // 宽限期后开始检测
        if (!isHoldingW)
        {
            // 没有按住 W 键，累计时间
            state.TimeSinceLastDamage += 0.03f; // 假设每帧约 0.03 秒

            // 检查是否应该造成伤害
            if (state.TimeSinceLastDamage >= DAMAGE_INTERVAL)
            {
                // 造成伤害
                int newHealth = Math.Max(0, pawn.Health - DAMAGE_AMOUNT);
                pawn.Health = newHealth;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

                Console.WriteLine($"[永动机] {player.PlayerName} 未按住 W 键，受到 {DAMAGE_AMOUNT} 点伤害，剩余生命: {newHealth}");

                // 提示玩家
                player.PrintToCenter($"💡 按住 W 键！\n-{DAMAGE_AMOUNT} HP");

                // 如果死亡
                if (newHealth <= 0)
                {
                    player.PrintToChat("💀 你没有按住 W 键，死亡了！");
                }

                // 重置计时器
                state.TimeSinceLastDamage = 0f;
            }
        }
        else
        {
            // 按住了 W 键，重置伤害计时器
            state.TimeSinceLastDamage = 0f;
        }
    }

    /// <summary>
    /// 玩家永动机状态
    /// </summary>
    private class PlayerKeepMovingState
    {
        /// <summary>
        /// 剩余宽限时间（秒）
        /// </summary>
        public float GraceTimeRemaining { get; set; }

        /// <summary>
        /// 自上次伤害以来经过的时间（秒）
        /// </summary>
        public float TimeSinceLastDamage { get; set; }
    }
}
