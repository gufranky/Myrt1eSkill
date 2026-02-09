using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using MyrtleSkill.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 传送技能 - 传送到玩家历史位置
/// 从所有玩家的历史位置中随机选择一个进行传送
/// </summary>
public class TeleportSkill : PlayerSkill
{
    public override string Name => "Teleport";
    public override string DisplayName => "🌀 瞬间移动";
    public override string Description => "传送到玩家历史位置！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 15.0f; // 15秒冷却

    public override void OnApply(CCSPlayerController player)
    {
        // 主动技能在获得时不需要做什么，等待玩家激活
        Console.WriteLine($"[瞬间移动] {player.PlayerName} 获得了瞬间移动技能");
        player.PrintToChat("🌀 你获得了瞬间移动技能！输入 !useskill 或按键激活！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        // 移除技能时的清理工作
        Console.WriteLine($"[瞬间移动] {player.PlayerName} 失去了瞬间移动技能");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        Console.WriteLine($"[瞬间移动] {player.PlayerName} 尝试使用传送技能");

        // 获取位置记录器
        var plugin = MyrtleSkill.Instance;
        if (plugin?.PositionRecorder == null)
        {
            player.PrintToChat("💫 位置记录器未启用！");
            return;
        }

        // 收集所有玩家的历史位置
        var allPositions = new List<(Features.PositionEntry, string)>();
        foreach (var p in Utilities.GetPlayers())
        {
            var history = plugin.PositionRecorder.GetPlayerHistory(p);
            if (history != null && history.Positions.Count > 0)
            {
                foreach (var pos in history.Positions)
                {
                    allPositions.Add((pos, history.PlayerName));
                }
            }
        }

        if (allPositions.Count == 0)
        {
            player.PrintToChat("💫 没有可用的传送位置！");
            return;
        }

        // 随机选择一个位置（带碰撞检测重试）
        var random = new Random();
        var targetPosition = default(CounterStrikeSharp.API.Modules.Utils.Vector);
        var ownerName = "";
        var selectedPosition = default(Features.PositionEntry);
        bool foundSafePosition = false;
        int maxAttempts = Math.Min(10, allPositions.Count);

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // 随机选择一个位置
            int randomIndex = random.Next(allPositions.Count);
            (selectedPosition, ownerName) = allPositions[randomIndex];

            targetPosition = new CounterStrikeSharp.API.Modules.Utils.Vector(
                selectedPosition.Position.X,
                selectedPosition.Position.Y,
                selectedPosition.Position.Z
            );

            // 检查位置是否安全
            if (SkillUtils.IsPositionSafe(targetPosition, player))
            {
                foundSafePosition = true;
                break;
            }

            Console.WriteLine($"[瞬间移动] 尝试 {attempt + 1}/{maxAttempts}: 位置不安全，重新选择");
        }

        if (!foundSafePosition)
        {
            player.PrintToChat("💫 无法找到安全传送位置！");
            Console.WriteLine($"[瞬间移动] {player.PlayerName} 传送失败");
            return;
        }

        // 计算时间差
        float timeAgo = Server.CurrentTime - selectedPosition.Timestamp;
        string timeDesc = timeAgo < 60
            ? $"{(int)timeAgo}秒前"
            : timeAgo < 3600
                ? $"{(int)(timeAgo / 60)}分钟前"
                : $"{(int)(timeAgo / 3600)}小时前";

        Console.WriteLine($"[瞬间移动] {player.PlayerName} 传送到 {ownerName} 的位置 ({timeDesc})");

        // 执行传送
        pawn.Teleport(targetPosition, pawn.AbsRotation, new Vector(0, 0, 0));

        // 显示效果
        player.PrintToCenter("🌀 瞬间移动！");
        player.PrintToChat($"🌀 已传送到 {ownerName} {timeDesc} 的位置！");

        Console.WriteLine($"[瞬间移动] {player.PlayerName} 成功使用传送技能");
    }
}
