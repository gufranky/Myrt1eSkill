using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MyrtleSkill;

/// <summary>
/// 超低重力事件 - 重力0.25 + 空中无扩散
/// </summary>
public class LowGravityPlusPlusEvent : EntertainmentEvent
{
    public override string Name => "LowGravityPlusPlus";
    public override string DisplayName => "超低重力";
    public override string Description => "重力大幅降低，空中射击无扩散！";

    private const float GravityMultiplier = 0.25f;
    private readonly Dictionary<int, float> _originalGravity = new();

    public override void OnApply()
    {
        Console.WriteLine("[超低重力] 设置重力为当前值的 " + GravityMultiplier + " 倍，启用无扩散");

        // 设置重力
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            // 保存原始重力值
            _originalGravity[player.Slot] = pawn.GravityScale;

            // 应用重力倍数
            pawn.GravityScale *= GravityMultiplier;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flGravityScale");
        }

        // 启用无扩散
        Server.ExecuteCommand("weapon_accuracy_nospread true");
    }

    public override void OnRevert()
    {
        Console.WriteLine("[超低重力] 恢复重力为原始值，禁用无扩散");

        // 恢复重力
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            // 恢复原始重力值
            if (_originalGravity.ContainsKey(player.Slot))
            {
                pawn.GravityScale = _originalGravity[player.Slot];
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flGravityScale");
            }
        }

        _originalGravity.Clear();

        // 禁用无扩散
        Server.ExecuteCommand("weapon_accuracy_nospread false");
    }
}
