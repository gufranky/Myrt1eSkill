using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace MyrtleSkill;

/// <summary>
/// 超级跳跃事件 - 开枪自动跳跃且无扩散，免疫落地伤害
/// </summary>
public class JumpPlusPlusEvent : EntertainmentEvent
{
    public override string Name => "JumpPlusPlus";
    public override string DisplayName => "超级跳跃";
    public override string Description => "开枪自动跳跃且无扩散！免疫落地伤害！";

    private ConVar? _fallDamageConVar;
    private float _originalFallDamageScale = 1.0f;

    public override void OnApply()
    {
        Console.WriteLine("[超级跳跃] 事件已激活，启用无扩散和免疫落地伤害");

        // 启用无扩散
        Server.ExecuteCommand("weapon_accuracy_nospread true");

        // 禁用落地伤害
        _fallDamageConVar = ConVar.Find("sv_falldamage_scale");
        if (_fallDamageConVar != null)
        {
            _originalFallDamageScale = _fallDamageConVar.GetPrimitiveValue<float>();
            _fallDamageConVar.SetValue(0.0f);
            Console.WriteLine("[超级跳跃] sv_falldamage_scale 已设置为 0");
        }
        else
        {
            Console.WriteLine("[超级跳跃] 警告：无法找到 sv_falldamage_scale ConVar");
        }

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToCenter("🦘 超级跳跃！\n开枪跳跃 + 无落地伤害！");
                player.PrintToChat("🦘 超级跳跃模式已启用！");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[超级跳跃] 事件已结束，禁用无扩散和恢复落地伤害");

        // 恢复落地伤害设置
        if (_fallDamageConVar != null)
        {
            _fallDamageConVar.SetValue(_originalFallDamageScale);
            Console.WriteLine($"[超级跳跃] sv_falldamage_scale 已恢复为 {_originalFallDamageScale}");
        }

        // 禁用无扩散
        Server.ExecuteCommand("weapon_accuracy_nospread false");

        // 显示提示
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("🦘 超级跳跃模式已禁用");
            }
        }
    }

    /// <summary>
    /// 处理武器射击事件（在主文件的 OnWeaponFire 中调用）
    /// 开枪时自动获得向上速度，不检测是否在地面
    /// </summary>
    public void HandleWeaponFire(EventWeaponFire @event)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        var pawn = player.PlayerPawn.Get();
        if (pawn == null || !pawn.IsValid)
            return;

        // 给予向上速度（不检测是否在地面）
        pawn.AbsVelocity.Z = 400.0f;

        Console.WriteLine($"[超级跳跃] {player.PlayerName} 开枪跳跃");
    }
}
