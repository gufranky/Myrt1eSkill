// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Events;

/// <summary>
/// 信号屏蔽事件 - 所有玩家的雷达都出现问题
/// </summary>
public class SignalJamEvent : EntertainmentEvent
{
    public override string Name => "SignalJam";
    public override string DisplayName => "📡 信号屏蔽";
    public override string Description => "所有玩家的雷达都失效了！无法查看敌人位置！";
    public override int Weight { get; set; } = 15;

    // ConVars
    private ConVar? _radarEnableConVar;
    private float _originalRadarEnable = 1.0f;

    public override void OnApply()
    {
        Console.WriteLine("[信号屏蔽] 事件已激活");

        // 1. 禁用雷达
        _radarEnableConVar = ConVar.Find("sv_radar_enable");
        if (_radarEnableConVar != null)
        {
            _originalRadarEnable = _radarEnableConVar.GetPrimitiveValue<float>();
            _radarEnableConVar.SetValue(0.0f);
            Console.WriteLine($"[信号屏蔽] sv_radar_enable 已设置为 0 (原值: {_originalRadarEnable})");
        }
        else
        {
            Console.WriteLine("[信号屏蔽] 警告：无法找到 sv_radar_enable ConVar");
        }

        // 2. 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("📡 信号屏蔽事件已启用！雷达失效！");
                player.PrintToCenter("📡 雷达信号被屏蔽！");

                // 播放音效
                player.EmitSound("UI.Pause");
            }
        }

        Server.PrintToChatAll("🌍 全局雷达已失效！只能靠眼睛和耳朵寻找敌人！");
    }

    public override void OnRevert()
    {
        Console.WriteLine("[信号屏蔽] 事件已结束");

        // 恢复雷达
        if (_radarEnableConVar != null)
        {
            _radarEnableConVar.SetValue(_originalRadarEnable);
            Console.WriteLine($"[信号屏蔽] sv_radar_enable 已恢复为 {_originalRadarEnable}");
        }

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("📡 信号屏蔽已结束！雷达恢复正常！");
                player.PrintToCenter("📡 雷达信号恢复！");

                // 播放音效
                player.EmitSound("UI.RoundStart");
            }
        }

        Server.PrintToChatAll("📡 雷达信号已恢复！");
    }
}
