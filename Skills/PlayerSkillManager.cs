using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill.Skills;

/// <summary>
/// 玩家技能管理器
/// 负责管理所有玩家技能，每回合为每个玩家随机分配技能
/// </summary>
public class PlayerSkillManager
{
    private readonly MyrtleSkill _plugin;
    private readonly Dictionary<string, PlayerSkill> _skills = new();
    private readonly Dictionary<int, List<PlayerSkill>> _playerSkills = new(); // 玩家槽位 -> 当前技能列表
    private readonly Dictionary<int, DateTime> _playerCooldowns = new(); // 玩家槽位 -> 冷却结束时间
    private readonly Dictionary<int, Queue<string>> _playerSkillHistory = new(); // 玩家槽位 -> 最近8个技能
    private const int MAX_HISTORY = 8; // 只记录最近8个技能
    private readonly Random _random = new();

    /// <summary>
    /// 强制技能列表（用于事件强制分配特定技能）
    /// 如果列表不为空，系统将使用此列表而非随机选择
    /// </summary>
    private List<string>? _forcedSkillNames = null;

    /// <summary>
    /// 技能系统是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true; // 默认启用

    /// <summary>
    /// 每个玩家每回合获得的技能数量（默认1个）
    /// </summary>
    public int SkillsPerPlayer { get; set; } = 1;

    /// <summary>
    /// 技能激活按键（默认为 E 键）
    /// </summary>
    public PlayerButtons SkillButton { get; set; } = PlayerButtons.Use;

    public PlayerSkillManager(MyrtleSkill plugin)
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
    /// <summary>
    /// 注册所有玩家技能
    /// </summary>
    private void RegisterSkills()
    {
        // 注册示例技能
        RegisterSkill(new TeleportSkill());      // 主动技能示例
        RegisterSkill(new SpeedBoostSkill());    // 被动技能示例
        RegisterSkill(new HighJumpSkill());      // 事件互斥示例
        RegisterSkill(new BotSummonSkill());     // 召唤队友技能
        RegisterSkill(new DumbBotSkill());       // 笨笨机器人技能
        RegisterSkill(new DecoyXRaySkill());     // 透视诱饵弹技能
        RegisterSkill(new ToxicSmokeSkill());    // 有毒烟雾弹技能
        RegisterSkill(new HealingSmokeSkill());  // 治疗烟雾弹技能
        RegisterSkill(new KillerFlashSkill());   // 杀手闪电技能
        RegisterSkill(new SuperFlashSkill());    // 超级闪光技能
        RegisterSkill(new TeamWhipSkill());      // 鞭策队友技能
        RegisterSkill(new SprintSkill());        // 短跑技能
        RegisterSkill(new DarknessSkill());      // 黑暗技能
        RegisterSkill(new AntiFlashSkill());     // 防闪光技能
        RegisterSkill(new RadarHackSkill());     // 雷达黑客技能
        RegisterSkill(new SecondChanceSkill());  // 第二次机会技能
        RegisterSkill(new EnemySpinSkill());     // 敌人旋转技能
        RegisterSkill(new MuhammadSkill());      // 穆罕默德技能
        RegisterSkill(new DisarmSkill());       // 裁军技能
        RegisterSkill(new MasterThiefSkill());  // 顶级小偷技能
        RegisterSkill(new ExplosiveShotSkill()); // 爆炸射击技能
        RegisterSkill(new GlazSkill());        // 格拉兹技能
        RegisterSkill(new FlashJumpSkill());    // 闪光跳跃技能
        RegisterSkill(new ArmoredSkill());      // 装甲技能
        RegisterSkill(new QuickShotSkill());    // 速射技能
        RegisterSkill(new MeitoSkill());        // 名刀技能
        RegisterSkill(new WallhackSkill());      // 透视技能
        RegisterSkill(new DeafSkill());          // 失聪技能
        RegisterSkill(new BigStomachSkill());    // 大胃袋技能
        RegisterSkill(new HighRiskHighRewardSkill()); // 高风险，高回报技能
        RegisterSkill(new HologramSkill());      // 全息图技能
        RegisterSkill(new GhostSkill());          // 鬼技能
        RegisterSkill(new KillInvincibilitySkill()); // 杀人无敌技能
        RegisterSkill(new DeathNoteSkill());     // 死神名册技能
        RegisterSkill(new SilentSkill());        // 沉默技能
        RegisterSkill(new SilencerSkill());       // 沉默技能（禁用敌人）
        RegisterSkill(new PushSkill());           // 推手技能
        RegisterSkill(new BlastOffSkill());        // 击飞咯技能
        RegisterSkill(new JackalSkill());         // 豺狼技能
        RegisterSkill(new HolyHandGrenadeSkill()); // 圣手榴弹技能
        RegisterSkill(new FrozenDecoySkill());     // 冷冻诱饵技能
        RegisterSkill(new FalconEyeSkill());       // 猎鹰之眼技能
        RegisterSkill(new FortniteSkill());        // 堡垒之夜技能
        RegisterSkill(new ReplicatorSkill());      // 复制品技能
        RegisterSkill(new ExplorerSkill());        // 探索者技能
        RegisterSkill(new TeleportAnchorSkill());  // 传送锚点技能
        RegisterSkill(new InfiniteAmmoSkill());    // 无限弹药技能
        RegisterSkill(new PhoenixSkill());         // 凤凰技能
        RegisterSkill(new PilotSkill());           // 飞行员技能
        RegisterSkill(new ThirdEyeSkill());        // 第三只眼技能
        RegisterSkill(new ChooseOneOfThreeSkill()); // 三选一技能
        RegisterSkill(new DuplicatorSkill());      // 复制者技能
        RegisterSkill(new FreeCameraSkill());      // 自由视角技能
        RegisterSkill(new WoodManSkill());         // 木头人技能
        RegisterSkill(new ZRYSkill());             // ZRY技能
        RegisterSkill(new LastStandSkill());        // 残局使者技能
        RegisterSkill(new GlitchSkill());           // 故障技能
        RegisterSkill(new MindHackSkill());         // 精神骇入技能
        RegisterSkill(new ProstheticSkill());       // 假肢技能
        RegisterSkill(new FocusSkill());            // 专注技能
        RegisterSkill(new AutoAimSkill());          // 自瞄技能
        RegisterSkill(new BladeMasterSkill());      // 剑圣技能
        RegisterSkill(new RangeFinderSkill());      // 测距仪技能

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
    /// <param name="player">可选的玩家参数，用于过滤该玩家最近获得的技能</param>
    public PlayerSkill? SelectRandomSkill(CCSPlayerController? player = null)
    {
        if (_skills.Count == 0)
            return null;

        // 获取当前事件名称
        string? currentEventName = _plugin.CurrentEvent?.Name;

        // 获取玩家最近获得的技能历史
        Queue<string>? playerHistory = null;
        if (player != null && player.IsValid && _playerSkillHistory.TryGetValue(player.Slot, out var history))
        {
            playerHistory = history;
        }

        // 过滤掉与当前事件互斥的技能和玩家最近获得的技能
        var availableSkills = _skills.Values
            .Where(s => s.Weight > 0) // 权重大于0
            .Where(s => string.IsNullOrEmpty(currentEventName) || !s.ExcludedEvents.Contains(currentEventName)) // 不与当前事件互斥
            .Where(s => playerHistory == null || !playerHistory.Contains(s.Name)) // 玩家最近3回合未获得
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
    /// 为指定的玩家应用指定的技能
    /// </summary>
    public void ApplySpecificSkillToPlayer(CCSPlayerController player, string skillName)
    {
        if (player == null || !player.IsValid)
        {
            Console.WriteLine($"[技能管理器] 玩家无效，无法应用技能");
            return;
        }

        var skill = GetSkill(skillName);
        if (skill == null)
        {
            Console.WriteLine($"[技能管理器] 未找到技能: {skillName}");
            return;
        }

        // 如果玩家已有技能，先移除
        RemoveSkillFromPlayer(player);

        // 初始化技能列表
        _playerSkills[player.Slot] = new List<PlayerSkill>();

        // 应用技能
        _playerSkills[player.Slot].Add(skill);
        skill.OnApply(player);

        // 记录到历史
        AddToPlayerHistory(player, skill.Name);

        Console.WriteLine($"[技能管理器] {player.PlayerName} 被强制赋予技能: {skill.DisplayName} ({(skill.IsActive ? "主动" : "被动")})");

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
    /// 为指定的玩家应用指定的多个技能（按顺序）
    /// </summary>
    public void ApplySkillsToPlayer(CCSPlayerController player, List<string> skillNames)
    {
        if (player == null || !player.IsValid)
        {
            Console.WriteLine($"[技能管理器] 玩家无效，无法应用技能");
            return;
        }

        if (skillNames == null || skillNames.Count == 0)
        {
            Console.WriteLine($"[技能管理器] 技能名称列表为空");
            return;
        }

        // 如果玩家已有技能，先移除
        RemoveSkillFromPlayer(player);

        // 初始化玩家的技能列表
        _playerSkills[player.Slot] = new List<PlayerSkill>();

        Console.WriteLine($"[技能管理器] {player.PlayerName} 将获得 {skillNames.Count} 个强制技能");

        // 按顺序应用所有技能
        for (int i = 0; i < skillNames.Count; i++)
        {
            var skillName = skillNames[i];
            var skill = GetSkill(skillName);

            if (skill == null)
            {
                Console.WriteLine($"[技能管理器] 警告：未找到技能: {skillName}");
                continue;
            }

            // 应用技能
            _playerSkills[player.Slot].Add(skill);
            skill.OnApply(player);

            Console.WriteLine($"[技能管理器] {player.PlayerName} 获得第{i + 1}个强制技能: {skill.DisplayName}");

            // 显示提示
            player.PrintToChat($"💫 技能{i + 1}: {skill.DisplayName} - {skill.Description}");

            // 如果是主动技能，提示如何使用
            if (skill.IsActive)
            {
                player.PrintToChat($"   ⌨️ 输入 !useskill 或按键激活技能");
                player.PrintToChat($"   ⏱️ 冷却时间：{skill.Cooldown}秒");
            }
        }

        // 显示总结
        var skills = _playerSkills[player.Slot];
        player.PrintToChat($"───────────────────");
        player.PrintToChat($"🎁 你获得了 {skills.Count} 个技能！");
        for (int i = 0; i < skills.Count; i++)
        {
            player.PrintToChat($"  {i + 1}. {skills[i].DisplayName}");
        }
        player.PrintToChat($"───────────────────");
    }

    /// <summary>
    /// 为指定的玩家应用指定的技能
    /// </summary>
    public void ApplySkillToPlayer(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 如果玩家已有技能，先移除
        RemoveSkillFromPlayer(player);

        // 检查是否有强制技能列表
        if (HasForcedSkills() && _forcedSkillNames != null)
        {
            Console.WriteLine($"[技能管理器] {player.PlayerName} 使用强制技能列表");
            ApplySkillsToPlayer(player, _forcedSkillNames);
            return;
        }

        // 初始化玩家的技能列表
        _playerSkills[player.Slot] = new List<PlayerSkill>();

        // 获取应该获得的技能数量
        int skillCount = SkillsPerPlayer;
        Console.WriteLine($"[技能管理器] {player.PlayerName} 将获得 {skillCount} 个技能");

        // 选择并应用第一个技能
        var firstSkill = SelectRandomSkill(player);
        if (firstSkill == null)
        {
            Console.WriteLine($"[技能管理器] 无法为 {player.PlayerName} 选择第一个技能");
            return;
        }

        // 应用第一个技能
        ApplySingleSkill(player, firstSkill, 1);

        // 如果需要第二个技能
        if (skillCount >= 2)
        {
            var secondSkill = SelectSecondSkill(player, firstSkill);
            if (secondSkill != null)
            {
                ApplySingleSkill(player, secondSkill, 2);

                // 如果需要第三个技能
                if (skillCount >= 3)
                {
                    var thirdSkill = SelectThirdSkill(player, firstSkill, secondSkill);
                    if (thirdSkill != null)
                    {
                        ApplySingleSkill(player, thirdSkill, 3);
                    }
                    else
                    {
                        Console.WriteLine($"[技能管理器] 无法为 {player.PlayerName} 选择第三个技能（无合适技能可用）");
                    }
                }
            }
            else
            {
                Console.WriteLine($"[技能管理器] 无法为 {player.PlayerName} 选择第二个技能（无合适技能可用）");
            }
        }

        // 显示总结
        var skills = _playerSkills[player.Slot];
        player.PrintToChat($"───────────────────");
        player.PrintToChat($"🎁 你获得了 {skills.Count} 个技能！");
        for (int i = 0; i < skills.Count; i++)
        {
            player.PrintToChat($"  {i + 1}. {skills[i].DisplayName}");
        }
        player.PrintToChat($"───────────────────");
    }

    /// <summary>
    /// 为玩家应用单个技能
    /// </summary>
    private void ApplySingleSkill(CCSPlayerController player, PlayerSkill skill, int index)
    {
        // 应用技能
        _playerSkills[player.Slot].Add(skill);
        skill.OnApply(player);

        // 记录到历史
        AddToPlayerHistory(player, skill.Name);

        Console.WriteLine($"[技能管理器] {player.PlayerName} 获得第{index}个技能: {skill.DisplayName} ({(skill.IsActive ? "主动" : "被动")})");

        // 显示提示
        player.PrintToChat($"💫 技能{index}: {skill.DisplayName} - {skill.Description}");

        // 如果是主动技能，提示如何使用
        if (skill.IsActive)
        {
            player.PrintToChat($"   ⌨️ 输入 !useskill 或按键激活技能");
            player.PrintToChat($"   ⏱️ 冷却时间：{skill.Cooldown}秒");
        }
    }

    /// <summary>
    /// 选择第二个技能（考虑互斥和主动技能限制）
    /// </summary>
    private PlayerSkill? SelectSecondSkill(CCSPlayerController player, PlayerSkill firstSkill)
    {
        if (_skills.Count == 0)
            return null;

        // 获取当前事件名称
        string? currentEventName = _plugin?.CurrentEvent?.Name;

        // 获取玩家最近获得的技能历史
        Queue<string>? playerHistory = null;
        if (player.IsValid && _playerSkillHistory.TryGetValue(player.Slot, out var history))
        {
            playerHistory = history;
        }

        // 收集第一个技能的互斥技能名称
        var excludedByFirstSkill = new HashSet<string>(firstSkill.ExcludedSkills);

        // 过滤可用技能
        var availableSkills = _skills.Values
            .Where(s => s.Weight > 0) // 权重大于0
            .Where(s => s.Name != firstSkill.Name) // 不能是同一个技能
            .Where(s => string.IsNullOrEmpty(currentEventName) || !s.ExcludedEvents.Contains(currentEventName)) // 不与当前事件互斥
            .Where(s => playerHistory == null || !playerHistory.Contains(s.Name)) // 玩家最近3回合未获得
            .Where(s => !excludedByFirstSkill.Contains(s.Name)) // 不与第一个技能互斥
            .Where(s => !firstSkill.IsActive || !s.IsActive) // 如果第一个是主动，第二个必须是被动
            .ToList();

        if (availableSkills.Count == 0)
        {
            Console.WriteLine("[技能管理器] 警告：没有可用的第二个技能（互斥/主动技能限制）");
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
                Console.WriteLine($"[技能管理器] 为 {player.PlayerName} 选择第二个技能: {skill.Name} (权重: {skill.Weight})");
                return skill;
            }
        }

        return availableSkills.FirstOrDefault();
    }

    /// <summary>
    /// 选择第三个技能（考虑与前两个技能的互斥和主动技能限制）
    /// </summary>
    private PlayerSkill? SelectThirdSkill(CCSPlayerController player, PlayerSkill firstSkill, PlayerSkill secondSkill)
    {
        if (_skills.Count == 0)
            return null;

        // 获取当前事件名称
        string? currentEventName = _plugin?.CurrentEvent?.Name;

        // 获取玩家最近获得的技能历史
        Queue<string>? playerHistory = null;
        if (player.IsValid && _playerSkillHistory.TryGetValue(player.Slot, out var history))
        {
            playerHistory = history;
        }

        // 收集第一个和第二个技能的互斥技能名称
        var excludedByFirstSkill = new HashSet<string>(firstSkill.ExcludedSkills);
        var excludedBySecondSkill = new HashSet<string>(secondSkill.ExcludedSkills);

        // 合并互斥集合
        var allExcludedSkills = new HashSet<string>(excludedByFirstSkill);
        foreach (var excluded in excludedBySecondSkill)
        {
            allExcludedSkills.Add(excluded);
        }

        // 检查前两个技能是否有主动技能
        bool hasActiveSkill = firstSkill.IsActive || secondSkill.IsActive;

        // 过滤可用技能
        var availableSkills = _skills.Values
            .Where(s => s.Weight > 0) // 权重大于0
            .Where(s => s.Name != firstSkill.Name && s.Name != secondSkill.Name) // 不能是前两个技能
            .Where(s => string.IsNullOrEmpty(currentEventName) || !s.ExcludedEvents.Contains(currentEventName)) // 不与当前事件互斥
            .Where(s => playerHistory == null || !playerHistory.Contains(s.Name)) // 玩家最近3回合未获得
            .Where(s => !allExcludedSkills.Contains(s.Name)) // 不与前两个技能互斥
            .Where(s => !hasActiveSkill || !s.IsActive) // 如果前两个中有主动，第三个必须是被动
            .ToList();

        if (availableSkills.Count == 0)
        {
            Console.WriteLine("[技能管理器] 警告：没有可用的第三个技能（互斥/主动技能限制）");
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
                Console.WriteLine($"[技能管理器] 为 {player.PlayerName} 选择第三个技能: {skill.Name} (权重: {skill.Weight})");
                return skill;
            }
        }

        return availableSkills.FirstOrDefault();
    }

    /// <summary>
    /// 移除玩家的技能
    /// </summary>
    public void RemoveSkillFromPlayer(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        if (_playerSkills.TryGetValue(player.Slot, out var skills))
        {
            foreach (var skill in skills)
            {
                skill.OnRevert(player);
            }
            _playerSkills.Remove(player.Slot);

            // 清除冷却时间记录（避免跨回合影响）
            _playerCooldowns.Remove(player.Slot);

            Console.WriteLine($"[技能管理器] 已移除 {player.PlayerName} 的 {skills.Count} 个技能");
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

        // 所有玩家分配完技能后，清除强制技能列表
        if (HasForcedSkills())
        {
            ClearForcedSkills();
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

        // 清除所有冷却时间记录（确保跨回合重置）
        _playerCooldowns.Clear();

        // 注意：不清空历史记录，让玩家记住之前获得过的技能
    }

    /// <summary>
    /// 获取玩家当前技能列表
    /// </summary>
    public List<PlayerSkill> GetPlayerSkills(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return new List<PlayerSkill>();

        return _playerSkills.TryGetValue(player.Slot, out var skills) ? skills : new List<PlayerSkill>();
    }

    /// <summary>
    /// 获取玩家的第一个技能（兼容旧代码）
    /// </summary>
    public PlayerSkill? GetPlayerSkill(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return null;

        if (_playerSkills.TryGetValue(player.Slot, out var skills) && skills.Count > 0)
        {
            // 优先返回主动技能，否则返回第一个
            return skills.FirstOrDefault(s => s.IsActive) ?? skills[0];
        }

        return null;
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

        // 清除玩家的开局 HUD（避免与技能菜单冲突）
        _plugin?.ClearPlayerHUD(player);

        // 获取玩家技能列表
        if (!_playerSkills.TryGetValue(player.Slot, out var skills) || skills.Count == 0)
        {
            player.PrintToChat("💫 你当前没有技能！");
            return;
        }

        // 找到第一个可用的主动技能
        PlayerSkill? activeSkill = null;
        foreach (var skill in skills)
        {
            if (skill.IsActive)
            {
                activeSkill = skill;
                break;
            }
        }

        if (activeSkill == null)
        {
            player.PrintToChat("💫 你当前没有主动技能！");
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
            activeSkill.OnUse(player);

            // 设置冷却
            _playerCooldowns[player.Slot] = DateTime.Now.AddSeconds(activeSkill.Cooldown);

            Console.WriteLine($"[技能管理器] {player.PlayerName} 使用了技能: {activeSkill.DisplayName}");
            player.PrintToChat($"💫 已使用技能：{activeSkill.DisplayName}");

            // 显示冷却时间
            if (activeSkill.Cooldown > 0)
            {
                player.PrintToCenter($"💫 技能冷却：{activeSkill.Cooldown}秒");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[技能管理器] 错误：{player.PlayerName} 使用技能 {activeSkill.DisplayName} 时出错: {ex.Message}");
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

    /// <summary>
    /// 添加技能到玩家历史记录（只保留最近3个）
    /// </summary>
    private void AddToPlayerHistory(CCSPlayerController player, string skillName)
    {
        if (player == null || !player.IsValid)
            return;

        // 确保玩家有历史记录队列
        if (!_playerSkillHistory.ContainsKey(player.Slot))
        {
            _playerSkillHistory[player.Slot] = new Queue<string>();
        }

        // 添加技能到队列
        var history = _playerSkillHistory[player.Slot];
        history.Enqueue(skillName);

        // 如果超过3个，移除最旧的
        if (history.Count > MAX_HISTORY)
        {
            history.Dequeue();
        }

        Console.WriteLine($"[技能管理器] {player.PlayerName} 的技能历史已更新，最近 {history.Count} 个技能");
    }

    /// <summary>
    /// 清空指定玩家的技能历史
    /// </summary>
    public void ClearPlayerHistory(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        if (_playerSkillHistory.TryGetValue(player.Slot, out var history))
        {
            history.Clear();
            Console.WriteLine($"[技能管理器] 已清空 {player.PlayerName} 的技能历史");
        }
    }

    /// <summary>
    /// 清空所有玩家的技能历史
    /// </summary>
    public void ClearAllPlayerHistory()
    {
        _playerSkillHistory.Clear();
        Console.WriteLine("[技能管理器] 已清空所有玩家的技能历史");
    }

    /// <summary>
    /// 设置强制技能列表（用于事件强制分配特定技能）
    /// </summary>
    public void SetForcedSkills(List<string> skillNames)
    {
        _forcedSkillNames = new List<string>(skillNames);
        Console.WriteLine($"[技能管理器] 已设置强制技能列表: {string.Join(", ", skillNames)}");
    }

    /// <summary>
    /// 清除强制技能列表
    /// </summary>
    public void ClearForcedSkills()
    {
        if (_forcedSkillNames != null)
        {
            Console.WriteLine($"[技能管理器] 已清除强制技能列表: {string.Join(", ", _forcedSkillNames)}");
            _forcedSkillNames = null;
        }
    }

    /// <summary>
    /// 检查是否有强制技能列表
    /// </summary>
    public bool HasForcedSkills()
    {
        return _forcedSkillNames != null && _forcedSkillNames.Count > 0;
    }
}

