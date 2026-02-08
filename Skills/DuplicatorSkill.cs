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
/// 复制者技能 - 主动技能
/// 选择一个敌人，复制他的技能
/// 需要依赖 MenuManagerCS2 插件
/// </summary>
public class DuplicatorSkill : PlayerSkill
{
    public override string Name => "Duplicator";
    public override string DisplayName => "📋 复制者";
    public override string Description => "选择一个敌人，复制他的技能！";
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
            Console.WriteLine("[复制者] MenuManager Core not found...");
            player.PrintToChat("❌ 需要安装 MenuManagerCS2 插件！");
            return;
        }

        Console.WriteLine($"[复制者] {player.PlayerName} 获得了复制者技能");
        player.PrintToChat("📋 你获得了复制者技能！输入 !useskill 或按键激活！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;
        _usedThisRound.Remove(slot);

        Console.WriteLine($"[复制者] {player.PlayerName} 失去了复制者技能");
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
            player.PrintToCenter("❌ 本回合已使用过复制者！");
            player.PrintToChat("❌ 本回合已使用过复制者技能！");
            return;
        }

        // 检查玩家是否还活着
        if (!player.PawnIsAlive)
        {
            player.PrintToChat("❌ 你已经死亡了！");
            return;
        }

        // 获取所有敌人
        var enemies = GetEnemies(player);
        if (enemies.Count == 0)
        {
            player.PrintToChat("❌ 没有可复制的敌人！");
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

            // 只能选择敌人（不同队伍）
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
            var menu = _menuApi!.GetMenu("📋 选择要复制技能的敌人");

            // 添加选项（显示为"玩家名 - 技能列表"）
            foreach (var enemy in enemies)
            {
                // 获取敌人的技能列表
                var enemySkills = Plugin?.SkillManager.GetPlayerSkills(enemy);
                var skillNames = enemySkills?.Select(s => s.DisplayName).ToList() ?? new List<string>();

                // 格式化技能列表
                var skillText = skillNames.Count > 0
                    ? string.Join(", ", skillNames) // 显示所有技能
                    : "无技能";

                // 菜单选项：玩家名 - 技能
                string optionText = $"{enemy.PlayerName} - {skillText}";

                menu.AddMenuOption(optionText, (player, option) =>
                {
                    // 玩家选择了这个敌人
                    CopySkills(player, enemy);
                });
            }

            // 打开菜单
            menu.Open(player);

            Console.WriteLine($"[复制者] {player.PlayerName} 正在选择要复制的敌人");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[复制者] 显示菜单时出错: {ex.Message}");
            player.PrintToChat("❌ 打开菜单失败！");
        }
    }

    /// <summary>
    /// 复制敌人的技能
    /// </summary>
    private void CopySkills(CCSPlayerController player, CCSPlayerController target)
    {
        if (player == null || !player.IsValid || target == null || !target.IsValid)
            return;

        try
        {
            // 获取敌人的所有技能
            var targetSkills = Plugin?.SkillManager.GetPlayerSkills(target);
            if (targetSkills == null || targetSkills.Count == 0)
            {
                player.PrintToChat($"❌ {target.PlayerName} 没有任何技能可复制！");
                return;
            }

            // 移除复制者技能本身
            Plugin?.SkillManager.RemoveSkillFromPlayer(player);

            // 复制所有敌人的技能
            foreach (var skill in targetSkills)
            {
                Plugin?.SkillManager.ApplySpecificSkillToPlayer(player, skill.Name);
            }

            // 标记为已使用
            _usedThisRound[player.Index] = true;

            // 获取复制的技能名称
            var copiedSkillNames = targetSkills.Select(s => s.DisplayName).ToList();
            var copiedSkillsText = string.Join(", ", copiedSkillNames);

            Console.WriteLine($"[复制者] {player.PlayerName} 复制了 {target.PlayerName} 的技能：{copiedSkillsText}");

            // 显示提示
            player.PrintToCenter($"📋 成功复制 {target.PlayerName} 的技能！");
            player.PrintToChat($"📋 你复制了 {target.PlayerName} 的技能！");
            player.PrintToChat($"💡 获得技能：{copiedSkillsText}");

            // 显示提示给目标
            target.PrintToCenter($"⚠️ 你的技能被 {player.PlayerName} 复制了！");
            target.PrintToChat($"⚠️ 你的技能被 {player.PlayerName} 复制了！");

            // 关闭菜单
            _menuApi?.CloseMenu(player);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[复制者] 复制技能时出错: {ex.Message}");
            player.PrintToChat("❌ 复制技能失败！");
        }
    }
}
