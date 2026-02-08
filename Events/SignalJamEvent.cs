// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills RadarHack (反向实现)

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Events;

/// <summary>
/// 信号屏蔽事件 - 所有玩家的雷达都失效
/// 完全复制自 jRandomSkills RadarHack 的反向实现
/// </summary>
public class SignalJamEvent : EntertainmentEvent
{
    public override string Name => "SignalJam";
    public override string DisplayName => "📡 信号屏蔽";
    public override string Description => "所有玩家的雷达都失效了！无法查看敌人位置！";
    public override int Weight { get; set; } = 15;

    // 是否已激活
    private bool _isActive = false;

    public override void OnApply()
    {
        Console.WriteLine("[信号屏蔽] 事件已激活");

        _isActive = true;

        // 清除所有玩家在雷达上的显示（复制自 jRandomSkills RadarHack 的反向操作）
        ClearAllRadar();

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("📡 信号屏蔽事件已启用！雷达失效！");
                player.PrintToCenter("📡 雷达信号被屏蔽！");

                // 播放音效
                player.EmitSound("UI.Pause");
            }
        }

        Server.PrintToChatAll("🌍 全局雷达已失效！只能靠眼睛和耳朵寻找敌人！");
    }

    public override void OnRevert()
    {
        Console.WriteLine("[信号屏蔽] 事件已结束");

        _isActive = false;

        // 恢复雷达显示
        RestoreAllRadar();

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("📡 信号屏蔽已结束！雷达恢复正常！");
                player.PrintToCenter("📡 雷达信号恢复！");

                // 播放音效
                player.EmitSound("UI.RoundStart");
            }
        }

        Server.PrintToChatAll("📡 雷达信号已恢复！");
    }

    /// <summary>
    /// 每帧更新 - 持续清除雷达显示（因为游戏会自动更新）
    /// </summary>
    public void OnTick()
    {
        if (!_isActive)
            return;

        // 每10帧清除一次（避免过于频繁）
        if (Server.TickCount % 10 != 0)
            return;

        ClearAllRadar();
    }

    /// <summary>
    /// 清除所有雷达显示（复制自 jRandomSkills RadarHack.SetEnemiesVisibleOnRadar 的反向操作）
    /// 信号屏蔽期间，所有人都看不到任何人（包括队友）
    /// </summary>
    private void ClearAllRadar()
    {
        int clearedCount = 0;

        // 对每个观察者
        foreach (var observer in Utilities.GetPlayers())
        {
            if (observer == null || !observer.IsValid || observer.PlayerPawn?.Value == null)
                continue;

            int observerIndex = (int)observer.Index - 1;
            Console.WriteLine($"[信号屏蔽] 清除观察者: {observer.PlayerName} (索引: {observerIndex})");

            // 清除所有其他人在该观察者雷达上的显示
            foreach (var target in Utilities.GetPlayers())
            {
                if (target == null || !target.IsValid || target.PlayerPawn?.Value == null)
                    continue;

                // 不处理自己
                if (target == observer)
                    continue;

                var targetPawn = target.PlayerPawn.Value;

                // 清除目标在观察者雷达上的显示
                uint oldMask = targetPawn.EntitySpottedState.SpottedByMask[0];
                targetPawn.EntitySpottedState.SpottedByMask[0] &= ~(1u << (int)(observerIndex % 32));
                uint newMask = targetPawn.EntitySpottedState.SpottedByMask[0];

                clearedCount++;

                if (oldMask != newMask)
                {
                    Console.WriteLine($"[信号屏蔽] 清除 {target.PlayerName} 对 {observer.PlayerName} 的雷达显示: 0x{oldMask:X8} -> 0x{newMask:X8}");
                }
            }
        }

        // 清除所有 C4 的显示
        var bombEntities = Utilities.FindAllEntitiesByDesignerName<CC4>("weapon_c4");
        foreach (var bomb in bombEntities)
        {
            if (bomb == null || !bomb.IsValid)
                continue;

            // 清除所有玩家的 C4 显示
            uint oldMask = bomb.EntitySpottedState.SpottedByMask[0];
            bomb.EntitySpottedState.SpottedByMask[0] = 0u;

            Console.WriteLine($"[信号屏蔽] 清除C4雷达显示: 0x{oldMask:X8} -> 0x0");
        }

        Console.WriteLine($"[信号屏蔽] 已清除 {clearedCount} 个雷达显示");
    }

    /// <summary>
    /// 恢复所有雷达显示（让队友互相显示）
    /// </summary>
    private void RestoreAllRadar()
    {
        // 对每个观察者
        foreach (var observer in Utilities.GetPlayers())
        {
            if (observer == null || !observer.IsValid || observer.PlayerPawn?.Value == null)
                continue;

            int observerIndex = (int)observer.Index - 1;
            var observerTeam = observer.Team;

            // 恢复队友的显示
            foreach (var target in Utilities.GetPlayers())
            {
                if (target == null || !target.IsValid || target.PlayerPawn?.Value == null)
                    continue;

                // 只恢复队友
                if (target.Team != observerTeam)
                    continue;

                // 不处理自己
                if (target == observer)
                    continue;

                var targetPawn = target.PlayerPawn.Value;

                // 设置队友在观察者雷达上可见
                targetPawn.EntitySpottedState.SpottedByMask[0] |= (1u << (int)(observerIndex % 32));
            }
        }

        Console.WriteLine("[信号屏蔽] 已恢复雷达显示");
    }
}
