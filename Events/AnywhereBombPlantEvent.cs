using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;

namespace MyrtleSkill;

/// <summary>
/// 任意下包事件 - 可以在任意位置下包
/// 完全复制 jRandomSkills Planter 技能的实现方式
/// </summary>
public class AnywhereBombPlantEvent : EntertainmentEvent
{
    public override string Name => "AnywhereBombPlant";
    public override string DisplayName => "任意下包";
    public override string Description => "可以在地图任意位置下包！";

    public override void OnApply()
    {
        Console.WriteLine("[任意下包] 事件已激活");

        // 显示提示给所有玩家
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("───────────────────");
                player.PrintToChat("💣 本回合事件：任意下包");
                player.PrintToChat("📝 你可以在地图任意位置下包！");
                player.PrintToChat("💡 拿着C4按E键即可下包");
                player.PrintToChat("───────────────────");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[任意下包] 事件已结束");
    }

    /// <summary>
    /// 处理下包后事件（在主文件的 OnBombPlanted 中调用）
    /// 完全复制 jRandomSkills Planter.BombPlanted 实现
    /// </summary>
    public void HandleBombPlanted(EventBombPlanted @event)
    {
        Console.WriteLine("[任意下包] 炸弹已下包，设置爆炸时间");

        // 完全复制 jRandomSkills 的实现（只设置 C4Blow 时间）
        var plantedBombs = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4");
        var plantedBomb = plantedBombs.FirstOrDefault();
        if (plantedBomb != null)
        {
            Server.NextFrame(() =>
            {
                if (plantedBomb.IsValid)
                {
                    // 只设置爆炸时间（60秒后爆炸）
                    plantedBomb.C4Blow = (float)Server.EngineTime + 60.0f;
                    Console.WriteLine($"[任意下包] 炸弹爆炸时间已设置（EngineTime: {Server.EngineTime}, BlowTime: {plantedBomb.C4Blow}）");
                }
            });
        }
        else
        {
            Console.WriteLine("[任意下包] 未找到已下包的炸弹！");
        }
    }

    /// <summary>
    /// 持续检查手持 C4 的玩家（在主文件的 OnServerPostEntityThink 中调用）
    /// 完全复制 jRandomSkills Planter.OnTick
    /// </summary>
    public void HandleServerPostEntityThink()
    {
        // 每60帧输出一次调试日志（避免日志过多）
        if (Server.TickCount % 60 == 0)
        {
            Console.WriteLine("[任意下包调试] HandleServerPostEntityThink 被调用");
        }

        // 对所有玩家设置 m_bInBombZone = true
        var players = Utilities.GetPlayers();
        foreach (var player in players)
        {
            if (player == null || !player.IsValid)
                continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            // 持续设置为在炸弹区域内（参考 jRandomSkills Planter.OnTick）
            Schema.SetSchemaValue<bool>(pawn.Handle, "CCSPlayerPawn", "m_bInBombZone", true);
        }
    }
}
