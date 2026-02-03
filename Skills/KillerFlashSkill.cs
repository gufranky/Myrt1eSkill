using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MyrtleSkill.Skills;

/// <summary>
/// 杀手闪电技能 - 任何被你的闪光弹完全致盲的人都会死亡（包括你自己）
/// </summary>
public class KillerFlashSkill : PlayerSkill
{
    public override string Name => "KillerFlash";
    public override string DisplayName => "⚡ 杀手闪电";
    public override string Description => "你的闪光弹变得致命！任何被完全致盲的人都会死亡（包括你自己！）";
    public override bool IsActive => false; // 被动技能

    // 致盲持续时间阈值（秒）
    private const float FLASH_DURATION_THRESHOLD = 2.2f;

    public override void OnApply(CCSPlayerController player)
    {
        Console.WriteLine($"[杀手闪电] {player.PlayerName} 获得了杀手闪电技能");

        // 给予玩家闪光弹
        player.GiveNamedItem("weapon_flashbang");

        player.PrintToChat("⚡ 你获得了杀手闪电技能！");
        player.PrintToChat("💡 任何被你的闪光弹完全致盲的人都会死亡！");
        player.PrintToChat("⚠️ 注意：你自己也会被影响！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        Console.WriteLine($"[杀手闪电] {player.PlayerName} 失去了杀手闪电技能");
    }

    /// <summary>
    /// 处理玩家致盲事件
    /// </summary>
    public static void HandlePlayerBlind(EventPlayerBlind @event, PlayerSkillManager skillManager)
    {
        var player = @event.Userid;
        var attacker = @event.Attacker;

        if (player == null || !player.IsValid || player.PlayerPawn.Value == null)
            return;

        if (attacker == null || !attacker.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        float flashDuration = pawn.FlashDuration;

        // 检查投掷者是否有杀手闪电技能
        var attackerSkill = skillManager.GetPlayerSkill(attacker);
        if (attackerSkill == null || attackerSkill.Name != "KillerFlash")
            return;

        // 检查致盲持续时间是否达到阈值
        if (flashDuration >= FLASH_DURATION_THRESHOLD)
        {
            Console.WriteLine($"[杀手闪电] {attacker.PlayerName} 的闪光弹致盲了 {player.PlayerName}，持续时间: {flashDuration:F2}秒");

            // 造成 999 点致命伤害
            Server.NextFrame(() =>
            {
                if (pawn != null && pawn.IsValid && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                {
                    var damageInfo = new CTakeDamageInfo
                    {
                        Damage = 999,
                        Attacker = attacker.PlayerPawn.Value,
                        BitsDamageType = (uint)DamageType_t.DMG_GENERIC
                    };
                    pawn.TakeDamage(damageInfo);
                    Console.WriteLine($"[杀手闪电] {player.PlayerName} 受到 999 伤害");
                }
            });

            // 显示消息
            if (player == attacker)
            {
                Server.PrintToChatAll($"⚡ {player.PlayerName} 被自己的杀手闪电闪死了！");
            }
            else
            {
                Server.PrintToChatAll($"⚡ {player.PlayerName} 被 {attacker.PlayerName} 的杀手闪电闪死了！");
            }
        }
    }
}
