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
    private bool _originalPlayerBotCompetitive = false;

    public bool AllowBotControl { get; private set; } = false;

    public BotManager(MyrtleSkill plugin)
    {
        _plugin = plugin;
    }

    /// <summary>
    /// 启用清理机器人功能（每回合开始时清除机器人，禁止玩家控制）
    /// </summary>
    public void EnableBotControl()
    {
        AllowBotControl = true;

        // 禁用玩家死后控制机器人
        _playerBotCompetitiveConVar = ConVar.Find("mp_player_bot_competitive");
        if (_playerBotCompetitiveConVar != null)
        {
            _originalPlayerBotCompetitive = _playerBotCompetitiveConVar.GetPrimitiveValue<bool>();
            _playerBotCompetitiveConVar.SetValue(false);
            Console.WriteLine($"[机器人控制] mp_player_bot_competitive 已设置为 false (原值: {_originalPlayerBotCompetitive})");
        }

        Console.WriteLine("[机器人管理] ✅ 已启用每回合清除机器人，玩家死后不可控制机器人");
    }

    /// <summary>
    /// 禁用清理机器人功能
    /// </summary>
    public void DisableBotControl()
    {
        AllowBotControl = false;

        // 恢复玩家死后控制机器人
        if (_playerBotCompetitiveConVar != null)
        {
            _playerBotCompetitiveConVar.SetValue(true);
            Console.WriteLine("[机器人控制] mp_player_bot_competitive 已设置为 true");
        }

        Console.WriteLine("[机器人管理] ❌ 已禁用每回合清除机器人，玩家死后可以控制机器人");
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

        // 如果启用清理功能，显示提示
        if (AllowBotControl)
        {
            Console.WriteLine("[机器人管理] 机器人清理已启用，机器人已被清除");
        }
    }

    /// <summary>
    /// 清理管理器
    /// </summary>
    public void Dispose()
    {
        // 恢复 mp_player_bot_competitive
        if (_playerBotCompetitiveConVar != null)
        {
            _playerBotCompetitiveConVar.SetValue(_originalPlayerBotCompetitive);
            Console.WriteLine($"[机器人控制] mp_player_bot_competitive 已恢复为 {_originalPlayerBotCompetitive}");
        }

        AllowBotControl = false;
    }
}
