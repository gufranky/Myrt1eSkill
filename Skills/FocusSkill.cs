// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace MyrtleSkill.Skills;

/// <summary>
/// 专注技能 - 被动技能
/// 射击时无后座力！
/// </summary>
public class FocusSkill : PlayerSkill
{
    public override string Name => "Focus";
    public override string DisplayName => "🎯 专注";
    public override string Description => "射击时无后座力！";
    public override bool IsActive => false; // 被动技能

    // 与速射技能互斥（两者都使用 weapon_recoil_scale ConVar）
    public override List<string> ExcludedSkills => new() { "QuickShot" };

    // 全局 ConVar（所有拥有该技能的玩家共享）
    private static ConVar? _recoilScaleConVar;
    private static float _originalRecoilScale = 1.0f;
    private static int _playerCount = 0; // 拥有该技能的玩家数量

    // 跟踪拥有该技能的玩家
    private static readonly HashSet<int> _enabledPlayers = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 添加到跟踪列表
        _enabledPlayers.Add(player.Slot);
        _playerCount++;

        // 首次应用时保存原始值并禁用后座力
        if (_playerCount == 1)
        {
            _recoilScaleConVar = ConVar.Find("weapon_recoil_scale");
            if (_recoilScaleConVar != null)
            {
                _originalRecoilScale = _recoilScaleConVar.GetPrimitiveValue<float>();
                _recoilScaleConVar.SetValue(0.0f);
                Console.WriteLine($"[专注] weapon_recoil_scale 从 {_originalRecoilScale} 设置为 0.0");
            }
        }

        Console.WriteLine($"[专注] {player.PlayerName} 获得了专注技能（当前玩家数: {_playerCount}）");

        player.PrintToChat("🎯 你获得了专注技能！");
        player.PrintToChat("💡 射击时无后座力！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 从跟踪列表移除
        _enabledPlayers.Remove(player.Slot);
        _playerCount--;

        Console.WriteLine($"[专注] {player.PlayerName} 失去了专注技能（当前玩家数: {_playerCount}）");

        // 如果没有玩家使用技能，恢复后座力
        if (_playerCount == 0)
        {
            if (_recoilScaleConVar != null)
            {
                _recoilScaleConVar.SetValue(_originalRecoilScale);
                Console.WriteLine($"[专注] weapon_recoil_scale 恢复为 {_originalRecoilScale}");
            }
        }
    }

    /// <summary>
    /// 清理所有状态（插件卸载或回合结束时调用）
    /// </summary>
    public static void Cleanup()
    {
        _enabledPlayers.Clear();
        _playerCount = 0;

        if (_recoilScaleConVar != null)
        {
            _recoilScaleConVar.SetValue(_originalRecoilScale);
            Console.WriteLine($"[专注] weapon_recoil_scale 已恢复为 {_originalRecoilScale}");
        }
    }
}
