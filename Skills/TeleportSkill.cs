using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using HelloWorldPlugin.ThirdParty;

namespace HelloWorldPlugin.Skills;

/// <summary>
/// 传送技能 - 主动技能示例
/// 玩家可以传送到随机位置
/// </summary>
public class TeleportSkill : PlayerSkill
{
    public override string Name => "Teleport";
    public override string DisplayName => "🌀 瞬间移动";
    public override string Description => "传送到地图上的随机位置！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 15.0f; // 15秒冷却

    public override void OnApply(CCSPlayerController player)
    {
        // 主动技能在获得时不需要做什么，等待玩家激活
        Console.WriteLine($"[瞬间移动] {player.PlayerName} 获得了瞬间移动技能");
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

        // 获取随机位置
        Vector? randomPosition = NavMesh.GetRandomPosition(maxAttempts: 20);
        if (randomPosition == null)
        {
            player.PrintToChat("💫 无法找到传送位置！");
            return;
        }

        // 传送玩家
        pawn.Teleport(randomPosition, pawn.AbsRotation, new Vector(0, 0, 0));

        // 显示效果
        player.PrintToCenter("🌀 瞬间移动！");
        player.PrintToChat($"🌀 已传送到随机位置！");

        Console.WriteLine($"[瞬间移动] {player.PlayerName} 使用了瞬间移动技能");
    }
}
