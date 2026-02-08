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
/// 三选一技能 - 主动技能
/// 给玩家一个菜单，随机抽取3个技能供选择
/// 需要依赖 MenuManagerCS2 插件
/// </summary>
public class ChooseOneOfThreeSkill : PlayerSkill
{
    public override string Name => "ChooseOneOfThree";
    public override string DisplayName => "🎰 三选一";
    public override string Description => "随机抽取3个技能，选择一个获得！";
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
            Console.WriteLine("[三选一] MenuManager Core not found...");
            player.PrintToChat("❌ 需要安装 MenuManagerCS2 插件！");
            return;
        }

        Console.WriteLine($"[三选一] {player.PlayerName} 获得了三选一技能");
        player.PrintToChat("🎰 你获得了三选一技能！输入 !useskill 或按键激活！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;
        _usedThisRound.Remove(slot);

        Console.WriteLine($"[三选一] {player.PlayerName} 失去了三选一技能");
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
            player.PrintToCenter("❌ 本回合已使用过三选一！");
            player.PrintToChat("❌ 本回合已使用过三选一技能！");
            return;
        }

        // 获取所有可用技能名称
        var allSkillNames = Plugin?.SkillManager.GetAllSkillNames();
        if (allSkillNames == null || allSkillNames.Count == 0)
        {
            player.PrintToChat("❌ 没有可用技能！");
            return;
        }

        // 转换为技能对象并过滤
        var availableSkills = new List<PlayerSkill>();
        foreach (var skillName in allSkillNames)
        {
            if (skillName == "ChooseOneOfThree")
                continue;

            var skill = Plugin?.SkillManager.GetSkill(skillName);
            if (skill != null && skill.Weight > 0)
            {
                availableSkills.Add(skill);
            }
        }

        if (availableSkills.Count < 3)
        {
            player.PrintToChat("❌ 可用技能数量不足！");
            return;
        }

        // 随机选择3个技能
        var random = new Random();
        var selectedSkills = availableSkills
            .OrderBy(x => random.Next())
            .Take(3)
            .ToList();

        // 显示菜单
        ShowChooseMenu(player, selectedSkills);
    }

    /// <summary>
    /// 显示选择菜单
    /// </summary>
    private void ShowChooseMenu(CCSPlayerController player, List<PlayerSkill> skills)
    {
        try
        {
            // 创建菜单
            var menu = _menuApi!.GetMenu("🎰 选择一个技能");

            // 添加选项
            for (int i = 0; i < skills.Count; i++)
            {
                var skill = skills[i];
                int index = i; // 闭包捕获

                menu.AddMenuOption($"{skill.DisplayName} - {skill.Description}", (player, option) =>
                {
                    // 玩家选择了这个技能
                    ApplySelectedSkill(player, skill);
                });
            }

            // 打开菜单
            menu.Open(player);

            Console.WriteLine($"[三选一] {player.PlayerName} 正在选择技能");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[三选一] 显示菜单时出错: {ex.Message}");
            player.PrintToChat("❌ 打开菜单失败！");
        }
    }

    /// <summary>
    /// 应用选中的技能
    /// </summary>
    private void ApplySelectedSkill(CCSPlayerController player, PlayerSkill selectedSkill)
    {
        if (player == null || !player.IsValid || selectedSkill == null)
            return;

        try
        {
            // 移除三选一技能本身
            Plugin?.SkillManager.RemoveSkillFromPlayer(player);

            // 应用选中的技能（保留其他技能）
            Plugin?.SkillManager.ApplySpecificSkillToPlayer(player, selectedSkill.Name);

            // 标记为已使用
            _usedThisRound[player.Index] = true;

            Console.WriteLine($"[三选一] {player.PlayerName} 选择了 {selectedSkill.DisplayName}");

            // 显示提示
            player.PrintToCenter($"✨ 获得了 {selectedSkill.DisplayName}！");
            player.PrintToChat($"✨ 你选择了 {selectedSkill.DisplayName}！");
            player.PrintToChat($"💡 {selectedSkill.Description}");

            // 关闭菜单
            _menuApi?.CloseMenu(player);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[三选一] 应用技能时出错: {ex.Message}");
            player.PrintToChat("❌ 应用技能失败！");
        }
    }
}
