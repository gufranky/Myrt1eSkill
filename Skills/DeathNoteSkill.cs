// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Menu;
using MenuManager;

namespace MyrtleSkill.Skills;

/// <summary>
/// 死神名册技能 - 主动技能
/// 使用菜单选择一名玩家，然后你和他一起死亡
/// 需要依赖 MenuManagerCS2 插件
/// </summary>
public class DeathNoteSkill : PlayerSkill
{
    public override string Name => "DeathNote";
    public override string DisplayName => "💀 死神名册";
    public override string Description => "选择一名玩家，你和他一起死亡！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 9999f; // 9999秒冷却（只能用一次）

    // MenuManager API
    private IMenuApi? _menuApi;
    private readonly PluginCapability<IMenuApi?> _menuCapability = new("menu:nfcore");

    // 追踪每回合是否已使用
    private readonly Dictionary<uint, bool> _usedThisRound = new();

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;
        _usedThisRound[slot] = false;

        // 获取 MenuManager API
        _menuApi = _menuCapability.Get();
        if (_menuApi == null)
        {
            Console.WriteLine("[死神名册] MenuManager Core not found...");
            player.PrintToChat("❌ 需要安装 MenuManagerCS2 插件！");
            return;
        }

        Console.WriteLine($"[死神名册] {player.PlayerName} 获得了死神名册技能");
        player.PrintToChat("💀 你获得了死神名册技能！输入 !useskill 或按键激活！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;
        _usedThisRound.Remove(slot);

        Console.WriteLine($"[死神名册] {player.PlayerName} 失去了死神名册技能");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 检查 MenuManager 是否可用
        if (_menuApi == null)
        {
            player.PrintToChat("❌ 需要安装 MenuManagerCS2 插件！");
            return;
        }

        var slot = player.Index;

        // 检查本回合是否已使用
        if (_usedThisRound.TryGetValue(slot, out var used) && used)
        {
            player.PrintToCenter("❌ 本回合已使用过死神名册！");
            player.PrintToChat("❌ 本回合已使用过死神名册技能！");
            return;
        }

        // 检查玩家是否还活着
        if (!player.PawnIsAlive)
        {
            player.PrintToChat("❌ 你已经死亡了！");
            return;
        }

        // 获取所有其他玩家
        var targets = GetTargets(player);
        if (targets.Count == 0)
        {
            player.PrintToChat("❌ 没有可选择的玩家！");
            return;
        }

        // 显示选择菜单
        ShowTargetMenu(player, targets);
    }

    /// <summary>
    /// 获取所有可选择的玩家（除了自己）
    /// </summary>
    private List<CCSPlayerController> GetTargets(CCSPlayerController player)
    {
        var targets = new List<CCSPlayerController>();

        foreach (var p in Utilities.GetPlayers())
        {
            if (p == null || !p.IsValid)
                continue;

            if (p == player)
                continue;

            if (!p.PawnIsAlive)
                continue;

            // 可以选择任何人（队友或敌人）
            targets.Add(p);
        }

        return targets;
    }

    /// <summary>
    /// 显示目标选择菜单
    /// </summary>
    private void ShowTargetMenu(CCSPlayerController player, List<CCSPlayerController> targets)
    {
        try
        {
            // 创建菜单
            var menu = _menuApi!.GetMenu("💀 选择要一起死亡的玩家");

            // 添加选项
            foreach (var target in targets)
            {
                // 获取目标的技能列表
                var targetSkills = Plugin?.SkillManager.GetPlayerSkills(target);
                var skillNames = targetSkills?.Select(s => s.DisplayName).ToList() ?? new List<string>();

                // 格式化技能列表
                var skillText = skillNames.Count > 0
                    ? string.Join(", ", skillNames.Take(3)) // 最多显示3个技能
                    : "无技能";

                // 菜单选项：玩家名 - 技能
                string optionText = $"{target.PlayerName} - {skillText}";

                menu.AddMenuOption(optionText, (player, option) =>
                {
                    // 玩家选择了这个目标
                    ApplyDeathNote(player, target);
                });
            }

            // 打开菜单
            menu.Open(player);

            Console.WriteLine($"[死神名册] {player.PlayerName} 正在选择目标");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[死神名册] 显示菜单时出错: {ex.Message}");
            player.PrintToChat("❌ 打开菜单失败！");
        }
    }

    /// <summary>
    /// 对双方应用死亡效果
    /// </summary>
    private void ApplyDeathNote(CCSPlayerController player, CCSPlayerController target)
    {
        if (player == null || !player.IsValid || target == null || !target.IsValid)
            return;

        try
        {
            // 标记为已使用
            _usedThisRound[player.Index] = true;

            // 双方都死亡
            player.PlayerPawn.Value?.CommitSuicide(false, true);
            target.PlayerPawn.Value?.CommitSuicide(false, true);

            Console.WriteLine($"[死神名册] {player.PlayerName} 使用了死神名册，与 {target.PlayerName} 一起死亡");

            // 显示提示给施法者
            player.PrintToCenter($"💀 你和 {target.PlayerName} 一起死亡了！");
            player.PrintToChat($"💀 使用死神名册！你和 {target.PlayerName} 一起死亡！");

            // 显示提示给目标
            target.PrintToCenter($"💀 你被 {player.PlayerName} 的死神名册带走了！");
            target.PrintToChat($"💀 {player.PlayerName} 使用了死神名册！你和他一起死亡了！");

            // 广播消息
            Server.PrintToChatAll($"📜 {player.PlayerName} 使用死神名册与 {target.PlayerName} 同归于尽！");

            // 关闭菜单
            _menuApi?.CloseMenu(player);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[死神名册] 应用死亡时出错: {ex.Message}");
            player.PrintToChat("❌ 死神名册失败！");
        }
    }
}
