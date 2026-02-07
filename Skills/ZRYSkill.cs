// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;

namespace MyrtleSkill.Skills;

/// <summary>
/// ZRY技能 - 被动技能
/// 无限诱饵弹，投掷后立即补充
/// </summary>
public class ZRYSkill : PlayerSkill
{
    public override string Name => "ZRY";
    public override string DisplayName => "💣 ZRY";
    public override string Description => "无限诱饵弹！投掷后立即补充！";
    public override bool IsActive => false; // 被动技能

    // 追踪启用自动补充的玩家
    private readonly HashSet<uint> _enabledPlayers = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;
        _enabledPlayers.Add(slot);

        // 给予初始诱饵弹
        GiveDecoyGrenades(player, 1);

        Console.WriteLine($"[ZRY] {player.PlayerName} 获得了ZRY技能");
        player.PrintToChat("💣 你获得了ZRY技能！");
        player.PrintToChat("💡 无限诱饵弹！投掷后立即补充！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;
        _enabledPlayers.Remove(slot);

        Console.WriteLine($"[ZRY] {player.PlayerName} 失去了ZRY技能");
    }

    /// <summary>
    /// 处理诱饵弹投掷事件 - 投掷后立即补充
    /// </summary>
    public void OnDecoyThrown(CCSPlayerController player, CDecoyGrenade decoy)
    {
        if (player == null || !decoy.IsValid)
            return;

        // 检查玩家是否有ZRY技能
        if (!_enabledPlayers.Contains(player.Index))
            return;

        Console.WriteLine($"[ZRY] {player.PlayerName} 投掷了诱饵弹");

        // 下一帧补充诱饵弹
        Server.NextFrame(() =>
        {
            if (player != null && player.IsValid && player.PawnIsAlive)
            {
                GiveDecoyGrenades(player, 1);
                player.PrintToChat("💣 诱饵弹已补充！");
            }
        });
    }

    /// <summary>
    /// 给予玩家诱饵弹
    /// </summary>
    private void GiveDecoyGrenades(CCSPlayerController player, int count)
    {
        if (player == null || !player.IsValid)
            return;

        try
        {
            // 给予诱饵弹
            for (int i = 0; i < count; i++)
            {
                player.GiveNamedItem("weapon_decoy");
            }

            Console.WriteLine($"[ZRY] 给予 {player.PlayerName} {count} 个诱饵弹");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ZRY] 给予诱饵弹时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 清理所有记录（回合结束时调用）
    /// </summary>
    public static void OnRoundStart()
    {
        Console.WriteLine("[ZRY] 新回合开始");
    }
}
