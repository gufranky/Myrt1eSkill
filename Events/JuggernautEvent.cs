using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace HelloWorldPlugin;

/// <summary>
/// 重装战士事件 - 高护甲低速度
/// </summary>
public class JuggernautEvent : EntertainmentEvent
{
    public override string Name => "Juggernaut";
    public override string DisplayName => "重装战士";
    public override string Description => "所有玩家获得500生命、200护甲，但移速降低30%！";

    private const int JuggernautHealth = 500;
    private const int JuggernautArmor = 200;
    private const float SpeedMultiplier = 0.7f;

    private readonly Dictionary<int, float> _originalSpeed = new();

    public override void OnApply()
    {
        Console.WriteLine("[重装战士] 设置护甲200，生命500，移速为当前值的 " + SpeedMultiplier + " 倍");

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            // 设置生命值（绝对值）
            pawn.Health = JuggernautHealth;
            pawn.MaxHealth = JuggernautHealth;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");

            // 设置护甲值（绝对值）
            pawn.ArmorValue = JuggernautArmor;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");

            // 保存原始速度值并应用速度倍数
            _originalSpeed[player.Slot] = pawn.VelocityModifier;
            pawn.VelocityModifier *= SpeedMultiplier;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");

            Console.WriteLine($"[重装战士] {player.PlayerName} 已成为重装战士");
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[重装战士] 恢复移速为原始值");

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            // 恢复移速
            if (_originalSpeed.ContainsKey(player.Slot))
            {
                pawn.VelocityModifier = _originalSpeed[player.Slot];
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
            }
        }

        _originalSpeed.Clear();
    }
}
