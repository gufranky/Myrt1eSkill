// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using MyrtleSkill.Utils;

namespace MyrtleSkill;

/// <summary>
/// 破产之枪事件 - 所有玩家打出的伤害改为扣除金钱
/// 伤害改为：扣除金钱 = 伤害 * 50
/// 如果金钱为0则直接死亡
/// 事件开始时所有玩家+5000金币
/// </summary>
public class BankruptcyWeaponEvent : EntertainmentEvent
{
    public override string Name => "BankruptcyWeapon";
    public override string DisplayName => "💸 破产之枪";
    public override string Description => "所有伤害改为扣除金钱！伤害×50！金钱为0直接死亡！开局+5000金币！";

    // 金钱倍数
    private const int MONEY_MULTIPLIER = 50;

    // 开局金币奖励
    private const int START_MONEY = 5000;

    public override void OnApply()
    {
        Console.WriteLine("[破产之枪] 事件已激活");

        // 给所有玩家+5000金币
        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive)
                continue;

            try
            {
                // 获取当前金钱
                var moneyServices = player.InGameMoneyServices;
                if (moneyServices == null)
                    continue;

                int currentMoney = 0;
                try
                {
                    currentMoney = moneyServices.Account;
                }
                catch
                {
                    currentMoney = 0;
                }

                // 增加5000金币
                moneyServices.Account = currentMoney + START_MONEY;

                // 通知客户端
                Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");

                player.PrintToChat($"💸 破产之枪模式！+{START_MONEY}金币！");
                player.PrintToChat($"⚠️ 所有伤害改为扣除金币（×{MONEY_MULTIPLIER}）");
                player.PrintToChat($"💰 当前金币：{currentMoney + START_MONEY}");

                Console.WriteLine($"[破产之枪] {player.PlayerName} 获得 {START_MONEY} 金币");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[破产之枪] 给 {player.PlayerName} 增加金币时出错: {ex.Message}");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[破产之枪] 事件已恢复");
    }

    /// <summary>
    /// 处理玩家受伤事件 - 将伤害改为扣除金钱
    /// 参考名刀的 HookPlayerHurt 实现
    /// </summary>
    public void HandlePlayerHurt(EventPlayerHurt @event)
    {
        var attacker = @event.Attacker;
        var victim = @event.Userid;

        if (attacker == null || !attacker.IsValid || !attacker.PawnIsAlive)
            return;

        if (victim == null || !victim.IsValid || !victim.PawnIsAlive)
            return;

        // 不能伤害自己
        if (attacker == victim)
            return;

        var victimPawn = victim.PlayerPawn.Value;
        if (victimPawn == null || !victimPawn.IsValid)
            return;

        // 获取伤害值
        int damage = @event.DmgHealth;
        if (damage <= 0)
            return;

        Console.WriteLine($"[破产之枪] {attacker.PlayerName} 对 {victim.PlayerName} 造成 {damage} 伤害，转换为扣钱");

        // 计算扣除的金钱
        int moneyToLose = damage * MONEY_MULTIPLIER;

        // 获取当前金币
        var moneyServices = victim.InGameMoneyServices;
        if (moneyServices == null)
            return;

        int inGameMoney = 0;
        try
        {
            inGameMoney = moneyServices.Account;
        }
        catch
        {
            inGameMoney = 0;
        }

        if (inGameMoney <= 0)
        {
            // 金币为0，直接击杀
            Console.WriteLine($"[破产之枪] {victim.PlayerName} 金币为0，直接击杀");

            // 使用999伤害（参考杀手闪光）
            SkillUtils.DealDamage(attacker, victim, 999);

            Server.PrintToChatAll($"💸 {victim.PlayerName} 因为破产被击杀！");
            victim.PrintToCenter("💸 你破产了！直接死亡！");
        }
        else
        {
            // 扣除金币
            int newMoney = Math.Max(0, inGameMoney - moneyToLose);

            // 设置新金币
            moneyServices.Account = newMoney;
            Utilities.SetStateChanged(victim, "CCSPlayerController", "m_pInGameMoneyServices");

            // 抵消伤害（设置为1点伤害，避免无敌）
            victimPawn.Health = Math.Max(1, victimPawn.Health - 1);
            Utilities.SetStateChanged(victimPawn, "CBaseEntity", "m_iHealth");

            Console.WriteLine($"[破产之枪] {victim.PlayerName} 失去 {moneyToLose} 金币：{inGameMoney} → {newMoney}");

            // 显示提示
            victim.PrintToCenter($"💸 失去 {moneyToLose} 金币！");
            attacker.PrintToChat($"💸 对 {victim.PlayerName} 造成 {damage} 伤害 = 扣除 {moneyToLose} 金币！");
        }
    }
}
