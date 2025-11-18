using GTA;
using GTA.Native;
using System;
using System.Linq;
using System.Windows.Forms;

/// <summary>
/// Main guard system manager
/// Handles guard spawning, shift management, backup system, and daily billing
/// </summary>
public class GuardManager : Script
{
    public static GuardSpawner _guardSpawner;
    private static BackupDispatchSystem _backupSystem;
    
    // Daily charges system
    private int _lastChargeDay = -1;
    private int _lastChargeHour = -1;

    public GuardManager()
    {
        try
        {
            _guardSpawner = new GuardSpawner("./scripts/GTA/Areas.xml");
            Logger.Log.Info("GuardSpawner initialized successfully.");
            //HelperClass.Notification("GuardSpawner initialized.");
            
            // Initialize backup system
            _backupSystem = new BackupDispatchSystem();
            Logger.Log.Info("BackupDispatchSystem initialized successfully.");
            //HelperClass.Notification("Backup system initialized.");
            
           

            Tick += OnTick;
            Aborted += OnAbort;
        }
        catch (Exception ex)
        {
            Logger.Log.Fatal($"[GuardManager] Error: {ex.Message}\n{ex.StackTrace}");
            HelperClass.Notification($"GuardManager Error: {ex.Message}");
        }
    }

    private void OnTick(object sender, EventArgs e)
    {
    Player player = Game.Player;
    _guardSpawner.CheckPlayerProximityAndSpawn(player);
    // Fast per-frame cleanup to help the engine reclaim dead/abandoned entities
    _guardSpawner.FastCleanupTick(player);
        
        // Update backup system
        _backupSystem?.Update();

        // Process daily maintenance charges
        ProcessDailyCharges();
        
        Logger.Log.Info($"[GuardManager] Tick: Player at {player.Character.Position}");
    }

    

    /// <summary>
    /// Process daily maintenance charges for all active guard areas
    /// Charges are deducted once per in-game day at midnight (00:00)
    /// </summary>
    private void ProcessDailyCharges()
    {
        if (GuardSpawner.areas == null || GuardSpawner.areas.Count == 0)
            return;
        
        int currentDay = Function.Call<int>(Hash.GET_CLOCK_DAY_OF_MONTH);
        int currentHour = Function.Call<int>(Hash.GET_CLOCK_HOURS);
        
        // Charge once per day at midnight (00:00)
        if (currentDay != _lastChargeDay || (currentDay == _lastChargeDay && currentHour == 0 && _lastChargeHour != 0))
        {
            int totalCharges = 0;
            int areasCharged = 0;
            
            // Calculate charges for all areas that have spawned guards
            foreach (var area in GuardSpawner.areas)
            {
                if (area.DailyCharges <= 0)
                    continue;
                
                // Only charge if this area has active guards or vehicles
                // Performance: Use Count comparison instead of .Any() for better performance
                bool hasActiveGuards = GuardSpawner.guardPeds.Count(g => g.AreaName == area.Name && g.guardPed != null && g.guardPed.Exists()) > 0;
                bool hasActiveVehicles = GuardSpawner.guardVehicles.Count(v => v.AreaName == area.Name && v.guardVehicle != null && v.guardVehicle.Exists()) > 0;
                
                if (hasActiveGuards || hasActiveVehicles)
                {
                    totalCharges += area.DailyCharges;
                    areasCharged++;
                    Logger.Log.Info($"Daily charge for area '{area.Name}': ${area.DailyCharges}");
                }
            }
            
            // Deduct the total from player's money
            if (totalCharges > 0)
            {
                Game.Player.Money -= totalCharges;
                
                string notification = areasCharged == 1 
                    ? $"~r~Daily Guard Maintenance~s~~n~~w~{areasCharged} area: -${totalCharges:N0}" 
                    : $"~r~Daily Guard Maintenance~s~~n~~w~{areasCharged} areas: -${totalCharges:N0}";
                    
                HelperClass.Notification(notification);

                Logger.Log.Info($"Total daily charges: ${totalCharges} from {areasCharged} areas. New balance: ${Game.Player.Money}");
            }
            
            // Update tracking variables
            _lastChargeDay = currentDay;
            _lastChargeHour = currentHour;
        }
        else if (currentHour != 0)
        {
            // Reset hour tracking when we leave midnight hour
            _lastChargeHour = currentHour;
        }
    }

    private void OnAbort(object sender, EventArgs e)
    {
        _guardSpawner?.UnInitialize();
        
        // Cleanup backup system
        if (_backupSystem != null)
        {
            _backupSystem.Cleanup();
            Logger.Log.Info("BackupDispatchSystem shutdown.");
        }
        
        Tick -= OnTick;
        Logger.Log.Info("[GuardManager] Script aborted.");
        //HelperClass.Notification("Guard system shutdown.");
    }
}
