using CounterStrikeSharp.API;

namespace MyrtleSkill;

/// <summary>
/// 顶级狂欢事件 - 同时启用两个随机事件
/// </summary>
public class TopTierPartyEvent : EntertainmentEvent
{
    public override string Name => "TopTierParty";
    public override string DisplayName => "🎊 顶级狂欢";
    public override string Description => "顶级狂欢！同时启用两个随机事件！混乱与乐趣并存！";

    private readonly Random _random = new();
    private EntertainmentEvent? _firstEvent;
    private EntertainmentEvent? _secondEvent;

    public override void OnApply()
    {
        Console.WriteLine("[顶级狂欢] 事件已激活");

        // 获取所有可用的事件
        var allEvents = Plugin?.EventManager?.GetAllEventNames();
        if (allEvents == null || allEvents.Count == 0)
        {
            Console.WriteLine("[顶级狂欢] 警告：无法获取事件列表");
            return;
        }

        // 过滤掉 NoEvent 和所有 TopTierParty 系列事件
        var availableEvents = allEvents
            .Where(name => name != "NoEvent" &&
                        name != "TopTierParty" &&
                        name != "TopTierPartyPlusPlus")
            .ToList();

        if (availableEvents.Count < 2)
        {
            Console.WriteLine("[顶级狂欢] 警告：可用事件不足2个");
            return;
        }

        // 随机选择两个不同的事件
        int firstIndex = _random.Next(availableEvents.Count);
        string firstEventName = availableEvents[firstIndex];

        // 移除第一个事件，避免重复
        availableEvents.RemoveAt(firstIndex);
        int secondIndex = _random.Next(availableEvents.Count);
        string secondEventName = availableEvents[secondIndex];

        // 获取事件实例
        _firstEvent = Plugin?.EventManager?.GetEvent(firstEventName);
        _secondEvent = Plugin?.EventManager?.GetEvent(secondEventName);

        if (_firstEvent == null || _secondEvent == null)
        {
            Console.WriteLine("[顶级狂欢] 警告：无法获取事件实例");
            return;
        }

        Console.WriteLine($"[顶级狂欢] 选中的事件: {_firstEvent.DisplayName} 和 {_secondEvent.DisplayName}");

        // 应用两个事件
        try
        {
            _firstEvent.OnApply();
            Console.WriteLine($"[顶级狂欢] 已应用事件 1: {_firstEvent.DisplayName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[顶级狂欢] 应用事件 1 时出错: {ex.Message}");
        }

        try
        {
            _secondEvent.OnApply();
            Console.WriteLine($"[顶级狂欢] 已应用事件 2: {_secondEvent.DisplayName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[顶级狂欢] 应用事件 2 时出错: {ex.Message}");
        }

        // 显示事件提示
        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsValid)
            {
                p.PrintToChat("───────────────────");
                p.PrintToChat("🎊 " + DisplayName);
                p.PrintToChat($"📝 {_firstEvent.DisplayName}");
                p.PrintToChat($"📝 {_secondEvent.DisplayName}");
                p.PrintToChat("───────────────────");
            }
        }

        Plugin?.AddTimer(3.0f, () =>
        {
            foreach (var p in Utilities.GetPlayers())
            {
                if (p.IsValid)
                {
                    p.PrintToCenter($"━━━━━━━━━━━━━━━━\n 🎊 {_firstEvent.DisplayName}\n 🎊 {_secondEvent.DisplayName}\n━━━━━━━━━━━━━━━━");
                }
            }
        });
    }

    public override void OnRevert()
    {
        Console.WriteLine("[顶级狂欢] 正在恢复事件");

        // 恢复两个事件
        if (_firstEvent != null)
        {
            try
            {
                _firstEvent.OnRevert();
                Console.WriteLine($"[顶级狂欢] 已恢复事件 1: {_firstEvent.DisplayName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[顶级狂欢] 恢复事件 1 时出错: {ex.Message}");
            }
        }

        if (_secondEvent != null)
        {
            try
            {
                _secondEvent.OnRevert();
                Console.WriteLine($"[顶级狂欢] 已恢复事件 2: {_secondEvent.DisplayName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[顶级狂欢] 恢复事件 2 时出错: {ex.Message}");
            }
        }

        _firstEvent = null;
        _secondEvent = null;
    }
}
