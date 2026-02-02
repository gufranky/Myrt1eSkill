using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MyrtleSkill.Features;

/// <summary>
/// 开局福利管理器 - 每回合开始时随机给一名玩家发放2000金钱
/// </summary>
public class WelfareManager
{
    private readonly MyrtleSkill _plugin;
    private readonly Random _random = new();

    public bool IsEnabled { get; private set; } = true;

    public WelfareManager(MyrtleSkill plugin)
    {
        _plugin = plugin;
    }

    /// <summary>
    /// 启用开局福利系统
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        Console.WriteLine("[开局福利系统] ✅ 已启用");
    }

    /// <summary>
    /// 禁用开局福利系统
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        Console.WriteLine("[开局福利系统] ❌ 已禁用");
    }

    /// <summary>
    /// 处理回合开始事件 - 随机给一名玩家发放2000金钱
    /// </summary>
    public void OnRoundStart()
    {
        if (!IsEnabled)
        {
            Console.WriteLine("[开局福利系统] 本回合已禁用，跳过");
            return;
        }

        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║     💰 开局福利系统 - 开始抽选 🎲     ║");
        Console.WriteLine("╚════════════════════════════════════════╝");

        // 随机选择一名玩家
        var players = Utilities.GetPlayers();
        var validPlayers = players.Where(p => p.IsValid && p.PawnIsAlive).ToList();

        if (validPlayers.Count == 0)
        {
            Console.WriteLine("❌ [开局福利] 警告：没有可用的玩家");
            return;
        }

        var luckyPlayer = validPlayers[_random.Next(validPlayers.Count)];

        // 发放2000金钱
        if (luckyPlayer.InGameMoneyServices != null)
        {
            var account = luckyPlayer.InGameMoneyServices.Account;
            luckyPlayer.InGameMoneyServices.Account = account + 2000;
            Utilities.SetStateChanged(luckyPlayer, "CCSPlayerController", "m_pInGameMoneyServices");
        }

        Console.WriteLine($"🎉 [开局福利] 玩家 {luckyPlayer.PlayerName} 获得了 2000 金钱！");

        // 显示聊天框提示（更醒目的版本）
        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsValid)
            {
                // 使用醒目的颜色和格式
                p.PrintToChat(" \x10"); // 清除行
                p.PrintToChat("═══════════════════════════════════════");
                p.PrintToChat(" \x07💰 开局福利触发！\x01"); // 浅色
                p.PrintToChat($" \x06🎲 天选之子：\x03 {luckyPlayer.PlayerName}\x01"); // 橙色 + 黄色
                p.PrintToChat(" \x05💵 获得 2000 金钱奖励！\x01"); // 浅绿色
                p.PrintToChat("═══════════════════════════════════════");
                p.PrintToChat(" "); // 空行分隔
            }
        }

        // 幸运玩家特别提示（屏幕中央 + 额外聊天框消息）
        luckyPlayer?.PrintToCenter($"━━━━━━━━━━━━━━━━\n 💰 +2000 金钱\n━━━━━━━━━━━━━━━━");
        if (luckyPlayer != null && luckyPlayer.IsValid)
        {
            luckyPlayer.PrintToChat(" \x04🌟 恭喜你！你是本回合的天选之子！\x01"); // 红色
            luckyPlayer.PrintToChat(" \x05💰 已获得 2000 金钱奖励！\x01"); // 浅绿色
        }
    }
}
