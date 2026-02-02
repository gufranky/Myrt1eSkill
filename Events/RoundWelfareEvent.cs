using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MyrtleSkill;

/// <summary>
/// 开局福利事件 - 每回合开始时给一名玩家随机发放2000金钱
/// </summary>
public class RoundWelfareEvent : EntertainmentEvent
{
    public override string Name => "RoundWelfare";
    public override string DisplayName => "💰 开局福利";
    public override string Description => "天降横财！每回合开始时随机给一名玩家发放2000金钱！";

    private readonly Random _random = new();
    private CCSPlayerController? _luckyPlayer;

    public override void OnApply()
    {
        Console.WriteLine("[开局福利] 事件已激活");

        // 随机选择一名玩家
        var players = Utilities.GetPlayers();
        var validPlayers = players.Where(p => p.IsValid && p.PawnIsAlive).ToList();

        if (validPlayers.Count == 0)
        {
            Console.WriteLine("[开局福利] 警告：没有可用的玩家");
            return;
        }

        _luckyPlayer = validPlayers[_random.Next(validPlayers.Count)];

        // 发放2000金钱
        if (_luckyPlayer.InGameMoneyServices != null)
        {
            var account = _luckyPlayer.InGameMoneyServices.Account;
            _luckyPlayer.InGameMoneyServices.Account = account + 2000;
            Utilities.SetStateChanged(_luckyPlayer, "CCSPlayerController", "m_pInGameMoneyServices");
        }

        Console.WriteLine($"[开局福利] 玩家 {_luckyPlayer.PlayerName} 获得了 2000 金钱");

        // 显示提示
        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsValid)
            {
                p.PrintToChat("───────────────────");
                p.PrintToChat("💰 开局福利");
                p.PrintToChat($"🎉 玩家 {_luckyPlayer.PlayerName} 获得了 2000 金钱！");
                p.PrintToChat("───────────────────");
            }
        }

        _luckyPlayer?.PrintToCenter($"━━━━━━━━━━━━━━━━\n 💰 +2000 金钱\n━━━━━━━━━━━━━━━━");
    }

    public override void OnRevert()
    {
        Console.WriteLine("[开局福利] 事件已恢复");
        _luckyPlayer = null;
    }
}
