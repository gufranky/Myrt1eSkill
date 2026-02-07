using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;
using MyrtleSkill.Events;

namespace MyrtleSkill;

/// <summary>
/// 娱乐事件管理器
/// 负责管理所有随机事件，每回合随机选择并执行
/// </summary>
public class EntertainmentEventManager
{
    private readonly MyrtleSkill _plugin;
    private readonly Dictionary<string, EntertainmentEvent> _events = new();
    private readonly Queue<string> _eventHistory = new(); // 最近3个事件历史
    private const int MAX_HISTORY = 3; // 只记录最近3个事件
    private readonly Random _random = new();

    /// <summary>
    /// 事件系统是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true; // 默认启用

    public EntertainmentEventManager(MyrtleSkill plugin)
    {
        _plugin = plugin;

        // 注册所有事件
        RegisterEvents();

        // 从配置加载权重
        LoadWeightsFromConfig();
    }

    /// <summary>
    /// 注册所有娱乐事件
    /// </summary>
    private void RegisterEvents()
    {
        // 在这里注册所有事件类
        RegisterEvent(new NoEventEvent());
        RegisterEvent(new LowGravityEvent());
        RegisterEvent(new LowGravityPlusPlusEvent());
        RegisterEvent(new HighSpeedEvent());
        RegisterEvent(new VampireEvent());
        RegisterEvent(new TeleportOnDamageEvent());
        RegisterEvent(new JumpOnShootEvent());
        RegisterEvent(new JumpPlusPlusEvent());
        RegisterEvent(new AnywhereBombPlantEvent());
        RegisterEvent(new MiniSizeEvent());
        RegisterEvent(new JuggernautEvent());
        RegisterEvent(new InfiniteBulletModeEvent());
        RegisterEvent(new SwapOnHitEvent());
        RegisterEvent(new SmallAndDeadlyEvent());
        RegisterEvent(new BlitzkriegEvent());
        RegisterEvent(new DecoyTeleportEvent());
        RegisterEvent(new XrayEvent());
        RegisterEvent(new SuperpowerXrayEvent());
        RegisterEvent(new ChickenModeEvent());
        RegisterEvent(new TopTierPartyEvent());
        RegisterEvent(new TopTierPartyPlusPlusEvent());
        RegisterEvent(new StayQuietEvent());
        RegisterEvent(new RainyDayEvent());
        RegisterEvent(new ScreamingRabbitEvent());
        RegisterEvent(new HeadshotOnlyEvent());
        RegisterEvent(new OneShotEvent());
        RegisterEvent(new DeadlyGrenadesEvent());
        RegisterEvent(new UnluckyCouplesEvent());
        RegisterEvent(new StrangersEvent());
        RegisterEvent(new AutoBhopEvent());
        RegisterEvent(new SlowMotionEvent());
        RegisterEvent(new FoggyEvent());
        RegisterEvent(new NoSkillEvent());
        RegisterEvent(new KeepMovingEvent());
        // RegisterEvent(new SoccerModeEvent());  // 暂时禁用，因为有bug
        RegisterEvent(new SuperRecoilEvent());
        RegisterEvent(new SuperKnockbackEvent());
        RegisterEvent(new BankruptcyEvent());
        RegisterEvent(new BankruptcyWeaponEvent());
        RegisterEvent(new DeafEvent());
        RegisterEvent(new MoreSkillsEvent());
        RegisterEvent(new SkillsPlusPlusEvent());
        RegisterEvent(new KillerSatelliteEvent());
        RegisterEvent(new InverseHeadshotEvent());

        Console.WriteLine("[事件管理器] 已注册 " + _events.Count + " 个娱乐事件");
    }

    /// <summary>
    /// 注册单个事件
    /// </summary>
    private void RegisterEvent(EntertainmentEvent entertainmentEvent)
    {
        if (!_events.ContainsKey(entertainmentEvent.Name))
        {
            entertainmentEvent.Register(_plugin);
            _events[entertainmentEvent.Name] = entertainmentEvent;
        }
        else
        {
            Console.WriteLine("[事件管理器] 警告：事件 '" + entertainmentEvent.Name + "' 已存在，跳过注册");
        }
    }

    /// <summary>
    /// 从配置文件加载事件权重
    /// </summary>
    private void LoadWeightsFromConfig()
    {
        if (_plugin.EventConfig?.EventWeights == null)
        {
            Console.WriteLine("[事件管理器] 警告：配置文件中没有事件权重配置");
            return;
        }

        foreach (var kvp in _plugin.EventConfig.EventWeights)
        {
            var @event = GetEvent(kvp.Key);
            if (@event != null)
            {
                @event.Weight = kvp.Value;
                Console.WriteLine("[事件管理器] 从配置加载权重: " + kvp.Key + " = " + kvp.Value);
            }
        }
    }

    /// <summary>
    /// 随机选择一个事件（基于权重）
    /// </summary>
    public EntertainmentEvent? SelectRandomEvent()
    {
        if (_events.Count == 0)
            return null;

        // 过滤掉最近3个回合的事件
        var availableEvents = _events.Values
            .Where(e => e.Weight > 0) // 权重大于0
            .Where(e => !_eventHistory.Contains(e.Name)) // 不在最近3个历史中
            .ToList();

        if (availableEvents.Count == 0)
        {
            Console.WriteLine("[事件管理器] 警告：没有可用的事件（所有事件都在最近3回合触发过）");
            return null;
        }

        // 计算总权重
        int totalWeight = availableEvents.Sum(e => e.Weight);

        if (totalWeight <= 0)
            return null;

        // 随机选择
        int randomWeight = _random.Next(totalWeight);
        int currentWeight = 0;

        EntertainmentEvent? selectedEvent = null;
        foreach (var @event in availableEvents)
        {
            currentWeight += @event.Weight;
            if (randomWeight < currentWeight)
            {
                selectedEvent = @event;
                break;
            }
        }

        selectedEvent ??= availableEvents.FirstOrDefault();

        // 记录到历史（只保留最近3个）
        if (selectedEvent != null)
        {
            _eventHistory.Enqueue(selectedEvent.Name);
            if (_eventHistory.Count > MAX_HISTORY)
            {
                _eventHistory.Dequeue();
            }
            Console.WriteLine($"[事件管理器] 随机选择事件: {selectedEvent.Name} (权重: {selectedEvent.Weight}, 历史记录: {_eventHistory.Count})");
        }

        return selectedEvent;
    }

    /// <summary>
    /// 根据名称获取事件
    /// </summary>
    public EntertainmentEvent? GetEvent(string name)
    {
        return _events.TryGetValue(name, out var entertainmentEvent) ? entertainmentEvent : null;
    }

    /// <summary>
    /// 获取所有事件名称
    /// </summary>
    public List<string> GetAllEventNames()
    {
        return _events.Values.OrderBy(e => e.Name).Select(e => e.Name).ToList();
    }

    /// <summary>
    /// 获取所有事件及其权重
    /// </summary>
    public Dictionary<string, int> GetAllEventWeights()
    {
        return _events.ToDictionary(k => k.Value.DisplayName, v => v.Value.Weight);
    }

    /// <summary>
    /// 获取事件权重
    /// </summary>
    public int GetEventWeight(string name)
    {
        var @event = GetEvent(name);
        return @event?.Weight ?? -1;
    }

    /// <summary>
    /// 设置事件权重
    /// </summary>
    public bool SetEventWeight(string name, int weight)
    {
        var @event = GetEvent(name);
        if (@event == null)
            return false;

        @event.Weight = weight;
        Console.WriteLine("[事件管理器] 事件 '" + name + "' 权重已设置为: " + weight);
        return true;
    }

    /// <summary>
    /// 获取事件总数
    /// </summary>
    public int GetEventCount()
    {
        return _events.Count;
    }

    /// <summary>
    /// 清空事件历史记录
    /// </summary>
    public void ClearEventHistory()
    {
        _eventHistory.Clear();
        Console.WriteLine("[事件管理器] 已清空事件历史记录");
    }

    /// <summary>
    /// 获取事件历史记录数量
    /// </summary>
    public int GetEventHistoryCount()
    {
        return _eventHistory.Count;
    }
}
