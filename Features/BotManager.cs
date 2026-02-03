using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace MyrtleSkill.Features;

/// <summary>
/// 机器人管理器 - 控制玩家死后操控机器人的功能
/// </summary>
public class BotManager
{
    private readonly MyrtleSkill _plugin;
    private ConVar? _playerBotCompetitiveConVar;
    private ConVar? _botQuotaConVar;
    private bool _originalPlayerBotCompetitive = false;
    private int _originalBotQuota = 0;

    public bool AllowBotControl { get; private set; } = false;

    public BotManager(MyrtleSkill plugin)
    {
        _plugin = plugin;
    }

    /// <summary>
    /// 启用玩家控制机器人功能
    /// </summary>
    public void EnableBotControl()
    {
        AllowBotControl = true;

        // 启用玩家死后控制机器人
        _playerBotCompetitiveConVar = ConVar.Find("mp_player_bot_competitive");
        if (_playerBotCompetitiveConVar != null)
        {
            _originalPlayerBotCompetitive = _playerBotCompetitiveConVar.GetPrimitiveValue<bool>();
            _playerBotCompetitiveConVar.SetValue(true);
            Console.WriteLine($"[机器人控制] mp_player_bot_competitive 已设置为 true (原值: {_originalPlayerBotCompetitive})");
        }

        // 保存并禁用自动补充机器人（避免清理后立刻补充）
        _botQuotaConVar = ConVar.Find("bot_quota");
        if (_botQuotaConVar != null)
        {
            _originalBotQuota = _botQuotaConVar.GetPrimitiveValue<int>();
            _botQuotaConVar.SetValue(0);
            Console.WriteLine($"[机器人控制] bot_quota 已设置为 0 (原值: {_originalBotQuota})");
        }

        Console.WriteLine("[机器人管理] ✅ 已启用玩家控制机器人功能");
    }

    /// <summary>
    /// 禁用玩家控制机器人功能
    /// </summary>
    public void DisableBotControl()
    {
        AllowBotControl = false;

        // 禁用玩家死后控制机器人
        if (_playerBotCompetitiveConVar != null)
        {
            _playerBotCompetitiveConVar.SetValue(false);
            Console.WriteLine("[机器人控制] mp_player_bot_competitive 已设置为 false");
        }

        // 恢复自动补充机器人设置
        if (_botQuotaConVar != null)
        {
            _botQuotaConVar.SetValue(_originalBotQuota);
            Console.WriteLine($"[机器人控制] bot_quota 已恢复为 {_originalBotQuota}");
        }

        Console.WriteLine("[机器人管理] ❌ 已禁用玩家控制机器人功能");
    }

    /// <summary>
    /// 每回合开始时清除所有机器人
    /// </summary>
    public void OnRoundStart()
    {
        int kickedBots = 0;

        // 获取所有玩家
        var players = Utilities.GetPlayers().ToList();

        foreach (var player in players)
        {
            if (player.IsValid && player.IsBot)
            {
                // 使用服务器命令踢出机器人
                Server.ExecuteCommand($"kickid {player.UserId}");
                kickedBots++;
            }
        }

        if (kickedBots > 0)
        {
            Console.WriteLine($"[机器人管理] 已清除 {kickedBots} 个机器人");
        }

        // 确保不会自动补充机器人
        if (_botQuotaConVar != null && _botQuotaConVar.GetPrimitiveValue<int>() != 0)
        {
            _botQuotaConVar.SetValue(0);
            Console.WriteLine("[机器人管理] 已重置 bot_quota 为 0，防止自动补充");
        }
    }

    /// <summary>
    /// 清理管理器
    /// </summary>
    public void Dispose()
    {
        // 恢复原始设置
        if (_playerBotCompetitiveConVar != null)
        {
            _playerBotCompetitiveConVar.SetValue(_originalPlayerBotCompetitive);
            Console.WriteLine($"[机器人控制] mp_player_bot_competitive 已恢复为 {_originalPlayerBotCompetitive}");
        }

        if (_botQuotaConVar != null)
        {
            _botQuotaConVar.SetValue(_originalBotQuota);
            Console.WriteLine($"[机器人控制] bot_quota 已恢复为 {_originalBotQuota}");
        }

        AllowBotControl = false;
    }
}
