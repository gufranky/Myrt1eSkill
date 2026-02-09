# Myrtle Skill Plugin Documentation

## Introduction

Myrtle Skill Plugin is a Counter-Strike 2 (CS2) entertainment plugin that adds random events and player skill systems to your server, increasing the fun and variety of the game.

## Version Information

- **Plugin Name**: Myrtle Skill Plugin
- **Current Version**: v2.10.0
- **CS2Sharp API Version**: 1.0.362
- **Number of Events**: 48
- **Number of Skills**: 65

## ⚠️ Important Notice

**This plugin is still under active development and may have the following issues:**

- 🔧 Some skills or events may not work properly
- ⚠️ Very low probability of game crashes
- 🐛 There may be undiscovered bugs

**Recommendations:**
- 📋 Test on a test server first
- 💾 Save game progress regularly
- 🐛 If you find any issues, please submit an Issue

---

## Prerequisites

### Required Dependencies

- **.NET 8.0 Runtime**
- **CounterStrikeSharp.API v1.0.362**

### Optional Dependencies

The following dependencies are not required, but certain skills need them to function properly:

#### CS2TraceRay

- **Purpose**: Provides ray casting capabilities
- **Required Skills**:
  - Check Scan (FreeCamera) - For vision detection
- **Download**: https://github.com/Source2ZE/CS2TraceRay

#### MenuManagerCS2

- **Purpose**: Provides WASD menu system
- **Required Skills**:
  - Choose One of Three (ChooseOneOfThree)
  - Death Note (DeathNote)
  - Duplicator (Duplicator)
  - Glitch (Glitch)
- **Download**: https://github.com/nf-rol/MenuManagerCS2

> 💡 **Note**: If these dependencies are not installed, related skills will not work properly, but other features will remain unaffected.

## Feature Overview

### 1. Entertainment Event System

Each round, one global event is randomly selected that affects all players.

**Current Number of Events**: 48 (including NoEvent and NoSkill)

### 2. Player Skill System

Each player randomly receives a personal skill at the start of a round, which can be either active or passive.

**Current Number of Skills**: 65

### 3. Starting Welfare System

Randomly distributes initial benefits (weapons, equipment, etc.) to players each round.

### 4. Bot Control System

Players can control bots after dying to continue fighting.

### 5. Execution Order

The system initializes in the following order:

1. **Starting Welfare** - Distributes initial benefits to players
2. **Entertainment Event** - Randomly selects a global event
3. **Player Skills** - Assigns personal skills to each player (1 second delay to ensure events are fully applied)

---

## Entertainment Event List

### Event Quick Reference Table

| Internal Name        | Display Name      | Description                                                         |
| -------------------- | ----------------- | ------------------------------------------------------------------- |
| AnywhereBombPlant    | Anywhere Plant    | Plant the bomb anywhere on the map!                                 |
| AutoBhop             | 🐰 Auto Bhop       | Auto bunnyhop enabled! Movement speed increased! Smoother jumping!   |
| Blitzkrieg           | ⚡ Blitzkrieg      | Game speed increased to 2x, everything is accelerating!              |
| ChickenMode          | 🐔 Chicken Mode    | All players become chickens! 1.1x speed, 50% health!                 |
| DeadlyGrenades       | 💣 Deadly Nades    | Unlimited HE grenades! Primary/secondary removed! Store disabled!    |
| DecoyTeleport        | 🎯 TP Decoy        | Teleport to decoy impact location! Auto receive decoys each round!   |
| HeadshotOnly         | 🎯 Headshots Only  | Only headshots deal damage!                                         |
| HighSpeed            | High Speed         | All players' movement speed doubled!                                |
| InfiniteAmmo         | 🔥 Infinite Ammo   | Infinite reserve ammo! Auto reload! No reload needed! Full firepower! |
| Juggernaut           | 🛡️ Juggernaut     | All players get 500 HP, 200 armor, but -30% movement speed!          |
| JumpOnShoot          | Jump on Shoot     | Automatically jump when shooting! Only triggers when on ground!      |
| JumpPlusPlus         | 🦘 Super Jump      | Auto jump on shot + no spread! Immune to fall damage!               |
| KeepMoving           | 🏃 Keep Moving     | Must hold W key constantly! Lose 10 HP every 0.75s if not moving!    |
| LowGravity           | 🌑 Low Gravity     | Players can jump higher!                                            |
| LowGravityPlusPlus   | 🌑 Ultra Low Grav  | Gravity greatly reduced, no bullet spread in mid-air!               |
| MiniSize             | Mini Size          | All players become mini-sized!                                      |
| NoEvent              | Normal Round       | No special effects this round, normal gameplay                      |
| NoSkill              | 😌 No Skills       | A more peaceful day, no skills for anyone!                          |
| OneShot              | 💥 One Shot AK     | All guns have only 1 bullet in the magazine! Reserve ammo retained! |
| RainyDay             | 🌧️ Rainy Day      | All players invisible! Randomly visible for 2s every 3-10s!         |
| ScreamingRabbit      | 🐰 Screaming Rabbit| All players emit positioning sound every 15s! Position exposed!      |
| SlowMotion           | 🎬 Slow Motion     | Game speed reduced to 0.5x! Everything slows down!                   |
| SmallAndDeadly       | Small & Deadly     | Players shrunk to 0.4x size, 1 HP! One hit kill!                      |
| StayQuiet            | 🤫 Stay Quiet      | Invisible when quiet! Reveal when making noise!                      |
| Strangers            | 👥 Strangers       | All models identical! FF enabled! No minimap! Random spawn points!   |
| SuperpowerXray       | 🦸 Superpower Xray | One player per team gets wallhack! Only superpower can see enemies! |
| SwapOnHit            | Hit Swap           | Swap positions with enemies when hitting them!                      |
| SoccerMode           | ⚽ Soccer Mode     | All items removed! Store disabled! T gets soccer ball! Goal = AK!   |
| SuperKnockback       | 💪 Super Knockback | Strongly knock back enemies when dealing damage! Send them flying!   |
| SuperRecoil          | 💥 Super Recoil    | Super strong recoil when shooting! Launch yourself!                  |
| Bankruptcy           | 💸 Bankruptcy      | Everyone bankrupt! Starting cash only 800!                           |
| Deaf                 | 🔇 Deaf            | Everyone hears no sounds! Global deafness!                           |
| KillerSatellite      | 🛰️ Killer Sat     | Everyone gets Killer Flash and Meito! Lethal flash & last stand!    |
| InverseHeadshot      | 🎯 Inverse HS      | Head damage 4x! Body damage 1/4x!                                    |
| TeleportOnDamage     | Hurt TP            | Randomly teleport when taking damage!                                |
| TopTierParty         | 🎊 Top Tier Party  | Ultimate party! Two random events at once! Chaos & fun combined!     |
| TopTierPartyPlusPlus | 🎊🎊 Top Tier++    | Extreme party! Three random events at once! Absolute chaos!          |
| UnluckyCouples       | 💑 Unlucky Couples | Players paired up! Paired players see each other & deal more dmg!   |
| Vampire              | Vampire           | Steal equal health when dealing damage!                              |
| Xray                 | 👁️ Xray           | All players can see each other through walls!                       |
| BankruptcyWeapon     | 💸 Weapon Broke    | All primary/secondary weapons 10x price! Can't afford good guns!     |
| ChooseCarnival       | 🎪 Choice Carnival | Each player chooses 1 of 3 random events!                            |
| FlyUp                | 🚀 Fly Mode        | All players can fly! Press space to ascend!                          |
| Foggy                | 🌫️ Foggy Mode      | Thick fog covers the map! Very low visibility!                       |
| InfiniteBulletMode   | 🔥 Infinite Bullet | Infinite bullets! No reload needed! Continuous fire!                 |
| KillerSatellite      | 🛰️ Killer Sat     | Everyone gets Killer Flash and Meito! Lethal flash & last stand!     |
| MoreSkills           | ⚔️ Double Skills   | Each player gets 2 skills! Double the fun!                           |
| SignalJam            | 📡 Signal Jam      | Radar disabled! No enemies on minimap!                               |
| SkillsPlusPlus       | ⚔️⚔️ Triple Skills | Each player gets 3 skills! Ultimate chaos!                           |
| ThirdPerson          | 👁️ Third Person    | All players forced into third-person view!                           |

### Event Weight Configuration

Default weight distribution (configurable):

- **NoEvent**: 100
- **NoSkill**: 100
- **KeepMoving**: 10
- **SoccerMode**: 10
- **SuperRecoil**: 10
- **TopTierParty**: 5
- **TopTierPartyPlusPlus**: 2
- **Other Events**: 10

**Probability Calculation**:

- Total Weight ≈ 607
- NoEvent Probability ≈ 16.5%
- NoSkill Probability ≈ 16.5%
- KeepMoving Probability ≈ 1.6%
- SoccerMode Probability ≈ 1.6%
- SuperRecoil Probability ≈ 1.6%
- TopTierParty Probability ≈ 0.8%
- TopTierPartyPlusPlus Probability ≈ 0.3%
- Other Events ≈ 1.6% each

---

## Player Skill List

### Skill Quick Reference Table

| Internal Name        | Display Name          | Description                                                | Type | Cooldown  |
| -------------------- | --------------------- | ---------------------------------------------------------- | ---- | --------- |
| AntiFlash            | ✨ Anti Flash          | Immune to flash! Your flash duration +50%! Get 1 flash! Replenish up to 2x! | Passive | None      |
| BotSummon            | 🤖 Bot Summon          | Summon bot to assist! Once per round!                      | Active | 9999s     |
| Darkness             | 🌑 Darkness            | Apply darkness effect to random enemy for 10s!             | Active | 30s       |
| Deaf                 | 🔇 Deaf                | Make random enemy unable to hear any sound for 10s!        | Active | 30s       |
| DecoyXRay            | 💣 Decoy Xray          | Start with 3 decoys! Explosion reveals enemy positions!    | Active | 9999s     |
| Disarm               | ✂️ Disarm              | 40% chance to make enemy drop weapon when shooting!        | Passive | None      |
| DumbBot              | 🤖 Dumb Bot            | Summon 300 HP meat shield! No gun but can tank!            | Active | 9999s     |
| EnemySpin            | 🔄 Enemy Spin          | 40% chance to rotate enemy 180° when shooting!             | Passive | None      |
| ExplosiveShot        | 💥 Explosive Shot      | 20-30% chance to cause explosion at target location!       | Passive | None      |
| FlashJump            | ✈️ Flash Jump          | Your flash makes enemies fly! Longer blind = higher flight! Get 1 flash! Replenish up to 2x! | Passive | None |
| Glaz                 | 🌫 Glaz                | See enemies through smoke! Get 3 smoke grenades!           | Passive | None      |
| HeavyArmor           | 🛡️ Heavy Armor         | Get 200 armor! 60% damage reduction! 80% speed!            | Passive | None      |
| HighJump             | 🦘 High Jump           | Gravity reduced to 70%, jump higher!                       | Passive | None      |
| KillerFlash          | ⚡ Killer Flash        | Your flash becomes lethal! Fully blinded enemies die! Get 1 flash! | Passive | None |
| MasterThief          | 🎭 Master Thief        | Teleport to enemy spawn! Stealthy infiltration!            | Active | 15s       |
| Meito                | ⚔️ Meito               | Fatal damage protection every 10s! Keep 1 HP! Once per round! | Passive | None      |
| QuickShot            | ⚡ Quick Shot          | No recoil! Max fire rate! Instant firing!                  | Passive | None      |
| RadarHack            | 📡 Radar Hack          | See enemies on radar! Know their positions!                | Passive | None      |
| SecondChance         | 🔄 Second Chance       | Respawn with 50 HP after death! Once per round!            | Passive | None      |
| SpeedBoost           | ⚡ Speed Boost         | Movement speed +50%!                                        | Passive | None      |
| Sprint               | 💨 Sprint              | Double jump to sprint! Hold movement keys to sprint!       | Passive | None      |
| TeamWhip             | 💉 Team Whip           | Shoot teammates to convert damage to healing! No FF!        | Passive | None      |
| Teleport             | 🌀 Teleport            | Teleport to your historical positions!                     | Active | 15s       |
| ToxicSmoke           | ☠️ Toxic Smoke         | Start with 1 toxic smoke! Damages enemies! Replenish 1x!    | Passive | None      |
| BigStomach           | 🍖 Big Stomach         | Gain 200-500 random HP on skill! Can exceed max HP!         | Passive | None      |
| Muhammad             | 💀 Muhammad            | You explode on death, killing nearby players!               | Passive | None      |
| Wallhack             | 👁️ Wallhack            | See enemies through walls!                                 | Passive | None      |
| AutoAim              | 🎯 Auto Aim            | Every hit counts as a headshot!                            | Passive | None      |
| Armored              | 🛡️ Armored             | Get random damage reduction (0.55 - 0.8x)!                  | Passive | None      |
| BladeMaster          | ⚔️ Blade Master        | 95% torso/80% leg block with knife! +15% speed!            | Passive | None      |
| BlastOff             | 🚀 Blast Off            | Automatically jump and launch into air when damaged!        | Passive | None      |
| ChooseOneOfThree     | 🎰 Choose One of Three | Draw 3 random skills, choose one! Requires MenuManagerCS2   | Active | 9999s     |
| DeathNote            | 💀 Death Note          | Choose a player, both of you die! Requires MenuManagerCS2   | Active | 9999s     |
| Duplicator           | 📋 Duplicator           | Choose an enemy, copy their skills! Requires MenuManagerCS2 | Active | 9999s     |
| Explorer             | 🔭 Explorer            | Create explorer entity! Enemies hit get blinded for 2.5s!   | Active | 15s       |
| FalconEye            | 🦅 Falcon Eye          | Click to activate aerial camera! View battlefield from above! | Active | 0s        |
| Focus                | 🎯 Focus               | No recoil when shooting!                                   | Passive | None      |
| Fortnite             | 🏗️ Fortnite            | Create destructible barriers!                              | Active | 2s        |
| FrozenDecoy          | ❄️ Frozen Decoy        | Your decoys freeze nearby players! Start with 1 (replenish 1x)! | Passive | None      |
| FreeCamera           | 🔍 Check Scan          | Top-down view, WASD move, scan after 10s for 5s vision! Requires CS2TraceRay | Active | 20s       |
| Ghost                | 👻 Ghost               | Completely invisible! Reveal when dealing/taking damage!    | Passive | None      |
| Glitch               | 📡 Glitch              | Choose an enemy, disable their radar for 15s! Requires MenuManagerCS2 | Active | 30s       |
| HealingSmoke         | 💚 Healing Smoke       | Start with 1 healing smoke! Heals teammates! Replenish 1x!  | Passive | None      |
| HighRiskHighReward   | 🎲 High Risk High Reward | Start with only 20 HP! Increase to 500 HP after killing!    | Passive | None      |
| HolyHandGrenade      | ✝️ Holy Hand Grenade   | Your HE deals 2.5x damage and radius! Start with 1 (replenish 1x)! | Passive | None      |
| Hologram             | 🎭 Hologram            | Create a hologram of yourself! Enemies hit take damage!    | Active | 9999s     |
| InfiniteAmmo         | 🔫 Infinite Ammo        | Infinite reserve ammo! Auto reload! No reload needed!       | Passive | None      |
| Jackal               | 🐺 Jackal               | Show position of enemy who last killed you!                | Active | 60s       |
| KillInvincibility    | ⚔️ Kill Invincibility  | Get 5s invincibility after killing enemies!                 | Passive | None      |
| LastStand            | 💪 Last Stand          | Last surviving player gets 2x damage and speed!            | Passive | None      |
| MasterThief          | 🎭 Master Thief        | Teleport to enemy spawn! Stealthy infiltration!             | Active | 15s       |
| MindHack             | 🧠 Mind Hack            | Invert enemy controls for 10s!                             | Active | 30s       |
| Phoenix              | 🔥 Phoenix              | Respawn with 100 HP after death! Once per round!           | Passive | None      |
| Pilot                | ✈️ Pilot               | Press E to fly! Press space to ascend!                     | Active | 0s        |
| Prosthetic           | 🦿 Prosthetic           | Don't drop weapon when arm hit!                            | Passive | None      |
| Push                 | 💪 Push                 | 25% chance to knock back enemy when shooting!              | Passive | None      |
| RangeFinder          | 📏 Range Finder         | Show distance to nearest enemy!                            | Active | 9999s     |
| Replicator           | 🎭 Replicator           | Create a clone of yourself! Enemies hit take damage!       | Active | 10s       |
| SecondChance         | 🔄 Second Chance       | Respawn with 50 HP after death! Once per round!            | Passive | None      |
| Silent               | 🤫 Silent               | Move and shoot without sound!                              | Passive | None      |
| Silencer             | 🔇 Silencer             | Shooting doesn't show on radar!                            | Passive | None      |
| SuperFlash           | ⚡ Super Flash         | Your flash blind duration doubled! Get 1 flash! Replenish up to 2x! | Passive | None      |
| TeleportAnchor       | ⚓ Teleport Anchor      | Set teleport anchor, teleport back anytime!                 | Active | 5s        |
| ThirdEye             | 👁️ Third Eye           | See enemies behind walls!                                  | Active | 9999s     |
| TimeRecall           | ⏪ Time Recall          | Return to position, view, and HP from 5 seconds ago!       | Active | 15s       |
| WoodMan              | 🪵 Wood Man             | -50% damage taken, but -30% movement speed!                 | Passive | None      |
| ZRY                  | 🔫 ZRY                  | Your bullets penetrate walls!                              | Passive | None      |

### Skill Type Description

- **Passive Skills**: Automatically take effect, no player action required
- **Active Skills**: Require player activation via command or key press, have cooldown time

### Skill Weight Configuration

Default weights (configurable):

- **All Skills**: 10 (unless otherwise specified)

### Event Exclusion System

Certain skills can specify which events they are incompatible with. When specific events are active, incompatible skills won't be assigned.

**Exclusion Example**:

- `HighJump` (Astronaut) is excluded from `LowGravity`, `LowGravityPlusPlus`, `JumpPlusPlus` events
- Reason: These effects all affect jumping/gravity, avoiding effect overlap or conflict

---

## Console Commands

### Entertainment Event System

| Command                              | Description                        |
| ----------------------------------- | ---------------------------------- |
| `css_event_enable`                  | Enable entertainment event system |
| `css_event_disable`                 | Disable entertainment event system |
| `css_event_status`                  | View current event info           |
| `css_event_list`                    | List all available events         |
| `css_event_weight <event> [weight]` | View/set event weight             |
| `css_event_weights`                 | View all event weights            |
| `css_forceevent <event>`            | Force specific event next round (debug) |

### Player Skill System

| Command                              | Description                          |
| ----------------------------------- | ------------------------------------ |
| `css_skill_enable`                  | Enable player skill system           |
| `css_skill_disable`                 | Disable player skill system          |
| `css_skill_status`                  | View skill system status & skills    |
| `css_skill_list`                    | List all available skills            |
| `css_skill_weight <skill> [weight]` | View/set skill weight                |
| `css_skill_weights`                 | View all skill weights               |
| `css_useskill`                      | Use/activate your skill (active)     |
| `css_forceskill <skill> [player]`   | Force specific skill on player (debug) |

### Starting Welfare System

| Command                 | Description                    |
| ----------------------- | ------------------------------ |
| `css_welfare_enable`    | Enable starting welfare system |
| `css_welfare_disable`   | Disable starting welfare system |
| `css_welfare_status`    | View welfare system status    |

### Bot Control System

| Command                  | Description                  |
| ------------------------- | ---------------------------- |
| `css_botcontrol_enable`  | Enable player bot control   |
| `css_botcontrol_disable` | Disable player bot control  |
| `css_botcontrol_status`  | View bot control status     |

### Position Recorder System

| Command                | Description                              |
| ---------------------- | ---------------------------------------- |
| `css_pos_history`      | View your position history (last 10)     |
| `css_pos_clear`        | Clear your position history              |
| `css_pos_stats`        | View position recorder statistics        |
| `css_pos_clear_all`    | Clear all players' position history      |

### Bomb Related Functions

| Command                              | Description                      |
| ----------------------------------- | -------------------------------- |
| `css_allowanywhereplant_enable`    | Enable anywhere bomb plant       |
| `css_allowanywhereplant_disable`   | Disable anywhere bomb plant      |
| `css_allowanywhereplant_status`    | View anywhere plant status       |
| `css_bombtimer_set <seconds>`      | Set bomb explosion time (seconds) |
| `css_bombtimer_status`             | View bomb explosion time         |

---

## Configuration Files

The plugin uses JSON format configuration files to manage weights.

**Configuration File Location**: `/addons/counterstrikesharp/addons/MyrtleSkill/config.json`

### Configuration File Structure

```json
{
  "EventWeights": {
    "NoEvent": 100,
    "NoSkill": 100,
    "LowGravity": 10,
    "LowGravityPlusPlus": 10,
    "HighSpeed": 10,
    "Vampire": 10,
    "TeleportOnDamage": 10,
    "JumpOnShoot": 10,
    "JumpPlusPlus": 10,
    "AnywhereBombPlant": 10,
    "MiniSize": 10,
    "Juggernaut": 10,
    "InfiniteAmmo": 10,
    "SwapOnHit": 10,
    "SmallAndDeadly": 10,
    "Blitzkrieg": 10,
    "DecoyTeleport": 10,
    "Xray": 10,
    "SuperpowerXray": 10,
    "ChickenMode": 10,
    "TopTierParty": 5,
    "TopTierPartyPlusPlus": 2,
    "StayQuiet": 10,
    "RainyDay": 10,
    "ScreamingRabbit": 10,
    "HeadshotOnly": 10,
    "OneShot": 10,
    "DeadlyGrenades": 10,
    "UnluckyCouples": 10,
    "Strangers": 10,
    "AutoBhop": 10,
    "SlowMotion": 10,
    "KeepMoving": 10,
    "SoccerMode": 10,
    "SuperRecoil": 10,
    "Bankruptcy": 10,
    "Deaf": 10,
    "KillerSatellite": 10,
    "InverseHeadshot": 10,
    "TeleportOnDamage": 10,
    "BankruptcyWeapon": 10,
    "ChooseCarnival": 10,
    "FlyUp": 10,
    "Foggy": 10,
    "InfiniteBulletMode": 10,
    "MoreSkills": 10,
    "SignalJam": 10,
    "SkillsPlusPlus": 10,
    "ThirdPerson": 10
  },
  "SkillWeights": {
    "Teleport": 10,
    "SpeedBoost": 10,
    "HighJump": 10,
    "Ninja": 10,
    "BotSummon": 10,
    "DumbBot": 10,
    "DecoyXRay": 10,
    "ToxicSmoke": 10
    // ... more skills
  },
  "Notes": "Higher weight = higher probability. Set to 0 to disable event/skill."
}
```

### Configuration Item Description

#### EventWeights

| Parameter | Type | Description |
|-----------|------|-------------|
| EventWeights | Dictionary&lt;string, int&gt; | Event weight dictionary, key is event internal name, value is weight |
| NoEvent | int | Normal round weight (default 100) |
| NoSkill | int | No skill round weight (default 100) |
| Other Events | int | Weight of each event (default 10 or 5) |

#### SkillWeights

| Parameter | Type | Description |
|-----------|------|-------------|
| SkillWeights | Dictionary&lt;string, int&gt; | Skill weight dictionary, key is skill internal name, value is weight |
| All Skills | int | Weight of each skill (default 10) |

#### Notes

| Parameter | Type | Description |
|-----------|------|-------------|
| Notes | string | Configuration file description text |

### Weight System Principles

1. **Weight Calculation**: System sums up all event weights
2. **Probability Calculation**: Single event probability = event weight / total weight
3. **Weight of 0**: Disables that event/skill from being selected

**Example**:
```
Total Weight = 100 (NoEvent) + 100 (NoSkill) + 10×46 (other events) = 660
NoEvent Probability = 100/660 ≈ 15.15%
KeepMoving Probability = 10/660 ≈ 1.52%
TopTierParty Probability = 5/660 ≈ 0.76%
```

### Modifying Weights

#### Method 1: Direct Edit Configuration File

1. Open `config.json` file
2. Modify corresponding event/skill weight value
3. Save file
4. Restart server or use reload command

#### Method 2: Console Commands

```bash
# View current weights
css_event_weights        # View all event weights
css_skill_weights         # View all skill weights

# Modify weights
css_event_weight LowGravity 20       # Set low gravity event weight to 20
css_skill_weight Teleport 5           # Set teleport skill weight to 5

# Disable events/skills
css_event_weight TopTierParty 0      # Disable top tier party event
css_skill_weight BigStomach 0         # Disable big stomach skill
```

### Common Configuration Scenarios

#### Scenario 1: Increase Peaceful Round Probability

```json
{
  "EventWeights": {
    "NoEvent": 200,
    "NoSkill": 200
  }
}
```

#### Scenario 2: Disable Specific Events

```json
{
  "EventWeights": {
    "TopTierPartyPlusPlus": 0,
    "KeepMoving": 0
  }
}
```

#### Scenario 3: Make Certain Events More Common

```json
{
  "EventWeights": {
    "LowGravity": 30,
    "InfiniteAmmo": 20,
    "HeadshotOnly": 15
  }
}
```

#### Scenario 4: Balance Skill Distribution

```json
{
  "SkillWeights": {
    "Teleport": 5,
    "SpeedBoost": 5,
    "Meito": 3,
    "BigStomach": 5
  }
}
```

### Configuration File Notes

1. **JSON Format**: Ensure configuration file is valid JSON format
2. **Value Range**: Weights recommended to use integers between 0-100
3. **Case Sensitive**: Event/skill names must match code definitions exactly
4. **Reload Timing**:
   - After modifying config file, restart server required
   - Weights modified via console commands take effect next round
5. **Default Values**: If an event/skill weight is not set, code default value will be used

### Verify Configuration

```bash
# Check if configuration is effective
css_event_status       # View current event
css_skill_status       # View current skills
css_event_weights      # Verify weights are correct
```

---

## Usage

### 1. Install Plugin

1. Copy compiled `MyrtleSkill.dll` file to:

   ```
   /addons/counterstrikesharp/addons/MyrtleSkill/
   ```
2. Ensure `config.json` is also in the same directory

### 2. Enable Features

Enter in server console:

**Enable Entertainment Event System**:

```
css_event_enable
```

**Enable Player Skill System**:

```
css_skill_enable
```

**Enable Starting Welfare System**:

```
css_welfare_enable
```

**Enable Bot Control**:

```
css_botcontrol_enable
```

### 3. Player Uses Skills

**Getting Skills**:

- Automatically receive random skill at round start
- System displays skill name and description in chat

**Using Active Skills**:

- Enter in chat: `!useskill` or `css_useskill`
- Wait for cooldown to end
- Skill effect takes effect immediately

**View Status**:

```
css_skill_status
```

---

## Technical Details

### Code Architecture

```
MyrtleSkill/
├── MyrtleSkill.cs              # Main plugin class
├── MyrtleSkill.csproj         # Project configuration file
├── Core/
│   ├── PluginCommands.cs       # All console commands
│   └── PluginConfig.cs        # Configuration file handler
├── Events/
│   ├── EntertainmentEvent.cs    # Event base class
│   ├── EntertainmentEventManager.cs # Event manager
│   └── [48 event files]       # Individual events
├── Skills/
│   ├── PlayerSkill.cs         # Skill base class
│   ├── PlayerSkillManager.cs  # Skill manager
│   └── [65 skill files]       # Individual skills
├── Features/
│   ├── WelfareManager.cs      # Starting welfare manager
│   ├── BotManager.cs          # Bot control manager
│   ├── BombPlantManager.cs    # Bomb feature manager
│   └── PositionRecorder.cs    # Position recorder
└── ThirdParty/
    └── NavMesh.cs             # Navigation mesh system
```

### Namespaces

- **MyrtleSkill** - Main namespace
- **MyrtleSkill.Core** - Core functionality
- **MyrtleSkill.Skills** - Skill system
- **MyrtleSkill.Features** - Feature modules

### Dependencies

- **.NET 8.0**
- **CounterStrikeSharp.API v1.0.362**

### API Version Compatibility

- Supports latest Counter-Strike 2 version
- Uses standard APIs provided by CS2Sharp
- No additional dependencies required

---

## FAQ

### Q: How do I disable a specific event or skill?

A: Set the corresponding event/skill weight to 0 in `config.json`.

### Q: Where are skill cooldowns configured?

A: Currently cooldowns are hardcoded in skill classes (`Cooldown` property). Future versions will support customization in config files.

### Q: Why wasn't my skill assigned?

A: Check the following:

1. Is skill system enabled (`css_skill_enable`)
2. Is skill weight greater than 0
3. Is current active event incompatible with the skill

### Q: Can events and skills run simultaneously?

A: Yes! The system is designed to run independently:
- Events only, no skills
- Skills only, no events
- Events + Skills simultaneously

### Q: How do I view debug information?

A: Check server console output, all important operations are logged.

---

## Changelog

### v2.0.0 (2026-01-30)

#### New Features

- ✅ Added complete player skill system
  - Support for active and passive skills
  - Implemented skill cooldown mechanism
  - Added event exclusion system
  - Added `css_useskill` command
- ✅ Added 32 entertainment events
  - AutoBhop, Blitzkrieg, ChickenMode, DeadlyGrenades
  - DecoyTeleport, HeadshotOnly, OneShot, RainyDay
  - ScreamingRabbit, SlowMotion, StayQuiet
  - Strangers, SuperpowerXray, TopTierParty
  - TopTierPartyPlusPlus, UnluckyCouples, Xray
  - AnywhereBombPlant, InfiniteAmmo, Juggernaut
  - And more...
- ✅ Added 25 player skills
  - Teleport (active - instant teleport)
  - SpeedBoost (passive - speed boost)
  - HighJump (passive, with event exclusion)
  - Glaz, AntiFlash, FlashJump, SecondChance
  - SummonAllies, SlayerLightning, Disarm
  - And more...

#### Core Improvements

- ⚡ Optimized execution order: Welfare → Events → Skills
- 🔧 Implemented event exclusion system to avoid conflicts
- 📊 Refactored damage multiplier calculation, supports multiple modifiers stacking
- ⚙️ Increased "peaceful round" probability (NoEvent/NoSkill weights raised to 100)
- 🎁 Added starting welfare system, random initial equipment distribution
- 🤖 Implemented bot control, players can continue fighting after death
- 📍 Added position recorder, supports teleportation skills

#### Technical Details

- Added `Skills/` directory and complete skill framework
- Implemented `PlayerSkill` and `PlayerSkillManager` classes
- Added skill management commands (enable/disable/status/list/weight)
- Implemented cooldown tracking and UI hint system
- Added position recording and history query functionality

---

## License

This project is open sourced under **GNU General Public License v3.0 (GPLv3)**.

© 2026 MyrtleSkill Plugin Contributors

**License Information**:

- ✅ Allows commercial use
- ✅ Allows modification
- ✅ Allows distribution
- ✅ Allows private use
- ⚠️ **Copyleft Clause**: Derivative works must use the same license
- ⚠️ **Must Disclose Source**: Source code must be provided when distributing
- ⚠️ **Must Include Original License**: GPLv3 license must be included when copying and distributing

**Main Features**:

- This is a strong copyleft license requiring all modifications and derivative works to also use GPLv3
- When distributing in binary form, source code must be provided simultaneously
- Source code can be provided by: distributing with the software, or providing a download link (maintain for at least 3 years)
- All modifications must be clearly marked with modification date

**Disclaimer**:

This plugin is provided "as is" without any express or implied warranty. The authors are not responsible for any claims or damages.

**Usage Notes**:

- This plugin is for entertainment and educational purposes only
- Please test thoroughly on a test server before deploying to production
- Certain features may affect game balance, adjust according to actual situation
- Any modifications and derivative works based on this code must also use GPLv3 license

**Full License Text**:

Please see the `LICENSE` file in the project root directory, or visit:
https://www.gnu.org/licenses/gpl-3.0.txt

---

## Acknowledgments & Sources

### Original Project

This project references and uses code and implementation ideas from the following open source projects:

**[jRandomSkills](https://github.com/Juzlus/jRandomSkills)** by Juzlus

- License: GNU General Public License v3.0
- GitHub: https://github.com/Juzlus/jRandomSkills
- Project Description: CS2 random skill plugin, adding chaos and fun to the game

**Special thanks for reference implementations of**:

- **Explosive Shot Skill** (`ExplosiveShotSkill.cs`)
  - Referenced jRandomSkills explosive bullet implementation
  - Used same HEGrenadeProjectile_CreateFunc signature
  - Adopted special angle identifier and parameter passing mechanism

### Modification Notes

This project has made the following modifications and extensions based on jRandomSkills implementation:

1. **Architecture Refactoring**:
   - Adopted object-oriented event and skill base class design
   - Implemented complete plugin architecture

2. **Feature Expansion**:
   - Added Keep Moving event (KeepMovingEvent)
   - Implemented independent event and skill management system
   - Added multiple original skills and events

3. **Code Optimization**:
   - Improved code organization and maintainability
   - Added detailed Chinese comments and documentation
   - Optimized implementation logic of some features

4. **Localization**:
   - Fully Chinese interface and documentation
   - Optimized for Chinese users

**Modification Date**: February 2026

**Modifier**: MyrtleSkill Plugin Contributors

---

## Support & Contributing

### How to Contribute

Issues and Pull Requests are welcome!

1. Fork this project
2. Create feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Open Pull Request

### Contact

For questions, suggestions, or bug reports, please contact via:

- **Submit Issue**: [GitHub Issues](TBD)
- **Discussions**: [GitHub Discussions](TBD)
- **Project Location**: E:\cs-plugin\HelloWorldPlugin

---

**Have Fun Playing!** 🎮
