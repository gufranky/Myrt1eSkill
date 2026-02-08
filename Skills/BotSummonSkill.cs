using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 召唤机器人技能 - 主动技能
/// 一回合只能用一次，召唤一个队友机器人帮你作战
/// </summary>
public class BotSummonSkill : PlayerSkill
{
    public override string Name => "BotSummon";
    public override string DisplayName => "🤖 召唤队友";
    public override string Description => "召唤机器人助阵，一回合一次！";
    public override bool IsActive => true;
    public override float Cooldown => 9999f; // 一回合只能用一次，设置超大冷却
    public override List<string> ExcludedEvents => new() { }; // 不与任何事件互斥

    // 追踪每回合是否已使用
    private readonly Dictionary<uint, bool> _usedThisRound = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;
        _usedThisRound[slot] = false;

        Console.WriteLine($"[召唤队友] {player.PlayerName} 获得了召唤能力");
        player.PrintToChat("🤖 你获得了召唤队友技能！输入 !useskill 或按键激活！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;
        _usedThisRound.Remove(slot);

        Console.WriteLine($"[召唤队友] {player.PlayerName} 失去了召唤能力");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;

        // 检查本回合是否已使用
        if (_usedThisRound.TryGetValue(slot, out var used) && used)
        {
            player.PrintToCenter("❌ 本回合已召唤过队友！");
            player.PrintToChat("❌ 本回合已使用过召唤队友技能！");
            return;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 获取玩家队伍
        var team = pawn.TeamNum;
        string teamName = team == (int)CsTeam.Terrorist ? "T" : "CT";

        try
        {
            // 增加机器人配额
            var botQuota = ConVar.Find("bot_quota");
            if (botQuota != null)
            {
                int currentQuota = botQuota.GetPrimitiveValue<int>();
                botQuota.SetValue(currentQuota + 1);
                Console.WriteLine($"[召唤队友] bot_quota 从 {currentQuota} 增加到 {currentQuota + 1}");
            }

            // 添加机器人到玩家所在队伍
            string command = team == (int)CsTeam.Terrorist ? "bot_add_t" : "bot_add_ct";
            Server.ExecuteCommand(command);

            // 标记为已使用
            _usedThisRound[slot] = true;

            // 延迟重命名机器人并设置金钱（等待机器人加入）
            Plugin?.AddTimer(0.5f, () =>
            {
                RenameLastBot(player.PlayerName);
            });

            // 显示提示
            player.PrintToCenter($"🤖 机器人队友已加入{teamName}阵营！");
            player.PrintToChat($"🤖 成功召唤机器人队友加入 {teamName} 阵营！");

            Console.WriteLine($"[召唤队友] {player.PlayerName} 召唤了一个 {teamName} 机器人");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[召唤队友] 召唤机器人时出错: {ex.Message}");
            player.PrintToChat("❌ 召唤失败！");
        }
    }

    /// <summary>
    /// 重命名最近加入的机器人并设置金钱
    /// </summary>
    private void RenameLastBot(string ownerName)
    {
        try
        {
            // 查找所有机器人
            var bots = Utilities.GetPlayers().Where(p => p.IsValid && p.IsBot).ToList();
            if (bots.Count == 0)
                return;

            // 获取最后一个加入的机器人
            var lastBot = bots.Last();
            if (lastBot != null && lastBot.IsValid)
            {
                // 设置机器人名字
                lastBot.PlayerName = $"[召唤] {ownerName}的助手";
                Console.WriteLine($"[召唤队友] 机器人已重命名为: {lastBot.PlayerName}");

                // 给机器人5000块钱
                if (lastBot.InGameMoneyServices != null)
                {
                    lastBot.InGameMoneyServices.Account = 5000;
                    Console.WriteLine($"[召唤队友] 机器人金钱已设置为5000");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[召唤队友] 重命名机器人时出错: {ex.Message}");
        }
    }
}
