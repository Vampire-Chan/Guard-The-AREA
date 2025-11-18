using System;
using System.Collections.Generic;
using GTA;
using Guarding.Core.Enums;

/// <summary>
/// Shared data models for backup system
/// </summary>
    
    public class BackupSquad
    {
        public BackupType SquadType { get; set; }
        public Vehicle Vehicle { get; set; }
        public Blip VehicleBlip { get; set; }
        public List<BackupGuard> Guards { get; set; } = new List<BackupGuard>();
        
        public DateTime SpawnTime { get; set; }
        public bool IsActive { get; set; }
        
        // For billing calculations
        public int InitialGuardCount { get; set; }
        public float InitialVehicleHealth { get; set; }
        
        // Combat end tracking
        public bool CombatEndCheck { get; set; } = false;
        public DateTime CombatEndTime { get; set; } = DateTime.MinValue;
        
        // Tactical helicopter support (for advanced systems)
        public object TacticalHelicopter { get; set; }
        public object DeploymentMode { get; set; }
        // Indicates that the squad has finished deploying its crew (rappel/landing finished)
        // and is eligible for cleanup when mission/AI decides to leave.
        public bool DeploymentComplete { get; set; } = false;
        // When DeploymentComplete is set, this records the time so we can apply a short grace delay
        // before cleanup (prevents racing with final animation tasks).
        public DateTime DeploymentCompleteTime { get; set; } = DateTime.MinValue;
    }
    
    public class BackupGuard
    {
        public Ped Ped { get; set; }
        public int InitialHealth { get; set; }
        public int InitialAmmo { get; set; }
        public bool HasWeapon { get; set; }
        public Blip Blip { get; set; }
    }
