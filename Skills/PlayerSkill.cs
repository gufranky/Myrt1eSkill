using CounterStrikeSharp.API.Core;

namespace MyrtleSkill.Skills;

/// <summary>
/// 玩家技能基类
/// 技能是针对单个玩家的效果，每个玩家在回合开始时随机抽取一个技能
/// </summary>
public abstract class PlayerSkill
{
    /// <summary>
    /// 技能唯一标识符
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// 技能显示名称
    /// </summary>
    public abstract string DisplayName { get; }

    /// <summary>
    /// 技能描述
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// 技能权重（用于随机选择）
    /// </summary>
    public int Weight { get; set; } = 10;

    /// <summary>
    /// 技能类型：true=主动技能（需要按键激活），false=被动技能（自动生效）
    /// </summary>
    public abstract bool IsActive { get; }

    /// <summary>
    /// 技能冷却时间（秒）。仅对主动技能有效。
    /// </summary>
    public virtual float Cooldown { get; } = 10.0f;

    /// <summary>
    /// 与该技能互斥的事件名称列表。在这些事件激活时，该技能不会被抽到。
    /// </summary>
    public virtual List<string> ExcludedEvents { get; } = new();

    /// <summary>
    /// 与该技能互斥的技能名称列表。如果玩家已有这些技能，该技能不会被抽到。
    /// </summary>
    public virtual List<string> ExcludedSkills { get; } = new();

    /// <summary>
    /// 插件引用
    /// </summary>
    protected MyrtleSkill? Plugin { get; private set; }

    /// <summary>
    /// 注册技能到插件
    /// </summary>
    public void Register(MyrtleSkill plugin)
    {
        Plugin = plugin;
    }

    /// <summary>
    /// 对玩家应用技能效果（被动技能在回合开始时调用，主动技能在获得技能时调用）
    /// </summary>
    /// <param name="player">目标玩家</param>
    public abstract void OnApply(CCSPlayerController player);

    /// <summary>
    /// 移除玩家的技能效果
    /// </summary>
    /// <param name="player">目标玩家</param>
    public abstract void OnRevert(CCSPlayerController player);

    /// <summary>
    /// 使用技能（仅主动技能需要实现此方法）
    /// </summary>
    /// <param name="player">使用技能的玩家</param>
    public virtual void OnUse(CCSPlayerController player)
    {
        // 被动技能不需要实现此方法
    }
}
