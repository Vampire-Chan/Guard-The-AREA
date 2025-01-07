using GTA.Native;
using GTA.UI;
using GTA;
using LemonUI.Menus;
using LemonUI;
using System.Collections.Generic;
using System;
using GTA.Math;

public class GunfightMenu : Script
{
    private ObjectPool menuPool;
    private NativeMenu mainMenu;
    private List<Ped> enemies = new List<Ped>();
    private List<Ped> guards = new List<Ped>();
    private List<Prop> spawnedProps = new List<Prop>();
    private List<Vehicle> spawnedVehicles = new List<Vehicle>();
    private NativeMenu modelSelectionMenu;
    //gangs
    private List<PedHash> enemyModels = new List<PedHash> { PedHash.Lost01GFY, PedHash.Lost03GMY, PedHash.Lost02GMY, PedHash.Lost01GMY, PedHash.BallaOrig01GMY, PedHash.BallaSout01GMY, PedHash.Korean01GMY, PedHash.MexGoon01GMY, PedHash.MexGoon02GMY, PedHash.SalvaGoon02GMY, PedHash.PoloGoon01GMY, PedHash.Ballas01GFY, PedHash.Families01GFY, PedHash.Vagos01GFY, PedHash.Ballas01GFY, PedHash.Families01GFY, PedHash.Vagos01GFY, PedHash.Ballas01GFY, PedHash.Families01GFY, PedHash.Vagos01GFY };
    //police
    private List<PedHash> guardModels = new List<PedHash> { PedHash.Cop01SFY, PedHash.Cop01SMM, PedHash.Cop01SMM, PedHash.Snowcop01SMM, PedHash.Hwaycop01SMY, PedHash.Swat01SMY, PedHash.FibOffice02SMM, PedHash.FibSec01SMM, PedHash.FibOffice01SMM, PedHash.CiaSec01SMM, PedHash.Ranger01SFY, PedHash.Ranger01SMY, PedHash.Prisguard01SMM, PedHash.Sheriff01SFY, PedHash.Sheriff01SMY, PedHash.Marine01SMY, PedHash.Marine03SMY, PedHash.Blackops03SMY, PedHash.Blackops02SMY, PedHash.Blackops01SMY, PedHash.ChemSec01SMM };

    public GunfightMenu()
    {
        MainScript._guardSpawner = new GuardSpawner("./scripts/GuardsPosition.xml", "./scripts/Guards.xml"); // Initialize guard spawner

        menuPool = new ObjectPool();

        // Initialize custom relationship groups
        InitializeGroups();

        // Main Menu
        mainMenu = new NativeMenu("Cinematic Gunfight", "Options", "Control enemies, guards, and actions.");
        menuPool.Add(mainMenu);

        // Model Selection Menu
        modelSelectionMenu = new NativeMenu("Model Selection", "Choose Models", "Select models for enemies and guards.");
        menuPool.Add(modelSelectionMenu);

        // Main Menu options
        var enemyOption = new NativeItem("Spawn Enemy", "Spawn a ped as an enemy.");
        var guardOption = new NativeItem("Spawn Guard", "Spawn a ped as a guard.");
        var startFightOption = new NativeItem("Start Fight", "Make the enemies and guards fight each other.");
        var backupOption = new NativeItem("Call Backup", "Spawn guards in vehicles who will drive to the scene and fight.");
        var modelSelectionOption = new NativeItem("Choose Models", "Select enemy and guard models.");
        var cleanupOption = new NativeItem("Cleanup", "Clear all spawned entities.");
        var spawnvehicleOption = new NativeItem("Spawn Vehicle", "Spawn a vehicle.");

        mainMenu.Add(enemyOption);
        mainMenu.Add(guardOption);
        mainMenu.Add(startFightOption);
        mainMenu.Add(backupOption);
        mainMenu.Add(modelSelectionOption);
        mainMenu.Add(cleanupOption);
        mainMenu.Add(spawnvehicleOption);

        // Model Selection Menu options
        var enemyModelOption = new NativeItem("Set Enemy Models", "Select the models for enemies.");
        var guardModelOption = new NativeItem("Set Guard Models", "Select the models for guards.");

        modelSelectionMenu.Add(enemyModelOption);
        modelSelectionMenu.Add(guardModelOption);

        enemyOption.Activated += (sender, args) => SpawnPed(true);
        guardOption.Activated += (sender, args) => SpawnPed(false);
        startFightOption.Activated += (sender, args) => StartFight();
        backupOption.Activated += (sender, args) => CallBackup();
        modelSelectionOption.Activated += (sender, args) => modelSelectionMenu.Visible = true;
        cleanupOption.Activated += (sender, args) => Cleanup();
        spawnvehicleOption.Activated += (sender, args) => SpawnVehicle();


        enemyModelOption.Activated += (sender, args) => EditModelArray(true);
        guardModelOption.Activated += (sender, args) => EditModelArray(false);

        Tick += OnTick;
        KeyDown += OnKeyDown;
    }

    private static RelationshipGroup stripperGroup = StringHash.AtStringHash("GUARD");
    private static RelationshipGroup gangGroup = StringHash.AtStringHash("ENEMY");

    private VehicleHash[] gangVehicleHashes =
{
    VehicleHash.Baller2, VehicleHash.SultanRS, VehicleHash.Buffalo, VehicleHash.Buccaneer, VehicleHash.Dominator,
    VehicleHash.Felon, VehicleHash.Prairie, VehicleHash.Tailgater, VehicleHash.Premier, VehicleHash.Blista
};

    private void SpawnVehicle(bool isGangVehicle = false)
    {
        var player = Game.Player.Character;
        var spawnPosition = player.Position + player.ForwardVector * 5;

        // Select a random vehicle model based on type
        var selectedArray = gangVehicleHashes;
        var randomVehicleModel = selectedArray[new Random().Next(selectedArray.Length)];
        var vehicleModel = new Model(randomVehicleModel);

        if (!vehicleModel.IsLoaded)
        {
            vehicleModel.Request();
            while (!vehicleModel.IsLoaded) Script.Wait(10);
        }

        var vehicle = World.CreateVehicle(vehicleModel, spawnPosition);
        if (vehicle != null)
        {
            spawnedVehicles.Add(vehicle);
            vehicle.PlaceOnGround();
            vehicle.IsEngineRunning = true; // Start the engine

            //vehicle.PrimaryColor = VehicleColor.MetallicSilver; // Civilian-style color

        }

        vehicleModel.MarkAsNoLongerNeeded();
    }


    private void InitializeGroups()
    {
        gangGroup = World.AddRelationshipGroup("ENEMY");
        stripperGroup = World.AddRelationshipGroup("GUARD");

        gangGroup.SetRelationshipBetweenGroups(gangGroup, Relationship.Companion);
        stripperGroup.SetRelationshipBetweenGroups(stripperGroup, Relationship.Companion);

        stripperGroup.SetRelationshipBetweenGroups(gangGroup, Relationship.Neutral);
        gangGroup.SetRelationshipBetweenGroups(stripperGroup, Relationship.Neutral);
    }

    private void SpawnPed(bool isEnemy)
    {
        var player = Game.Player.Character;
        Vector3 spawnPosition = Vector3.Zero;
        spawnPosition += player.Position + player.ForwardVector * 5;
        var modelList = isEnemy ? enemyModels : guardModels;
        var randomModel = modelList[new Random().Next(modelList.Count)];
        var pedModel = new Model(randomModel);

        if (!pedModel.IsLoaded)
        {
            pedModel.Request();
            while (!pedModel.IsLoaded) Script.Wait(10);
        }

        var ped = World.CreatePed(pedModel, spawnPosition);
        if (ped != null)
        {
            ped.RelationshipGroup = isEnemy ? gangGroup : stripperGroup;
            GivePedRandomEquipment(ped);
            if (isEnemy) enemies.Add(ped); else guards.Add(ped);
        }

        pedModel.MarkAsNoLongerNeeded();
    }

    private static GTA.WeaponHash[] WeaponHash = { GTA.WeaponHash.Pistol, GTA.WeaponHash.CombatPistol, GTA.WeaponHash.PumpShotgun, GTA.WeaponHash.CarbineRifle, GTA.WeaponHash.AssaultRifle };
    private void GivePedRandomEquipment(Ped ped)
    {
        ped.Armor = new Random().Next(50, 100);
        ped.Health = 200;
        ped.Armor = 300;
        ped.Weapons.RemoveAll();
        var randomWeapon = WeaponHash[new Random().Next(WeaponHash.Length)];
        ped.Weapons.Give(randomWeapon, 300, true, true);
    }

    private void StartFight()
    {
        enemies.ForEach(enemy => enemy?.Task.ShootAt(enemy.Position.Around(10)));
        foreach (var enemy in enemies) { enemy?.Task.CombatHatedTargetsAroundPed(300f); enemy.CombatAbility = CombatAbility.Professional; }
        foreach (var guard in guards) { guard?.Task.CombatHatedTargetsAroundPed(300f); guard.CombatMovement = CombatMovement.WillAdvance; guard.CombatAbility = CombatAbility.Professional; }
    }

    private void CallBackup()
    {
        var playerPos = Game.Player.Character.Position;
        var spawnPosition = World.GetNextPositionOnStreet(playerPos.Around(60));
        var vehicleModel = new Model(VehicleHash.Police);

        if (!vehicleModel.IsLoaded)
        {
            vehicleModel.Request();
            while (!vehicleModel.IsLoaded) Script.Wait(10);
        }

        var vehicle = World.CreateVehicle(vehicleModel, spawnPosition);
        if (vehicle != null)
        {
            spawnedVehicles.Add(vehicle);
            //vehicle.PlaceOnNextStreet();
            var ped = World.CreatePed(PedHash.Cop01SFY, vehicle.Position);
            ped.Task.WarpIntoVehicle(vehicle, VehicleSeat.Driver);
            vehicle.ForwardSpeed = 30;
            ped.Weapons.Give(WeaponHash[new Random().Next(WeaponHash.Length)], 100, true, true);
            ped.Task.DriveTo(vehicle, playerPos, 50, VehicleDrivingFlags.None, 30);
        }

        vehicleModel.MarkAsNoLongerNeeded();
    }

    private void EditModelArray(bool isEnemy)
    {
        string input = Game.GetUserInput(isEnemy ? "Enter Enemy Models (comma-separated)" : "Enter Guard Models (comma-separated)");
        if (!string.IsNullOrEmpty(input))
        {
            var modelNames = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var modelList = isEnemy ? enemyModels : guardModels;

            modelList.Clear();
            foreach (var name in modelNames)
            {
                if (Enum.TryParse(name.Trim(), out PedHash result)) modelList.Add(result);
            }
        }
    }

    private void OnTick(object sender, EventArgs e)
    {
        menuPool.Process();
        new MainScript().OnTick();
    }

    private void OnKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
    {
        if (e.KeyCode == System.Windows.Forms.Keys.F5) mainMenu.Visible = !mainMenu.Visible;
    }

    public void Cleanup()
    {
        foreach (var ped in World.GetAllPeds()) ped?.Delete();
        foreach (var vehicle in World.GetAllVehicles()) vehicle?.Delete();
        //foreach (var prop in World.GetAllProps()) prop?.Delete();
        //foreach (var pickup in World.GetAllPickupObjects()) pickup?.Delete();

        enemies.Clear();
        guards.Clear();
        spawnedVehicles.Clear();
        spawnedProps.Clear();
    }
}
