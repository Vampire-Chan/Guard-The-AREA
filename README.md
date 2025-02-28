
# Guard The AREA

A Grand Theft Auto V mod that adds dynamic guard spawning and security systems to various locations in Los Santos.

## Table of Contents
1. [Description](#description)
2. [Features](#features)
3. [Configuration](#configuration)
    - [Areas.xml Configuration](#areasxml-configuration)
    - [Guards.xml Configuration](#guardsxml-configuration)
    - [Guards.xml Example and Explanation](#guardsxml-example-and-explanation)
    - [Scenarios](#scenarios)
4. [Examples](#examples)
5. [Custom Usage](#custom-usage)
6. [Important Notes](#important-notes)
7. [Installation](#installation)
8. [Contributing](#contributing)
9. [Credits](#credits)
10. [License](#license)
11. [Version History](#version-history)

## Description

Guard The AREA is a mod designed to enhance the security and guard presence in GTA V. It allows users to add customizable guard patrols, security checkpoints, and dynamic guard behaviors to various locations throughout Los Santos. The mod aims to create more realistic and interactive security systems, while remaining consistent with the game's environment.

## Features

- **Dynamic Guard System**: Allows for configurable guard spawning based on player proximity.
- **Multiple Guard Types**: Supports different guard types with varying behaviors and equipment:
    - Gruppe6 Security Guards
    - Military Personnel
    - LSPD Officers
    - NOOSE Units
    - Sheriff Deputies
    - Park Rangers
    - Private Security
    - Federal Agents (IAA/FIB)
    - Merryweather Security
    - Gang Security (Families Guards for Franklin's House only, tho you can add more and make ambient families gaurd as well, and other gangs too.)
- **Area-Specific Security**: Enables customized security configurations for different locations.
- **Guard Behavior & AI**: Implements intelligent guard positioning, patrols, and dynamic combat responses.
- **Vehicle Integration**: Integrates security vehicles, vehicle patrol (the gunner on the machine gun), and checkpoints.
- **Customization**: Utilizes an XML-based configuration system for defining guard loadouts, behaviors, and relationships.
- **Additional Functionalities**: Can be used to create guard points, secure your own house, custom roadblocks, sniper positions, and various vehicle spawn points.

## Configuration

### Areas.xml Configuration

This file defines guarded locations and their associated spawn points.

```xml
<Areas>
    <Area name="AreaName" model="guard_type" respects="player_name|any">
        <SpawnPoint type="ped|vehicle|plane|helicopter|boat|largevehicle|mounted" scenario="scenario_name" interior="true|false">
            <Position x="0.0" y="0.0" z="0.0" />
            <Heading>0.0</Heading>
        </SpawnPoint>
    </Area>
</Areas>
````

#### Area Attributes

  - `name`:  A unique identifier for the area. Example: "MichaelHouse", "NooseHQ".
  - `model`:  Specifies the guard type to be spawned in this area. This must correspond to an entry defined in `Guards.xml`.
  - `respects`: Specifies if to respect the Player - Franklin, Michael or Trevor or all. ANY is for Player of any kind, and their names explicitly respects them only.

#### SpawnPoint Attributes

  - `type`:  Defines the type of entity to spawn at this point.
      - `ped`: Spawns a human guard.
      - `vehicle`: Spawns a standard ground vehicle.
      - `helicopter`: Spawns an aircraft (helicopter).
      - `boat`: Spawns a water vehicle (boat).
      - `largevehicle`: Spawns a large ground vehicle, such as a Stockade.
      - `mounted`: Spawns a mounted vehicle, typically a vehicle with a mounted weapon like an Insurgent.
      - `plane`: Spawns a plane which needs a big runway space, such as Titan.
  - `scenario`: (Optional) Specifies a scenario for the spawned entity to perform, influencing its behavior. (use with ped type spawn)
  - `interior`: A boolean value (true/false) indicating if the spawn point is located inside a building. (use with ped type spawn)

#### Position Configuration

```xml
<Position x="-817.86" y="175.38" z="72.23" />
<Heading>93.34</Heading>
```

  - `x,y,z`:  World coordinates for the spawn point.
  - `Heading`:  The heading or direction in degrees (0-360) the spawned entity will face.

### Guards.xml Configuration

This file defines the different guard types and their associated equipment, models, and vehicles.  The `group` attribute in the `<Guard>` tag is intended for filtering by COP, ARMY, SECURITY_GUARD, PRIVATE_SECURITY and more. Check pedrelationship.dat or another file which tells information on groups of ped type. (Custom AI Mods like PEV:DTAR has new sets of Relationship group so utilize them as per needed). Use different name if you want those guards to be respectful to you and you only even if they are of same type as others (dont make army, swat and cop pedtype as your own personal guards).

```xml
<Guards>
    <Guard name="guard_type" group="group">
        <PedModel>model_name</PedModel>
        <Weapon>weapon_name</Weapon>
        <VehicleModel>vehicle_name</VehicleModel>
        <LargeVehicleModel>large_name</LargeVehicleModel>
        <HelicopterModel>heli_name</HelicopterModel>
        <BoatModel>boat_name</BoatModel>
        <PlaneModel>plane_name</PlaneModel>
        <MountedVehicleModel>mounted_name</MountedVehicleModel>
    </Guard>
</Guards>
```

#### Guard Types Available

This mod supports the following guard types, each configurable within the `Guards.xml` file:

1.  `gruppe6_guard`: Gruppe 6 Security
2.  `army1_guard`: Military Personnel
3.  `lspd_guard`: LSPD Officers
4.  `noose_guard`: NOOSE Units
5.  `lssd_guard`: Sheriff Deputies
6.  `park_ranger_guard`: Park Rangers
7.  `security_guard`: Generic Security
8.  `iaa_guard`: IAA Agents
9.  `fib_guard`: FIB Agents
10. `mw_guard`: Merryweather Security
11. `families_guard`: Franklin's Families Guards

### Guards.xml Example and Explanation

This section provides an example `Guards.xml` configuration snippet and explains the function of each tag.

```xml
<Guards>
	<Guard name="gruppe6_guard"  >
		<PedModel>s_m_m_armoured_02</PedModel>
		<PedModel>s_m_m_armoured_01</PedModel>
		<Weapon>WEAPON_ASSAULTRIFLE</Weapon>
		<Weapon>WEAPON_ADVANCEDRIFLE</Weapon>
		<Weapon>WEAPON_CARBINERIFLE</Weapon>
		<Weapon>WEAPON_SMG</Weapon>
		<Weapon>WEAPON_MILITARYRIFLE</Weapon>
		<Weapon>WEAPON_BULLPUPRIFLE</Weapon>
		<Weapon>WEAPON_CARBINERIFLE_MK2</Weapon>
		<Weapon>WEAPON_COMBATPISTOL</Weapon>
		<Weapon>WEAPON_HEAVYPISTOL</Weapon>
		<VehicleModel>STOCKADE</VehicleModel>
		<VehicleModel>STOCKADE3</VehicleModel>
		<VehicleModel>SPEEDO</VehicleModel>
		<VehicleModel>police4</VehicleModel>
		<VehicleModel>sheriff</VehicleModel>
		<VehicleModel>policet</VehicleModel>
		<VehicleModel>riot</VehicleModel>
		<HelicopterModel></HelicopterModel>
		<BoatModel></BoatModel>
		<PlaneModel></PlaneModel>
		<LargeVehicleModel></LargeVehicleModel>
		<MountedVehicleModel>insurgent</MountedVehicleModel>
	</Guard>
</Guards>
```

  - **`<Guards>`**:  The root element that encapsulates all guard definitions.
  - **`<Guard name="gruppe6_guard">`**: Defines a specific guard type. The `name` attribute ("gruppe6\_guard" in this example) is used to reference this guard type in `Areas.xml`.
  - **`<PedModel>model_name</PedModel>`**: Specifies pedestrian models for the guard. Multiple `<PedModel>` tags allow for random model selection when spawning.
  - **`<Weapon>weapon_name</Weapon>`**:  Defines weapons assigned to the guard. Multiple `<Weapon>` tags allow for a selection of weapons the guard might use.
  - **`<VehicleModel>vehicle_name</VehicleModel>`**: Defines vehicle models associated with this guard type. These vehicles can be spawned when using the "vehicle" spawn type in `Areas.xml`.
  - **`<HelicopterModel>`, `<BoatModel>`, `<PlaneModel>`, `<LargeVehicleModel>`, `<MountedVehicleModel>`**:  These tags are intended for specifying dedicated models for helicopter, boat, plane, large vehicle, and mounted vehicle spawns, respectively, for this guard type. In this example, they are empty, indicating no specific models are defined for these spawn types for `gruppe6_guard`.
  - It's upto you if you want to keep those tags or not, but any one of those tags are required, if you don't want to use any simply delete the whole Guard tag and use different type from within.
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

A comprehensive list of available scenarios can be found within the GTA V game files (scenario metadata). Any valid GTA V scenario name can be used in the configuration.

> **Note**:  The effectiveness of scenarios can vary with different guard behaviors. In-game testing is recommended to ensure desired outcomes.

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

## Custom Usage

This mod's flexible design allows for various custom applications, such as:

  - **Law Enforcement Enhancements**: Create roadblocks with LSPD or NOOSE units, establish sniper positions with FIB agents, or enhance police presence in specific areas.
  - **Gang Territory**: Define gang territories with custom gang security, like Families guards around Franklin's house.
  - **Private Security Details**: Implement private security details for businesses or player-owned properties using Gruppe 6 or Merryweather guards.
  - **Scenario-Specific Security**: Trigger different security setups based on in-game events or time of day. (This is in WIP for now)

## Important Notes

1.  **Coordinate System**: Use in-game position logging (typically activated by pressing the 'L' key) to obtain accurate world coordinates for spawn points.
2.  **Performance Considerations**: Be mindful of the number of spawn points, especially in densely populated areas, to avoid performance issues.
3.  **Scenario Usage**:  The effectiveness of scenarios may depend on location and guard type. Testing is crucial to achieve the desired behavior. `WORLD_HUMAN_GUARD_STAND` is well-suited for checkpoint scenarios.
4.  **Vehicle Spawns**:  Ensure sufficient space is available for vehicle spawn points to prevent vehicles from spawning in unintended locations or clipping into objects.

## Installation

1.  Ensure Grand Theft Auto V is updated to the latest version.
2.  Install [Script Hook V](https://www.google.com/url?sa=E&source=gmail&q=https://www.dev-c.com/gtav/scripthookv/).
3.  Copy the mod files into the Grand Theft Auto V game directory.
4.  Configure guard locations and properties in `Areas.xml`.
5.  Customize guard types and equipment in `Guards.xml`.

## Contributing

Contributions to this project are welcome. Please submit pull requests for bug fixes or feature additions, or create issues to report bugs and suggest new features.

## Credits

Created by Vampire-Chan.

## License

This project is distributed under standard open-source license terms. See the LICENSE file for complete details.

## Version History

  - **Version 1.0 - "It's Alive\!" (Initial Version)**

    > Initial release incorporating the core features of spawning guards and vehicles in designated areas.

  - **Version 1.1 - "Scenario Tasks"**

    > Introduced scenario tasks to guards, enabling behaviors like guarding, looking around, and using flashlights.

  - **Version 1.2 - "Expanded Spawn Types and Tactical Options"**

    > Expanded functionality to include Law Snipers, Roadblocks, and additional vehicle spawn types (helicopter, boat, mounted, vehicles) to enhance tactical deployment options.

  - **Version 1.3 - "Bug Fixes and Spawn Type Refinement"**

    > Addressed null reference exceptions and refined spawn type usage to ensure proper model assignments for each spawn type, preventing vehicles from spawning in inappropriate locations like helipads or boats on land. Implemented dedicated model lists for each spawn type. New spawn types are: plane, mounted, largevehicle, vehicle, helicopter and boat. these can be used. For ped type it's ped. 

