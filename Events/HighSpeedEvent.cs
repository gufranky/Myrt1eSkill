using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace HelloWorldPlugin;

/// <summary>
/// 高速移动事件 - 玩家移速翻倍
/// </summary>
public class HighSpeedEvent : EntertainmentEvent
{
    public override string Name => "HighSpeed";
    public override string DisplayName => "高速移动";
    public override string Description => "所有玩家移速翻倍！";

    private const float SpeedMultiplier = 2.0f;
    private readonly Dictionary<int, float> _originalSpeed = new();

    public override void OnApply()
    {
        Console.WriteLine("[高速移动] 设置移速为当前值的 " + SpeedMultiplier + " 倍");

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            // 保存原始速度值
            _originalSpeed[player.Slot] = pawn.VelocityModifier;

            // 应用速度倍数
            pawn.VelocityModifier *= SpeedMultiplier;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[高速移动] 恢复移速为原始值");

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            // 恢复原始速度值
            if (_originalSpeed.ContainsKey(player.Slot))
            {
                pawn.VelocityModifier = _originalSpeed[player.Slot];
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
            }
        }

        _originalSpeed.Clear();
    }
}
