using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace MyrtleSkill;

/// <summary>
/// 无限弹药事件 - 弹药永不耗尽
/// </summary>
public class InfiniteAmmoEvent : EntertainmentEvent
{
    public override string Name => "InfiniteAmmo";
    public override string DisplayName => "🔫 无限弹药";
    public override string Description => "弹药永不耗尽！火力全开！";

    private ConVar? _svCheatConVar;
    private ConVar? _infiniteAmmoConVar;
    private int _originalSvCheat = 0;
    private int _originalInfiniteAmmo = 0;

    public override void OnApply()
    {
        Console.WriteLine("[无限弹药] 事件已激活");

        // 1. 启用作弊模式
        _svCheatConVar = ConVar.Find("sv_cheats");
        if (_svCheatConVar != null)
        {
            _originalSvCheat = _svCheatConVar.GetPrimitiveValue<int>();
            _svCheatConVar.SetValue(1);
            Console.WriteLine($"[无限弹药] sv_cheats 已设置为 1 (原值: {_originalSvCheat})");
        }

        // 2. 启用无限弹药
        _infiniteAmmoConVar = ConVar.Find("sv_infinite_ammo");
        if (_infiniteAmmoConVar != null)
        {
            _originalInfiniteAmmo = _infiniteAmmoConVar.GetPrimitiveValue<int>();
            _infiniteAmmoConVar.SetValue(1);
            Console.WriteLine($"[无限弹药] sv_infinite_ammo 已设置为 1 (原值: {_originalInfiniteAmmo})");
        }

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToCenter("🔫 无限弹药！\n弹药永不耗尽！");
                player.PrintToChat("🔫 无限弹药模式已启用！");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[无限弹药] 事件已恢复");

        // 恢复无限弹药
        if (_infiniteAmmoConVar != null)
        {
            _infiniteAmmoConVar.SetValue(_originalInfiniteAmmo);
            Console.WriteLine($"[无限弹药] sv_infinite_ammo 已恢复为 {_originalInfiniteAmmo}");
        }

        // 恢复作弊模式
        if (_svCheatConVar != null)
        {
            _svCheatConVar.SetValue(_originalSvCheat);
            Console.WriteLine($"[无限弹药] sv_cheats 已恢复为 {_originalSvCheat}");
        }
    }
}
