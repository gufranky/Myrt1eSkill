using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MyrtleSkill;

/// <summary>
/// 迷你尺寸事件 - 玩家模型缩小
/// </summary>
public class MiniSizeEvent : EntertainmentEvent
{
    public override string Name => "MiniSize";
    public override string DisplayName => "迷你尺寸";
    public override string Description => "所有玩家变成迷你尺寸！";

    private const float ScaleMultiplier = 0.5f;
    private readonly Dictionary<int, float> _originalScale = new();

    public override void OnApply()
    {
        Console.WriteLine("[迷你尺寸] 设置玩家尺寸为当前值的 " + ScaleMultiplier + " 倍");

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive) continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            // 设置玩家模型缩放
            var sceneNode = pawn.CBodyComponent?.SceneNode;
            if (sceneNode != null)
            {
                // 保存当前缩放值（覆盖可能存在的旧值）
                _originalScale[player.Slot] = sceneNode.Scale;

                // 应用缩放倍数
                sceneNode.Scale *= ScaleMultiplier;
                pawn.AcceptInput("SetScale", null, null, sceneNode.Scale.ToString());
                Server.NextFrame(() =>
                {
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");
                });
            }

            Console.WriteLine($"[迷你尺寸] {player.PlayerName} 已缩小");
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[迷你尺寸] 正在恢复玩家尺寸为原始值");
        Console.WriteLine($"[迷你尺寸] _originalScale 包含 { _originalScale.Count} 个玩家");

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            // 恢复玩家模型缩放
            var sceneNode = pawn.CBodyComponent?.SceneNode;
            if (sceneNode != null && _originalScale.ContainsKey(player.Slot))
            {
                float originalScale = _originalScale[player.Slot];
                sceneNode.Scale = originalScale;
                pawn.AcceptInput("SetScale", null, null, originalScale.ToString());
                Server.NextFrame(() =>
                {
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");
                });

                Console.WriteLine($"[迷你尺寸] {player.PlayerName} 已恢复 (Scale: {originalScale})");
            }
            else if (sceneNode != null)
            {
                Console.WriteLine($"[迷你尺寸] 警告：{player.PlayerName} 的 sceneNode 为 null");
            }
            else if (!_originalScale.ContainsKey(player.Slot))
            {
                Console.WriteLine($"[迷你尺寸] 警告：{player.PlayerName} 不在 _originalScale 字典中");
            }
        }

        _originalScale.Clear();
        Console.WriteLine("[迷你尺寸] 已清空 _originalScale 字典");
    }
}
