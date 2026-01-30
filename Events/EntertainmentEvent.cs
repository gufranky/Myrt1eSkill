using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace HelloWorldPlugin;

/// <summary>
/// 娱乐事件基类
/// 所有随机事件都应继承此类
/// </summary>
public abstract class EntertainmentEvent
{
    /// <summary>
    /// 事件唯一标识名称（英文）
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// 事件显示名称（中文，用于提示玩家）
    /// </summary>
    public abstract string DisplayName { get; }

    /// <summary>
    /// 事件描述
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// 事件权重（权重越高，被选中的概率越大）
    /// </summary>
    public virtual int Weight { get; set; } = 10;

    /// <summary>
    /// 插件引用
    /// </summary>
    protected HelloWorldPlugin? Plugin { get; private set; }

    /// <summary>
    /// 注册事件到插件
    /// </summary>
    public virtual void Register(HelloWorldPlugin plugin)
    {
        Plugin = plugin;
    }

    /// <summary>
    /// 事件应用时调用
    /// </summary>
    public abstract void OnApply();

    /// <summary>
    /// 事件恢复时调用（用于撤销上一回合的影响）
    /// </summary>
    public virtual void OnRevert()
    {
        // 默认不做任何操作，子类可以重写以实现恢复逻辑
    }
}
