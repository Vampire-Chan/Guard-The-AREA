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
  - Gang Security (e.g., Families)
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
- `name`: Unique identifier for the area (e.g., "MichealHouse", "NooseHeadQ")
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

Defines guard types and their equipment:

```xml
<Guards>
    <Guard name="guard_type">
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
A complete list of available scenarios can be found in the GTA V scenario metadata files. You can use any valid GTA V scenario name in your configuration. The scenarios listed above are just commonly used examples that work well with guard behaviors.

To use additional scenarios:
1. Find the scenario name from GTA V's scenario metadata.
2. Add it to your Area configuration:
```xml
<SpawnPoint type="ped" scenario="SCENARIO_NAME">
    <Position x="0.0" y="0.0" z="0.0" />
    <Heading>0.0</Heading>
</SpawnPoint>
```

> **Note**: Not all GTA V scenarios will work appropriately with guard behaviors. Test scenarios in-game to ensure they provide the desired behavior.

## Examples

### Example 1: Military Base Guard Post
```xml
<Area name="ArmyGate1" model="army1_guard">
    <SpawnPoint type="ped">
        <Position x="-1614.36" y="2806.01" z="17.73" />
        <Heading>293.30</Heading>
    </SpawnPoint>
    <!-- Additional spawn points -->
</Area>
```

### Example 2: Private Security with Vehicles
```xml
<Area name="SecurityCheckpoint" model="gruppe6_guard">
    <SpawnPoint type="ped" scenario="WORLD_HUMAN_GUARD_STAND">
        <Position x="0.0" y="0.0" z="0.0" />
        <Heading>90.0</Heading>
    </SpawnPoint>
    <SpawnPoint type="vehicle">
        <Position x="5.0" y="0.0" z="0.0" />
        <Heading>90.0</Heading>
    </SpawnPoint>
</Area>
```

### Example 3: Interior Guard Configuration
```xml
<Area name="BuildingSecurity" model="security_guard">
    <SpawnPoint type="ped" scenario="WORLD_HUMAN_GUARD_STAND" interior="true">
        <Position x="100.0" y="100.0" z="30.0" />
        <Heading>180.0</Heading>
    </SpawnPoint>
</Area>
```

## Custom Usage

This mod can be used for a variety of custom setups beyond standard guard patrols, including:
- **Roadblocks**: Set up custom roadblocks with guards and vehicles.
- **Sniper Spots**: Position guards at strategic sniper spots.
- **Checkpoints**: Create security checkpoints with guards and vehicles.
- **Interior Security**: Secure inside buildings with interior spawn points.

> **Important**: Avoid using guards and law enforcement types in the same location as they have conflicting relationships. Modify `relationships.dat` if necessary to adjust behaviors before setting up mixed security types.

## Important Notes

1. **Coordinate System**
   - Use in-game position logging (Press 'L' key) to get accurate coordinates.
   - Heading: 0° = North, 90° = East, 180° = South, 270° = West.

2. **Performance Considerations**
   - Limit spawn points in high-traffic areas.
   - Consider using `interior="true"` for proper indoor spawning.
   - Use scenarios appropriately for more realistic behaviors.

3. **Position Logging**
   - Enable position logging in `Guarding.ini`.
   - Use the 'L' key (configurable) to log coordinates.
   - Logged positions are saved in XML format for easy copying.

4. **Scenario Usage**
   - When no scenario is specified, guards will use default patrol behavior.
   - Some scenarios work better in specific locations (e.g., WORLD_HUMAN_GUARD_STAND for checkpoints).
   - Not all scenarios work with all guard types.

5. **Vehicle Spawns**
   - Ensure adequate space for vehicle spawns.
   - Vehicle types must match those defined in `Guards.xml`.
   - Consider terrain and road access for vehicle spawn points.

## Installation

1. Ensure you have the latest version of GTA V.
2. Install Script Hook V.
3. Copy mod files to the game directory.
4. Configure guard locations in `Areas.xml`.
5. Adjust guard settings in `Guards.xml`.

## Contributing

Contributions are welcome! Please feel free to submit pull requests or create issues for bugs and feature requests.

## Credits

Created by Vampire-Chan.

## License

This project is licensed under standard open-source terms. See LICENSE file for details.

This README.md file provides an in-depth guide on configuring and using the Guard The AREA mod, ensuring users can fully utilize the mod's features and customize their game experience effectively.
