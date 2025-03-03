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

//[InternalBufferCapacity(4)]
//public struct WeaponInfo:IBufferElementData
//{
//    public WeaponTypes weapon_type;
//    public WeaponModes weapon_mode; // instant, or delayed projectile
//    public WeaponStatus status;

//    //public StructureType projectile_type;
//    public Entity target;
//    public float radius;
//    public float cooldown_left; // 0 means ready to fire
//    public float cooldown_max;
//    public int ammo_count;
//    public int ammo_max;
//    public float base_damage;
//}
//public enum WeaponStatus:byte
//{
//    NotSearching,
//    AlreadySearching,
//}
[System.Serializable]
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
        return (~(1U << value) & 0b1111);
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
    public static CachedTurretTransform from_localtransform(LocalTransform lt)
    {
        return new CachedTurretTransform() { c0 = lt.Position, c1 = lt.Rotation };
    }
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

    CC_Passive_Scan, // command center passive scan
    Cannon,

    Channeled_Start = 16,
    Laser_Offensive,
    
    Projectile_Start = 32,
    Projectile,
    Spawn_Fighter, // not used.
    Spawn_Hatchling, // not used.
    Spawn_Swarm,
    Projectile_Platform, // platform missile, platform weapon to flying drone enemies
    Repair, // channeled
    Laser_Offensive_Charged,//��ҽ�����������
    
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