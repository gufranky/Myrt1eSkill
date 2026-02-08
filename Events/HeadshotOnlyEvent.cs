// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace MyrtleSkill;

/// <summary>
/// 只有爆头事件 - 只有爆头才能造成伤害
/// 使用 mp_damage_headshot_only ConVar 实现
/// </summary>
public class HeadshotOnlyEvent : EntertainmentEvent
{
    public override string Name => "HeadshotOnly";
    public override string DisplayName => "🎯 只有爆头";
    public override string Description => "只有爆头才能造成伤害！";

    private ConVar? _headshotOnlyConVar;
    private bool _originalValue = false;

    public override void OnApply()
    {
        Console.WriteLine("[只有爆头] 事件已激活");

        // 获取ConVar
        _headshotOnlyConVar = ConVar.Find("mp_damage_headshot_only");
        if (_headshotOnlyConVar != null)
        {
            // 保存原始值
            _originalValue = _headshotOnlyConVar.GetPrimitiveValue<bool>();

            // 设置为只有爆头模式
            _headshotOnlyConVar.SetValue(true);
            Console.WriteLine("[只有爆头] mp_damage_headshot_only 已设置为 true");
        }
        else
        {
            Console.WriteLine("[只有爆头] 警告：无法找到 mp_damage_headshot_only ConVar");
        }

        // 显示事件提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("───────────────────");
                player.PrintToChat("🎯 只有爆头模式已启用！");
                player.PrintToChat("💢 只有爆头才能造成伤害！");
                player.PrintToChat("💢 其他部位攻击无效！");
                player.PrintToChat("───────────────────");
            }
        }

        Server.PrintToChatAll("🎯 只有爆头才能造成伤害！瞄准头部！");
    }

    public override void OnRevert()
    {
        Console.WriteLine("[只有爆头] 事件已恢复");

        // 恢复 ConVar
        if (_headshotOnlyConVar != null)
        {
            _headshotOnlyConVar.SetValue(_originalValue);
            Console.WriteLine($"[只有爆头] mp_damage_headshot_only 已恢复为 {_originalValue}");
        }

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("🎯 只有爆头模式已禁用");
            }
        }

        Server.PrintToChatAll("🎯 伤害已恢复正常");
    }
}
