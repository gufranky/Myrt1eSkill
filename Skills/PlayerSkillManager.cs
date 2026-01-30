using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace HelloWorldPlugin.Skills;

/// <summary>
/// 玩家技能管理器
/// 负责管理所有玩家技能，每回合为每个玩家随机分配技能
/// </summary>
public class PlayerSkillManager
{
    private readonly HelloWorldPlugin _plugin;
    private readonly Dictionary<string, PlayerSkill> _skills = new();
    private readonly Dictionary<int, PlayerSkill> _playerSkills = new(); // 玩家槽位 -> 当前技能
    private readonly Dictionary<int, DateTime> _playerCooldowns = new(); // 玩家槽位 -> 冷却结束时间
    private readonly Random _random = new();

    /// <summary>
    /// 技能系统是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// 技能激活按键（默认为 E 键）
    /// </summary>
    public PlayerButtons SkillButton { get; set; } = PlayerButtons.Use;

    public PlayerSkillManager(HelloWorldPlugin plugin)
    {
        _plugin = plugin;

        // 注册所有技能
        RegisterSkills();

        // 从配置加载权重
        LoadWeightsFromConfig();
    }

    /// <summary>
    /// 注册所有玩家技能
    /// </summary>
    private void RegisterSkills()
    {
        // 注册示例技能
        RegisterSkill(new TeleportSkill());      // 主动技能示例
        RegisterSkill(new SpeedBoostSkill());    // 被动技能示例
        RegisterSkill(new HighJumpSkill());      // 事件互斥示例

        Console.WriteLine("[技能管理器] 已注册 " + _skills.Count + " 个玩家技能");
    }

    /// <summary>
    /// 注册单个技能
    /// </summary>
    private void RegisterSkill(PlayerSkill skill)
    {
        if (!_skills.ContainsKey(skill.Name))
        {
            skill.Register(_plugin);
            _skills[skill.Name] = skill;
        }
        else
        {
            Console.WriteLine("[技能管理器] 警告：技能 '" + skill.Name + "' 已存在，跳过注册");
        }
    }

    /// <summary>
    /// 从配置文件加载技能权重
    /// </summary>
    private void LoadWeightsFromConfig()
    {
        if (_plugin.Config?.SkillWeights == null)
        {
            Console.WriteLine("[技能管理器] 警告：配置文件中没有技能权重配置");
            return;
        }

        foreach (var kvp in _plugin.Config.SkillWeights)
        {
            var skill = GetSkill(kvp.Key);
            if (skill != null)
            {
                skill.Weight = kvp.Value;
                Console.WriteLine("[技能管理器] 从配置加载权重: " + kvp.Key + " = " + kvp.Value);
            }
        }
    }

    /// <summary>
    /// 随机选择一个技能（基于权重）
    /// </summary>
    public PlayerSkill? SelectRandomSkill()
    {
        if (_skills.Count == 0)
            return null;

        // 获取当前事件名称
        string? currentEventName = _plugin.CurrentEvent?.Name;

        // 过滤掉与当前事件互斥的技能
        var availableSkills = _skills.Values
            .Where(s => s.Weight > 0) // 权重大于0
            .Where(s => string.IsNullOrEmpty(currentEventName) || !s.ExcludedEvents.Contains(currentEventName)) // 不与当前事件互斥
            .ToList();

        if (availableSkills.Count == 0)
        {
            Console.WriteLine("[技能管理器] 警告：没有可用的技能（可能都被当前事件排除了）");
            return null;
        }

        // 计算总权重
        int totalWeight = availableSkills.Sum(s => s.Weight);

        if (totalWeight <= 0)
            return null;

        // 随机选择
        int randomWeight = _random.Next(totalWeight);
        int currentWeight = 0;

        foreach (var skill in availableSkills)
        {
            currentWeight += skill.Weight;
            if (randomWeight < currentWeight)
            {
                if (!string.IsNullOrEmpty(currentEventName))
                {
                    Console.WriteLine($"[技能管理器] 在事件 '{currentEventName}' 下选择技能: {skill.Name} (权重: {skill.Weight})");
                }
                return skill;
            }
        }

        return availableSkills.FirstOrDefault();
    }

    /// <summary>
    /// 为玩家应用技能
    /// </summary>
    public void ApplySkillToPlayer(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 如果玩家已有技能，先移除
        RemoveSkillFromPlayer(player);

        // 随机选择技能
        var skill = SelectRandomSkill();
        if (skill == null)
        {
            Console.WriteLine($"[技能管理器] 无法为 {player.PlayerName} 选择技能");
            return;
        }

        // 应用技能
        _playerSkills[player.Slot] = skill;
        skill.OnApply(player);

        Console.WriteLine($"[技能管理器] {player.PlayerName} 获得技能: {skill.DisplayName} ({(skill.IsActive ? "主动" : "被动")})");

        // 显示提示
        player.PrintToChat($"💫 你获得了技能：{skill.DisplayName}");
        player.PrintToChat($"📝 {skill.Description}");

        // 如果是主动技能，提示如何使用
        if (skill.IsActive)
        {
            player.PrintToChat($"⌨️ 输入 !useskill 或按键激活技能");
            player.PrintToChat($"⏱️ 冷却时间：{skill.Cooldown}秒");
        }
    }

    /// <summary>
    /// 移除玩家的技能
    /// </summary>
    public void RemoveSkillFromPlayer(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        if (_playerSkills.TryGetValue(player.Slot, out var skill))
        {
            skill.OnRevert(player);
            _playerSkills.Remove(player.Slot);
            Console.WriteLine($"[技能管理器] 已移除 {player.PlayerName} 的技能: {skill.DisplayName}");
        }
    }

    /// <summary>
    /// 为所有玩家应用技能
    /// </summary>
    public void ApplySkillsToAllPlayers()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                ApplySkillToPlayer(player);
            }
        }
    }

    /// <summary>
    /// 移除所有玩家的技能
    /// </summary>
    public void RemoveAllPlayerSkills()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                RemoveSkillFromPlayer(player);
            }
        }

        _playerSkills.Clear();
    }

    /// <summary>
    /// 获取玩家当前技能
    /// </summary>
    public PlayerSkill? GetPlayerSkill(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return null;

        return _playerSkills.TryGetValue(player.Slot, out var skill) ? skill : null;
    }

    /// <summary>
    /// 根据名称获取技能
    /// </summary>
    public PlayerSkill? GetSkill(string name)
    {
        return _skills.TryGetValue(name, out var skill) ? skill : null;
    }

    /// <summary>
    /// 获取所有技能名称
    /// </summary>
    public List<string> GetAllSkillNames()
    {
        return _skills.Values.OrderBy(s => s.Name).Select(s => s.Name).ToList();
    }

    /// <summary>
    /// 获取所有技能及其权重
    /// </summary>
    public Dictionary<string, int> GetAllSkillWeights()
    {
        return _skills.ToDictionary(k => k.Value.DisplayName, v => v.Value.Weight);
    }

    /// <summary>
    /// 获取技能权重
    /// </summary>
    public int GetSkillWeight(string name)
    {
        var skill = GetSkill(name);
        return skill?.Weight ?? -1;
    }

    /// <summary>
    /// 设置技能权重
    /// </summary>
    public bool SetSkillWeight(string name, int weight)
    {
        var skill = GetSkill(name);
        if (skill == null)
            return false;

        skill.Weight = weight;
        Console.WriteLine("[技能管理器] 技能 '" + name + "' 权重已设置为: " + weight);
        return true;
    }

    /// <summary>
    /// 玩家使用技能（通过命令或按键）
    /// </summary>
    public void UsePlayerSkill(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        // 获取玩家技能
        if (!_playerSkills.TryGetValue(player.Slot, out var skill))
        {
            player.PrintToChat("💫 你当前没有技能！");
            return;
        }

        // 检查是否为主动技能
        if (!skill.IsActive)
        {
            player.PrintToChat($"💫 {skill.DisplayName} 是被动技能，无需激活！");
            return;
        }

        // 检查冷却
        if (_playerCooldowns.TryGetValue(player.Slot, out var cooldownEnd))
        {
            if (DateTime.Now < cooldownEnd)
            {
                var remaining = (cooldownEnd - DateTime.Now).TotalSeconds;
                player.PrintToCenter($"💫 技能冷却中... {remaining:F1}秒");
                return;
            }
        }

        // 使用技能
        try
        {
            skill.OnUse(player);

            // 设置冷却
            _playerCooldowns[player.Slot] = DateTime.Now.AddSeconds(skill.Cooldown);

            Console.WriteLine($"[技能管理器] {player.PlayerName} 使用了技能: {skill.DisplayName}");
            player.PrintToChat($"💫 已使用技能：{skill.DisplayName}");

            // 显示冷却时间
            if (skill.Cooldown > 0)
            {
                player.PrintToCenter($"💫 技能冷却：{skill.Cooldown}秒");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[技能管理器] 错误：{player.PlayerName} 使用技能 {skill.DisplayName} 时出错: {ex.Message}");
            player.PrintToChat($"💫 技能使用失败！");
        }
    }

    /// <summary>
    /// 获取玩家技能剩余冷却时间
    /// </summary>
    public float GetPlayerSkillCooldown(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return 0;

        if (_playerCooldowns.TryGetValue(player.Slot, out var cooldownEnd))
        {
            var remaining = (cooldownEnd - DateTime.Now).TotalSeconds;
            return remaining > 0 ? (float)remaining : 0;
        }

        return 0;
    }

    /// <summary>
    /// 获取技能总数
    /// </summary>
    public int GetSkillCount()
    {
        return _skills.Count;
    }
}

