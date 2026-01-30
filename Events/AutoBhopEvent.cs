using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace HelloWorldPlugin;

/// <summary>
/// 自动Bhop事件 - 自动连跳，移动更流畅
/// </summary>
public class AutoBhopEvent : EntertainmentEvent
{
    public override string Name => "AutoBhop";
    public override string DisplayName => "🐰 自动Bhop";
    public override string Description => "自动连跳启用！移动速度提升！跳跃更流畅！";

    // ConVars
    private ConVar? _enableBunnyhoppingConVar;
    private ConVar? _maxSpeedConVar;
    private ConVar? _accelerateConVar;
    private bool _originalEnableBunnyhopping = false;
    private float _originalMaxSpeed = 320.0f;
    private float _originalAccelerate = 5.5f;

    public override void OnApply()
    {
        Console.WriteLine("[自动Bhop] 事件已激活");

        // 1. 启用自动连跳
        _enableBunnyhoppingConVar = ConVar.Find("sv_enablebunnyhopping");
        if (_enableBunnyhoppingConVar != null)
        {
            _originalEnableBunnyhopping = _enableBunnyhoppingConVar.GetPrimitiveValue<bool>();
            _enableBunnyhoppingConVar.SetValue(true);
            Console.WriteLine($"[自动Bhop] sv_enablebunnyhopping 已设置为 true (原值: {_originalEnableBunnyhopping})");
        }
        else
        {
            Console.WriteLine("[自动Bhop] 警告：无法找到 sv_enablebunnyhopping ConVar");
        }

        // 2. 提高最大移动速度
        _maxSpeedConVar = ConVar.Find("sv_maxspeed");
        if (_maxSpeedConVar != null)
        {
            _originalMaxSpeed = _maxSpeedConVar.GetPrimitiveValue<float>();
            _maxSpeedConVar.SetValue(500.0f); // 提高到500
            Console.WriteLine($"[自动Bhop] sv_maxspeed 已设置为 500 (原值: {_originalMaxSpeed})");
        }
        else
        {
            Console.WriteLine("[自动Bhop] 警告：无法找到 sv_maxspeed ConVar");
        }

        // 3. 提高加速度（让移动更灵敏）
        _accelerateConVar = ConVar.Find("sv_accelerate");
        if (_accelerateConVar != null)
        {
            _originalAccelerate = _accelerateConVar.GetPrimitiveValue<float>();
            _accelerateConVar.SetValue(10.0f); // 提高到10（默认5.5）
            Console.WriteLine($"[自动Bhop] sv_accelerate 已设置为 10 (原值: {_originalAccelerate})");
        }
        else
        {
            Console.WriteLine("[自动Bhop] 警告：无法找到 sv_accelerate ConVar");
        }

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToCenter("🐰 自动Bhop模式！\n连跳加速已启用！速度提升！");
                player.PrintToChat("🐰 自动Bhop模式已启用！连续跳跃获得更高速度！");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[自动Bhop] 事件已恢复");

        // 恢复自动连跳设置
        if (_enableBunnyhoppingConVar != null)
        {
            _enableBunnyhoppingConVar.SetValue(_originalEnableBunnyhopping);
            Console.WriteLine($"[自动Bhop] sv_enablebunnyhopping 已恢复为 {_originalEnableBunnyhopping}");
        }

        // 恢复最大移动速度
        if (_maxSpeedConVar != null)
        {
            _maxSpeedConVar.SetValue(_originalMaxSpeed);
            Console.WriteLine($"[自动Bhop] sv_maxspeed 已恢复为 {_originalMaxSpeed}");
        }

        // 恢复加速度
        if (_accelerateConVar != null)
        {
            _accelerateConVar.SetValue(_originalAccelerate);
            Console.WriteLine($"[自动Bhop] sv_accelerate 已恢复为 {_originalAccelerate}");
        }

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("🐰 自动Bhop模式已禁用");
            }
        }
    }
}
