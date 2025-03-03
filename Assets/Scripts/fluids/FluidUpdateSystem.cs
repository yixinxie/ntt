using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
unsafe public partial struct MachineSimulationSystem : ISystem
{
    partial struct fluids_flow_update : IJobEntity
    {
        public BufferLookup<FluidMachineInputInventory> adj_in_inventory;
        public BufferLookup<FluidMachineOutputInventory> adj_out_inventory;
        public float flow_coef;
        public float pres_coef;
        void exchange(ref FluidStates self, ref FluidStates other)
        {
            var pressure_diff = self.pressure_ratio() - other.pressure_ratio();
            var flow_rate = pressure_diff * flow_coef;
            if (flow_rate > 0f)
            {
                // flow outwards
                var adjustment = min_three(flow_rate, self.volumes, other.max_volume - other.volumes);
                //var adjustment = math.min(math.min(flow_rate, self_foi.volumes), );
                //var adjustment_int = (int)math.round(adjustment);
                self.adjust_unchecked(-adjustment);
                other.adjust_unchecked(adjustment);
            }
            else if (flow_rate < 0f)
            {
                // flow inwards
                var adjustment = math.min(math.min(-flow_rate, self.max_volume - self.volumes), other.volumes);
                self.adjust_unchecked(adjustment);
                other.adjust_unchecked(-adjustment);
            }
            self.pressure = self.volumes / self.max_volume * pres_coef;
            other.pressure = other.volumes / other.max_volume * pres_coef;
        }
        void Execute(DynamicBuffer<FluidFlowBDTarget> fluid_targets, FluidPipeInventory self_foi)
        {
            for (int i = 0; i < fluid_targets.Length; ++i)
            {
                var self_pressure_ratio = self_foi.fs.pressure_ratio();
                var target_info = fluid_targets[i];
                if (target_info.index_in_target_machine >= 0)
                {
                    var input_inv_db = adj_in_inventory[target_info.value];
                    var input_inv = input_inv_db[target_info.index_in_target_machine];
                    exchange(ref self_foi.fs, ref input_inv.fs);
                    input_inv_db[target_info.index_in_target_machine] = input_inv;
                }
                else if (target_info.index_in_source_machine >= 0)
                {
                    var out_inv_db = adj_out_inventory[target_info.value];
                    var out_inv = out_inv_db[target_info.index_in_source_machine];
                    exchange(ref self_foi.fs, ref out_inv.fs);
                    out_inv_db[target_info.index_in_source_machine] = out_inv;
                }

            }

        }
    }
    //unsafe struct fluid_entity_merge : IJob
    //{
    //    public NativeArray<Entity> machine_targets;
    //    public ComponentLookup<ASMFluidRecipe> fluid_recipes_dict;
    //    public BufferLookup<MachineFluidInputEntityRef> miferef_dict;
    //    public BufferLookup<FluidEntity2MachineRef> fe2mr_dict2;
    //    public Entity fluid_entity_prefab;
    //    public EntityCommandBuffer ecb;
    //    public void Execute()
    //    {
    //        NativeArray<ASMFluidRecipe> recipes = new NativeArray<ASMFluidRecipe>(machine_targets.Length, Allocator.Temp);
    //        for(int i = 0; i < machine_targets.Length; ++i)
    //        {
    //            recipes[i] = fluid_recipes_dict[machine_targets[i]];
    //        }
    //        var base_recipe = recipes[0];
    //        var self_mfier_db = miferef_dict[machine_targets[0]];
    //        self_mfier_db.Clear();
    //        for (int i = 0; i < ASMFluidRecipe.MaxFluidTypes; ++i)
    //        {
    //            var fluid_type = base_recipe.src_types[i];
    //            if (fluid_type == 0) break;
    //            NativeList<Entity> machines_tmp = new NativeList<Entity>(4, Allocator.Temp);
    //            NativeList<int> machines_recipe_indices_tmp = new NativeList<int>(4, Allocator.Temp);
    //            for (int j = 1; j < recipes.Length; ++j)
    //            {
    //                var reci = recipes[j];
    //                for(int k = 0; k < ASMFluidRecipe.MaxFluidTypes; ++k)
    //                {
    //                    if (reci.src_types[k] == fluid_type)
    //                    {
    //                        machines_tmp.Add(machine_targets[j]);
    //                        machines_recipe_indices_tmp.Add(k);
    //                        break;
    //                    }
    //                }
    //            }
    //            if(machines_tmp.Length > 0)
    //            {
    //                // merge all their fluid entities.
    //                NativeHashSet<Entity> homo = new NativeHashSet<Entity>(4, Allocator.Temp);
    //                for(int j = 0; j < machines_tmp.Length; ++j)
    //                {
    //                    var refs = miferef_dict[machines_tmp[j]];
    //                    var fluid_entity = refs[machines_recipe_indices_tmp[j]].value;
    //                    homo.Add(fluid_entity);
    //                }
    //                var homo_na = homo.ToNativeArray(Allocator.Temp);
    //                if(homo_na.Length > 1)
    //                {
    //                    for(int j = 1; j < homo_na.Length; ++j)
    //                    {

    //                    }
    //                }
    //                // self assignment
    //                self_mfier_db.Add(new MachineFluidInputEntityRef() { value = homo_na[0] });
    //            }
    //            else
    //            {
    //                // create a new one
    //                var new_fluid_entity = ecb.Instantiate(fluid_entity_prefab);
    //                var ref_db = ecb.SetBuffer<FluidEntity2MachineRef>(new_fluid_entity);
    //                ref_db.Add(new FluidEntity2MachineRef() { value = machine_targets[0] });

    //            }
    //        }

    //    }
    //}
    //public void chemical_update()
    //{
    //    var fluid_container_dict = GetComponentLookup<FluidContainerInventory>();
    //    Entities.ForEach((Entity entity, DynamicBuffer<MachineFluidInputEntityRef> input_refs, DynamicBuffer<MachineFluidOutputEntityRef> output_refs, ref AssemblerTimeLeft timeleft, in ASMFluidRecipe recipe) =>
    //    {
    //        if (timeleft.value == 0)
    //        {
    //            bool met = FluidContainerInventory.check_inputs(input_refs, fluid_container_dict, recipe);
    //            if(met)
    //            {
    //                // start the machine
    //                FluidContainerInventory.deduct_inputs(input_refs, fluid_container_dict, recipe);
    //                timeleft.value = recipe.max_time;
    //            }
    //        }
    //        else
    //        {
    //            timeleft.value--;
    //            if(timeleft.value == 0)
    //            {
    //                FluidContainerInventory.do_outputs(output_refs, fluid_container_dict, recipe);

    //                bool output_not_full = FluidContainerInventory.check_outputs(output_refs, fluid_container_dict, recipe);
    //                bool met = FluidContainerInventory.check_inputs(input_refs, fluid_container_dict, recipe);
    //                if (met && output_not_full)
    //                {
    //                    // start the machine
    //                    FluidContainerInventory.deduct_inputs(input_refs, fluid_container_dict, recipe);
    //                    timeleft.value = recipe.max_time;
    //                }
    //            }
    //        }
    //    }).Run();

    //    //var fluid_container_dict = GetComponentLookup<FluidContainerInventory>();
    //    Entities.ForEach((Entity entity, DynamicBuffer<MachineFluidInputEntityRef> input_refs, DynamicBuffer<MachineFluidOutputEntityRef> output_refs, ref HeatExchangeTimeLeft he_states, ref MachineTemperature temp, in ASMFluidRecipe recipe) =>
    //    {
    //        if (he_states.value == 0)
    //        {
    //            bool met = FluidContainerInventory.check_inputs(input_refs, fluid_container_dict, recipe);
    //            if (met)
    //            {
    //                // start the machine
    //                FluidContainerInventory.deduct_inputs(input_refs, fluid_container_dict, recipe);
    //                he_states.value = recipe.max_time;
    //            }
    //        }
    //        else
    //        {
    //            he_states.value--;
    //            temp.value += he_states.temperature_diff;
    //            if (he_states.value == 0)
    //            {
    //                FluidContainerInventory.do_outputs(output_refs, fluid_container_dict, recipe);

    //                bool output_not_full = FluidContainerInventory.check_outputs(output_refs, fluid_container_dict, recipe);
    //                bool met = FluidContainerInventory.check_inputs(input_refs, fluid_container_dict, recipe);
    //                if (met && output_not_full)
    //                {
    //                    // start the machine
    //                    FluidContainerInventory.deduct_inputs(input_refs, fluid_container_dict, recipe);
    //                    he_states.value = recipe.max_time;
    //                }
    //            }
    //        }
    //    }).Run();
    //}

    public static void AddFluidPipeComponents(EntityManager em, Entity entity)
    {
        em.AddComponent(entity, new ComponentTypeSet(new ComponentType[]{
            typeof(FluidFlowBDTarget),
            typeof(FluidPipeInventory),
            //typeof(FluidFrameUpdateStates),
            
        }));
    }

    public static void AddFluidMachineComponents(EntityManager em, Entity entity)
    {
        em.AddComponent(entity, new ComponentTypeSet(new ComponentType[]{
            typeof(FluidMachineOutputInventory),
            typeof(FluidMachineInputInventory),
            //typeof(FluidFrameUpdateStates),
            
        }));
    }
}

[InternalBufferCapacity(4)]
unsafe public struct FluidFlowBDTarget:IBufferElementData // fluid pipe bidirectional targets
{
    public Entity value;
    public short index_in_target_machine;
    public short index_in_source_machine;
}

[System.Serializable]
unsafe public struct FluidPipeInventory : IComponentData
{
    //public const int MaxFluidTypes = 4; // per container
    public FluidStates fs;
}

public struct FluidStates
{
    public ushort types;
    public float volumes;
    public float max_volume;
    public float pressure;
    public float pressure_ratio()
    {
        return pressure / max_volume;
    }

    public void adjust_unchecked(float amount)
    {
        volumes += amount;
    }
}
public struct FluidMachineInputInventory : IBufferElementData
{
    //public const int MaxFluidTypes = 4; // per container
    public FluidStates fs;
    public Entity source; // source pipe, reference only?
    
}

[System.Serializable]
public struct FluidMachineOutputInventory : IBufferElementData
{
    public FluidStates fs;
    public Entity target; // output pipe

    //unsafe public static bool check_inputs(DynamicBuffer<MachineFluidInputEntityRef> fluid_refs, ComponentLookup<FluidOutputInventory> fci_dict, ASMFluidRecipe recipe)
    //{
    //    bool met = true;
    //    for (int i = 0; i < fluid_refs.Length; ++i)
    //    {
    //        if (fluid_refs[i].value.Equals(Entity.Null))
    //        {
    //            met = false;
    //            break;
    //        }
    //        var input_container = fci_dict[fluid_refs[i].value];
    //        if (input_container.amount < recipe.src_counts[i])
    //        {
    //            met = false;
    //            break;
    //        }
    //    }
    //    return met;
    //}

    //unsafe public static bool heat_exchange_check_input_singular(DynamicBuffer<MachineFluidInputEntityRef> fluid_refs, ComponentLookup<FluidOutputInventory> fci_dict)
    //{
    //    var amount = fci_dict[fluid_refs[0].value];
    //    return amount.amount > 0;
    //}

    //unsafe public static bool heat_exchange_check_output_singular(DynamicBuffer<MachineFluidOutputEntityRef> fluid_refs, ComponentLookup<FluidOutputInventory> fci_dict)
    //{
    //    var amount = fci_dict[fluid_refs[0].value];
    //    return amount.amount + 1 <= amount.max_volume;
    //}

    //unsafe public static void deduct_inputs(DynamicBuffer<MachineFluidInputEntityRef> fluid_refs, ComponentLookup<FluidOutputInventory> fci_dict, ASMFluidRecipe recipe)
    //{
    //    for (int i = 0; i < fluid_refs.Length; ++i)
    //    {

    //        var input_container = fci_dict[fluid_refs[i].value];
    //        input_container.amount -= recipe.src_counts[i];
    //        fci_dict[fluid_refs[i].value] = input_container;
    //    }
    //}

    //unsafe public static bool check_outputs(DynamicBuffer<MachineFluidOutputEntityRef> fluid_refs, ComponentLookup<FluidOutputInventory> fci_dict, ASMFluidRecipe recipe)
    //{
    //    bool valid = true;
    //    for (int i = 0; i < fluid_refs.Length; ++i)
    //    {
    //        if (fluid_refs[i].value.Equals(Entity.Null))
    //        {
    //            valid = false;
    //            break;
    //        }
    //        var input_container = fci_dict[fluid_refs[i].value];
    //        if (input_container.amount + recipe.output_count[i] > input_container.max_volume)
    //        {
    //            valid = false;
    //            break;
    //        }
    //    }
    //    return valid;
    //}
    //unsafe public static void do_outputs(DynamicBuffer<MachineFluidOutputEntityRef> fluid_refs, ComponentLookup<FluidOutputInventory> fci_dict, ASMFluidRecipe recipe)
    //{
    //    for (int i = 0; i < fluid_refs.Length; ++i)
    //    {
    //        var input_container = fci_dict[fluid_refs[i].value];
    //        input_container.amount += recipe.output_count[i];
    //        fci_dict[fluid_refs[i].value] = input_container;

    //    }
    //}
}
/// <summary>
/// also the output inventoryd
/// </summary>
//public struct FluidInventory : IComponentData
//{
//    public const int MaxFluidTypes = 4; // per container
//    public uint types;
//    public ulong volumes;
//}


// not on fluid container entities, attached to machines
[System.SerializableAttribute]
public struct ASMFluidRecipe : IComponentData
{
    public const int MaxFluidTypes = 4; // per recipe
    unsafe public fixed byte src_types[MaxFluidTypes];
    unsafe public fixed byte src_counts[MaxFluidTypes];
    //public byte src_count_0, src_count_1, src_count_2, src_count_3;
    unsafe public fixed byte output_types[MaxFluidTypes];
    unsafe public fixed byte output_count[MaxFluidTypes];
    public byte max_time;
    //unsafe public fixed int srcs[4];
    //unsafe public ItemType src_types(int idx)
    //{
    //    if (idx < 0 || idx >= 4)
    //        return ItemType.None;

    //    return (ItemType)src_type[idx];
    //}


    //unsafe public int src_counts(int idx)
    //{
    //    if (idx < 0 || idx >= 4)
    //        return 0;

    //    return src_count[idx];
    //}
    //public void dbg_set(ItemType itemtype, int count)
    //{
    //    unsafe
    //    {
    //        src_type[0] = (ushort)itemtype;
    //        src_count[0] = (byte)count;
    //    }
    //}

    //public bool is_exist()
    //{
    //    return src_types(0) != ItemType.None;
    //}

    //public bool is_equals(ASMRecipe_Ntv recipe)
    //{
    //    for (int i = 0; i < 4; i++)
    //    {
    //        if (src_types(i) != recipe.src_types(i))
    //            return false;
    //    }

    //    if (output_type != recipe.output_type)
    //        return false;

    //    return true;
    //}
}