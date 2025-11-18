# GTA XML Configuration

Place these files in your `scripts/GTA/` folder for the mod to load: `Areas.xml`, `Guards.xml`, `ScenarioLists.xml`.

- `Areas.xml` - Defines zones, spawn points, backups, and shift schedules
- `Guards.xml` - Defines guard types, ped models, weapon lists, and vehicle pools
- `ScenarioLists.xml` - Defines scenario names used for guard behavior

Items to keep in mind:
- Use the `name` attribute consistently so `Areas.xml` references the correct guard `model` from `Guards.xml`.
- Ensure all numeric values are valid floats or ints, and positions are valid coordinates in the game.
- Validate XML by using a standard XML editor and make sure each tag is closed correctly.
