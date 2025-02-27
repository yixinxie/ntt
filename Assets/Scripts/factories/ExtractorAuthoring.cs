using JetBrains.Annotations;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// for the latest mobile. only support simple item type. no purity. from multiple types, one output type at a time. selectable in UI.
[DisallowMultipleComponent]
public class ExtractorAuthoring : MonoBehaviour
{
    public bool npc_owned;
    public int def_player_id;
    public static void AddPowerConsumerCD<T>(Baker<T> baker, Entity target) where T : UnityEngine.Component
    {
        baker.AddComponent(target, new ComponentTypeSet(new ComponentType[]
            {
                typeof(ProductionModifier),
                typeof(PowerGridRef),
                typeof(PowerConnectorRef),
                
            }));
    }

    public static void AddPowerConnectorCD<T>(Baker<T> baker, Entity target) where T : UnityEngine.Component
    {
        baker.AddComponent(target, new ComponentTypeSet(new ComponentType[]
            {
                typeof(PowerGridRef),
            }));
    }
    public static void AddPowerGeneratorCD<T>(Baker<T> baker, Entity target) where T : UnityEngine.Component
    {
        baker.AddComponent(target, new ComponentTypeSet(new ComponentType[]
            {
                typeof(PowerGeneratorStates),
                typeof(PowerGridRef),
                typeof(PowerConnectorRef),
            }));
    }

    //public static void AddPowerExtractorCD<T>(Baker<T> baker, Entity target) where T : UnityEngine.Component
    //{
    //    baker.AddComponent(target, new ComponentTypeSet(new ComponentType[]
    //        {
    //            typeof(PowerGeneratorStates),
    //            typeof(PowerGridRef),
    //            typeof(PowerGridRef_SCD),
    //            typeof(ProductionModifier),
    //        }));
    //}
    //public static void AddPowerStorageCD<T>(Baker<T> baker, Entity target) where T : UnityEngine.Component
    //{
    //    baker.AddComponent(target, new ComponentTypeSet(new ComponentType[]
    //        {
    //            typeof(PowerGeneratorStates),
    //            typeof(PowerStorageStates),
    //            typeof(PowerGridRef),
    //        }));
    //}
    public class Bakery : Baker<ExtractorAuthoring>
    {
        public override void Bake(ExtractorAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);


            //PlayerFleetAuthoring.AddCombatComponents(entity, dstManager, is_client);
            //var ship_states = dstManager.GetBuffer<ShipStatesInFleet>(entity);
            //var ssif = new ShipStatesInFleet();
            //ssif.def_values();
            //ssif.enabled = 1;
            //ssif.current_health = ssif.total_health = 1500;
            //ssif.weapon_type = WeaponTypes_V2.NoWeapon;
            //ship_states.Add(ssif);

            AddComponent(entity, new ComponentTypeSet(new ComponentType[]
            {
                typeof(ExtractorProductionStates),
                typeof(ExtractorCoverTargetElement),
                typeof(MachineOutputInventory),
                typeof(ExtraProductionStates),

                //typeof(MachineAdjacentOutputEntity),
                //typeof(OutputAlternate),
                typeof(CachedWorkingStates),
                //typeof(StorageCellLimit),
            }));
            //SetComponent(entity, new ExtractorProductionStates() { total = ASMConstants.ExtractorCycleDuration[0], batch_count = ASMConstants.ExtractorBatchCount[0] }); // not necessary.
            SetComponent(entity, new ExtractorProductionStates() { batch_count = 1, total = 3 });
            SetComponent(entity, new MachineOutputInventory() { item_type = 1 });
            AssemblerAuthoring.AddCommonMachineComponents(this, entity);
            //TileRouterAuthoring.AddDirectTransportComponents(this, entity, authoring.is_client);
            {
                //AddComponent(entity, new ComponentTypeSet(new ComponentType[]{
                //    typeof(MachineAnimationEntityRef),
                //}));
                //SetComponent(entity, new AnimationInterpolation() { cycle_length = 24f });
                //AddComponent(entity, new ComponentTypes(new ComponentType[]
                //{
                //    //typeof(InputAlternate),

                //    //typeof(StorageCellLimit),
                //}));

                //SetComponent(entity, new StorageCellLimit() { value = ASMConstants.ExtractorCapacities[0] });
            }
            //SetComponent(entity, new TypeCached() { structure_type = StructureType.Extractor});
            SetComponent(entity, new GalacticType() { value = GTypes.Extractor });

            //AddPowerConsumerCD(this, entity);
            //SetComponent(entity, new ProductionModifier() { idle_consumption = 5, full_consumption = 10 });
        }
    }
}
public partial struct ExtractorProductionStates:IComponentData
{
    public float left;
    public short total;
    public byte batch_count;
    //public byte bonus_progress;
    //public ushort bonus_increment;
}
[System.Serializable]
public partial struct ExtraProductionStates : IComponentData
{
    //public byte batch_count;
    public ushort bonus_increment;

    // RandomizedMachineOutput
    public ushort pool_size;
    public uint current_seed;

    // relocated for byte alignment
    public byte bonus_progress;

    // RandomizedMachineOutput section
    public bool get_hit()
    {
        if (pool_size == 0)
        {
            return true;
        }
        Unity.Mathematics.Random rng = default;
        rng.state = current_seed;
        bool ret = rng.NextUInt(pool_size) == 1;
        current_seed = rng.state;
        return ret;
    }
    public void init_test(uint initial_seed, ushort _pool_size)
    {
        current_seed = Unity.Mathematics.Random.CreateFromIndex(initial_seed).state;
        pool_size = _pool_size;
    }
    public void init_seed(uint initial_seed)
    {
        current_seed = Unity.Mathematics.Random.CreateFromIndex(initial_seed).state;
        pool_size = 0;
    }
    // RandomizedMachineOutput section ends
    public int aggre(int batch_count)
    {
        int final_amount = batch_count;
        int tmp_bonus_progress = bonus_progress + bonus_increment;
        if (tmp_bonus_progress >= 100)
        {
            int tmp_rate = tmp_bonus_progress / 100;
            tmp_bonus_progress = tmp_bonus_progress % 100;
            final_amount += batch_count * tmp_rate;
        }
        bonus_progress = (byte)tmp_bonus_progress;
        return final_amount;
    }
}
public struct MachinePlatformStatesCached:IComponentData
{
    public uint value;
}

public struct MachineTemperature:IComponentData
{
    public int value;

}