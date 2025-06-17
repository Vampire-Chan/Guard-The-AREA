
# 🚨 DispatchConfig.xml - Customize Law Enforcement Dispatches

This XML file is the brain behind how police, SWAT, army, and other units respond in your custom GTA-style system. It allows you to fully control who gets dispatched, with what vehicles and weapons, based on various conditions.

---

### 🧩 File Structure Overview

`DispatchConfig.xml` has two main sections:

1.  **`<DispatchVehicleInfo>`**: This is where you define different groups of dispatch units (like "Heli", "Car", "Boat"). For each group, you specify the vehicles they use, the types of pilots and soldiers, their weapons, and even vehicle modifications.

2.  **`<WantedLevels>`**: This section links your defined dispatch groups to the game's wanted levels (1 to 5 stars). It decides which units (Air, Ground, or Sea) respond at each star level.

Together, these sections give you the power to **customize who responds, how they behave, and with what gear**, depending on the wanted level and the player's location.

---

## 💡 Advanced Details for `<DispatchVehicleInfo>`

This section dives into the specifics of setting up your dispatch units.

### ✅ Multiple Entries Allowed

You can define **multiple** of the following items within each `<Dispatch>` group to add variety and randomization:

* **`<Vehicle>`**: Add different vehicle models for a single dispatch type.
* **`<Weapon>`**: Include various primary and secondary weapons for soldiers.
* **`<Soldier>`** and **`<Pilot>`**: List multiple ped models to randomize the appearance of your units.
* **`<Mods>`**: Apply multiple modification types to a single vehicle.

---

### 🚙 `<Vehicle>` Tag

The `<Vehicle>` tag defines a specific vehicle model and its behavior.

```xml
<Vehicle model="polmav" task="rappel attack land" region="CITY">
    </Vehicle>
```

* **`model`**: This is the **game's internal name for the vehicle**.
* **`task`**: Specifies how this vehicle will behave. You can list multiple tasks separated by spaces.
    * **`rappel`**: For helicopters that can drop soldiers using ropes.
    * **`attack`**: The vehicle will actively chase and engage targets (e.g., shoot at the player).
    * **`land`**: For helicopters that will land and deploy troops.
    * **`harass`**: For **ground vehicles** to chase the target without constantly attacking.
    * **(No task)**: If no task is specified, it's treated as a passive patrol or response unit (for ground vehicles only).
    * 🛑 *Note: Planes are currently not supported.*
* **`region` (Optional)**: If specified (e.g., `CITY`, `COUNTRY`), the vehicle will only spawn if the player is in that specific region.

---

### 🔧 `<Attributes>` - Vehicle Customization

The `<Attributes>` section allows you to customize the performance and appearance of a vehicle.

```xml
<Attributes>
    <Mods type="MOD_ENGINE" index="2"/>
    <Mods type="MOD_BUMPER_FRONT" index="-1"/>
    <Health engine="1200" body="2000" petrol="800"/>
    <Livery set="Livery" index="1"/>
    <Livery set="Livery2" index="0"/>
</Attributes>
```

#### `<Mods>` Information:

* **`type`**: The specific type of modification (e.g., `MOD_SPOILER`, `MOD_ENGINE`).
* **`index`**: The index of the mod to apply. Use **`-1` for a random mod** of that type.
* ✅ You can apply **multiple different mod types** to a single vehicle.

#### `<Health>` Information:

* **`engine`**: Sets the engine health.
* **`body`**: Sets the body health.
* **`petrol`**: Sets the fuel capacity.

#### `<Livery>` Information:

* **`set`**: Specifies which livery set to use. This can be `"Livery"` or `"Livery2"`, depending on what the vehicle model supports.
* **`index`**: The specific livery number (e.g., `0`, `1`, `2`).
* You can define **two liveries** if the vehicle model supports both `Livery` and `Livery2`.

---

### 🎨 Supported Mod Types (Common Examples)

| Type                | Description                 |
| :------------------ | :-------------------------- |
| `MOD_SPOILER`       | Spoilers                    |
| `MOD_BUMPER_FRONT`  | Front bumper                |
| `MOD_BUMPER_REAR`   | Rear bumper                 |
| `MOD_ENGINE`        | Engine upgrade              |
| `MOD_BRAKES`        | Brake upgrade               |
| `MOD_TRANSMISSION`  | Transmission upgrade        |
| `MOD_SUSPENSION`    | Suspension                  |
| `MOD_ARMOR`         | Vehicle armor               |
| `MOD_HORNS`         | Horn sound                  |
| `MOD_WHEELS`        | Wheel styles (visual only)  |
| `MOD_TURBO`         | Turbo boost (performance)   |
| `MOD_INTERIOR1`     | Interior part 1             |
| `MOD_INTERIOR2`     | Interior part 2             |
| `MOD_INTERIOR3`     | Interior part 3             |
| *And many more...* | *(Refer to game files for full list)* |

---

### 👮‍♂️ `<Soldiers>` and `<Pilots>`

These sections define the **ped (character) models** that will be used for soldiers and pilots in a dispatch group.

```xml
<Soldiers>
    <Soldier>s_m_y_marine_01</Soldier>
    <Soldier>s_m_y_blackops_01</Soldier>
</Soldiers>
```

* Use any **valid GTA ped model name**.
* **Browse available ped models here:** [https://docs.rage.mp/files/](https://docs.rage.mp/files/) → **Peds**

---

### 🔫 `<Weapons>` and `<Weapon>` (Soldier Weapons)

This defines the weapons carried by the soldiers. You can have `PrimaryWeapons` and `SecondaryWeapons`.

```xml
<Weapon name="WEAPON_CARBINERIFLE" attachments="COMPONENT_AT_AR_FLSH COMPONENT_AT_SCOPE_MEDIUM" ammo="300" magazine=""/>
```

* **`name`**: The **game's internal weapon name**.
* **`attachments`**: A space-separated list of **component names** (attachments) for the weapon.
* **`ammo`**: The total amount of ammunition the soldier will have for this weapon.
* **`magazine` (Optional)**: The number of magazines. If empty, it's typically derived from `ammo` or set by default.

* **Find weapon names and component names (attachments) here:** [https://docs.rage.mp/files/](https://docs.rage.mp/files/) → **Weapons**

---

### 🚀 `<VehicleWeapons>` (Mounted Vehicle Weapons)

This section is for weapons that are built into the vehicle itself, like on helicopters or armed cars.

```xml
<VehicleWeapons>
    <Weapon name="VEHICLE_WEAPON_PLAYER_BUZZARD" flag="vehicle"/>
</VehicleWeapons>
```

* **`name`**: The **game's internal name for the vehicle-mounted weapon**.
* **`flag`**: Defines how the weapon can be used:
    * **`vehicle`**: The weapon is used while inside the vehicle (e.g., helicopter guns).
    * **`foot`**: The weapon can be used by a character standing on the vehicle (if applicable).
    * **`rappel`**: The weapon can be used while rappelling (if the vehicle and weapon support it).

* You can define **multiple `<Weapon>` tags** within `<VehicleWeapons>`.

---

## 🌟 `<WantedLevels>` Section

This part of the XML links your defined dispatch groups to the player's wanted level.

```xml
<WantedLevel star="One">
    <DispatchType type="Air">
        <DispatchSet>Heli</DispatchSet>
    </DispatchType>
    </WantedLevel>
```

* **`star`**: Specifies the wanted level (e.g., "One", "Two", "Three", "Four", "Five").
* **`DispatchType`**: Defines the category of dispatch (`Air`, `Sea`, or `Ground`).
* **`DispatchSet`**: This must be the **`name`** of a `<Dispatch>` group defined in the `<DispatchVehicleInfo>` section (e.g., "Heli", "Car", "Boat").

This setup means you can decide, for example, that at a "Three" star wanted level, the "Heli" dispatch group handles air responses, "Boat" handles sea responses, and "Car" handles ground responses.

---

## ✅ Quick Reference Summary

| Tag / Attribute       | Purpose                                     | Multiple Allowed? | Random Option? |
| :-------------------- | :------------------------------------------ | :---------------- | :------------- |
| **`<Dispatch>`** | Defines a group of units (Heli, Car, Boat)  | ✅                |                |
| **`<Vehicle>`** | Adds a specific vehicle to a dispatch group | ✅                |                |
| `task` (in Vehicle)   | Vehicle behavior type (attack, rappel, etc.)| ✅                |                |
| `region` (in Vehicle) | Spawns only if player is in that region     |                   |                |
| **`<Mods>`** | Customizes vehicle performance/appearance   | ✅                | `index="-1"`   |
| **`<Livery>`** | Sets vehicle paint jobs                     | ✅ (two sets)     |                |
| **`<Pilot>`** | Defines pilot ped models                    | ✅                |                |
| **`<Soldier>`** | Defines soldier ped models                  | ✅                |                |
| **`<Weapons>`** | Container for soldier weapons               |                   |                |
| **`<Weapon>`** | Defines a soldier's weapon and attachments  | ✅                |                |
| **`<VehicleWeapons>`**| Container for vehicle-mounted weapons       |                   |                |
| **`<Weapon>`** (in VW)| Defines a mounted weapon on a vehicle       | ✅                |                |
| **`<WantedLevel>`** | Links dispatch groups to wanted stars       | ✅                |                |
| **`<DispatchSet>`** | Refers to a `<Dispatch>` name               |                   |                |

---

Feel free to customize this XML to create unique and challenging dispatch scenarios for your game!
