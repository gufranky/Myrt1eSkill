using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 速射技能 - 被动技能
/// 无后坐力，射速最大化，可以瞬间开火
/// </summary>
public class QuickShotSkill : PlayerSkill
{
    public override string Name => "QuickShot";
    public override string DisplayName => "⚡ 速射";
    public override string Description => "无后坐力！射速最大化！瞬间开火！";
    public override bool IsActive => false; // 被动技能

    // 与专注技能互斥（两者都使用 weapon_recoil_scale ConVar）
    public override List<string> ExcludedSkills => new() { "Focus" };

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
                Console.WriteLine($"[速射] weapon_recoil_scale 从 {_originalRecoilScale} 设置为 0.0");
            }
        }

        Console.WriteLine($"[速射] {player.PlayerName} 获得了速射技能（当前玩家数: {_playerCount}）");

        player.PrintToChat("⚡ 你获得了速射技能！");
        player.PrintToChat("🔫 无后坐力！射速最大化！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 从跟踪列表移除
        _enabledPlayers.Remove(player.Slot);
        _playerCount--;

        Console.WriteLine($"[速射] {player.PlayerName} 失去了速射技能（当前玩家数: {_playerCount}）");

        // 如果没有玩家使用技能，恢复后座力
        if (_playerCount == 0)
        {
            if (_recoilScaleConVar != null)
            {
                _recoilScaleConVar.SetValue(_originalRecoilScale);
                Console.WriteLine($"[速射] weapon_recoil_scale 恢复为 {_originalRecoilScale}");
            }
        }
    }

    /// <summary>
    /// 每帧更新 - 射速最大化（后坐力已通过 ConVar 禁用）
    /// </summary>
    public static void OnTick(PlayerSkillManager skillManager)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid)
                continue;

            // 检查玩家是否有速射技能
            var skills = skillManager.GetPlayerSkills(player);
            if (skills.Count == 0)
                continue;

            var quickShotSkill = skills.FirstOrDefault(s => s.Name == "QuickShot");
            if (quickShotSkill == null)
                continue;

            var pawn = player.PlayerPawn?.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            var weaponServices = pawn.WeaponServices;
            if (weaponServices == null || weaponServices.ActiveWeapon == null || !weaponServices.ActiveWeapon.IsValid)
                continue;

            var weapon = weaponServices.ActiveWeapon.Value;
            if (weapon == null || !weapon.IsValid)
                continue;

            // 设置武器下次攻击时间为当前时间（射速最大化）
            weapon.NextPrimaryAttackTick = Server.TickCount;
            weapon.NextSecondaryAttackTick = Server.TickCount;

            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextSecondaryAttackTick");
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
            Console.WriteLine($"[速射] weapon_recoil_scale 已恢复为 {_originalRecoilScale}");
        }
    }
}
