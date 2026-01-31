using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace MyrtleSkill;

    /// <summary>
    /// 超低重力事件 - 重力0.2 + 空中无扩散
    /// </summary>
public class LowGravityPlusPlusEvent : EntertainmentEvent
{
    public override string Name => "LowGravityPlusPlus";
    public override string DisplayName => "🌑 超低重力";
    public override string Description => "重力大幅降低，空中射击无扩散！";

    private const float TARGET_GRAVITY = 0.2f; // 直接设置为目标值
    private ConVar? _svGravity;
    private float _originalGravity = 800.0f;

    public override void OnApply()
    {
        Console.WriteLine("[超低重力] 设置重力为 " + TARGET_GRAVITY + "，启用无扩散");

        // 获取并保存 sv_gravity ConVar
        _svGravity = ConVar.Find("sv_gravity");
        if (_svGravity != null)
        {
            _originalGravity = _svGravity.GetPrimitiveValue<float>();

            // 设置全局重力（正常值是800，设置为160即0.2倍）
            _svGravity.SetValue(_originalGravity * TARGET_GRAVITY);
            Console.WriteLine($"[超低重力] sv_gravity 从 {_originalGravity} 设置为 {_originalGravity * TARGET_GRAVITY}");
        }

        // 启用无扩散
        Server.ExecuteCommand("weapon_accuracy_nospread 1");
    }

    public override void OnRevert()
    {
        Console.WriteLine("[超低重力] 恢复重力为原始值，禁用无扩散");

        // 恢复全局重力
        if (_svGravity != null)
        {
            _svGravity.SetValue(_originalGravity);
            Console.WriteLine($"[超低重力] sv_gravity 恢复为 {_originalGravity}");
        }

        // 禁用无扩散
        Server.ExecuteCommand("weapon_accuracy_nospread 0");
    }
}
