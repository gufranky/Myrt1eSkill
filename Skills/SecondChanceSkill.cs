using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;

namespace MyrtleSkill.Skills;

/// <summary>
/// 第二次机会技能 - 死亡后以50血复活
/// </summary>
public class SecondChanceSkill : PlayerSkill
{
    public override string Name => "SecondChance";
    public override string DisplayName => "🔄 第二次机会";
    public override string Description => "死亡后，你会以相同的生命值复活！每回合限用一次！";
    public override bool IsActive => false; // 被动技能

    // 复活血量
    private const int REVIVE_HEALTH = 50;

    // 跟踪已使用第二次机会的玩家
    private static readonly ConcurrentDictionary<int, byte> _secondChanceUsed = new();
    // 跟踪玩家死亡前的护甲值
    private static readonly ConcurrentDictionary<int, int> _playerArmor = new();

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[第二次机会] {player.PlayerName} 获得了第二次机会技能");

        // 不再在获得技能时修改血量和护甲

        player.PrintToChat("🔄 你获得了第二次机会技能！");
        player.PrintToChat($"💀 死亡后会以 {REVIVE_HEALTH} 血复活！");
        player.PrintToChat("⚠️ 每回合只能使用一次！护甲会保留！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        // 移除技能时清理记录
        _secondChanceUsed.TryRemove(player.Slot, out _);
        _playerArmor.TryRemove(player.Slot, out _);

        Console.WriteLine($"[第二次机会] {player.PlayerName} 失去了第二次机会技能");
    }

    /// <summary>
    /// 处理玩家受伤事件
    /// </summary>
    public static void HandlePlayerHurt(EventPlayerHurt @event)
    {
        var victim = @event.Userid;
        if (victim == null || !victim.IsValid)
            return;

        var victimPawn = victim.PlayerPawn.Value;
        if (victimPawn == null || !victimPawn.IsValid)
            return;

        // 检查玩家是否有第二次机会技能
        var skillManager = MyrtleSkill.Instance?.SkillManager;
        if (skillManager == null)
            return;

        var skill = skillManager.GetPlayerSkill(victim);
        if (skill == null || skill.Name != "SecondChance")
            return;

        // 检查是否死亡（血量 <= 0）且还没使用过第二次机会
        if (victimPawn.Health > 0 || _secondChanceUsed.ContainsKey(victim.Slot))
            return;

        Console.WriteLine($"[第二次机会] {victim.PlayerName} 死亡，触发第二次机会复活");

        // 保存当前护甲值
        int currentArmor = victimPawn.ArmorValue;
        _playerArmor[victim.Slot] = currentArmor;

        // 标记已使用
        _secondChanceUsed.TryAdd(victim.Slot, 0);

        // 复活时只设置血量，不影响护甲
        SetHealthOnly(victim, REVIVE_HEALTH);

        // 恢复护甲
        if (currentArmor > 0)
        {
            victimPawn.ArmorValue = currentArmor;
            Utilities.SetStateChanged(victimPawn, "CCSPlayerPawn", "m_ArmorValue");
            Console.WriteLine($"[第二次机会] {victim.PlayerName} 恢复护甲: {currentArmor}");
        }

        var spawn = GetSpawnVector(victim);
        if (spawn != null)
        {
            victimPawn.Teleport(spawn, victimPawn.AbsRotation, new Vector(0, 0, 0));
        }

        // 显示提示
        victim.PrintToCenter("🔄 第二次机会！");
        victim.PrintToChat($"🔄 你使用了第二次机会！以 {REVIVE_HEALTH} 血复活！护甲已保留！");

        Server.PrintToChatAll($"🔄 {victim.PlayerName} 使用了第二次机会复活！");
    }

    /// <summary>
    /// 回合开始时清理使用记录
    /// </summary>
    public static void OnRoundStart()
    {
        _secondChanceUsed.Clear();
        _playerArmor.Clear();
        Console.WriteLine("[第二次机会] 新回合开始，清空使用记录");
    }

    /// <summary>
    /// 只设置玩家血量，不影响护甲
    /// </summary>
    private static void SetHealthOnly(CCSPlayerController player, int health)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        pawn.Health = health;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        Console.WriteLine($"[第二次机会] {player.PlayerName} 血量设置为 {health}，护甲保持不变");
    }

    /// <summary>
    /// 获取出生点位置
    /// </summary>
    private static Vector? GetSpawnVector(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return null;

        var absOrigin = pawn.AbsOrigin;

        // 根据队伍选择出生点
        string spawnPointName = player.Team == CsTeam.Terrorist
            ? "info_player_terrorist"
            : "info_player_counterterrorist";

        var spawns = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>(spawnPointName).ToList();
        if (spawns.Count != 0)
        {
            var random = new Random();
            var randomSpawn = spawns[random.Next(spawns.Count)];
            return randomSpawn.AbsOrigin;
        }

        // 如果找不到出生点，返回当前位置
        return absOrigin;
    }
}
