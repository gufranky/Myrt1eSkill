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
/// 沉默技能 - 主动技能
/// 使用菜单选择对方一名玩家，禁用其所有技能
/// 需要依赖 MenuManagerCS2 插件
/// </summary>
public class SilencerSkill : PlayerSkill
{
    public override string Name => "Silencer";
    public override string DisplayName => "🔇 沉默";
    public override string Description => "选择一名敌人，禁用其所有技能！";
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
            Console.WriteLine("[沉默] MenuManager Core not found...");
            player.PrintToChat("❌ 需要安装 MenuManagerCS2 插件！");
            return;
        }

        Console.WriteLine($"[沉默] {player.PlayerName} 获得了沉默技能");
        player.PrintToChat("🔇 你获得了沉默技能！输入 !useskill 或按键激活！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;
        _usedThisRound.Remove(slot);

        Console.WriteLine($"[沉默] {player.PlayerName} 失去了沉默技能");
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
            player.PrintToCenter("❌ 本回合已使用过沉默！");
            player.PrintToChat("❌ 本回合已使用过沉默技能！");
            return;
        }

        // 获取所有敌人
        var enemies = GetEnemies(player);
        if (enemies.Count == 0)
        {
            player.PrintToChat("❌ 没有可选择的敌人！");
            return;
        }

        // 显示选择菜单
        ShowTargetMenu(player, enemies);
    }

    /// <summary>
    /// 获取所有敌人
    /// </summary>
    private List<CCSPlayerController> GetEnemies(CCSPlayerController player)
    {
        var enemies = new List<CCSPlayerController>();

        foreach (var p in Utilities.GetPlayers())
        {
            if (p == null || !p.IsValid)
                continue;

            if (p == player)
                continue;

            if (!p.PawnIsAlive)
                continue;

            // 检查是否是敌人（不同队伍）
            if (player.PlayerPawn.Value?.TeamNum != p.PlayerPawn.Value?.TeamNum)
            {
                enemies.Add(p);
            }
        }

        return enemies;
    }

    /// <summary>
    /// 显示目标选择菜单
    /// </summary>
    private void ShowTargetMenu(CCSPlayerController player, List<CCSPlayerController> enemies)
    {
        try
        {
            // 创建菜单
            var menu = _menuApi!.GetMenu("🔇 选择要沉默的敌人");

            // 添加选项（显示为"玩家名 - 技能列表"）
            foreach (var enemy in enemies)
            {
                // 获取敌人的技能列表
                var enemySkills = Plugin?.SkillManager.GetPlayerSkills(enemy);
                var skillNames = enemySkills?.Select(s => s.DisplayName).ToList() ?? new List<string>();

                // 格式化技能列表
                var skillText = skillNames.Count > 0
                    ? string.Join(", ", skillNames.Take(3)) // 最多显示3个技能
                    : "无技能";

                // 菜单选项：玩家名 - 技能
                string optionText = $"{enemy.PlayerName} - {skillText}";

                menu.AddMenuOption(optionText, (player, option) =>
                {
                    // 玩家选择了这个敌人
                    ApplySilence(player, enemy);
                });
            }

            // 打开菜单
            menu.Open(player);

            Console.WriteLine($"[沉默] {player.PlayerName} 正在选择目标");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[沉默] 显示菜单时出错: {ex.Message}");
            player.PrintToChat("❌ 打开菜单失败！");
        }
    }

    /// <summary>
    /// 对敌人应用沉默效果
    /// </summary>
    private void ApplySilence(CCSPlayerController player, CCSPlayerController target)
    {
        if (player == null || !player.IsValid || target == null || !target.IsValid)
            return;

        try
        {
            // 获取目标当前的所有技能
            var targetSkills = Plugin?.SkillManager.GetPlayerSkills(target);
            if (targetSkills == null || targetSkills.Count == 0)
            {
                player.PrintToChat($"❌ {target.PlayerName} 没有任何技能！");
                return;
            }

            // 移除目标的所有技能
            Plugin?.SkillManager.RemoveSkillFromPlayer(target);

            // 标记为已使用
            _usedThisRound[player.Index] = true;

            // 记录被移除的技能
            var removedSkillNames = targetSkills.Select(s => s.DisplayName).ToList();
            var removedSkillsText = string.Join(", ", removedSkillNames);

            Console.WriteLine($"[沉默] {player.PlayerName} 沉默了 {target.PlayerName}，移除技能：{removedSkillsText}");

            // 显示提示给施法者
            player.PrintToCenter($"🔇 沉默了 {target.PlayerName}！");
            player.PrintToChat($"🔇 成功沉默 {target.PlayerName}！");
            player.PrintToChat($"💡 移除技能：{removedSkillsText}");

            // 显示提示给受害者
            target.PrintToCenter($"🔇 你的技能被 {player.PlayerName} 沉默了！");
            target.PrintToChat($"🔇 你的技能被 {player.PlayerName} 沉默了！");
            target.PrintToChat($"💡 失去技能：{removedSkillsText}");

            // 关闭菜单
            _menuApi?.CloseMenu(player);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[沉默] 应用沉默时出错: {ex.Message}");
            player.PrintToChat("❌ 沉默失败！");
        }
    }
}
