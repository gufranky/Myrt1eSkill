// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace MyrtleSkill.Skills;

/// <summary>
/// 时间控制者技能 - 主动技能
/// 你可以操控游戏速度，在 0.75x、1x、1.5x 之间循环切换
/// </summary>
public class TimeControllerSkill : PlayerSkill
{
    public override string Name => "TimeController";
    public override string DisplayName => "⏰ 时间控制者";
    public override string Description => "按 [css_useskill] 操控游戏速度！在 0.75x、1x、1.5x 之间循环切换！";
    public override bool IsActive => true; // 主动技能
    public override float Cooldown => 0.1f; // 几乎无CD（0.1秒防止意外连点）

    // 速度档位
    private static readonly float[] SpeedLevels = { 0.75f, 1.0f, 1.5f };

    // 跟踪每个玩家的当前速度索引
    private readonly Dictionary<ulong, int> _playerSpeedIndex = new();

    // 跟踪当前激活的速度（全局）
    private float _currentSpeed = 1.0f;

    public override void OnApply(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 初始化为正常速度（索引1）
        _playerSpeedIndex[player.SteamID] = 1;

        Console.WriteLine($"[时间控制者] {player.PlayerName} 获得了时间控制者技能");

        player.PrintToChat("⏰ 你获得了时间控制者技能！");
        player.PrintToChat("💡 输入 !useskill 或按键切换游戏速度！");
        player.PrintToChat("🔄 速度档位：0.75x → 1x → 1.5x → 0.75x ...");
        player.PrintToChat("⚠️ 注意：会影响所有玩家！");
    }

    public override void OnRevert(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        // 恢复正常速度
        SetGameSpeed(1.0f);

        _playerSpeedIndex.Remove(player.SteamID);

        Console.WriteLine($"[时间控制者] {player.PlayerName} 失去了时间控制者技能");
    }

    public override void OnUse(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        // 获取当前玩家的速度索引
        if (!_playerSpeedIndex.TryGetValue(player.SteamID, out int currentIndex))
        {
            currentIndex = 1; // 默认为正常速度
            _playerSpeedIndex[player.SteamID] = currentIndex;
        }

        // 计算下一个速度索引
        int nextIndex = (currentIndex + 1) % SpeedLevels.Length;
        float nextSpeed = SpeedLevels[nextIndex];

        // 应用新速度
        SetGameSpeed(nextSpeed);

        // 更新索引
        _playerSpeedIndex[player.SteamID] = nextIndex;

        Console.WriteLine($"[时间控制者] {player.PlayerName} 将游戏速度设置为 {nextSpeed}x");

        // 通知所有玩家
        Server.PrintToChatAll($"⏰ {player.PlayerName} 操控了时间流速！");
        Server.PrintToChatAll($"🚀 当前游戏速度：{nextSpeed}x");
    }

    /// <summary>
    /// 设置游戏速度
    /// </summary>
    private void SetGameSpeed(float speed)
    {
        try
        {
            // 使用 ConVar 设置游戏速度
            var svCheats = ConVar.Find("sv_cheats");
            if (svCheats != null)
            {
                bool originalValue = svCheats.GetPrimitiveValue<bool>();
                svCheats.SetValue(true);

                var hostTimescale = ConVar.Find("host_timescale");
                if (hostTimescale != null)
                {
                    hostTimescale.SetValue(speed);
                    _currentSpeed = speed;
                    Console.WriteLine($"[时间控制者] 游戏速度已设置为 {speed}x");
                }

                svCheats.SetValue(originalValue);
            }
            else
            {
                // 直接尝试设置（可能需要sv_cheats）
                var hostTimescale = ConVar.Find("host_timescale");
                if (hostTimescale != null)
                {
                    hostTimescale.SetValue(speed);
                    _currentSpeed = speed;
                    Console.WriteLine($"[时间控制者] 游戏速度已设置为 {speed}x");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[时间控制者] 设置游戏速度时出错: {ex.Message}");
        }
    }
}
