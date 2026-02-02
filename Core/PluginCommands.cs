using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace MyrtleSkill.Core;

/// <summary>
/// 插件命令处理类
/// 负责处理所有控制台命令
/// </summary>
public class PluginCommands
{
    private readonly MyrtleSkill _plugin;

    public PluginCommands(MyrtleSkill plugin)
    {
        _plugin = plugin;
    }

    /*
    #region 重甲战士命令

    public void CommandEnableHeavyArmor(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (_plugin.HeavyArmorManager.IsEnabled)
        {
            commandInfo.ReplyToCommand("重甲战士模式已经是启用状态！");
            return;
        }

        _plugin.HeavyArmorManager.IsEnabled = true;
        string message = "✅ 重甲战士模式已启用！下一回合将随机选择重甲战士。";

        if (player == null)
        {
            Console.WriteLine("[重甲幸运玩家插件] " + message);
            commandInfo.ReplyToCommand(message);
        }
        else
        {
            player.PrintToChat("[重甲战士] " + message);
            Console.WriteLine("[重甲幸运玩家插件] " + player.PlayerName + " 启用了重甲战士模式");
        }

        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsValid && p != player)
            {
                p.PrintToChat("🎮 重甲战士模式已启用！");
            }
        }
    }

    public void CommandDisableHeavyArmor(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!_plugin.HeavyArmorManager.IsEnabled)
        {
            commandInfo.ReplyToCommand("重甲战士模式已经是禁用状态！");
            return;
        }

        _plugin.HeavyArmorManager.IsEnabled = false;

        string message = "❌ 重甲战士模式已禁用！";

        if (player == null)
        {
            Console.WriteLine("[重甲幸运玩家插件] " + message);
            commandInfo.ReplyToCommand(message);
        }
        else
        {
            player.PrintToChat("[重甲战士] " + message);
            Console.WriteLine("[重甲幸运玩家插件] " + player.PlayerName + " 禁用了重甲战士模式");
        }

        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsValid && p != player)
            {
                p.PrintToChat("🎮 重甲战士模式已禁用！");
            }
        }
    }

    public void CommandStatusHeavyArmor(CCSPlayerController? player, CommandInfo commandInfo)
    {
        string status = _plugin.HeavyArmorManager.IsEnabled ? "✅ 启用" : "❌ 禁用";
        string currentWarrior = _plugin.HeavyArmorManager.CurrentPlayer != null && _plugin.HeavyArmorManager.CurrentPlayer.IsValid
            ? "🛡️ 当前重甲战士: " + _plugin.HeavyArmorManager.CurrentPlayer.PlayerName
            : "🛡️ 当前无重甲战士";

        if (player == null)
        {
            commandInfo.ReplyToCommand("=== 重甲战士插件状态 ===");
            commandInfo.ReplyToCommand("状态: " + status);
            commandInfo.ReplyToCommand(currentWarrior);
        }
        else
        {
            player.PrintToChat("=== 重甲战士插件状态 ===");
            player.PrintToChat("状态: " + status);
            player.PrintToChat(currentWarrior);
        }
    }

    #endregion
    */

    #region 娱乐事件命令

    public void CommandEventEnable(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (_plugin.EventManager.IsEnabled)
        {
            commandInfo.ReplyToCommand("娱乐事件系统已经是启用状态！");
            return;
        }

        _plugin.EventManager.IsEnabled = true;
        string message = "🎲 娱乐事件系统已启用！下回合将开始随机事件。";

        if (player == null)
        {
            Console.WriteLine("[娱乐事件] " + message);
            commandInfo.ReplyToCommand(message);
        }
        else
        {
            player.PrintToChat("[娱乐事件] " + message);
            Console.WriteLine("[娱乐事件] " + player.PlayerName + " 启用了娱乐事件系统");
        }

        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsValid && p != player)
            {
                p.PrintToChat("🎲 娱乐事件系统已启用！");
            }
        }
    }

    public void CommandEventDisable(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!_plugin.EventManager.IsEnabled)
        {
            commandInfo.ReplyToCommand("娱乐事件系统已经是禁用状态！");
            return;
        }

        _plugin.EventManager.IsEnabled = false;

        if (_plugin.CurrentEvent != null)
        {
            _plugin.CurrentEvent.OnRevert();
            _plugin.CurrentEvent = null;
        }

        string message = "🚫 娱乐事件系统已禁用！";

        if (player == null)
        {
            Console.WriteLine("[娱乐事件] " + message);
            commandInfo.ReplyToCommand(message);
        }
        else
        {
            player.PrintToChat("[娱乐事件] " + message);
            Console.WriteLine("[娱乐事件] " + player.PlayerName + " 禁用了娱乐事件系统");
        }

        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsValid && p != player)
            {
                p.PrintToChat("🎲 娱乐事件系统已禁用！");
            }
        }
    }

    public void CommandEventStatus(CCSPlayerController? player, CommandInfo commandInfo)
    {
        string status = _plugin.EventManager.IsEnabled ? "✅ 启用" : "❌ 禁用";
        string current = _plugin.CurrentEvent != null
            ? "🎲 当前事件: " + _plugin.CurrentEvent.Name
            : "🎲 当前无事件";
        string previous = _plugin.PreviousEvent != null
            ? "📜 上回合事件: " + _plugin.PreviousEvent.Name
            : "📜 上回合无事件";

        if (player == null)
        {
            commandInfo.ReplyToCommand("=== 娱乐事件系统状态 ===");
            commandInfo.ReplyToCommand("系统状态: " + status);
            commandInfo.ReplyToCommand(current);
            commandInfo.ReplyToCommand(previous);
        }
        else
        {
            player.PrintToChat("=== 娱乐事件系统状态 ===");
            player.PrintToChat("系统状态: " + status);
            player.PrintToChat(current);
            player.PrintToChat(previous);
        }
    }

    public void CommandEventList(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var events = _plugin.EventManager.GetAllEventNames();
        if (player == null)
        {
            commandInfo.ReplyToCommand("=== 可用事件列表 (" + events.Count + "个) ===");
            foreach (var eventName in events)
            {
                commandInfo.ReplyToCommand("  • " + eventName);
            }
        }
        else
        {
            player.PrintToChat("=== 可用事件列表 (" + events.Count + "个) ===");
            foreach (var eventName in events)
            {
                player.PrintToChat("  • " + eventName);
            }
        }
    }

    public void CommandEventWeights(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var weights = _plugin.EventManager.GetAllEventWeights();
        if (player == null)
        {
            commandInfo.ReplyToCommand("=== 事件权重列表 ===");
            foreach (var kvp in weights)
            {
                commandInfo.ReplyToCommand("  " + kvp.Key + ": " + kvp.Value);
            }
        }
        else
        {
            player.PrintToChat("=== 事件权重列表 ===");
            foreach (var kvp in weights)
            {
                player.PrintToChat("  " + kvp.Key + ": " + kvp.Value);
            }
        }
    }

    public void CommandEventWeight(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (commandInfo.ArgCount < 1)
        {
            string message = "用法: css_event_weight <事件英文名称> [权重值]";
            if (player == null)
                commandInfo.ReplyToCommand(message);
            else
                player.PrintToChat(message);
            return;
        }

        string eventName = commandInfo.GetArg(1);

        if (commandInfo.ArgCount == 1)
        {
            int weight = _plugin.EventManager.GetEventWeight(eventName);
            if (weight >= 0)
            {
                string message = "事件 '" + eventName + "' 的权重: " + weight;
                if (player == null)
                    commandInfo.ReplyToCommand(message);
                else
                    player.PrintToChat(message);
            }
            else
            {
                string message = "未找到事件: " + eventName;
                if (player == null)
                    commandInfo.ReplyToCommand(message);
                else
                    player.PrintToChat(message);
            }
            return;
        }

        if (!int.TryParse(commandInfo.GetArg(2), out int newWeight))
        {
            string message = "权重值必须是整数！";
            if (player == null)
                commandInfo.ReplyToCommand(message);
            else
                player.PrintToChat(message);
            return;
        }

        if (newWeight < 0)
        {
            string message = "权重值不能小于0！";
            if (player == null)
                commandInfo.ReplyToCommand(message);
            else
                player.PrintToChat(message);
            return;
        }

        bool success = _plugin.EventManager.SetEventWeight(eventName, newWeight);
        string resultMessage;
        if (success)
        {
            resultMessage = "✅ 事件 '" + eventName + "' 的权重已设置为 " + newWeight;
            if (newWeight == 0)
            {
                resultMessage += " (事件已禁用)";
            }
        }
        else
        {
            resultMessage = "❌ 未找到事件: " + eventName;
        }

        if (player == null)
            commandInfo.ReplyToCommand(resultMessage);
        else
            player.PrintToChat(resultMessage);
    }

    public void CommandForceEvent(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (commandInfo.ArgCount < 1)
        {
            string message = "用法: css_forceevent <事件英文名称>";
            if (player == null)
                commandInfo.ReplyToCommand(message);
            else
                player.PrintToChat(message);
            return;
        }

        string eventName = commandInfo.GetArg(1);

        // 验证事件是否存在
        var targetEvent = _plugin.EventManager.GetEvent(eventName);
        if (targetEvent == null)
        {
            string message = "❌ 未找到事件: " + eventName + "\n使用 css_event_list 查看所有可用事件";
            if (player == null)
                commandInfo.ReplyToCommand(message);
            else
                player.PrintToChat(message);
            return;
        }

        // 设置强制事件
        _plugin.ForcedEventName = eventName;

        string successMessage = $"✅ 下回合将强制触发事件: {targetEvent.DisplayName} ({targetEvent.Name})";
        if (player == null)
        {
            Console.WriteLine("[娱乐事件] " + successMessage);
            commandInfo.ReplyToCommand(successMessage);
        }
        else
        {
            player.PrintToChat("[娱乐事件] " + successMessage);
            Console.WriteLine("[娱乐事件] " + player.PlayerName + " 设置了强制事件: " + eventName);
        }
    }

    #endregion

    #region 炸弹相关命令

    public void CommandEnableAllowAnywherePlant(CCSPlayerController? player, CommandInfo commandInfo)
    {
        _plugin.BombPlantManager.AllowAnywherePlant = true;
        string message = "✅ 任意下包功能已启用！";
        if (player == null)
        {
            Console.WriteLine(message);
            commandInfo.ReplyToCommand(message);
        }
        else
        {
            player.PrintToChat(message);
        }
    }

    public void CommandDisableAllowAnywherePlant(CCSPlayerController? player, CommandInfo commandInfo)
    {
        _plugin.BombPlantManager.AllowAnywherePlant = false;
        string message = "❌ 任意下包功能已禁用！";
        if (player == null)
        {
            Console.WriteLine(message);
            commandInfo.ReplyToCommand(message);
        }
        else
        {
            player.PrintToChat(message);
        }
    }

    public void CommandAllowAnywherePlantStatus(CCSPlayerController? player, CommandInfo commandInfo)
    {
        string status = _plugin.BombPlantManager.AllowAnywherePlant ? "✅ 启用" : "❌ 禁用";
        string message = "任意下包功能状态: " + status;
        if (player == null)
        {
            Console.WriteLine(message);
            commandInfo.ReplyToCommand(message);
        }
        else
        {
            player.PrintToChat(message);
        }
    }

    public void CommandSetBombTimer(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (commandInfo.ArgCount < 1)
        {
            commandInfo.ReplyToCommand("用法: css_bombtimer_set <时间（秒）>");
            return;
        }

        if (!float.TryParse(commandInfo.GetArg(1), out float time))
        {
            commandInfo.ReplyToCommand("请输入有效的数字！");
            return;
        }

        if (time < 5 || time > 300)
        {
            commandInfo.ReplyToCommand("时间范围必须在 5 到 300 秒之间！");
            return;
        }

        _plugin.BombPlantManager.BombTimer = time;
        string message = "✅ 炸弹爆炸时间已设置为 " + _plugin.BombPlantManager.BombTimer + " 秒";
        if (player == null)
        {
            Console.WriteLine(message);
            commandInfo.ReplyToCommand(message);
        }
        else
        {
            player.PrintToChat(message);
        }
    }

    public void CommandBombTimerStatus(CCSPlayerController? player, CommandInfo commandInfo)
    {
        string message = "炸弹爆炸时间: " + _plugin.BombPlantManager.BombTimer + " 秒";
        if (player == null)
        {
            Console.WriteLine(message);
            commandInfo.ReplyToCommand(message);
        }
        else
        {
            player.PrintToChat(message);
        }
    }

    #endregion

    #region 玩家技能命令

    public void CommandSkillEnable(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (_plugin.SkillManager.IsEnabled)
        {
            commandInfo.ReplyToCommand("玩家技能系统已经是启用状态！");
            return;
        }

        _plugin.SkillManager.IsEnabled = true;
        string message = "✅ 玩家技能系统已启用！下一回合每个玩家将获得随机技能。";

        if (player == null)
        {
            Console.WriteLine("[玩家技能系统] " + message);
            commandInfo.ReplyToCommand(message);
        }
        else
        {
            player.PrintToChat("[技能系统] " + message);
            Console.WriteLine("[玩家技能系统] " + player.PlayerName + " 启用了技能系统");
        }

        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsValid && p != player)
            {
                p.PrintToChat("💫 玩家技能系统已启用！");
            }
        }
    }

    public void CommandSkillDisable(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!_plugin.SkillManager.IsEnabled)
        {
            commandInfo.ReplyToCommand("玩家技能系统已经是禁用状态！");
            return;
        }

        _plugin.SkillManager.IsEnabled = false;

        string message = "❌ 玩家技能系统已禁用！";

        if (player == null)
        {
            Console.WriteLine("[玩家技能系统] " + message);
            commandInfo.ReplyToCommand(message);
        }
        else
        {
            player.PrintToChat("[技能系统] " + message);
            Console.WriteLine("[玩家技能系统] " + player.PlayerName + " 禁用了技能系统");
        }

        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsValid && p != player)
            {
                p.PrintToChat("💫 玩家技能系统已禁用！");
            }
        }
    }

    public void CommandSkillStatus(CCSPlayerController? player, CommandInfo commandInfo)
    {
        string status = _plugin.SkillManager.IsEnabled ? "✅ 启用" : "❌ 禁用";

        if (player == null)
        {
            commandInfo.ReplyToCommand("=== 玩家技能系统状态 ===");
            commandInfo.ReplyToCommand("状态: " + status);
            commandInfo.ReplyToCommand("已注册技能数: " + _plugin.SkillManager.GetSkillCount());
        }
        else
        {
            player.PrintToChat("=== 玩家技能系统状态 ===");
            player.PrintToChat("状态: " + status);
            player.PrintToChat("已注册技能数: " + _plugin.SkillManager.GetSkillCount());

            // 显示玩家当前技能
            var currentSkill = _plugin.SkillManager.GetPlayerSkill(player);
            if (currentSkill != null)
            {
                player.PrintToChat("💫 你的当前技能: " + currentSkill.DisplayName);
                player.PrintToChat("📝 " + currentSkill.Description);
            }
            else
            {
                player.PrintToChat("💫 你当前没有技能");
            }
        }
    }

    public void CommandSkillList(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var skills = _plugin.SkillManager.GetAllSkillNames();

        if (player == null)
        {
            commandInfo.ReplyToCommand("=== 可用技能列表 ===");
            foreach (var skillName in skills)
            {
                var skill = _plugin.SkillManager.GetSkill(skillName);
                if (skill != null)
                {
                    commandInfo.ReplyToCommand($"{skill.DisplayName} (权重: {skill.Weight})");
                }
            }
        }
        else
        {
            player.PrintToChat("=== 可用技能列表 ===");
            foreach (var skillName in skills)
            {
                var skill = _plugin.SkillManager.GetSkill(skillName);
                if (skill != null)
                {
                    player.PrintToChat($"{skill.DisplayName} (权重: {skill.Weight})");
                }
            }
        }
    }

    public void CommandSkillWeight(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (commandInfo.ArgCount < 2)
        {
            commandInfo.ReplyToCommand("用法: css_skill_weight <技能名称> [权重]");
            return;
        }

        string skillName = commandInfo.ArgByIndex(1);
        var skill = _plugin.SkillManager.GetSkill(skillName);

        if (skill == null)
        {
            commandInfo.ReplyToCommand($"错误：找不到技能 '{skillName}'");
            return;
        }

        // 如果只有技能名称，显示当前权重
        if (commandInfo.ArgCount == 2)
        {
            string message = $"技能 '{skill.DisplayName}' 当前权重: {skill.Weight}";
            if (player == null)
            {
                commandInfo.ReplyToCommand(message);
            }
            else
            {
                player.PrintToChat(message);
            }
            return;
        }

        // 设置新权重
        if (!int.TryParse(commandInfo.ArgByIndex(2), out int newWeight) || newWeight < 0)
        {
            commandInfo.ReplyToCommand("错误：权重必须是非负整数");
            return;
        }

        _plugin.SkillManager.SetSkillWeight(skillName, newWeight);
        string successMessage = $"✅ 技能 '{skill.DisplayName}' 权重已设置为: {newWeight}";

        if (player == null)
        {
            Console.WriteLine("[玩家技能系统] " + successMessage);
            commandInfo.ReplyToCommand(successMessage);
        }
        else
        {
            player.PrintToChat(successMessage);
        }
    }

    public void CommandSkillWeights(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var weights = _plugin.SkillManager.GetAllSkillWeights();

        if (player == null)
        {
            commandInfo.ReplyToCommand("=== 所有技能权重 ===");
            foreach (var kvp in weights.OrderBy(x => x.Key))
            {
                commandInfo.ReplyToCommand($"{kvp.Key}: {kvp.Value}");
            }
        }
        else
        {
            player.PrintToChat("=== 所有技能权重 ===");
            foreach (var kvp in weights.OrderBy(x => x.Key))
            {
                player.PrintToChat($"{kvp.Key}: {kvp.Value}");
            }
        }
    }

    public void CommandUseSkill(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null)
        {
            commandInfo.ReplyToCommand("此命令只能由玩家使用！");
            return;
        }

        if (!_plugin.SkillManager.IsEnabled)
        {
            player.PrintToChat("💫 技能系统未启用！");
            return;
        }

        _plugin.SkillManager.UsePlayerSkill(player);
    }

    public void CommandForceSkill(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (commandInfo.ArgCount < 1)
        {
            string message = "用法: css_forceskill <技能英文名称> [玩家名称]";
            if (player == null)
                commandInfo.ReplyToCommand(message);
            else
                player.PrintToChat(message);
            return;
        }

        string skillName = commandInfo.GetArg(1);

        // 验证技能是否存在
        var targetSkill = _plugin.SkillManager.GetSkill(skillName);
        if (targetSkill == null)
        {
            string message = "❌ 未找到技能: " + skillName + "\n使用 css_skill_list 查看所有可用技能";
            if (player == null)
                commandInfo.ReplyToCommand(message);
            else
                player.PrintToChat(message);
            return;
        }

        // 如果指定了玩家名称
        CCSPlayerController? targetPlayer = player;
        if (commandInfo.ArgCount >= 2)
        {
            string playerName = commandInfo.GetArg(2);
            targetPlayer = Utilities.GetPlayers().FirstOrDefault(p =>
                p.IsValid && p.PlayerName.Contains(playerName, StringComparison.OrdinalIgnoreCase));

            if (targetPlayer == null)
            {
                string message = "❌ 未找到玩家: " + playerName;
                if (player == null)
                    commandInfo.ReplyToCommand(message);
                else
                    player.PrintToChat(message);
                return;
            }
        }
        else if (player == null)
        {
            commandInfo.ReplyToCommand("从控制台使用时必须指定玩家名称！");
            commandInfo.ReplyToCommand("用法: css_forceskill <技能英文名称> <玩家名称>");
            return;
        }

        // 应用指定技能
        if (targetPlayer == null)
        {
            string message = "❌ 目标玩家无效";
            if (player == null)
                commandInfo.ReplyToCommand(message);
            else
                player.PrintToChat(message);
            return;
        }

        _plugin.SkillManager.ApplySpecificSkillToPlayer(targetPlayer, skillName);

        string successMessage = $"✅ 玩家 {targetPlayer.PlayerName} 被强制赋予技能: {targetSkill.DisplayName} ({targetSkill.Name})";
        if (player == null)
        {
            Console.WriteLine("[玩家技能系统] " + successMessage);
            commandInfo.ReplyToCommand(successMessage);
        }
        else
        {
            player.PrintToChat("[技能系统] " + successMessage);
            Console.WriteLine("[玩家技能系统] " + player.PlayerName + " 为 " + targetPlayer.PlayerName + " 强制赋予技能: " + skillName);
        }
    }

    #endregion
}
