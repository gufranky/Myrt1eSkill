using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;
using System.Drawing;

namespace MyrtleSkill;

/// <summary>
/// 保持安静事件 - 玩家在发出声音时显形，保持安静时隐身
/// 基于声音事件拦截实现，精确检测脚步声、跳跃声等
/// </summary>
public class StayQuietEvent : EntertainmentEvent
{
    public override string Name => "StayQuiet";
    public override string DisplayName => "🤫 保持安静";
    public override string Description => "保持安静时隐身！发出声音会现身！";

    private const float VisibilityCooldown = 3.0f; // 现身后3秒才能再次隐身
    private readonly Dictionary<ulong, PlayerVisibilityState> _playerStates = new();

    // CS2声音事件哈希列表（来自jRandomSkills）
    private readonly uint[] _footstepSoundEvents = new uint[]
    {
        3109879199, 70939233, 1342713723, 2722081556, 1909915699, 3193435079, 2300993891,
        3847761506, 4084367249, 1342713723, 3847761506, 2026488395, 2745524735, 2684452812,
        2265091453, 1269567645, 520432428, 3266483468, 1346129716, 2061955732, 2240518199,
        2829617974, 1194677450, 1803111098, 3749333696, 29217150, 1692050905, 2207486967,
        2633527058, 3342414459, 988265811, 540697918, 1763490157, 3755338324, 3161194970,
        3753692454, 3166948458, 3997353267, 3161194970, 3753692454, 3166948458, 3997353267,
        809738584, 3368720745, 3295206520, 3184465677, 123085364, 3123711576, 737696412,
        1403457606, 1770765328, 892882552, 3023174225, 4163677892, 3952104171, 4082928848,
        1019414932, 1485322532, 1161855519, 1557420499, 1163426340, 809738584, 3368720745,
        2708661994, 2479376962, 3295206520, 1404198078, 1194093029, 1253503839, 2189706910,
        1218015996, 96240187, 1116700262, 84876002, 1598540856, 2231399653
    };

    private readonly uint[] _otherSoundEvents = new uint[]
    {
        2551626319, 765706800, 2860219006, 2162652424, 117596568, 740474905,
        1661204257, 3009312615, 1506215040, 115843229, 3299941720, 1016523349,
        2684452812, 2067683805, 4160462271, 1543118744, 585390608, 3802757032,
        2302139631, 2546391140, 144629619, 4152012084, 4113422219, 1627020521,
        2899365092, 819435812, 3218103073, 961838155, 1535891875, 1826799645,
        3460445620, 1818046345, 3666896632, 3099536373, 1440734007, 1409986305,
        1939055066, 782454593, 4074593561, 1540837791, 3257325156
    };

    public override void OnApply()
    {
        Console.WriteLine("[保持安静] 事件已激活");

        // 初始化所有玩家的状态
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive) continue;

            _playerStates[player.SteamID] = new PlayerVisibilityState
            {
                IsVisible = false,
                LastActionTime = Server.CurrentTime - VisibilityCooldown
            };

            // 设置初始隐身
            SetPlayerVisibility(player, false);
        }

        // 注册声音事件拦截和其他事件
        if (Plugin != null)
        {
            // 注册UserMessage监听（声音事件，ID=208）
            Plugin.HookUserMessage(208, OnPlayerMakeSound);
            Plugin.RegisterEventHandler<EventWeaponFire>(OnWeaponFire, HookMode.Post);
            Plugin.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Post);
            Plugin.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
            Plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
            Plugin.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Post);
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[保持安静] 事件已恢复");

        // 移除监听
        if (Plugin != null)
        {
            Plugin.UnhookUserMessage(208, OnPlayerMakeSound);
            Plugin.DeregisterEventHandler<EventWeaponFire>(OnWeaponFire, HookMode.Post);
            Plugin.DeregisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Post);
            Plugin.RemoveListener<Listeners.CheckTransmit>(OnCheckTransmit);
            Plugin.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
            Plugin.DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Post);
        }

        // 恢复所有玩家可见
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;

            SetPlayerVisibility(player, true);
        }

        _playerStates.Clear();
    }

    /// <summary>
    /// 监听玩家发出声音事件（基于UserMessage拦截）
    /// </summary>
    private HookResult OnPlayerMakeSound(UserMessage um)
    {
        var soundevent = um.ReadUInt("soundevent_hash");
        var userIndex = um.ReadUInt("source_entity_index");
        if (userIndex == 0) return HookResult.Continue;

        // 检查是否是我们关注的声音类型
        bool isFootstep = _footstepSoundEvents.Contains(soundevent);
        bool isOtherSound = _otherSoundEvents.Contains(soundevent);

        if (!isFootstep && !isOtherSound)
            return HookResult.Continue;

        // 找到发出声音的玩家
        var player = Utilities.GetPlayers().FirstOrDefault(p =>
            p.PlayerPawn?.Value != null &&
            p.PlayerPawn.Value.IsValid &&
            p.PlayerPawn.Value.Index == userIndex);

        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        var playerState = _playerStates.GetValueOrDefault(player.SteamID);
        if (playerState == null)
            return HookResult.Continue;

        // 如果玩家当前是隐身状态，让他显形
        if (!playerState.IsVisible)
        {
            MakePlayerVisible(player);

            // 记录声音类型用于调试
            string soundType = isFootstep ? "脚步声" : "其他声音";
            Console.WriteLine($"[保持安静] {player.PlayerName} 发出了{soundType}，哈希: {soundevent}");
        }

        return HookResult.Continue;
    }

    /// <summary>
    /// 监听武器开火
    /// </summary>
    private HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        var playerState = _playerStates.GetValueOrDefault(player.SteamID);
        if (playerState == null)
            return HookResult.Continue;

        if (!playerState.IsVisible)
        {
            MakePlayerVisible(player);
        }

        return HookResult.Continue;
    }

    /// <summary>
    /// 监听玩家受伤
    /// </summary>
    private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        var playerState = _playerStates.GetValueOrDefault(player.SteamID);
        if (playerState == null)
            return HookResult.Continue;

        if (!playerState.IsVisible)
        {
            MakePlayerVisible(player);
        }

        return HookResult.Continue;
    }

    /// <summary>
    /// 让玩家现身
    /// </summary>
    private void MakePlayerVisible(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var playerState = _playerStates.GetValueOrDefault(player.SteamID);
        if (playerState == null)
            return;

        playerState.IsVisible = true;
        playerState.LastActionTime = Server.CurrentTime;
        SetPlayerVisibility(player, true);

        player.PrintToChat("👣 你发出了声音，隐身失效！");
    }

    /// <summary>
    /// 检查传输时控制玩家可见性
    /// </summary>
    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (_playerStates.Count == 0)
            return;

        // 检查是否有玩家可以重新隐身
        float currentTime = Server.CurrentTime;
        foreach (var kvp in _playerStates)
        {
            var state = kvp.Value;
            if (state.IsVisible && (currentTime - state.LastActionTime) >= VisibilityCooldown)
            {
                // 可以重新隐身了
                var player = Utilities.GetPlayers().FirstOrDefault(p => p.SteamID == kvp.Key);
                if (player != null && player.IsValid && player.PawnIsAlive)
                {
                    state.IsVisible = false;
                    SetPlayerVisibility(player, false);
                    player.PrintToChat("🤫 你安静了，重新进入隐身状态！");
                }
            }
        }

        foreach (var (info, observer) in infoList)
        {
            if (observer == null || !observer.IsValid)
                continue;

            // 检查每个玩家的可见性
            foreach (var kvp in _playerStates)
            {
                ulong steamID = kvp.Key;
                var state = kvp.Value;

                // 如果玩家处于隐身状态且不是观察者自己
                if (!state.IsVisible && observer.SteamID != steamID)
                {
                    var player = Utilities.GetPlayers().FirstOrDefault(p => p.SteamID == steamID);
                    if (player == null || !player.IsValid)
                        continue;

                    var pawn = player.PlayerPawn.Value;
                    if (pawn == null || !pawn.IsValid)
                        continue;

                    // 移除玩家实体，使其不可见
                    info.TransmitEntities.Remove(pawn.Index);

                    // 也移除武器
                    if (pawn.WeaponServices != null)
                    {
                        foreach (var weapon in pawn.WeaponServices.MyWeapons)
                        {
                            if (weapon != null && weapon.IsValid)
                            {
                                info.TransmitEntities.Remove(weapon.Index);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 玩家生成时设置初始状态
    /// </summary>
    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        _playerStates[player.SteamID] = new PlayerVisibilityState
        {
            IsVisible = false,
            LastActionTime = Server.CurrentTime - VisibilityCooldown
        };

        SetPlayerVisibility(player, false);
        player.PrintToCenter("🤫 保持安静隐身模式！");

        return HookResult.Continue;
    }

    /// <summary>
    /// 玩家死亡时清理状态
    /// </summary>
    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        _playerStates.Remove(player.SteamID);

        return HookResult.Continue;
    }

    /// <summary>
    /// 设置玩家可见性（包括武器）
    /// 参考 jRandomSkills 的实现，同时设置玩家和武器的透明度
    /// </summary>
    private void SetPlayerVisibility(CCSPlayerController player, bool visible)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        // 设置玩家身体透明度
        var color = visible ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(0, 255, 255, 255);
        var shadowStrength = visible ? 1.0f : 0.0f;

        pawn.Render = color;
        pawn.ShadowStrength = shadowStrength;
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");

        // 设置武器透明度（参考 jRandomSkills Ninja 技能）
        SetWeaponVisibility(player, visible);
    }

    /// <summary>
    /// 设置武器可见性
    /// 参考 jRandomSkills 实现，武器隐身速度是玩家的2倍
    /// </summary>
    private void SetWeaponVisibility(CCSPlayerController player, bool visible)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn?.WeaponServices == null)
            return;

        // 武器使用更激进的透明度设置（完全隐身时 alpha=0）
        var weaponColor = visible
            ? Color.FromArgb(255, 255, 255, 255)
            : Color.FromArgb(0, 255, 255, 255);

        foreach (var weapon in pawn.WeaponServices.MyWeapons)
        {
            if (weapon != null && weapon.IsValid)
            {
                var weaponEntity = weapon.Value;
                if (weaponEntity != null && weaponEntity.IsValid)
                {
                    weaponEntity.Render = weaponColor;
                    Utilities.SetStateChanged(weaponEntity, "CBaseModelEntity", "m_clrRender");
                }
            }
        }
    }

    /// <summary>
    /// 玩家可见性状态
    /// </summary>
    private class PlayerVisibilityState
    {
        public bool IsVisible { get; set; }
        public float LastActionTime { get; set; }
    }
}
