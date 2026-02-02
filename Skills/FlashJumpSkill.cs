using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MyrtleSkill.Skills;

/// <summary>
/// 闪光跳跃技能 - 被动技能
/// 被你的闪光弹闪到的玩家会根据致盲时长获得向上的跳跃速度
/// </summary>
public class FlashJumpSkill : PlayerSkill
{
    public override string Name => "FlashJump";
    public override string DisplayName => "✈️ 闪光跳跃";
    public override string Description => "你的闪光弹会让敌人飞起来！致盲时间越长飞得越高！";
    public override bool IsActive => false; // 被动技能

    // 跳跃速度计算参数
    private const float BASE_JUMP_VELOCITY = 200f;     // 基础跳跃速度
    private const float MAX_JUMP_VELOCITY = 800f;      // 最大跳跃速度
    private const float VELOCITY_PER_SECOND = 200f;    // 每秒致盲时间增加的速度

    // 给予的闪光弹数量
    private const int FLASHBANG_COUNT = 3;

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[闪光跳跃] {player.PlayerName} 获得了闪光跳跃技能");

        // 给予闪光弹
        GiveFlashbangs(player, FLASHBANG_COUNT);

        player.PrintToChat("✈️ 你获得了闪光跳跃技能！");
        player.PrintToChat("💡 被你的闪光弹闪到的敌人会飞起来！");
        player.PrintToChat($"💣 获得了 {FLASHBANG_COUNT} 颗闪光弹（投掷后自动补充）！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        Console.WriteLine($"[闪光跳跃] {player.PlayerName} 失去了闪光跳跃技能");
    }

    /// <summary>
    /// 给予玩家指定数量的闪光弹
    /// </summary>
    private void GiveFlashbangs(CCSPlayerController player, int count)
    {
        if (player == null || !player.IsValid)
            return;

        try
        {
            for (int i = 0; i < count; i++)
            {
                player.GiveNamedItem("weapon_flashbang");
            }

            Console.WriteLine($"[闪光跳跃] 给予 {player.PlayerName} {count} 个闪光弹");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[闪光跳跃] 给予闪光弹时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理玩家致盲事件 - 让被闪的敌人飞起来
    /// </summary>
    public static void HandlePlayerBlind(EventPlayerBlind @event, PlayerSkillManager skillManager)
    {
        var player = @event.Userid;          // 被闪到的玩家
        var attacker = @event.Attacker;      // 投掷者

        if (player == null || !player.IsValid)
            return;

        if (attacker == null || !attacker.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 检查投掷者是否有闪光跳跃技能
        var attackerSkill = skillManager.GetPlayerSkill(attacker);
        if (attackerSkill?.Name != "FlashJump")
            return;

        // 获取致盲持续时间
        float flashDuration = pawn.FlashDuration;

        if (flashDuration <= 0)
            return;

        // 计算跳跃速度（基于致盲时长）
        float jumpVelocity = BASE_JUMP_VELOCITY + (flashDuration * VELOCITY_PER_SECOND);
        jumpVelocity = Math.Min(jumpVelocity, MAX_JUMP_VELOCITY); // 限制最大速度

        Console.WriteLine($"[闪光跳跃] {attacker.PlayerName} 的闪光弹致盲了 {player.PlayerName}，时长: {flashDuration:F2}秒，跳跃速度: {jumpVelocity:F2}");

        // 应用向上的速度
        pawn.AbsVelocity.Z = jumpVelocity;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_vecAbsVelocity");

        // 显示提示
        player.PrintToCenter($"✈️ 你被闪到了！向上飞起！");
        attacker.PrintToChat($"✈️ {player.PlayerName} 被闪到了，飞向天空！");

        // 自动补充闪光弹给投掷者
        Server.NextFrame(() =>
        {
            if (attacker.IsValid && attackerSkill is FlashJumpSkill flashJumpSkill)
            {
                flashJumpSkill.GiveFlashbangs(attacker, 1);
                attacker.PrintToChat("✈️ 闪光弹已自动补充！");
            }
        });
    }
}
