using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 笨笨机器人技能 - 主动技能
/// 一回合只能用一次，召唤一个血量300但没有枪的肉盾机器人
/// </summary>
public class DumbBotSkill : PlayerSkill
{
    public override string Name => "DumbBot";
    public override string DisplayName => "🤖 笨笨机器人";
    public override string Description => "召唤300血肉盾，没枪但能抗！";
    public override bool IsActive => true;
    public override float Cooldown => 9999f; // 一回合只能用一次
    public override List<string> ExcludedEvents => new() { };

    // 追踪每回合是否已使用
    private readonly Dictionary<uint, bool> _usedThisRound = new();

    // 追踪笨笨机器人列表（用于防止捡枪）
    private static readonly List<int> _dumbBotSlots = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;
        _usedThisRound[slot] = false;

        Console.WriteLine($"[笨笨机器人] {player.PlayerName} 获得了召唤能力");
        player.PrintToChat("🤖 你获得了召唤笨笨机器人技能！输入 !useskill 或按键激活！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;
        _usedThisRound.Remove(slot);

        Console.WriteLine($"[笨笨机器人] {player.PlayerName} 失去了召唤能力");
    }

    /// <summary>
    /// 清理所有笨笨机器人记录（回合结束时调用）
    /// </summary>
    public static void ClearDumbBots()
    {
        _dumbBotSlots.Clear();
        Console.WriteLine("[笨笨机器人] 已清理所有笨笨机器人记录");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;

        // 检查本回合是否已使用
        if (_usedThisRound.TryGetValue(slot, out var used) && used)
        {
            player.PrintToCenter("❌ 本回合已召唤过笨笨机器人！");
            player.PrintToChat("❌ 本回合已使用过召唤笨笨机器人技能！");
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
            // 配置服务器机器人参数
            SetupBotServerSettings();

            // 增加机器人配额
            var botQuota = ConVar.Find("bot_quota");
            if (botQuota != null)
            {
                int currentQuota = botQuota.GetPrimitiveValue<int>();
                botQuota.SetValue(currentQuota + 1);
                Console.WriteLine($"[笨笨机器人] bot_quota 从 {currentQuota} 增加到 {currentQuota + 1}");
            }

            // 添加机器人到玩家所在队伍
            string command = team == (int)CsTeam.Terrorist ? "bot_add_t" : "bot_add_ct";
            Server.ExecuteCommand(command);

            // 标记为已使用
            _usedThisRound[slot] = true;

            // 延迟配置机器人（等待机器人加入）
            Plugin?.AddTimer(0.5f, () =>
            {
                ConfigureDumbBot(player, teamName);
            });

            // 显示提示
            player.PrintToCenter($"🤖 笨笨机器人已加入{teamName}阵营！");
            player.PrintToChat($"🤖 成功召唤笨笨机器人（300血肉盾）！");

            Console.WriteLine($"[笨笨机器人] {player.PlayerName} 召唤了一个 {teamName} 笨笨机器人");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[笨笨机器人] 召唤时出错: {ex.Message}");
            player.PrintToChat("❌ 召唤失败！");
        }
    }

    /// <summary>
    /// 配置笨笨机器人属性
    /// </summary>
    private void ConfigureDumbBot(CCSPlayerController owner, string teamName)
    {
        try
        {
            // 查找所有机器人
            var bots = Utilities.GetPlayers().Where(p => p.IsValid && p.IsBot).ToList();
            if (bots.Count == 0)
                return;

            // 获取最后一个加入的机器人
            var lastBot = bots.Last();
            if (lastBot == null || !lastBot.IsValid)
                return;

            var botPawn = lastBot.PlayerPawn.Value;
            if (botPawn == null || !botPawn.IsValid)
                return;

            // 设置机器人名字
            lastBot.PlayerName = $"🤖 {owner.PlayerName}的笨笨肉盾";

            // 设置血量为300
            botPawn.Health = 300;
            botPawn.MaxHealth = 300;

            // 移除所有武器
            RemoveAllWeapons(botPawn);

            // 记录为笨笨机器人（用于防止捡枪）
            _dumbBotSlots.Add(lastBot.Slot);

            // 启动持续监控，防止机器人捡枪
            StartWeaponMonitoring(lastBot);

            Console.WriteLine($"[笨笨机器人] 已配置机器人: {lastBot.PlayerName}, HP=300, 无武器, Slot={lastBot.Slot}");

            // 通知所有人
            foreach (var p in Utilities.GetPlayers())
            {
                if (p.IsValid)
                {
                    p.PrintToChat($"🤖 {owner.PlayerName} 召唤了笨笨机器人（300血肉盾）！");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[笨笨机器人] 配置机器人时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 移除机器人的所有武器
    /// </summary>
    private void RemoveAllWeapons(CCSPlayerPawn botPawn)
    {
        try
        {
            var weaponServices = botPawn.WeaponServices;
            if (weaponServices == null)
                return;

            // 获取所有武器
            var weapons = weaponServices.MyWeapons.ToList();
            foreach (var weaponHandle in weapons)
            {
                var weapon = weaponHandle.Get();
                if (weapon != null && weapon.IsValid)
                {
                    // 使用命令移除武器
                    Server.ExecuteCommand($"ent_remove {weapon.Index}");
                    Console.WriteLine($"[笨笨机器人] 移除了武器: {weapon.DesignerName}");
                }
            }

            Console.WriteLine($"[笨笨机器人] 已移除所有武器");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[笨笨机器人] 移除武器时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 配置服务器机器人参数
    /// </summary>
    private void SetupBotServerSettings()
    {
        try
        {
            // 设置机器人难度为中等（1=中等, 0=简单, 2=困难, 3=专家）
            var botDifficulty = ConVar.Find("bot_difficulty");
            if (botDifficulty != null)
            {
                botDifficulty.SetValue(1);
                Console.WriteLine("[笨笨机器人] bot_difficulty 设置为 1 (中等)");
            }

            // 允许机器人在玩家后加入
            var botJoinAfterPlayer = ConVar.Find("bot_join_after_player");
            if (botJoinAfterPlayer != null)
            {
                botJoinAfterPlayer.SetValue(1);
                Console.WriteLine("[笨笨机器人] bot_join_after_player 设置为 1");
            }

            // 设置机器人加入延迟（毫秒）
            var botJoinDelay = ConVar.Find("bot_join_delay");
            if (botJoinDelay != null)
            {
                botJoinDelay.SetValue(0.0f);
                Console.WriteLine("[笨笨机器人] bot_join_delay 设置为 0");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[笨笨机器人] 配置服务器参数时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 启动武器监控，持续检查并移除笨笨机器人捡起的武器
    /// </summary>
    private void StartWeaponMonitoring(CCSPlayerController bot)
    {
        if (Plugin == null)
            return;

        // 每0.5秒检查一次
        Plugin.AddTimer(0.5f, () =>
        {
            MonitorBotWeapons(bot);
        });
    }

    /// <summary>
    /// 监控机器人武器并移除
    /// </summary>
    private void MonitorBotWeapons(CCSPlayerController bot)
    {
        try
        {
            // 检查机器人是否有效
            if (bot == null || !bot.IsValid || !bot.IsBot)
            {
                // 机器人无效，从列表移除
                if (bot != null)
                {
                    _dumbBotSlots.Remove(bot.Slot);
                }
                return;
            }

            var botPawn = bot.PlayerPawn.Value;
            if (botPawn == null || !botPawn.IsValid)
            {
                // 机器人已死亡，从列表移除
                _dumbBotSlots.Remove(bot.Slot);
                return;
            }

            // 检查机器人是否存活（生命值大于0）
            if (botPawn.Health <= 0)
            {
                // 机器人已死亡，从列表移除
                _dumbBotSlots.Remove(bot.Slot);
                return;
            }

            // 检查是否有武器
            var weaponServices = botPawn.WeaponServices;
            if (weaponServices == null)
            {
                // 继续监控
                StartWeaponMonitoring(bot);
                return;
            }

            var weapons = weaponServices.MyWeapons.ToList();
            if (weapons.Count > 0)
            {
                Console.WriteLine($"[笨笨机器人] 检测到 {bot.PlayerName} 尝试捡枪，移除所有武器！");
                RemoveAllWeapons(botPawn);
            }

            // 继续监控（递归调用）
            StartWeaponMonitoring(bot);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[笨笨机器人] 监控武器时出错: {ex.Message}");
        }
    }
}
