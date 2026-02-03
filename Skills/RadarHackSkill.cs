// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details
// Based on jRandomSkills by Juzlus (RadarHack skill)

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 雷达黑客技能 - 雷达上可以看到敌人
/// </summary>
public class RadarHackSkill : PlayerSkill
{
    public override string Name => "RadarHack";
    public override string DisplayName => "📡 雷达黑客";
    public override string Description => "雷达上可以看到敌人！知晓敌人位置！";
    public override bool IsActive => false; // 被动技能

    // 与透视事件互斥
    public override List<string> ExcludedEvents => new() { "Xray", "SuperpowerXray" };

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[雷达黑客] {player.PlayerName} 获得了雷达黑客技能");
        player.PrintToChat("📡 你获得了雷达黑客技能！");
        player.PrintToChat("💡 雷达上可以看到敌人的位置！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        Console.WriteLine($"[雷达黑客] {player.PlayerName} 失去了雷达黑客技能");
    }

    /// <summary>
    /// 每帧更新（在MyrtleSkill的OnServerPostEntityThink中调用）
    /// </summary>
    public void OnTick(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        SetEnemiesVisibleOnRadar(player);
    }

    /// <summary>
    /// 设置敌人在雷达上可见
    /// </summary>
    private void SetEnemiesVisibleOnRadar(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || player.PlayerPawn?.Value == null)
            return;

        int playerIndex = (int)player.Index - 1;

        // 让所有敌人在雷达上可见
        foreach (var enemy in Utilities.GetPlayers())
        {
            if (!enemy.IsValid || !enemy.PawnIsAlive)
                continue;

            if (enemy.Team == player.Team)
                continue;

            var enemyPawn = enemy.PlayerPawn.Value;
            if (enemyPawn == null)
                continue;

            // 设置敌人在该玩家的雷达上可见
            enemyPawn.EntitySpottedState.SpottedByMask[0] |= (1u << (int)(playerIndex % 32));
        }

        // 让C4在雷达上可见
        var bombEntities = Utilities.FindAllEntitiesByDesignerName<CC4>("weapon_c4").ToList();
        if (bombEntities.Any())
        {
            var bomb = bombEntities.FirstOrDefault();
            if (bomb != null && bomb.IsValid)
            {
                bomb.EntitySpottedState.SpottedByMask[0] |= (1u << (int)(playerIndex % 32));
            }
        }
    }
}
