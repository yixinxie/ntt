using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[DisallowMultipleComponent]
 
public class AssemblerAuthoring : MonoBehaviour
{
    // Add fields to your component here. Remember that:
    //
    // * The purpose of this class is to store data for authoring purposes - it is not for use while the game is
    //   running.
    // 
    // * Traditional Unity serialization rules apply: fields must be public or marked with [SerializeField], and
    //   must be one of the supported types.
    //
    // For example,
    //    public float scale;
    public Entity[] rot_parts;
    public bool is_client;
    public int def_player_id;
    public ASMRecipe_Ntv initial_recipe;
    //public MachineSubTypes machine_subtype;

    
    //
    public static void AddCommonMachineComponents<T>(Baker<T> em, Entity entity) where T: Component
    {
        em.AddComponent(entity, new ComponentTypeSet(new ComponentType[]{
            
            typeof(GalacticType),
            //typeof(ASMEntityGUID),
            typeof(PlayerID_CD),
            typeof(SimulationGroup),
        }
        ));
        
        em.SetSharedComponent(entity, SimulationGroup.Client());
        em.AddComponent(entity, new ComponentTypeSet(new ComponentType[]{
            //typeof(MachineGizmosHeader_Diagnostic),
            //typeof(MachineGizmosHeader_ItemIcon),
            typeof(ColliderRef),
            typeof(MeshGORef),
            //typeof(MeshGORef),
            //typeof(AnimationInterpolation),
            //typeof(MachineTransferAnimTimes),
            //typeof(MachineItemGizmos),
            //typeof(AssemblerSubSecondTimer),
            //typeof(CardEffectVisual),
        }
        ));
    }
    public class Bakery : Baker<AssemblerAuthoring>
    {
        public override void Bake(AssemblerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddCommonMachineComponents(this, entity);
            //AssemblerAuthoring.AddPowerConsumerCD(this, entity);
            //TileRouterAuthoring.AddDirectTransportComponents(this, entity, authoring.is_client);
            AddComponent(entity, new ComponentTypeSet(new ComponentType[]{
                typeof(MachineOutputInventory),
                typeof(AssemblerInputInventory),
                typeof(ExtraProductionStates),
                typeof(CachedWorkingStates),
                //typeof(MachineDirectTransportDirTargetPositions),
            }
                ));

            //SetComponent(entity, new StructureMeshInfo() { category = prefab_category, index = (int)prefab_index });

            //SetComponent(entity, new PowerConsumption() { value = structure_info.power_req });
            //SetComponent(entity, new StorageCellLimit() { value = structure_info.storage_limit });


            var gtype = new GalacticType();
            gtype.value = GTypes.Assembler;
            //gtype.machine_init(authoring.machine_subtype);
            SetComponent(entity, gtype);
            SetComponent(entity, new PlayerID_CD() { value = authoring.def_player_id });

            AddComponent(entity, new ComponentTypeSet(new ComponentType[]
                {
                //typeof(AssemblerInputInventory),
                //typeof(MachineOutputInventory),
                typeof(AssemblerTimeLeft),
                //typeof(OrientationInGrid),
                //typeof(PowerConsumption),
                    //typeof(StorageCellLimit),
                }));
            //SetComponent(entity, new StorageCellLimit() { value = 50 });

            var aii = new AssemblerInputInventory();
            aii.recipe = authoring.initial_recipe;
            SetComponent(entity, new AssemblerInputInventory() { recipe = authoring.initial_recipe });
            SetComponent(entity, new MachineOutputInventory() { item_type = (ushort)authoring.initial_recipe.output_type });

#if UNITY_EDITOR
            //AddComponent(entity, typeof(AssemblerInputInventoryDebug));
#endif
            //if (authoring.is_client)
            //{

            //    SetComponent(entity, new AnimationInterpolation() { cycle_length = 10f });

            //    if (authoring.rot_parts != null && authoring.rot_parts.Length > 0)
            //    {
            //        AddComponent(entity, new RotatingPiece() { value = authoring.rot_parts[0] });
            //        if (authoring.rot_parts.Length > 1)
            //            AddComponent(entity, new RotatingPiece2() { value = authoring.rot_parts[1] });
            //        else
            //            AddComponent(entity, new RotatingPiece2());
            //    }
            //}
        }
    }
}
// synced to the client to indicate launch intervals
public struct RocketLaunchInterval:IComponentData
{
    public int value; // firing once every X seconds.
}
// cyclic, server only
[System.Serializable]
unsafe public struct RocketLaunchHistory : IComponentData
{
    public const int bucket_total_count = 8;
    public fixed long buckets[bucket_total_count];
    public byte ptr;

    //public static int log_production(int val)
    //{
    //    /* initial values:
    //     * 0 => 0
    //     * 1 => 1
    //     * 2 => 2
    //     * 3 => 3
    //     * 4 => 3
    //     * 5 => 4
    //     * 6 => 4
    //     * 7 => 4
    //     * 8 => 4
    //     * 9 => 5
    //     * */
    //    if (val == 0)
    //    {
    //        return 0;
    //    }
    //    return math.ceillog2(val) + 1;
    //}
    //public static int decode(byte internal_value)
    //{
    //    int power = internal_value - 1;
    //    if (power < 0) return 0;
    //    return (int)math.round(math.pow(2, power));
    //}
    public int encode(long current)
    {
        float sum = 0f;
        int i = 0;
        for(; i < bucket_total_count; ++i)
        {
            var ptr_test = ptr - i - 1;
            if(ptr_test < 0)
            {
                ptr_test += bucket_total_count;
                
            }
            if (buckets[ptr_test] == 0) break;
            var diff = current - buckets[ptr_test];
            sum += diff;
        }
        if (i < 4) return 0;
        return (int)math.max(math.round(sum / i), 1);
        //int prev_ptr = ((int)ptr) - 1;
        //if (prev_ptr < 0) prev_ptr += bucket_total_count;
        //int log2 = log_production(buckets[prev_ptr]);
        //return (byte)math.clamp(log2, 0, 255);
    }

//}

//[System.Serializable]
//unsafe public struct MachinePulseHistory : IComponentData
//{
    public void signal(long tstamp)
    {
        buckets[ptr] = tstamp;
        ptr = (byte)((ptr + 1) % bucket_total_count);
    }
    //public void elapse()
    //{
    //    elapsed++;
    //    if (elapsed >= bucket_length)
    //    {
    //        elapsed = 0;
    //        ptr++;
    //        ptr = (byte)(ptr % bucket_total_count);
    //    }
    //}
}
// cyclic

[System.Serializable]
unsafe public struct MachineWorkingStateHistory:IComponentData
{
    public float running_duration; // summed
    public float duration_measured;
    //public fixed float value[MaxHistoryLength];
    //public int ptr;
    public void update_state(byte state, float dt)
    {
        const float total_time = 15f;
        duration_measured += dt;
        float elapsed = dt;
        if(duration_measured > total_time)
        {
            var exceeded_time = duration_measured - total_time;
            duration_measured = total_time;
            if (state == 0)
            {
                elapsed = exceeded_time;
                running_duration -= elapsed;
                if (running_duration < float.Epsilon)
                {
                    running_duration = 0f;
                }
                return;
            }
        }
        if(state == 1)
        {
            running_duration += elapsed;
            if (running_duration > total_time)
            {
                running_duration = total_time;
            }
        }
    }

    
    public float get_perc()
    {
        float ratio = 0f;
        if (duration_measured > float.Epsilon)
        {
            ratio = running_duration / duration_measured;
        }
        return ratio;
    }
}

// on extractors only.
unsafe public partial struct MachineAdjacentTransferDirs:IComponentData
{
    // each value corresponds to a direction index. the indexing scheme is similar to MachineAdjacentOutputEntity
    public fixed byte dirs[6];
    public byte count;

    public byte dirs_dbg_0;
    public byte dirs_dbg_1;
    public byte dirs_dbg_2;
    public byte dirs_dbg_3;
    public byte dirs_dbg_4;
    public byte dirs_dbg_5;
    //public bool has_dir(byte val)
    //{
    //    for(int i = 0; i < count; ++i)
    //    {
    //        if (dirs[i] == val)
    //            return true;
    //    }
    //    return false;
    //}
    public void append(int idx)
    {
        if (count == 0) dirs_dbg_0 = (byte)idx;
        else if (count == 1) dirs_dbg_1 = (byte)idx;
        else if (count == 2) dirs_dbg_2 = (byte)idx;
        else if (count == 3) dirs_dbg_3 = (byte)idx;
        else if (count == 4) dirs_dbg_4 = (byte)idx;
        else if (count == 5) dirs_dbg_5 = (byte)idx;

        dirs[count] = (byte)idx;
        count++;
    }
}


public struct SimulationGroup : ISharedComponentData
{
    const int ClientGroup = 1;
    const int ServerGroup = 2;
    const int PlanGroup = 3;
    public int value;

    public bool IsClient { get { return value == ClientGroup; } }
    public bool IsServer { get { return value == ServerGroup; } }
    public bool IsPlanned { get { return value == PlanGroup; } }


    public static SimulationGroup Client()
    {
        return new SimulationGroup() { value = ClientGroup };
    }
    public static SimulationGroup Server()
    {
        return new SimulationGroup() { value = ServerGroup };
    }
    public static SimulationGroup Plan()
    {
        return new SimulationGroup() { value = PlanGroup };
    }

}