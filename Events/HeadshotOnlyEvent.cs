using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace MyrtleSkill;

/// <summary>
/// 只有爆头事件 - 只有爆头才能造成伤害
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
            Console.WriteLine("[只有爆头] mp_damage_headshot_only 已设置为 1");
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
                player.PrintToChat(" 🎯 只有爆头模式已启用！");
            }
        }

        // 注册玩家生成事件
        if (Plugin != null)
        {
            Plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[只有爆头] 事件已恢复");

        // 1. 恢复 ConVar
        if (_headshotOnlyConVar != null)
        {
            _headshotOnlyConVar.SetValue(_originalValue);
            Console.WriteLine($"[只有爆头] mp_damage_headshot_only 已恢复为 {_originalValue}");
        }

        // 2. 移除事件监听
        if (Plugin != null)
        {
            Plugin.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
        }

        // 3. 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("🎯 只有爆头模式已禁用");
            }
        }
    }

    /// <summary>
    /// 玩家生成时显示提示
    /// </summary>
    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;


        return HookResult.Continue;
    }
}
