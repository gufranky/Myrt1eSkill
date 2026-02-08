// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

namespace MyrtleSkill.Skills;

/// <summary>
/// 高风险，高回报技能 - 被动技能
/// 获得技能时血量降低到20，击杀敌人后血量增加到500
/// </summary>
public class HighRiskHighRewardSkill : PlayerSkill
{
    public override string Name => "HighRiskHighReward";
    public override string DisplayName => "🎲 高风险，高回报";
    public override string Description => "开局只有20点血！击杀敌人后血量增加到500！";
    public override bool IsActive => false; // 被动技能
    public override float Cooldown => 0f; // 被动技能无冷却

    // 与其他生存技能互斥
    public override List<string> ExcludedSkills => new() { "BigStomach", "Juggernaut", "SecondChance", "Meito" };

    // 追踪已获得击杀奖励的玩家
    private static readonly HashSet<ulong> _rewardedPlayers = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 设置血量为20
        pawn.Health = 20;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        Console.WriteLine($"[高风险，高回报] {player.PlayerName} 的血量已设置为20");

        // 显示提示
        player.PrintToCenter("🎲 高风险！血量：20");
        player.PrintToChat("🎲 你获得了高风险，高回报技能！");
        player.PrintToChat("💀 开局只有20点血！击杀敌人后血量增加到500！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 移除追踪记录
        _rewardedPlayers.Remove(player.SteamID);

        // 恢复血量到100
        pawn.Health = 100;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        Console.WriteLine($"[高风险，高回报] {player.PlayerName} 失去了高风险，高回报技能，血量已恢复到100");
    }

    /// <summary>
    /// 处理玩家击杀事件
    /// </summary>
    public void OnPlayerDeath(EventPlayerDeath @event)
    {
        var attacker = @event.Attacker;
        if (attacker == null || !attacker.IsValid)
            return;

        // 检查击杀者是否有高风险，高回报技能
        var skills = Plugin?.SkillManager.GetPlayerSkills(attacker);
        if (skills == null || skills.Count == 0)
            return;

        var highRiskSkill = skills.FirstOrDefault(s => s.Name == "HighRiskHighReward");
        if (highRiskSkill == null)
            return;

        // 检查是否已经获得过奖励
        if (_rewardedPlayers.Contains(attacker.SteamID))
        {
            Console.WriteLine($"[高风险，高回报] {attacker.PlayerName} 已经获得过击杀奖励");
            return;
        }

        var pawn = attacker.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 检查击杀者是否还活着（可能被反杀）
        if (pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            return;

        // 设置血量为500
        pawn.Health = 500;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        // 标记已获得奖励
        _rewardedPlayers.Add(attacker.SteamID);

        Console.WriteLine($"[高风险，高回报] {attacker.PlayerName} 击杀敌人，血量增加到500");

        // 显示提示
        attacker.PrintToCenter("🎲 高回报！血量：500");
        attacker.PrintToChat("🎲 高回报！血量已增加到500！");
    }

    /// <summary>
    /// 清理记录（回合结束时调用）
    /// </summary>
    public static void ClearRewardedPlayers()
    {
        _rewardedPlayers.Clear();
        Console.WriteLine("[高风险，高回报] 已清理所有奖励记录");
    }
}
