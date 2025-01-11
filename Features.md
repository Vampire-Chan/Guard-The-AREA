This project contains three sub-projects:

1. Guard System
2. Vehicle Parking System (Later)
3. More Scenarios System (Later)

## Project 1: Guard System

The Guard System aims to replicate the default game-styled guard system within Grand Theft Auto 5. Guards have a relationship with the player and will react to the player's and NPC's actions. Guards spawn at specific points within an area, patrol the area, and have different types based on the area.

### Areas and Guard Types

- **Michael's House** - Guarded by Gruppe6 Sechs
- **Mission Row Police Station** - Guarded by LSPD
- **Palomino Heights NOOSE Headquarters** - Guarded by LSPD, NOOSE, and Security
- **Franklin's House** - Guarded by Families

Each area has multiple spawn points and different guard types. A central point is mathematically created from all spawn points in an area, which is used for spawn and despawn triggers. Guards spawn when the player is within 100m of the central point and despawn when the player is beyond 150m.

### Guard Details

- Guards are spawned with random models and equipped with a variety of weapons (primary and secondary).
- Guards have health, armor, abilities, and relationships.
- Relationships determine interactions with the player and other NPCs. For example, Gruppe6 Sechs guards are friendly to Michael and will not attack him, while Franklin's family guards are respectful to Franklin but hostile to other gangs. None means no relationship is assigned to the guards, example is Police or LSPD, NOOSE.

### Spawn and Despawn Logic

- Guards are stored in an array and despawned when the player leaves the area.
- Continuous checks ensure that guards are spawned and despawned based on the player's location.
- If a guard is attacked, all guards in the area will respond to the hostility.

### Configuration

The guard system uses an XML configuration file to store data, including area names, guard types, spawn points, headings, weapons, health, armor, abilities, relationships, and guard models. This allows for easy configuration and customization.

### Additional Details

- **Spawn Points**: Each area has multiple spawn points where guards can appear. These points are predefined and used to calculate a central point for spawn/despawn triggers.
- **Central Point Calculation**: A central point is calculated from all spawn points in an area. This point is used to determine when guards should spawn or despawn based on the player's distance.
- **Weapon Variety**: Guards are equipped with random weapons, ensuring variety. Each guard has a primary and secondary weapon.
- **Health and Armor**: Guards have varying levels of health and armor, making some more resilient than others.
- **Abilities**: Guards may have different abilities, enhancing their effectiveness in various situations.
- **Relationships**: Guards have predefined relationships with the player and other NPCs, affecting their behavior and interactions.
- **Continuous Monitoring**: The system continuously monitors the player's location to manage guard spawning and despawning dynamically.
- **Hostility Response**: If a guard is attacked, all guards in the area will respond, ensuring coordinated defense.

This detailed configuration allows for a dynamic and immersive guard system that enhances the gameplay experience.