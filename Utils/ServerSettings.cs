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
    private static bool _originalFallDamageValue = false;

    // 友军伤害自动踢人
    private static ConVar? _autoKickConVar;
    private static bool _originalAutoKickValue = false;

    // 派对模式
    private static ConVar? _partyModeConVar;
    private static int _originalPartyModeValue = 0;

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

        Console.WriteLine("[服务器设置] ✅ 娱乐服务器全局设置已应用");
        Console.WriteLine("   - sv_cheat: true (作弊模式)");
        Console.WriteLine("   - mp_friendlyfire: true (友军伤害)");
        Console.WriteLine("   - mp_falldamage: false (禁用坠落伤害)");
        Console.WriteLine("   - mp_autokick: 0 (禁用友军伤害踢人)");
        Console.WriteLine("   - mp_maxrounds_draw: 1 (启用平局)");
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
            _fallDamageConVar = ConVar.Find("mp_falldamage");
            if (_fallDamageConVar != null)
            {
                _originalFallDamageValue = _fallDamageConVar.GetPrimitiveValue<bool>();
                _fallDamageConVar.SetValue(false);
                Console.WriteLine($"[服务器设置] mp_falldamage 已设置为 false (原值: {_originalFallDamageValue})");
            }
            else
            {
                Console.WriteLine("[服务器设置] ⚠️ 警告：无法找到 mp_falldamage ConVar");
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
                _fallDamageConVar.SetValue(_originalFallDamageValue);
                Console.WriteLine($"[服务器设置] mp_falldamage 已恢复为 {_originalFallDamageValue}");
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
                _originalPartyModeValue = _partyModeConVar.GetPrimitiveValue<int>();
                _partyModeConVar.SetValue(1);
                Console.WriteLine($"[服务器设置] sv_party_mode 已设置为 1 (原值: {_originalPartyModeValue})");
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
}
