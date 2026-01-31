using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace MyrtleSkill;

/// <summary>
/// 低重力事件 - 玩家跳跃更高
/// </summary>
public class LowGravityEvent : EntertainmentEvent
{
    public override string Name => "LowGravity";
    public override string DisplayName => "🌑 低重力";
    public override string Description => "玩家可以跳得更高！";

    private const float TARGET_GRAVITY = 0.5f; // 直接设置为目标值
    private ConVar? _svGravity;
    private float _originalGravity = 800.0f;

    public override void OnApply()
    {
        Console.WriteLine("[低重力] 设置重力为 " + TARGET_GRAVITY + " 倍");

        // 获取并保存 sv_gravity ConVar
        _svGravity = ConVar.Find("sv_gravity");
        if (_svGravity != null)
        {
            _originalGravity = _svGravity.GetPrimitiveValue<float>();

            // 设置全局重力（正常值是800，设置为400即0.5倍）
            _svGravity.SetValue(_originalGravity * TARGET_GRAVITY);
            Console.WriteLine($"[低重力] sv_gravity 从 {_originalGravity} 设置为 {_originalGravity * TARGET_GRAVITY}");
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[低重力] 恢复重力为原始值");

        // 恢复全局重力
        if (_svGravity != null)
        {
            _svGravity.SetValue(_originalGravity);
            Console.WriteLine($"[低重力] sv_gravity 恢复为 {_originalGravity}");
        }
    }
}
