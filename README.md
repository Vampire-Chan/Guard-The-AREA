# Guard The AREA

A Grand Theft Auto V mod that adds dynamic guard spawning and security systems to various locations in Los Santos.

## Table of Contents
1. [Description](#description)
2. [Features](#features)
3. [Configuration](#configuration)
   - [Areas.xml Configuration](#areasxml-configuration)
   - [Guards.xml Configuration](#guardsxml-configuration)
   - [Scenarios](#scenarios)
4. [Examples](#examples)
5. [Custom Usage](#custom-usage)
6. [Important Notes](#important-notes)
7. [Installation](#installation)
8. [Contributing](#contributing)
9. [Credits](#credits)
10. [License](#license)

## Description

Guard The AREA is a sophisticated mod that enhances the security and guard presence in GTA V by adding customizable guard patrols, security checkpoints, and dynamic guard behaviors across different locations. The mod focuses on creating more realistic and interactive security systems while respecting the game's lore and character dynamics.

## Features

- **Dynamic Guard System**: Configurable guard spawning based on player proximity.
- **Multiple Guard Types**: Different behaviors and equipment for various guard types:
  - Gruppe6 Security Guards
  - Military Personnel
  - LSPD Officers
  - NOOSE Units
  - Sheriff Deputies
  - Park Rangers
  - Private Security
  - Federal Agents (IAA/FIB)
  - Merryweather Security
  - Gang Security (e.g., Families but only for Franklin House)
- **Area-Specific Security**: Customized security setups for various locations.
- **Guard Behavior & AI**: Intelligent guard positioning, patrols, and dynamic combat responses.
- **Vehicle Integration**: Security vehicles, vehicle patrols, and checkpoints.
- **Customization**: XML-based configuration system for guard loadouts, behaviors, and relationships.
- **Additional Functionalities**: Can be used for custom roadblocks, sniper spots, and more.

## Configuration

### Areas.xml Configuration

Defines guarded locations and spawn points:

```xml
<Areas>
    <Area name="AreaName" model="guard_type">
        <SpawnPoint type="ped|vehicle|helicopter|boat" scenario="scenario_name" interior="true|false">
            <Position x="0.0" y="0.0" z="0.0" />
            <Heading>0.0</Heading>
        </SpawnPoint>
    </Area>
</Areas>
```
#### Area Attributes
- `name`: Unique identifier for the area (e.g., "MichaelHouse", "NooseHQ")
- `model`: Guard type to spawn (must match Guards.xml entries)

#### SpawnPoint Attributes
- `type`: Type of spawn
  - `ped`: Human guard
  - `vehicle`: Ground vehicle
  - `helicopter`: Aircraft
  - `boat`: Water vehicle
- `scenario`: Guard behavior scenario (optional)
- `interior`: Whether spawn is inside a building (true/false)

#### Position Configuration
```xml
<Position x="-817.86" y="175.38" z="72.23" />
<Heading>93.34</Heading>
```
- `x,y,z`: World coordinates
- `Heading`: Direction in degrees (0-360)

### Guards.xml Configuration

Defines guard types and their equipment.
Use COP, ARMY, GUARD, BLACKOPS only; otherwise, leave it empty.

```xml
<Guards>
    <Guard name="guard_type" group="group">
        <PedModel>model_name</PedModel>
        <Weapon>weapon_name</Weapon>
        <VehicleModel>vehicle_name</VehicleModel>
    </Guard>
</Guards>
```

#### Guard Types Available
1. `gruppe6_guard`: Gruppe 6 Security
2. `army1_guard`: Military Personnel
3. `lspd_guard`: LSPD Officers
4. `noose_guard`: NOOSE Units
5. `lssd_guard`: Sheriff Deputies
6. `park_ranger_guard`: Park Rangers
7. `security_guard`: Generic Security
8. `iaa_guard`: IAA Agents
9. `fib_guard`: FIB Agents
10. `mw_guard`: Merryweather Security
11. `families_guard`: Franklin's Families Guards

#### Model Configuration
```xml
<PedModel>s_m_m_security_01</PedModel>
<Weapon>WEAPON_PISTOL</Weapon>
<VehicleModel>police</VehicleModel>
```

### Scenarios

#### Default Available Scenarios
```
WORLD_HUMAN_AA_COFFEE
WORLD_HUMAN_AA_SMOKE
WORLD_HUMAN_BINOCULARS
WORLD_HUMAN_CLIPBOARD
WORLD_HUMAN_COP_IDLES
WORLD_HUMAN_DRINKING
WORLD_HUMAN_GUARD_PATROL
WORLD_HUMAN_GUARD_STAND
WORLD_HUMAN_GUARD_STAND_ARMY
WORLD_HUMAN_SECURITY_SHINE_TORCH
WORLD_HUMAN_SMOKING
WORLD_HUMAN_STAND_MOBILE
```

#### Additional Scenarios
A complete list of available scenarios can be found in the GTA V scenario metadata files. You can use any valid GTA V scenario name in your configuration.

> **Note**: Not all scenarios will work appropriately with guard behaviors. Test scenarios in-game to ensure they provide the desired behavior.

## Examples

### Example 1: Military Base Guard Post
```xml
<Area name="ArmyGate1" model="army1_guard">
    <SpawnPoint type="ped">
        <Position x="-1614.36" y="2806.01" z="17.73" />
        <Heading>293.30</Heading>
    </SpawnPoint>
</Area>
```

### Example 2: Private Security with Vehicles
```xml
<Area name="SecurityCheckpoint" model="gruppe6_guard">
    <SpawnPoint type="ped" scenario="WORLD_HUMAN_GUARD_STAND">
        <Position x="0.0" y="0.0" z="0.0" />
        <Heading>90.0</Heading>
    </SpawnPoint>
</Area>
```

## Important Notes

1. **Coordinate System**: Use in-game position logging (Press 'L' key) to get accurate coordinates.
2. **Performance Considerations**: Limit spawn points in high-traffic areas.
3. **Scenario Usage**: Some scenarios work better in specific locations (e.g., WORLD_HUMAN_GUARD_STAND for checkpoints).
4. **Vehicle Spawns**: Ensure adequate space for vehicle spawns.

## Installation

1. Ensure you have the latest version of GTA V.
2. Install Script Hook V.
3. Copy mod files to the game directory.
4. Configure guard locations in `Areas.xml`.
5. Adjust guard settings in `Guards.xml`.

## Contributing

Contributions are welcome! Please submit pull requests or create issues for bugs and feature requests.

## Credits

Created by Vampire-Chan.

## License

This project is licensed under standard open-source terms. See LICENSE file for details.

