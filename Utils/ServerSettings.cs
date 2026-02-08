using CounterStrikeSharp.API.Modules.Cvars;

namespace MyrtleSkill.Utils;

/// <summary>
/// 娱乐服务器全局设置管理器
/// 统一管理所有娱乐服务器需要的全局 ConVar 设置
/// </summary>
public static class ServerSettings
{
    // 作弊模式
    private static ConVar? _svCheatConVar;
    private static bool _originalSvCheat = false;

    // 友军伤害
    private static ConVar? _friendlyFireConVar;
    private static bool _originalFriendlyFireValue = false;

    // 坠落伤害
    private static ConVar? _fallDamageConVar;
    private static float _originalFallDamageScale = 1.0f;

    // 友军伤害自动踢人
    private static ConVar? _autoKickConVar;
    private static bool _originalAutoKickValue = false;

    // 派对模式
    private static ConVar? _partyModeConVar;
    private static bool _originalPartyModeValue = false;

    // 机器人难度
    private static ConVar? _botDifficultyConVar;
    private static int _originalBotDifficultyValue = 1;

    // 机器人加入延迟
    private static ConVar? _botJoinDelayConVar;
    private static float _originalBotJoinDelayValue = 0.0f;

    // 机器人等待人类指令
    private static ConVar? _botDeferToHumanConVar;
    private static bool _originalBotDeferToHumanValue = true;

    /// <summary>
    /// 初始化所有娱乐服务器全局设置
    /// 在插件加载时调用
    /// </summary>
    public static void InitializeAllSettings()
    {
        EnableCheatMode();
        EnableFriendlyFire();
        DisableFallDamage();
        DisableFriendlyFireKick();
        EnablePartyMode();
        SetupBotSettings();

        Console.WriteLine("[服务器设置] ✅ 娱乐服务器全局设置已应用");
        Console.WriteLine("   - sv_cheats: true (作弊模式)");
        Console.WriteLine("   - mp_friendlyfire: true (友军伤害)");
        Console.WriteLine("   - sv_falldamage_scale: 0 (禁用坠落伤害)");
        Console.WriteLine("   - mp_autokick: false (禁用友军伤害踢人)");
        Console.WriteLine("   - sv_party_mode: true (启用派对模式)");
        Console.WriteLine("   - bot_difficulty: 3 (专家难度)");
        Console.WriteLine("   - bot_join_delay: 0 (立即加入)");
        Console.WriteLine("   - bot_defer_to_human_goals: 0 (机器人更主动)");
    }

    /// <summary>
    /// 恢复所有娱乐服务器全局设置
    /// 在插件卸载时调用
    /// </summary>
    public static void RestoreAllSettings()
    {
        RestoreCheatMode();
        RestoreFriendlyFire();
        RestoreFallDamage();
        RestoreFriendlyFireKick();
        RestorePartyMode();
        RestoreBotSettings();

        Console.WriteLine("[服务器设置] ✅ 娱乐服务器全局设置已恢复");
    }

    #region 作弊模式

    /// <summary>
    /// 启用作弊模式（某些插件功能需要）
    /// </summary>
    private static void EnableCheatMode()
    {
        try
        {
            _svCheatConVar = ConVar.Find("sv_cheats");
            if (_svCheatConVar != null)
            {
                _originalSvCheat = _svCheatConVar.GetPrimitiveValue<bool>();
                _svCheatConVar.SetValue(true);
                Console.WriteLine($"[服务器设置] sv_cheats 已设置为 true (原值: {_originalSvCheat})");
            }
            else
            {
                Console.WriteLine("[服务器设置] ⚠️ 警告：无法找到 sv_cheats ConVar");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[服务器设置] ❌ 启用作弊模式出错：{ex.Message}");
        }
    }

    /// <summary>
    /// 恢复作弊模式设置
    /// </summary>
    private static void RestoreCheatMode()
    {
        try
        {
            if (_svCheatConVar != null)
            {
                _svCheatConVar.SetValue(_originalSvCheat);
                Console.WriteLine($"[服务器设置] sv_cheats 已恢复为 {_originalSvCheat}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[服务器设置] ❌ 恢复作弊模式出错：{ex.Message}");
        }
    }

    #endregion

    #region 友军伤害

    /// <summary>
    /// 启用友军伤害
    /// </summary>
    private static void EnableFriendlyFire()
    {
        try
        {
            _friendlyFireConVar = ConVar.Find("mp_friendlyfire");
            if (_friendlyFireConVar != null)
            {
                _originalFriendlyFireValue = _friendlyFireConVar.GetPrimitiveValue<bool>();
                _friendlyFireConVar.SetValue(true);
                Console.WriteLine($"[服务器设置] mp_friendlyfire 已设置为 true (原值: {_originalFriendlyFireValue})");
            }
            else
            {
                Console.WriteLine("[服务器设置] ⚠️ 警告：无法找到 mp_friendlyfire ConVar");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[服务器设置] ❌ 启用友军伤害出错：{ex.Message}");
        }
    }

    /// <summary>
    /// 恢复友军伤害设置
    /// </summary>
    private static void RestoreFriendlyFire()
    {
        try
        {
            if (_friendlyFireConVar != null)
            {
                _friendlyFireConVar.SetValue(_originalFriendlyFireValue);
                Console.WriteLine($"[服务器设置] mp_friendlyfire 已恢复为 {_originalFriendlyFireValue}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[服务器设置] ❌ 恢复友军伤害出错：{ex.Message}");
        }
    }

    #endregion

    #region 坠落伤害

    /// <summary>
    /// 禁用坠落伤害
    /// </summary>
    private static void DisableFallDamage()
    {
        try
        {
            _fallDamageConVar = ConVar.Find("sv_falldamage_scale");
            if (_fallDamageConVar != null)
            {
                _originalFallDamageScale = _fallDamageConVar.GetPrimitiveValue<float>();
                _fallDamageConVar.SetValue(0.0f);
                Console.WriteLine($"[服务器设置] sv_falldamage_scale 已设置为 0 (原值: {_originalFallDamageScale})");
            }
            else
            {
                Console.WriteLine("[服务器设置] ⚠️ 警告：无法找到 sv_falldamage_scale ConVar");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[服务器设置] ❌ 禁用坠落伤害出错：{ex.Message}");
        }
    }

    /// <summary>
    /// 恢复坠落伤害设置
    /// </summary>
    private static void RestoreFallDamage()
    {
        try
        {
            if (_fallDamageConVar != null)
            {
                _fallDamageConVar.SetValue(_originalFallDamageScale);
                Console.WriteLine($"[服务器设置] sv_falldamage_scale 已恢复为 {_originalFallDamageScale}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[服务器设置] ❌ 恢复坠落伤害出错：{ex.Message}");
        }
    }

    #endregion

    #region 友军伤害踢人保护

    /// <summary>
    /// 禁用友军伤害自动踢人功能
    /// </summary>
    private static void DisableFriendlyFireKick()
    {
        try
        {
            _autoKickConVar = ConVar.Find("mp_autokick");
            if (_autoKickConVar != null)
            {
                _originalAutoKickValue = _autoKickConVar.GetPrimitiveValue<bool>();
                _autoKickConVar.SetValue(false);
                Console.WriteLine($"[服务器设置] mp_autokick 已设置为 false (原值: {_originalAutoKickValue})");
            }
            else
            {
                Console.WriteLine("[服务器设置] ⚠️ 警告：无法找到 mp_autokick ConVar");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[服务器设置] ❌ 禁用友军伤害踢人出错：{ex.Message}");
        }
    }

    /// <summary>
    /// 恢复友军伤害自动踢人功能
    /// </summary>
    private static void RestoreFriendlyFireKick()
    {
        try
        {
            if (_autoKickConVar != null)
            {
                _autoKickConVar.SetValue(_originalAutoKickValue);
                Console.WriteLine($"[服务器设置] mp_autokick 已恢复为 {_originalAutoKickValue}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[服务器设置] ❌ 恢复友军伤害踢人出错：{ex.Message}");
        }
    }

    #endregion

    #region 派对模式

    /// <summary>
    /// 启用派对模式（允许平局）
    /// </summary>
    private static void EnablePartyMode()
    {
        try
        {
            _partyModeConVar = ConVar.Find("sv_party_mode");
            if (_partyModeConVar != null)
            {
                _originalPartyModeValue = _partyModeConVar.GetPrimitiveValue<bool>();
                _partyModeConVar.SetValue(true);
                Console.WriteLine($"[服务器设置] sv_party_mode 已设置为 true (原值: {_originalPartyModeValue})");
            }
            else
            {
                Console.WriteLine("[服务器设置] ⚠️ 警告：无法找到 sv_party_mode ConVar");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[服务器设置] ❌ 启用派对模式出错：{ex.Message}");
        }
    }

    /// <summary>
    /// 恢复派对模式设置
    /// </summary>
    private static void RestorePartyMode()
    {
        try
        {
            if (_partyModeConVar != null)
            {
                _partyModeConVar.SetValue(_originalPartyModeValue);
                Console.WriteLine($"[服务器设置] sv_party_mode 已恢复为 {_originalPartyModeValue}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[服务器设置] ❌ 恢复派对模式出错：{ex.Message}");
        }
    }

    #endregion

    #region 机器人设置

    /// <summary>
    /// 配置机器人参数（专家难度）
    /// </summary>
    private static void SetupBotSettings()
    {
        try
        {
            // 设置机器人难度为专家（3=专家, 0=简单, 1=中等, 2=困难）
            _botDifficultyConVar = ConVar.Find("bot_difficulty");
            if (_botDifficultyConVar != null)
            {
                _originalBotDifficultyValue = _botDifficultyConVar.GetPrimitiveValue<int>();
                _botDifficultyConVar.SetValue(3);
                Console.WriteLine($"[服务器设置] bot_difficulty 已设置为 3 (专家) (原值: {_originalBotDifficultyValue})");
            }
            else
            {
                Console.WriteLine("[服务器设置] ⚠️ 警告：无法找到 bot_difficulty ConVar");
            }

            // 设置机器人加入延迟为0（立即加入）
            _botJoinDelayConVar = ConVar.Find("bot_join_delay");
            if (_botJoinDelayConVar != null)
            {
                _originalBotJoinDelayValue = _botJoinDelayConVar.GetPrimitiveValue<float>();
                _botJoinDelayConVar.SetValue(0.0f);
                Console.WriteLine($"[服务器设置] bot_join_delay 已设置为 0 (原值: {_originalBotJoinDelayValue})");
            }
            else
            {
                Console.WriteLine("[服务器设置] ⚠️ 警告：无法找到 bot_join_delay ConVar");
            }

            // 禁用机器人等待人类指令（让机器人更主动）
            _botDeferToHumanConVar = ConVar.Find("bot_defer_to_human_goals");
            if (_botDeferToHumanConVar != null)
            {
                _originalBotDeferToHumanValue = _botDeferToHumanConVar.GetPrimitiveValue<bool>();
                _botDeferToHumanConVar.SetValue(false);
                Console.WriteLine($"[服务器设置] bot_defer_to_human_goals 已设置为 false (原值: {_originalBotDeferToHumanValue})");
            }
            else
            {
                Console.WriteLine("[服务器设置] ⚠️ 警告：无法找到 bot_defer_to_human_goals ConVar");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[服务器设置] ❌ 配置机器人设置出错：{ex.Message}");
        }
    }

    /// <summary>
    /// 恢复机器人设置
    /// </summary>
    private static void RestoreBotSettings()
    {
        try
        {
            if (_botDifficultyConVar != null)
            {
                _botDifficultyConVar.SetValue(_originalBotDifficultyValue);
                Console.WriteLine($"[服务器设置] bot_difficulty 已恢复为 {_originalBotDifficultyValue}");
            }

            if (_botJoinDelayConVar != null)
            {
                _botJoinDelayConVar.SetValue(_originalBotJoinDelayValue);
                Console.WriteLine($"[服务器设置] bot_join_delay 已恢复为 {_originalBotJoinDelayValue}");
            }

            if (_botDeferToHumanConVar != null)
            {
                _botDeferToHumanConVar.SetValue(_originalBotDeferToHumanValue);
                Console.WriteLine($"[服务器设置] bot_defer_to_human_goals 已恢复为 {_originalBotDeferToHumanValue}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[服务器设置] ❌ 恢复机器人设置出错：{ex.Message}");
        }
    }

    #endregion
}
