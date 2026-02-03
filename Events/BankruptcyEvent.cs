// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill;

/// <summary>
/// 全员破产事件 - 将所有玩家金币设置为800
/// </summary>
public class BankruptcyEvent : EntertainmentEvent
{
    public override string Name => "Bankruptcy";
    public override string DisplayName => "💸 全员破产";
    public override string Description => "所有人都破产了！金币只有800！";

    // 破产后的金币数额
    private const int BANKRUPTCY_MONEY = 800;

    public override void OnApply()
    {
        Console.WriteLine("[全员破产] 事件已激活");

        // 获取所有玩家
        var players = Utilities.GetPlayers();
        foreach (var player in players)
        {
            if (!player.IsValid)
                continue;

            var moneyServices = player.InGameMoneyServices;
            if (moneyServices == null)
                continue;

            // 设置金币为800
            moneyServices.Account = BANKRUPTCY_MONEY;

            // 通知客户端更新
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");

            // 发送提示
            player.PrintToCenter($"💸 破产了！金币已重置为 {BANKRUPTCY_MONEY}");
            player.PrintToChat($"💸 全员破产！你的金币现在是 {BANKRUPTCY_MONEY}");
        }

        Console.WriteLine($"[全员破产] 已将 {players.Count} 名玩家的金币设置为 {BANKRUPTCY_MONEY}");
    }
}
