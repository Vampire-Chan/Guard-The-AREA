# Guard The AREA

An advanced security guard simulation mod for GTA V that brings realistic private security forces into the game. Guards patrol defined areas, respond to threats, work in shifts, and coordinate with vehicles and backup units.

## Table of Contents
- [Overview](#guard-the-area)
- [Quick Start](#quick-start)
- [Documentation](#documentation)
- [XML Configuration Guide](#xml-configuration-guide)
- [Contributing](#contributing)
- [License](#license)

## What You'll Experience

### Dynamic Security Areas
- **Multiple Protected Zones**: Guards can protect mansions, businesses, military bases, or any custom location you define
- **Overlapping Coverage**: Areas can overlap (e.g., main gates + rooftop snipers) with independent guard management
- **Automatic Spawn/Despawn**: Guards spawn when you enter an area and despawn when you leave to optimize performance

### Intelligent Guard Behavior
- **Realistic Patrols**: Guards walk designated routes, stand at posts, or patrol in vehicles
- **Combat Response**: Guards engage threats, take cover, and call for backup when under attack
- **Shift System**: Guards arrive and depart at scheduled times using vehicles
- **Return to Duty**: After dealing with threats, guards return to their assigned posts
- **Death Tracking**: Dead guards won't respawn at their position until you leave and re-enter the area

### Vehicle Integration
- **Ground Vehicles**: Guards arrive in cars/trucks and patrol in mounted vehicles
- **Helicopters**: Aerial support with door gunners for high-security areas
- **Tactical Response**: Vehicles respond to combat situations and provide backup
- **Shift Changes**: Guards use vehicles to arrive at and depart from their shifts

### Automatic Backup System
- **Dynamic Reinforcements**: Guards call for backup when under attack (no player interaction required)
- **Multiple Unit Types**: Ground vehicles, tactical helicopters, and attack helicopters
- **Coordinated Response**: Backup units arrive at the combat location and engage threats
- **Wave-Based Spawning**: Multiple waves of reinforcements based on combat intensity and duration
- **Fee System**: Configurable costs and cooldowns for different backup types

### Relationship System
- **Guard Factions**: Different security companies can be allies or enemies
- **Player Respect**: Guards can be friendly, neutral, or hostile based on configuration
- **Cross-Area Recognition**: Guards from different areas recognize each other based on faction settings
- **Law Enforcement**: Configurable relationships with police and military

## Quick Start

### Prerequisites
1. **Script Hook V** - Download from [ScriptHookV](http://www.dev-c.com/gtav/scripthookv/)
   - Extract `ScriptHookV.dll` and `dinput8.dll` to your GTA V root directory

2. **Script Hook V .NET** - Download from [GitHub](https://github.com/scripthookvdotnet/scripthookvdotnet/releases)
   - Extract `ScriptHookVDotNet.asi`, `ScriptHookVDotNet2.dll`, and `ScriptHookVDotNet3.dll` to your GTA V root directory

### Installing Guard The AREA Mod

1. **Create Scripts Folder**
   - Navigate to your GTA V root directory (e.g., `C:\Program Files\Rockstar Games\Grand Theft Auto V\`)
   - Create a folder named `scripts` if it doesn't exist

2. **Install the Mod**
   - Copy `gta.dll` to the `scripts` folder
   - The final path should look like: `...\Grand Theft Auto V\scripts\gta.dll`

3. **XML Configuration Files**
   - Create a `GTA` folder inside your `scripts` folder (optional but recommended)
   - Place the following XML files in the `scripts` folder:
     - `Areas.xml` - Defines protected areas and guard spawn points
     - `Guards.xml` - Defines guard models, weapons, and behavior
     - `ScenarioLists.xml` - Defines guard scenarios (patrols, standing posts, etc.)

### Folder Structure
```
Grand Theft Auto V\
├── ScriptHookV.dll
├── dinput8.dll
├── ScriptHookVDotNet.asi
├── ScriptHookVDotNet2.dll
├── ScriptHookVDotNet3.dll
└── scripts\
    ├── gta.dll (this mod)
    ├── GTA\Areas.xml
    ├────── Guards.xml
    └────── ScenarioLists.xml
```

### Verifying Installation

1. Launch GTA V
2. Look for a `GTA` folder in your scripts directory
3. Enter a defined area to see guards spawn

## Detailed Documentation

Full documentation for configuration and code can be found in the `docs` folder and is compatible with MkDocs for building a static website.

Use the following to preview the docs locally:

```powershell
pip install mkdocs mkdocs-material
mkdocs serve
```

Documentation pages included:
- Architecture
- Module Reference
- XML Configuration Reference
- Usage and Deployment

See `docs/` for the full content and the generated site at `site/` after running `mkdocs build`.
## Contributing

Please read `docs/contributing.md` for contribution guidelines and the project standards.

## License

This project currently has no LICENSE file. If you want to set a license, add `LICENSE` to the root of the repository.


## Documentation

The repository includes a full MkDocs site under `docs/` and `mkdocs.yml` to build the website. To preview:

```powershell
pip install mkdocs mkdocs-material
mkdocs serve
```

To deploy automatically to GitHub Pages, the workflow `.github/workflows/deploy-docs.yml` can be used.

## XML Configuration Guide

The mod uses three XML files to configure all aspects of the security system. Below is a detailed guide for editing each file.

### 1. Areas.xml - Defining Protected Zones

This file defines where guards will spawn and patrol.

#### Basic Structure
```xml
<?xml version="1.0" encoding="utf-8"?>
<Areas>
  <Area name="AreaName" model="guard_model" scenario="ScenarioName" 
        shiftEnabled="true" shiftDuration="6-12,12-18,18-2,2-6"
        allowsBackup="true" charges="500"
        respects="MICHAEL, FRANKLIN">
    <BackupFees>
        <Aerial cost="5000" cooldown="30" />
        <Airstrike cost="50000" cooldown="30" />
        <Ground cost="15000" cooldown="30" />
    </BackupFees>
    <SpawnPoint type="ped" scenario="OverrideScenario" interior="true">
      <Position x="-819.19" y="179.33" z="70.22"/>
      <Heading>108.0</Heading>
    </SpawnPoint>
  </Area>
</Areas>
```

#### Area Properties

**Basic Information:**
- `name`: Unique identifier for this area (must be unique across all areas)
- `model`: Guard type from Guards.xml to use for this area
- `scenario`: Default behavior pattern from ScenarioLists.xml
- `respects`: Characters the guards respect (MICHAEL, FRANKLIN, TREVOR), ALL is an option for any player type.

**Shift System Settings:**
- `shiftEnabled`: Set to `true` to enable shift changes
- `shiftDuration`: Schedule for guard shifts in format "start-end,start-end" (24-hour format)
  - Example: "6-12,12-18,18-2,2-6" = 6am-12pm, 12pm-6pm, 6pm-2am, 2am-6am

**Backup System Settings:**
- `allowsBackup`: Allow guards to call for reinforcements (true/false)
- `charges`: Daily maintenance fee deducted from character account
- `BackupFees`: Cost and cooldown settings for different backup types

**Spawn Points:**
```xml
<SpawnPoint type="ped" scenario="OverrideScenario" interior="true">
  <Position x="-819.19" y="179.33" z="70.22"/>
  <Heading>108.0</Heading>
</SpawnPoint>
```

**Spawn Point Types:**
- `ped` - Guard on foot at specified position
- `vehicle` - Stationary vehicle parked
- `mounted` - Stationary vehicle with guard in turret/gunner seat
- `largevehicle` - Large  vehicles (multiple guards)
- `helicopter` - Aerial  with door gunners
- `plane` - Fixed-wing aircraft 
- `boat` - Water-based 

**Spawn Point Properties:**
- `type`: Type of spawn (ped, vehicle, helicopter, etc.)
- `scenario`: Override scenario that takes precedence over area default
- `interior`: Whether this spawn point is inside (affects positioning)

### 2. Guards.xml - Defining Guard Types

This file defines the appearance, weapons, and behavior of different guard types.

#### Basic Structure
```xml
<?xml version="1.0" ?>
<Guards>
  <Guard name="guard_name" group="RELATIONSHIP_GROUP">
    <PedModel>ped_model_hash</PedModel>
    <PedModel>another_ped_model</PedModel>
    <Weapon>WEAPON_HASH</Weapon>
    <VehicleModel>vehicle_model</VehicleModel>
    <HelicopterModel>heli_model</HelicopterModel>
    <BoatModel>boat_model</BoatModel>
    <PlaneModel>plane_model</PlaneModel>
    <LargeVehicleModel>large_vehicle_model</LargeVehicleModel>
    <MountedVehicleModel>vehicle_model</MountedVehicleModel>
  </Guard>
</Guards>
```

#### Guard Properties

**Basic Information:**
- `name`: Unique identifier used in Areas.xml
- `group`: Relationship group name used for guard interactions

**Ped Models:**
- Multiple `<PedModel>` entries - guards will randomly select from these
- Use GTA V ped model hashes (e.g., `s_m_m_security_01`, `s_m_y_swat_01`)

**Weapons:**
- Multiple `<Weapon>` entries - guards will randomly select one
- Available weapons: `WEAPON_PISTOL`, `WEAPON_COMBATPISTOL`, `WEAPON_ASSAULTRIFLE`, `WEAPON_CARBINERIFLE`, `WEAPON_SMG`, `WEAPON_PUMPSHOTGUN`, `WEAPON_SNIPERRIFLE`, and so on etc.

**Vehicle Models by Type:**
- `<VehicleModel>` - Standard patrol vehicles
- `<HelicopterModel>` - Aerial patrol vehicles 
- `<BoatModel>` - Water-based patrol vehicles
- `<PlaneModel>` - Air-based patrol vehicles
- `<LargeVehicleModel>` - Large patrol vehicles
- `<MountedVehicleModel>` - Vehicles with mounted weapons (turrets, etc.)

#### Complete Guard Example
```xml
<Guard name="private_security" group="PRIVATE_SECURITY">
  <PedModel>s_m_m_security_01</PedModel>
  <PedModel>s_m_m_armoured_01</PedModel>
  <PedModel>s_m_y_swat_01</PedModel>
  <Weapon>WEAPON_CARBINERIFLE</Weapon>
  <Weapon>WEAPON_ASSAULTRIFLE</Weapon>
  <Weapon>WEAPON_PISTOL</Weapon>
  <VehicleModel>police4</VehicleModel>
  <VehicleModel>riot</VehicleModel>
  <VehicleModel>stockade</VehicleModel>
  <HelicopterModel>MAVERICK</HelicopterModel>
  <MountedVehicleModel>insurgent</MountedVehicleModel>
</Guard>
```

Note: If your repository contains a malformed or concatenated `Guards.xml` (multiple XML headers or repeated `<Guards>` sections), use the canonical file `GTA/Guards.cleaned.xml` as the source of truth and run the `tools\apply-canonical-guards.ps1` script to re-write `GTA/Guards.xml` automatically.

#### Common Guard Configuration Types

**Standard Security:**
```xml
<Guard name="standard_guard" group="SECURITY_GUARD">
  <PedModel>s_m_m_security_01</PedModel>
  <Weapon>WEAPON_PISTOL</Weapon>
  <VehicleModel>police</VehicleModel>
</Guard>
```

**Armed Security:**
```xml
<Guard name="armed_guard" group="ARMED_SECURITY">
  <PedModel>s_m_m_armoured_01</PedModel>
  <PedModel>s_m_y_swat_01</PedModel>
  <Weapon>WEAPON_CARBINERIFLE</Weapon>
  <Weapon>WEAPON_PUMPSHOTGUN</Weapon>
  <VehicleModel>riot</VehicleModel>
  <VehicleModel>insurgent</VehicleModel>
</Guard>
```

**Military/Police:**
```xml
<Guard name="military_guard" group="ARMY">
  <PedModel>s_m_y_marine_01</PedModel>
  <PedModel>s_m_y_marine_02</PedModel>
  <Weapon>WEAPON_CARBINERIFLE</Weapon>
  <Weapon>WEAPON_ADVANCEDRIFLE</Weapon>
  <VehicleModel>CRUSADER</VehicleModel>
  <VehicleModel>BARRACKS</VehicleModel>
</Guard>
```

### 3. ScenarioLists.xml - Defining Guard Behaviors

This file defines what guards do when on duty (patrol routes, standing positions, etc.).

#### Basic Structure
```xml
<?xml version="1.0" ?>
<Scenarios>
  <Scenario name="ScenarioName">
    <Name>WORLD_HUMAN_GUARD_STAND</Name>
    <Name>WORLD_HUMAN_SECURITY_SHINE_TORCH</Name>
    <Name>WORLD_HUMAN_CLIPBOARD</Name>
  </Scenario>
</Scenarios>
```

#### Available Scenario Types

**Standing Guard:**
- `WORLD_HUMAN_GUARD_STAND` - Guard stands at attention at assigned position
- `WORLD_HUMAN_GUARD_STAND_FACILITY` - Guard stands at security desk
- `WORLD_HUMAN_SECURITY_SHINE_TORCH` - Guard shines flashlight around

**Patrol Behaviors:**
- `WORLD_HUMAN_GUARD_PATROL` - Guard patrols in small area around position
- `WORLD_HUMAN_COP_IDLES` - Guard stands idle in cop-like manner

**Ambient Activities:**
- `WORLD_HUMAN_SMOKING` - Guard takes smoking break
- `WORLD_HUMAN_STAND_MOBILE` - Guard talks on phone
- `WORLD_HUMAN_HANG_OUT_STREET` - Guard loiters casually
- `WORLD_HUMAN_CLIPBOARD` - Guard looks at clipboard

**Observation Behaviors:**
- `WORLD_HUMAN_BINOCULARS` - Guard uses binoculars to watch area
- `WORLD_HUMAN_STAND_IMPATIENT` - Guard waits impatiently

**Social Behaviors:**
- `WORLD_HUMAN_PARTYING` - Guard relaxes/socializes
- `WORLD_HUMAN_DRINKING` - Guard drinks beverage
- `WORLD_HUMAN_TOURIST_MOBILE` - Guard looks at phone/takes photos

> There are more but its upto you if you can add them or no.
#### Complete Scenario Examples

**Simple Gate Guard:**
```xml
<Scenario name="Guard">
  <Name>WORLD_HUMAN_GUARD_STAND</Name>
  <Name>WORLD_HUMAN_GUARD_STAND_FACILITY</Name>
  <Name>WORLD_HUMAN_SECURITY_SHINE_TORCH</Name>
</Scenario>
```

**Perimeter Patrol:**
```xml
<Scenario name="Patrol">
  <Name>WORLD_HUMAN_GUARD_PATROL</Name>
  <Name>WORLD_HUMAN_COP_IDLES</Name>
  <Name>WORLD_HUMAN_BINOCULARS</Name>
</Scenario>
```

**Ambient Security:**
```xml
<Scenario name="Ambient">
  <Name>WORLD_HUMAN_SMOKING</Name>
  <Name>WORLD_HUMAN_STAND_MOBILE</Name>
  <Name>WORLD_HUMAN_HANG_OUT_STREET</Name>
</Scenario>
```

## Advanced Features

### Shift System Configuration
The shift system allows guards to arrive and depart at scheduled times using vehicles:

- Guards arrive in vehicles 10-15 minutes before their shift starts
- Departure happens 15 minutes before the next shift
- Vehicles can be different from patrol vehicles
- Shifts can overlap for smooth transitions

### Backup System Configuration
The automatic backup system responds to combat situations:

- When guards enter combat, they automatically call for backup
- Backup types depend on what's configured in the guard's vehicle lists
- Costs and cooldowns are configurable per area
- Wave-based spawning with increasing intensity during extended combat

### Relationship Groups
Different guard types can be configured to work together or against each other:
- `PRIVATE_SECURITY` - Private security guards
- `ARMY` - Military personnel
- `COP` - Police officers
- `MERRYWEATHER` - Military contractors
- Custom groups can be created for faction-specific interactions

## Tips and Best Practices

### Performance Optimization
- **Limit active areas**: Don't create too many overlapping areas in one location
- **Guard count**: 10-20 guards per area is reasonable; 50+ may impact performance (if chaos happens, backup may arrive but that can crash game, due to game's internal pools limits)

### Realistic Guard Placement
- **Gates and entrances**: Place standing guards with `WORLD_HUMAN_GUARD_STAND`
- **Rooftops**: Use snipers with `WORLD_HUMAN_BINOCULARS` or standing positions
- **Patrol routes**: Keep routes logical (along fences, around buildings)
- **Vehicle positions**: Place vehicles near roads or parking areas

### Combat Tuning
- **Backup units**: Configure appropriate vehicle types for the security level needed
- **Relationship groups**: Set up proper cooperation between different guard types

### Shift System
- **Realistic timing**: 6-12, 12-18, 18-2, 2-6 = 6-hour shifts covering 24 hours
- **Vehicles**: Ensure enough vehicles for guards to arrive/depart

### Backup System
- **Cost Management**: Set appropriate fees to make backup usage strategic
- **Cooldowns**: Prevent spam usage of expensive backup types
- **Vehicle Types**: Configure appropriate vehicle lists for desired backup types

### Finding Coordinates
1. Use a trainer mod to teleport or display coordinates (or simply press L where you stand, or in vehicle)
2. Stand where you want a guard/spawn point (or press L there to log down the coordinates in the PlyPos.log)
3. Note X, Y, Z, and current heading (or directly copy paste from PlyPos.log to XML)
4. Add to XML with appropriate properties

## Troubleshooting

### Common Installation Issues
- **File Not Found Errors**: Ensure all XML files are in the scripts folder
- **Script Hook V Errors**: Verify ScriptHookV.dll and dinput8.dll are in the GTA V root
- **.NET Framework Missing**: Ensure ScriptHookVDotNet is properly installed
- **Mod Not Loading**: Check that gta.dll is in the scripts folder

### Guards Not Spawning
- Check XML syntax (proper opening/closing tags)
- Verify `Areas.xml` is in scripts/GTA folder
- Ensure ped models exist in GTA V
- Check logs for errors (Logging.log in scripts/GTA folder)
- Verify that the area coordinates are accessible in-game
- Make sure `shiftEnabled` areas have valid shift times

### Guards Attacking Player
- Check that `respects` attribute includes the current character
- Ensure proper relationship group setup in Guards.xml
- Check that vehicle spawns are correctly configured
- Verify that the guard's relationship settings allow respect for the player
- Some guards may attack if the player has a wanted level

### Shift Changes Not Working
- Verify `shiftEnabled="true"` in Areas.xml
- Ensure shiftDuration format is correct ("6-12,12-18,18-2,2-6")
- Check that vehicles exist for guard transport (in Guards.xml)
- Verify that the game time is progressing (shifts depend on in-game time)
- Make sure the area has vehicle spawn points for shift transport

### Backup System Not Working
- Ensure `allowsBackup="true"` in Areas.xml
- Verify guard configuration has appropriate vehicle models in Guards.xml
- Check that backup fees are properly configured
- Guards must be in combat for backup to trigger automatically
- Verify that the guard's vehicle lists have valid vehicle models
- Check that the area has active guards that can call for backup

### Performance Issues
- Reduce number of guards per area (aim for 10-20 per area)
- Decrease number of active areas
- Limit complex vehicle configurations (helicopters, planes cost more performance)
- Use fewer spawn points per area
- Close to the minimum number of areas that provide the experience you want

### Advanced Troubleshooting
- **Log Files**: Check `scripts\GTA\Logging.log` for detailed error messages
- **Test in Safe Area**: Start by testing in an open area like the desert to avoid conflicts
- **Backup Logs**: Check `scripts\GTA\PlyPos.log` for player position information
- **XML Validation**: Use an XML validator to check for syntax errors
- **Coordinate Validation**: Use debug tools to verify area coordinates are valid

### Known Limitations
- The mod relies on ScriptHookV and ScriptHookVDotNet, so ensure they're up to date
- Very large areas with many spawn points may impact performance
- Complex vehicle configurations (especially helicopters) require more system resources
- Some vehicle models may not work properly for certain spawn types
- In Story Mode, performance may be affected by other scripts

## Mod Customization Examples (imaginary values, use correct in game)

### Military Base Setup
```xml
<Area name="MilitaryBase" model="army_guard" scenario="Patrol" 
      shiftEnabled="true" shiftDuration="6-18"
      allowsBackup="true" charges="1000"
      respects="MICHAEL,FRANKLIN">
  <BackupFees>
      <Aerial cost="8000" cooldown="45" />
      <Airstrike cost="80000" cooldown="60" />
      <Ground cost="25000" cooldown="45" />
  </BackupFees>
  <SpawnPoint type="ped">
    <Position x="2400.0" y="3300.0" z="50.0" />
    <Heading>180.0</Heading>
  </SpawnPoint>
  <SpawnPoint type="mounted">
    <Position x="2410.0" y="3300.0" z="50.0" />
    <Heading>180.0</Heading>
  </SpawnPoint>
  <SpawnPoint type="vehicle">
    <Position x="2405.0" y="3290.0" z="50.0" />
    <Heading>0.0</Heading>
  </SpawnPoint>
</Area>
```

### Luxury Mansion Security
```xml
<Area name="LuxuryMansion" model="private_security" scenario="Guard"
      shiftEnabled="true" shiftDuration="6-14,14-22,22-6"
      allowsBackup="true" charges="3000"
      respects="MICHAEL,FRANKLIN,TREVOR">
  <BackupFees>
      <Aerial cost="3000" cooldown="20" />
      <Airstrike cost="30000" cooldown="30" />
      <Ground cost="10000" cooldown="20" />
  </BackupFees>
  <SpawnPoint type="ped">
    <Position x="-810.0" y="177.0" z="72.5" />
    <Heading>90.0</Heading>
  </SpawnPoint>
  <SpawnPoint type="ped">
    <Position x="-820.0" y="180.0" z="72.5" />
    <Heading>0.0</Heading>
  </SpawnPoint>
</Area>
```

## Credits

**Developed by:** Vampire-Chan
**ScriptHookV:** Alexander Blade
**ScriptHookVDotNet:** crosire & kagin & contributors
**GTA V:** Rockstar Games

## License

This mod is free to use and modify for personal use. Do not redistribute without permission.

---

**Enjoy your enhanced security experience in Los Santos!**