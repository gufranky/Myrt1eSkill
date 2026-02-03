# MyrtleSkill Plugin - GPL v3 Compliance

## License
This project is licensed under GNU General Public License v3.0.
See LICENSE file for the full text.

## Original Source
This project includes code and design concepts from:
- **jRandomSkills** by Juzlus (https://github.com/Juzlus/jRandomSkills)
- **dRandomSkills** by Jakub Bartosik (D3X) (https://github.com/jakubbartosik/dRandomSkills)

Both projects are licensed under GNU General Public License v3.0.

## Code Attribution

### Skills (derived from jRandomSkills)
| File | Original Component | Notes |
|------|-------------------|-------|
| `ExplosiveShotSkill.cs` | ExplosiveShot | Explosion mechanics, HEGrenadeProjectile_CreateFunc signature |
| `WallhackSkill.cs` | Wallhack/Xray | Glow effect implementation, CheckTransmit mechanism |
| `DarknessSkill.cs` | Darkness | Brightness parameter (0.01) |
| `DisarmSkill.cs` | Disarmament | Weapon drop mechanics |
| `HighJumpSkill.cs` | Astronaut | ActualGravityScale usage |
| `ToxicSmokeSkill.cs` | ToxicSmoke | Smoke grenade handling and color modification |
| `DecoyXRaySkill.cs` | DecoyXray | Decoy-based x-ray vision |
| `RadarHackSkill.cs` | RadarHack | Enemy radar visibility |

### Events (derived from jRandomSkills)
| File | Original Component | Notes |
|------|-------------------|-------|
| `StayQuietEvent.cs` | Ghost | Sound event hash lists, CheckTransmit visibility |
| `RainyDayEvent.cs` | Ghost | CheckTransmit visibility, weapon visibility handling |
| `ChickenModeEvent.cs` | ChickenModel | Model transformation, visibility handling |
| `XrayEvent.cs` | Xray | Full team x-ray vision |
| `SuperpowerXrayEvent.cs` | SuperpowerXray | Selective x-ray ability |

### Other References
| Location | Original | Notes |
|----------|----------|-------|
| `MyrtleSkill.cs` (smoke grenade handling) | ToxicSmoke | Using OwnerEntity instead of Thrower |
| Various skills | Multiple jRandomSkills components | Parameter values, implementation patterns |

## Modifications Made
- **Architecture**: Complete refactoring with event and skill base classes
- **New Features**: KeepMovingEvent, custom event/weight system
- **Code Quality**: Improved organization, error handling, Chinese localization
- **Integration**: Unified plugin framework with centralized managers

## Contributors
- **myrt1e** - Project architecture, implementation, localization
- **Juzlus** - Original jRandomSkills implementation (111 skills)
- **Jakub Bartosik (D3X)** - Original dRandomSkills implementation

## License Compliance
This project complies with GPLv3 requirements:
- ✅ Original copyright notices preserved
- ✅ Source code attributions documented
- ✅ Modifications clearly marked
- ✅ Same license (GPLv3) applied to derivative works
- ✅ License text included with distribution

## Last Updated
2026-02-03
