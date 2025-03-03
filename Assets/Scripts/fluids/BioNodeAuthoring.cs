using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public class BioNodeAuthoring : MonoBehaviour
{

    public void Convert(Entity entity, EntityManager dstManager)
    {
        //AddComponent(entity, new ComponentTypeSet(new ComponentType[]{
        //    typeof(BioNodeTag),
        //    typeof(UnitStats),
        //    typeof(TypeCached),
        //    typeof(CombatTargets),
        //    typeof(BioNodeSphereRadius),
        //    typeof(BioResource),
        //    typeof(ForwardSync),
        //    typeof(CombatDroneRef),
        //    typeof(OutputAlternate),
        //    typeof(ChannelRef),
        //    typeof(DroneSpawnCooldown),
        //    typeof(DroneCount),
        //    typeof(CachedTurretTransform)
        //    //typeof(WarpCooldown),
        //    //typeof(BioNodeLinkRef),

            

        //}));

        //AddComponent(entity, new ComponentTypeSet(new ComponentType[]{
        //    typeof(DroneSpawnStates),
        //    typeof(DroneSpawnDelay),
        //    typeof(DroneSpawnType),
        //}));
        //AddComponent(entity, new ComponentTypeSet(new ComponentType[]{
        //    typeof(MeshGORef),typeof(ColliderRef),
        //    typeof(BioNodeLinkRef),
        //    typeof(WeaponInfoV2),
        //    typeof(UnitLevel),
        //    typeof(CombatTeam),
        //    typeof(StorageCell),
        //    typeof(StorageCellLimitMulti),
        //    typeof(BioNodeRegenStates),
        //    typeof(BioNodeNutrientCalcDelta_Self),
        //    typeof(BioNodeNutrientCalcDelta_Adj),
        //    //typeof(MeshCreateCmd),
        //}));
        //SetComponent(entity, new UnitLevel() { value = 1 });
        //SetComponent(entity, new BioNodeSphereRadius() { value = 4.85f });
        //SetComponent(entity, new DroneSpawnCooldown() { min_interval = 12f, elapsed = 12f, tcem = 1f });
        //SetComponent(entity, new DroneCount() { max_count = 16 });
        ////SetComponent(entity, new BioNodeRegenCooldown() { value = BioNodeRegenCooldown.Cooldown });
    }


}
public struct BioNodeRegenStates:IComponentData
{
    public float regen_bonus;
    public float value; // when it reaches 0, it will regen health.
    public float regen_time_left; // when it reaches 0, it will regen health.
    public bool is_regening;

}
public struct ProjectileRef:IBufferElementData
{
    public Entity host;
}

public struct BioNodeTag : IComponentData
{
    public const float cap = 200f;
    public float nutrient;
}
// used for generating biolink
public struct BioNodeSphereRadius : IComponentData
{
    public float value;
}

public struct BioNodeNutrientCalcDelta_Self : IComponentData
{
    public float value;
}
public struct BioNodeNutrientCalcDelta_Adj : IComponentData
{
    public float value;
}
[InternalBufferCapacity(4)]
public struct BioNodeLinkRef : IBufferElementData
{
    public Entity node_entity;
    public int link_id;
    public byte positive;
}
public struct BioNodeLinkCreateCmd : IComponentData { }


public struct NutrientGenerator : IComponentData { }

// long range missile-based attacker
//public struct SporeLauncher : IComponentData 
//{
//    public const float cooldown = 2f;
//    public float timeleft;
//}



public struct DroneCount : IComponentData
{
    public int max_count;
}

// provides healing and iron-dome like defensive mechanisms.
public struct ProtectorNode : IComponentData { }


public struct BioLinkTag : IComponentData
{
    public Entity source;
    public Entity target;
}
public struct BioNutrientLevel : IComponentData
{
    public float value;
}
public struct DroneSpawnCooldown:IComponentData
{
    public float min_interval;
    public float elapsed;
    public float tcem; // TempCombatEfficiencyMultiplier
}
//public enum BioNodeTypes:byte
//{
//    Generator,
//    Hatchery,
//    SporeLauncher,
//    Protector,
//    Obstacle, // hack!

//    Swarm_Spawn_DNA,// hack again!
//    Swarm_Spawn_2,// hack again!

//}