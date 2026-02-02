using CounterStrikeSharp.API;

namespace MyrtleSkill;

/// <summary>
/// 顶级狂欢++ 事件 - 同时启用三个随机事件
/// </summary>
public class TopTierPartyPlusPlusEvent : EntertainmentEvent
{
    public override string Name => "TopTierPartyPlusPlus";
    public override string DisplayName => "🎊🎊 顶级狂欢++";
    public override string Description => "终极狂欢！同时启用三个随机事件！绝对的混乱与极致的乐趣！";

    private readonly Random _random = new();
    private EntertainmentEvent? _firstEvent;
    private EntertainmentEvent? _secondEvent;
    private EntertainmentEvent? _thirdEvent;

    public override void OnApply()
    {
        Console.WriteLine("[顶级狂欢++] 事件已激活");

        // 获取所有可用的事件
        var allEvents = Plugin?.EventManager?.GetAllEventNames();
        if (allEvents == null || allEvents.Count == 0)
        {
            Console.WriteLine("[顶级狂欢++] 警告：无法获取事件列表");
            return;
        }

        // 过滤掉 NoEvent 和 TopTierParty 系列
        var availableEvents = allEvents
            .Where(name => name != "NoEvent" &&
                        name != "TopTierParty" &&
                        name != "TopTierPartyPlusPlus")
            .ToList();

        if (availableEvents.Count < 3)
        {
            Console.WriteLine("[顶级狂欢++] 警告：可用事件不足3个");
            return;
        }

        // 随机选择三个不同的事件
        int firstIndex = _random.Next(availableEvents.Count);
        string firstEventName = availableEvents[firstIndex];

        availableEvents.RemoveAt(firstIndex);
        int secondIndex = _random.Next(availableEvents.Count);
        string secondEventName = availableEvents[secondIndex];

        availableEvents.RemoveAt(secondIndex);
        int thirdIndex = _random.Next(availableEvents.Count);
        string thirdEventName = availableEvents[thirdIndex];

        // 获取事件实例
        _firstEvent = Plugin?.EventManager?.GetEvent(firstEventName);
        _secondEvent = Plugin?.EventManager?.GetEvent(secondEventName);
        _thirdEvent = Plugin?.EventManager?.GetEvent(thirdEventName);

        if (_firstEvent == null || _secondEvent == null || _thirdEvent == null)
        {
            Console.WriteLine("[顶级狂欢++] 警告：无法获取事件实例");
            return;
        }

        Console.WriteLine($"[顶级狂欢++] 选中的事件: {_firstEvent.DisplayName}, {_secondEvent.DisplayName} 和 {_thirdEvent.DisplayName}");

        // 应用三个事件
        try
        {
            _firstEvent.OnApply();
            Console.WriteLine($"[顶级狂欢++] 已应用事件 1: {_firstEvent.DisplayName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[顶级狂欢++] 应用事件 1 时出错: {ex.Message}");
        }

        try
        {
            _secondEvent.OnApply();
            Console.WriteLine($"[顶级狂欢++] 已应用事件 2: {_secondEvent.DisplayName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[顶级狂欢++] 应用事件 2 时出错: {ex.Message}");
        }

        try
        {
            _thirdEvent.OnApply();
            Console.WriteLine($"[顶级狂欢++] 已应用事件 3: {_thirdEvent.DisplayName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[顶级狂欢++] 应用事件 3 时出错: {ex.Message}");
        }

        // 显示事件提示
        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsValid)
            {
                p.PrintToChat("───────────────────");
                p.PrintToChat("🎊🎊 " + DisplayName);
                p.PrintToChat($"📝 {_firstEvent.DisplayName}");
                p.PrintToChat($"📝 {_secondEvent.DisplayName}");
                p.PrintToChat($"📝 {_thirdEvent.DisplayName}");
                p.PrintToChat("───────────────────");
            }
        }

        Plugin?.AddTimer(3.0f, () =>
        {
            foreach (var p in Utilities.GetPlayers())
            {
                if (p.IsValid)
                {
                    p.PrintToCenter($"━━━━━━━━━━━━━━━━\n 🎊 {_firstEvent.DisplayName}\n 🎊 {_secondEvent.DisplayName}\n 🎊 {_thirdEvent.DisplayName}\n━━━━━━━━━━━━━━━━");
                }
            }
        });

        // 额外的延时提示
        Plugin?.AddTimer(6.0f, () =>
        {
            foreach (var p in Utilities.GetPlayers())
            {
                if (p.IsValid)
                {
                    p.PrintToChat("🎊🎊🎊 顶级狂欢已启动！准备好迎接混乱吧！");
                }
            }
        });
    }

    public override void OnRevert()
    {
        Console.WriteLine("[顶级狂欢++] 正在恢复事件");

        // 恢复三个事件（按相反顺序：后进先出）
        if (_thirdEvent != null)
        {
            try
            {
                _thirdEvent.OnRevert();
                Console.WriteLine($"[顶级狂欢++] 已恢复事件 1: {_thirdEvent.DisplayName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[顶级狂欢++] 恢复事件 1 时出错: {ex.Message}");
            }
        }

        if (_secondEvent != null)
        {
            try
            {
                _secondEvent.OnRevert();
                Console.WriteLine($"[顶级狂欢++] 已恢复事件 2: {_secondEvent.DisplayName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[顶级狂欢++] 恢复事件 2 时出错: {ex.Message}");
            }
        }

        if (_firstEvent != null)
        {
            try
            {
                _firstEvent.OnRevert();
                Console.WriteLine($"[顶级狂欢++] 已恢复事件 3: {_firstEvent.DisplayName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[顶级狂欢++] 恢复事件 3 时出错: {ex.Message}");
            }
        }

        _firstEvent = null;
        _secondEvent = null;
        _thirdEvent = null;
    }
}
