// MyrtleSkill Plugin - GNU GPL v3.0
// See LICENSE and ATTRIBUTION.md for details

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;

namespace MyrtleSkill;

/// <summary>
/// 足球模式事件 - 所有玩家没收物品，禁用商店，在T家生成足球
/// 足球进入CT区域时，给每个T发一把AK
/// </summary>
public class SoccerModeEvent : EntertainmentEvent
{
    public override string Name => "SoccerMode";
    public override string DisplayName => "⚽ 足球模式";
    public override string Description => "没收全部物品！禁用商店！在T家生成足球！足球进CT区给T发AK！";

    private CPhysicsProp? _soccerBall = null;
    private ConVar? _buyAllowGunsConVar;
    private int _originalBuyAllowGuns = 1;
    private ConVar? _buyTimeConVar;
    private float _originalBuyTime = 90.0f;

    // 物理相关 ConVar
    private ConVar? _turbophysicsConVar;
    private bool _originalTurbophysics = false;

    // 标志：事件是否激活
    private bool _isActive = false;

    // 记录足球是否已经进过CT区
    private bool _hasEnteredCTZone = false;

    public override void OnApply()
    {
        Console.WriteLine("[足球模式] 事件已激活");
        _isActive = true;
        _hasEnteredCTZone = false;

        // 1. 禁用商店（彻底禁用购买）
        _buyAllowGunsConVar = ConVar.Find("mp_buy_allow_guns");
        if (_buyAllowGunsConVar != null)
        {
            _originalBuyAllowGuns = _buyAllowGunsConVar.GetPrimitiveValue<int>();
            _buyAllowGunsConVar.SetValue(0);
            Console.WriteLine($"[足球模式] mp_buy_allow_guns 已设置为 0 (原值: {_originalBuyAllowGuns})");
        }

        // 禁用购买时间（设为0秒，彻底禁用商店）
        _buyTimeConVar = ConVar.Find("mp_buytime");
        if (_buyTimeConVar != null)
        {
            _originalBuyTime = _buyTimeConVar.GetPrimitiveValue<float>();
            _buyTimeConVar.SetValue(0.0f);
            Console.WriteLine($"[足球模式] mp_buytime 已设置为 0 (原值: {_originalBuyTime})");
        }

        // 启用物理模拟（使足球可以被踢动）
        _turbophysicsConVar = ConVar.Find("sv_turbophysics");
        if (_turbophysicsConVar != null)
        {
            _originalTurbophysics = _turbophysicsConVar.GetPrimitiveValue<bool>();
            _turbophysicsConVar.SetValue(true);
            Console.WriteLine($"[足球模式] sv_turbophysics 已设置为 true (原值: {_originalTurbophysics})");
        }

        // 2. 没收所有玩家物品
        RemoveAllWeaponsFromAllPlayers();

        // 3. 生成足球
        SpawnSoccerBall();

        // 4. 注册实体生成监听
        if (Plugin != null)
        {
            Plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
        }

        // 5. 启动定时器检查足球位置（每0.5秒检查一次，避免性能问题）
        Plugin?.AddTimer(0.5f, CheckBallPositionTimer, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);

        // 显示提示（保留聊天框提示，移除屏幕中间提示，统一由HUD显示）
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid)
            {
                player.PrintToChat("⚽ 足球模式已启用！");
                player.PrintToChat("🚫 所有物品已没收！商店已禁用！");
                player.PrintToChat("💡 把足球踢进CT区域，每个T获得一把AK！");
            }
        }
    }

    public override void OnRevert()
    {
        Console.WriteLine("[足球模式] 事件已恢复");

        // 首先停止tick检查（防止在恢复过程中继续执行）
        _isActive = false;

        // 延迟执行恢复操作，避免在监听器中使用
        Server.NextFrame(() =>
        {
            try
            {
                // 1. 恢复商店设置
                if (_buyAllowGunsConVar != null)
                {
                    _buyAllowGunsConVar.SetValue(_originalBuyAllowGuns);
                    Console.WriteLine($"[足球模式] mp_buy_allow_guns 已恢复为 {_originalBuyAllowGuns}");
                }

                if (_buyTimeConVar != null)
                {
                    _buyTimeConVar.SetValue(_originalBuyTime);
                    Console.WriteLine($"[足球模式] mp_buytime 已恢复为 {_originalBuyTime}");
                }

                // 恢复物理模拟设置
                if (_turbophysicsConVar != null)
                {
                    _turbophysicsConVar.SetValue(_originalTurbophysics);
                    Console.WriteLine($"[足球模式] sv_turbophysics 已恢复为 {_originalTurbophysics}");
                }

                // 2. 移除足球（使用 try-catch 保护）
                try
                {
                    if (_soccerBall != null && _soccerBall.IsValid)
                    {
                        _soccerBall.Remove();
                        _soccerBall = null;
                        Console.WriteLine("[足球模式] 足球已移除");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[足球模式] 移除足球时出错: {ex.Message}");
                }

                // 3. 移除事件监听
                if (Plugin != null)
                {
                    Plugin.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);

                    // 停止定时器（timerHandle 会在下次检查时自动失效，因为 _isActive = false）
                    Console.WriteLine("[足球模式] 事件监听器已移除");
                }

                // 显示提示
                foreach (var player in Utilities.GetPlayers())
                {
                    if (player.IsValid)
                    {
                        player.PrintToChat("⚽ 足球模式已结束");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[足球模式] 恢复时出错: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// 没收所有玩家的所有物品
    /// </summary>
    private void RemoveAllWeaponsFromAllPlayers()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive)
                continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            var weaponServices = pawn.WeaponServices;
            if (weaponServices == null)
                continue;

            // 移除所有武器
            var weaponsToRemove = new List<CBasePlayerWeapon>();
            foreach (var weaponHandle in weaponServices.MyWeapons)
            {
                if (!weaponHandle.IsValid)
                    continue;

                var weapon = weaponHandle.Get();
                if (weapon == null || !weapon.IsValid)
                    continue;

                weaponsToRemove.Add(weapon);
            }

            foreach (var weapon in weaponsToRemove)
            {
                weapon.Remove();
            }

            Console.WriteLine($"[足球模式] 已没收 {player.PlayerName} 的所有物品");
        }
    }

    /// <summary>
    /// 在T家出生点生成足球
    /// </summary>
    private void SpawnSoccerBall()
    {
        // 获取T家出生点
        var spawnPoints = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist").ToList();
        if (spawnPoints.Count == 0)
        {
            Console.WriteLine("[足球模式] 错误：未找到T家出生点！");
            return;
        }

        // 随机选择一个T家出生点
        var random = new Random();
        var randomSpawn = spawnPoints[random.Next(spawnPoints.Count)];
        if (randomSpawn == null || !randomSpawn.IsValid || randomSpawn.AbsOrigin == null)
        {
            Console.WriteLine("[足球模式] 错误：无法获取出生点位置！");
            return;
        }

        // 创建足球
        _soccerBall = Utilities.CreateEntityByName<CPhysicsProp>("prop_physics_override");
        if (_soccerBall == null || !_soccerBall.IsValid)
        {
            Console.WriteLine("[足球模式] 错误：无法创建足球实体！");
            return;
        }

        // 设置足球模型
        _soccerBall.SetModel("models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl");

        // 设置位置（略微抬高，避免卡在地上）
        var position = new Vector(
            randomSpawn.AbsOrigin.X,
            randomSpawn.AbsOrigin.Y,
            randomSpawn.AbsOrigin.Z + 32  // 抬高更多一些
        );

        _soccerBall.Teleport(position, new QAngle(0, 0, 0), new Vector(0, 0, 0));

        // 生成实体
        _soccerBall.DispatchSpawn();

        // 唤醒物理实体，使其可以被踢动
        Server.NextFrame(() =>
        {
            if (_soccerBall != null && _soccerBall.IsValid)
            {
                // 唤醒物理实体
                _soccerBall.AcceptInput("Wake");

                // 给一个初始的向上速度，确保它活跃
                if (_soccerBall.AbsVelocity != null)
                {
                    _soccerBall.AbsVelocity.X = 0;
                    _soccerBall.AbsVelocity.Y = 0;
                    _soccerBall.AbsVelocity.Z = 10;
                    Utilities.SetStateChanged(_soccerBall, "CBaseEntity", "m_vecAbsVelocity");
                }

                Console.WriteLine($"[足球模式] 足球已在T家生成，位置: ({position.X:F0}, {position.Y:F0}, {position.Z:F0})");
                Console.WriteLine("[足球模式] 足球物理属性已激活（可被踢动）");
            }
        });
    }

    /// <summary>
    /// 玩家生成时没收物品
    /// </summary>
    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            if (!player.IsValid || !player.PawnIsAlive)
                return;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                return;

            var weaponServices = pawn.WeaponServices;
            if (weaponServices == null)
                return;

            // 移除所有武器
            var weaponsToRemove = new List<CBasePlayerWeapon>();
            foreach (var weaponHandle in weaponServices.MyWeapons)
            {
                if (!weaponHandle.IsValid)
                    continue;

                var weapon = weaponHandle.Get();
                if (weapon == null || !weapon.IsValid)
                    continue;

                weaponsToRemove.Add(weapon);
            }

            foreach (var weapon in weaponsToRemove)
            {
                weapon.Remove();
            }

            Console.WriteLine($"[足球模式] {player.PlayerName} 生成时已没收所有物品");
        });

        return HookResult.Continue;
    }

    /// <summary>
    /// 定时器回调：检查足球位置（每0.5秒执行一次）
    /// </summary>
    private void CheckBallPositionTimer()
    {
        // 立即检查是否激活，防止在恢复过程中执行
        if (!_isActive)
            return;

        if (_soccerBall == null || !_soccerBall.IsValid)
            return;

        // 检查足球是否在CT区域
        CheckBallInCTZone();
    }

    /// <summary>
    /// 检查足球是否进入CT区域
    /// </summary>
    private void CheckBallInCTZone()
    {
        if (_soccerBall == null || !_soccerBall.IsValid || _soccerBall.AbsOrigin == null)
            return;

        var ballPosition = _soccerBall.AbsOrigin;

        // 获取所有CT出生点
        var ctSpawnPoints = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist").ToList();
        if (ctSpawnPoints.Count == 0)
            return;

        // 检查足球是否在任意一个CT出生点附近（半径200单位内）
        bool inCTZone = false;
        foreach (var spawn in ctSpawnPoints)
        {
            if (spawn == null || !spawn.IsValid || spawn.AbsOrigin == null)
                continue;

            var distance = Math.Sqrt(
                Math.Pow(ballPosition.X - spawn.AbsOrigin.X, 2) +
                Math.Pow(ballPosition.Y - spawn.AbsOrigin.Y, 2) +
                Math.Pow(ballPosition.Z - spawn.AbsOrigin.Z, 2)
            );

            if (distance < 200) // CT区域半径200单位
            {
                inCTZone = true;
                break;
            }
        }

        // 如果足球进入CT区域且之前未进入过，给T发AK
        if (inCTZone && !_hasEnteredCTZone)
        {
            _hasEnteredCTZone = true;
            GiveAKToTerrorists();

            // 显示提示
            foreach (var player in Utilities.GetPlayers())
            {
                if (player.IsValid)
                {
                    player.PrintToCenter("⚽ 足球进入CT区！T队获得AK！");
                    player.PrintToChat("⚽ 足球进入CT区！所有T获得AK47！");
                }
            }

            Console.WriteLine("[足球模式] 足球进入CT区域，已给所有T发AK47");
        }
    }

    /// <summary>
    /// 给所有T发AK47
    /// </summary>
    private void GiveAKToTerrorists()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.Team != CsTeam.Terrorist || !player.PawnIsAlive)
                continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            // 给予AK47
            player.GiveNamedItem("weapon_ak47");

            Console.WriteLine($"[足球模式] {player.PlayerName} 获得了AK47");
        }
    }
}
