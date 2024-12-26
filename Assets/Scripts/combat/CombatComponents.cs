/**
 * Copyright 2021-2022 Chongqing Centauri Technology LLC.
 * All Rights Reserved.
 * 
 */
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;

[InternalBufferCapacity(4)]
public struct TargetSearchRequest:IBufferElementData
{
    public float radius;
    public short weapon_index; // weapon info does not change throughout a fight
    public WeaponTypes cached_weapon_type;
}
[InternalBufferCapacity(4)]
public struct WeaponInfo:IBufferElementData
{
    public WeaponTypes weapon_type;
    public WeaponModes weapon_mode; // instant, or delayed projectile
    public WeaponStatus status;

    //public StructureType projectile_type;
    public Entity target;
    public float radius;
    public float cooldown_left; // 0 means ready to fire
    public float cooldown_max;
    public int ammo_count;
    public int ammo_max;
    public float base_damage;
}
public enum WeaponStatus:byte
{
    NotSearching,
    AlreadySearching,
}
public struct StorageCell:IBufferElementData
{
    public ushort itemtype;
    public ushort item_count;
}
public struct BeamVisualEntity:IBufferElementData
{
    public Entity value;
}
public struct CombatTeam : IComponentData
{

    public int value;
    public const uint max = 4;
    public const uint EnemyTeam = 1;
    public const uint PlayerTeam = 0;
    public uint HostileTeamMask()
    {
        return (~(1U << (value)) & 0b1111);
    }
    public uint FriendlyTeamMask()
    {
        return (1U << value) & 0b1111;
    }
}
public struct CachedTurretTransform:IComponentData
{
    public float3 c0;
    public quaternion c1;
}
// found on colliders
public struct ColliderHostRef:IComponentData
{
    public Entity value;
}

// found on data entities
public struct ColliderRef : IComponentData
{
    public Entity value;
}
[System.Serializable]
public struct UnitStats : IComponentData
{
    //public ShipClasses ship_class; // should be removed.
    public float health;
    public float max_health;
    public float attack;
    public float defense;
    public byte armor_types;
}
// WeaponTypes determine what kind of entity this weapon will target
public enum WeaponTypes:byte
{

    Laser_Defensive,// platform laser
    Laser_Offensive,
    Laser_Offensive_Cont,// ³æÈºÐ¡·É»ú¹¥»÷ÀØÉä
    Cannon_Multi, // cannon targets vehicles, channeled
    Projectile_Multi, // projectile targets vehicles
    Repair_Multi,
    Spawn_Fighter, // not used.
    Spawn_Hatchling, // not used.
    Spawn_Swarm,
    Cannon_Vehicle,
    Cannon,
    Projectile,
    Projectile_Platform, // platform missile, platform weapon to flying drone enemies
    Repair, // channeled
    Laser_Offensive_Charged,//Íæ¼Ò½¢´¬¹¥»÷ÀØÉä
    Spawn_Fighter_Defensive,
    
    Cannon_Platform,
}


public enum WeaponTypes_V2 : byte
{
    // instant weapons
    Laser,
    InstantWeapons, // less than
    Repair,
    DelayedWeapons, // greater than
    // delayed weapons
    Cannon,
    Missile,
    Spore, // omni-directional
    NoWeapon,
    
}

//                laser - defensive projectile cannon	vehicle	character
//laser-defensive		                    Y			            Y
//laser-offensive				                            Y	    Y
//projectile				                                Y	
//cannon				                                    Y	    Y

// laser -> instant
// 
public enum WeaponModes : byte
{
    Projectile,
    Instant,
    Instant_OneShot,
    Spawn,
}