// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Menu;
using MenuManager;
using System.Collections.Concurrent;

namespace MyrtleSkill.Skills;

/// <summary>
/// 故障技能 - 主动技能
/// 使用菜单选择对方一名玩家，禁用其雷达
/// 需要依赖 MenuManagerCS2 插件
/// </summary>
public class GlitchSkill : PlayerSkill
{
    public override string Name => "Glitch";
    public override string DisplayName => "📡 故障";
    public override string Description => "选择一名敌人，禁用其雷达！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 30.0f; // 30秒冷却

    // MenuManager API
    private IMenuApi? _menuApi;
    private readonly PluginCapability<IMenuApi?> _menuCapability = new("menu:nfcore");

    // 追踪每回合使用次数
    private readonly Dictionary<uint, int> _usageCount = new();

    // 每回合最大使用次数
    private const int MAX_USES_PER_ROUND = 2;

    // 雷达禁用持续时间（秒）
    private const float GLITCH_DURATION = 15.0f;

    // 追踪被禁用雷达的玩家
    private static readonly ConcurrentDictionary<ulong, GlitchInfo> _glitchedPlayers = new();

    // 故障信息
    private class GlitchInfo
    {
        public CCSPlayerController? Victim { get; set; }
        public float EndTime { get; set; }
    }

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;
        _usageCount[slot] = 0;

        // 获取 MenuManager API
        _menuApi = _menuCapability.Get();
        if (_menuApi == null)
        {
            Console.WriteLine("[故障] MenuManager Core not found...");
            player.PrintToChat("❌ 需要安装 MenuManagerCS2 插件！");
            return;
        }

        Console.WriteLine($"[故障] {player.PlayerName} 获得了故障技能");
        player.PrintToChat("📡 你获得了故障技能！输入 !useskill 或按键激活！");
        player.PrintToChat($"💡 本回合最多使用 {MAX_USES_PER_ROUND} 次，禁用雷达 {GLITCH_DURATION} 秒！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var slot = player.Index;
        _usageCount.Remove(slot);

        Console.WriteLine($"[故障] {player.PlayerName} 失去了故障技能");
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

        // 检查本回合使用次数
        int currentCount = _usageCount.TryGetValue(slot, out var count) ? count : 0;
        if (currentCount >= MAX_USES_PER_ROUND)
        {
            player.PrintToCenter($"❌ 本回合已使用{MAX_USES_PER_ROUND}次故障技能！");
            player.PrintToChat($"❌ 本回合已使用{MAX_USES_PER_ROUND}次故障技能！");
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
            var menu = _menuApi!.GetMenu("📡 选择要禁用雷达的敌人");

            // 添加选项（显示为"玩家名"）
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
                    ApplyGlitch(player, enemy);
                });
            }

            // 打开菜单
            menu.Open(player);

            Console.WriteLine($"[故障] {player.PlayerName} 正在选择目标");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[故障] 显示菜单时出错: {ex.Message}");
            player.PrintToChat("❌ 打开菜单失败！");
        }
    }

    /// <summary>
    /// 对敌人应用故障效果（禁用雷达）
    /// </summary>
    private void ApplyGlitch(CCSPlayerController player, CCSPlayerController target)
    {
        if (player == null || !player.IsValid || target == null || !target.IsValid)
            return;

        try
        {
            // 检查目标是否已经被故障
            if (_glitchedPlayers.ContainsKey(target.SteamID))
            {
                player.PrintToChat($"❌ {target.PlayerName} 已经被故障影响了！");
                return;
            }

            // 标记为已使用
            _usageCount[player.Index] = _usageCount.TryGetValue(player.Index, out var count) ? count + 1 : 1;

            // 计算结束时间
            float endTime = Server.CurrentTime + GLITCH_DURATION;

            // 记录故障效果
            _glitchedPlayers.TryAdd(target.SteamID, new GlitchInfo
            {
                Victim = target,
                EndTime = endTime
            });

            Console.WriteLine($"[故障] {player.PlayerName} 对 {target.PlayerName} 施加了故障效果，持续时间：{GLITCH_DURATION}秒");

            // 显示提示给施法者
            player.PrintToCenter($"📡 {target.PlayerName} 的雷达已禁用！");
            player.PrintToChat($"📡 成功对 {target.PlayerName} 施加故障！");
            player.PrintToChat($"⏱️ 持续时间：{GLITCH_DURATION} 秒");

            // 显示提示给目标
            target.PrintToCenter($"📡 你的雷达被 {player.PlayerName} 禁用了！");
            target.PrintToChat($"📡 你的雷达被 {player.PlayerName} 禁用了！");
            target.PrintToChat($"⏱️ 持续时间：{GLITCH_DURATION} 秒");

            // 播放音效
            target.EmitSound("UI.Pause");

            // 关闭菜单
            _menuApi?.CloseMenu(player);

            // 设置定时器移除故障效果
            Plugin?.AddTimer(GLITCH_DURATION, () =>
            {
                RemoveGlitch(target);
            });

            // 注册 OnTick 监听器来处理雷达禁用
            if (Plugin != null && _glitchedPlayers.Count == 1)
            {
                Plugin.RegisterListener<Listeners.OnTick>(OnTick);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[故障] 应用故障时出错: {ex.Message}");
            player.PrintToChat("❌ 故障失败！");
        }
    }

    /// <summary>
    /// 每帧更新 - 处理雷达禁用效果
    /// </summary>
    public void OnTick()
    {
        // 如果没有故障玩家，移除监听器
        if (_glitchedPlayers.IsEmpty)
        {
            Plugin?.RemoveListener<Listeners.OnTick>(OnTick);
            return;
        }

        float currentTime = Server.CurrentTime;

        // 检查每个故障玩家的效果是否过期
        var expiredPlayers = new List<ulong>();

        foreach (var kvp in _glitchedPlayers)
        {
            var steamID = kvp.Key;
            var glitchInfo = kvp.Value;

            // 如果效果过期，标记为待移除
            if (currentTime >= glitchInfo.EndTime)
            {
                expiredPlayers.Add(steamID);
            }
        }

        // 移除过期的故障效果
        foreach (var steamID in expiredPlayers)
        {
            if (_glitchedPlayers.TryRemove(steamID, out var glitchInfo))
            {
                if (glitchInfo.Victim != null && glitchInfo.Victim.IsValid)
                {
                    glitchInfo.Victim.PrintToChat("📡 你的雷达已恢复正常！");
                    glitchInfo.Victim.EmitSound("UI.RoundStart");
                }
            }
        }
    }

    /// <summary>
    /// 移除故障效果
    /// </summary>
    private void RemoveGlitch(CCSPlayerController victim)
    {
        if (victim == null || !victim.IsValid)
            return;

        // 从故障列表中移除
        _glitchedPlayers.TryRemove(victim.SteamID, out _);

        Console.WriteLine($"[故障] {victim.PlayerName} 的雷达已恢复正常");

        // 通知玩家
        victim.PrintToChat("📡 你的雷达已恢复正常！");
        victim.EmitSound("UI.RoundStart");
    }

    /// <summary>
    /// 清理所有故障效果（回合结束时调用）
    /// </summary>
    public static void ClearAllGlitches()
    {
        foreach (var kvp in _glitchedPlayers)
        {
            var steamID = kvp.Key;
            var glitchInfo = kvp.Value;

            if (glitchInfo.Victim != null && glitchInfo.Victim.IsValid)
            {
                glitchInfo.Victim.PrintToChat("📡 回合结束，故障效果已移除");
            }
        }

        _glitchedPlayers.Clear();
        Console.WriteLine("[故障] 已清理所有故障效果");
    }

    /// <summary>
    /// 玩家死亡时移除故障效果
    /// </summary>
    public static void OnPlayerDeath(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        if (_glitchedPlayers.ContainsKey(player.SteamID))
        {
            _glitchedPlayers.TryRemove(player.SteamID, out _);
            Console.WriteLine($"[故障] {player.PlayerName} 死亡，故障效果已移除");
        }
    }
}
