using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
public interface IDBGCMP_cd<T>
{

}
[System.SerializableAttribute]
public struct ASMRecipe_Ntv
{
    unsafe public fixed ushort src_type[4];
    unsafe public fixed byte src_count[4];
    public ItemType output_type;
    public ItemType output_type2;

    public byte output_batch;
    public byte output_batch2;
    public short max_time;

    //unsafe public fixed int srcs[4];
    unsafe public ItemType src_types(int idx)
    {
        if (idx < 0 || idx >= 4)
            return ItemType.None;

        return (ItemType)src_type[idx];
    }


    unsafe public int src_counts(int idx)
    {
        if (idx < 0 || idx >= 4)
            return 0;

        return src_count[idx];
    }
    public void dbg_set(ItemType itemtype, int count)
    {
        unsafe
        {
            src_type[0] = (ushort)itemtype;
            src_count[0] = (byte)count;
        }
    }

    public bool is_exist()
    {
        return src_types(0) != ItemType.None;
    }

    public bool is_equals(ASMRecipe_Ntv recipe)
    {
        for (int i = 0; i < 4; i++)
        {
            if (src_types(i) != recipe.src_types(i))
                return false;
        }

        if (output_type != recipe.output_type)
            return false;

        return true;
    }
}
public partial struct MachineOutputInventory : IComponentData, IDBGCMP_cd<MachineOutputInventory>
{
    public ushort item_type;
    //public int count;
    public short count;
    //public short count_short;

    public ushort item_type2;
    public short count2;
    //public int count { get { return count_short; }set { count_short = (short)value; } }
    public void ResetForSchematic()
    {
        count = 0;
        count2 = 0;
    }
    public bool IsFull(int limit)
    {
        return count >= limit || count2 >= limit;
    }

    //public void cmp(ComponentLookup<MachineOutputInventory> cd_dict, Entity e0, Entity e1, NativeList<DiffRecord> records)
    //{
    //    MachineOutputInventory moi0 = cd_dict[e0];
    //    MachineOutputInventory moi1 = cd_dict[e1];
    //    if (moi0.item_type != moi1.item_type || moi0.item_type2 != moi1.item_type2)
    //    {
    //        records.Add(new DiffRecord() { diff_value = moi1.item_type, type = cmp_types.MOI, entity0 = e0, entity1 = e1, param0 = moi0.item_type });
    //    }

    //}
}
unsafe public partial struct AssemblerInputInventory : IComponentData, IDBGCMP_cd<AssemblerInputInventory>
{
    public const int MaxCount = 4; // max number of ingredients
    public const int MaxOutputStack = 5; // max output stack. not used in TILE
    public const int MaxInputBatchReserved = 2;

    public fixed byte src_item_count[4];
    //public ulong cached_filter; // keeping this for compatibility
#if !ASM_QUEUE
    //public int current_stack; // output
#endif
    public ASMRecipe_Ntv recipe;
    public void Reset()
    {
        src_item_count[0] = src_item_count[1] = src_item_count[2] = src_item_count[3] = 0;
    }
   
    public int src_count(int idx)
    {
        return src_item_count[idx];
    }
//    public bool ShouldTake(int idx)
//    {
//#if ASM_QUEUE
//        return recipe.src_types(idx) != 0 &&  // if the ith ingredient is of empty, it should not take anything.
//            src_count(idx) <= recipe.src_counts(idx) * 2; // if the input slot has enough already.
//#else
//        return //current_stack <= 10 && 
//            recipe.src_types(idx) != 0 &&  // if the ith ingredient is of empty, it should not take anything.
//            src_count(idx) <= recipe.src_counts(idx) * MaxOutputStack; // if the input slot has enough already.
//#endif
//    }

    public void RecalculateFilter()
    {
        //cached_filter = 0;
        //for (int i = 0; i < MaxCount; ++i)
        //{
        //    if (ShouldTake(i))
        //    {
        //        InserterInventory.SetFilterByIndex(ref cached_filter, i, (int)recipe.src_types(i));
        //    }
        //}
    }

    public bool contains_source(int item_type)
    {
        if ((int)recipe.src_type[0] == item_type)
        {
            return true;
        }
        else if ((int)recipe.src_type[1] == item_type)
        {
            return true;
        }
        else if ((int)recipe.src_type[2] == item_type)
        {
            return true;
        }
        else if ((int)recipe.src_type[3] == item_type)
        {
            return true;
        }
        return false;

    }
    public int NeedItemCount(int item_type, int count)
    {
        if ((int)recipe.src_type[0] == item_type)
        {
            var tmp = 255 - count - src_item_count[0];
            if (tmp < 0)
                return count + tmp;
            else
                return count;
        }
        else if ((int)recipe.src_type[1] == item_type)
        {
            var tmp = 255 - count - src_item_count[1];
            if (tmp < 0)
                return count + tmp;
            else
                return count;
        }
        else if ((int)recipe.src_type[2] == item_type)
        {
            var tmp = 255 - count - src_item_count[2];
            if (tmp < 0)
                return count + tmp;
            else
                return count;
        }
        else if ((int)recipe.src_type[3] == item_type)
        {
            var tmp = 255 - count - src_item_count[3];
            if (tmp < 0)
                return count + tmp;
            else
                return count;
        }
        return 0;
    }

    public int NeedItemPlural(int item_type, int multiplier)
    {
        if ((int)recipe.src_type[0] == item_type)
        {
            return math.min(recipe.src_count[0] * multiplier, 255) - src_item_count[0];
        }
        else if ((int)recipe.src_type[1] == item_type)
        {
            return math.min(recipe.src_count[1] * multiplier, 255) - src_item_count[1];
        }
        else if ((int)recipe.src_type[2] == item_type)
        {
            return math.min(recipe.src_count[2] * multiplier, 255) - src_item_count[2];
        }
        else if ((int)recipe.src_type[3] == item_type)
        {
            return math.min(recipe.src_count[3] * multiplier, 255) - src_item_count[3];
        }
        return 0;
    }

    public int NeedItemPlural_indexcached(int item_type, int multiplier, out int index_out)
    {
        if (recipe.src_type[0] == item_type)
        {
            index_out = 0;
            return recipe.src_count[0] * multiplier - src_item_count[0];
        }
        else if (recipe.src_type[1] == item_type)
        {
            index_out = 1;
            return recipe.src_count[1] * multiplier - src_item_count[1];
        }
        else if (recipe.src_type[2] == item_type)
        {
            index_out = 2;
            return recipe.src_count[2] * multiplier - src_item_count[2];
        }
        else if (recipe.src_type[3] == item_type)
        {
            index_out = 3;
            return recipe.src_count[3] * multiplier - src_item_count[3];
        }
        index_out = -1;
        return 0;
    }
    public void PushPlural_indexed(int index, byte count)
    {
        src_item_count[index] += count;
    }

    public void PushPlural(int item_type, int count)
    {
        if ((int)recipe.src_type[0] == item_type)
        {
            src_item_count[0] += (byte)count;
        }
        else if ((int)recipe.src_type[1] == item_type)
        {
            src_item_count[1] += (byte)count;
        }
        else if ((int)recipe.src_type[2] == item_type)
        {
            src_item_count[2] += (byte)count;
        }
        else if ((int)recipe.src_type[3] == item_type)
        {
            src_item_count[3] += (byte)count;
        }
    }
    

    public bool can_start()
    {
        if (recipe.src_type[0] != (ushort)ItemType.None && src_item_count[0] < recipe.src_count[0])
        {
            return false;
        }
        if (recipe.src_type[1] != (ushort)ItemType.None && src_item_count[1] < recipe.src_count[1])
        {
            return false;
        }
        if (recipe.src_type[2] != (ushort)ItemType.None && src_item_count[2] < recipe.src_count[2])
        {
            return false;
        }
        if (recipe.src_type[3] != (ushort)ItemType.None && src_item_count[3] < recipe.src_count[3])
        {
            return false;
        }
        // #if ASM_QUEUE
        // #else
        //         if (current_stack >= MaxOutputStack) return false;
        // #endif
        if (recipe.output_type == 0) return false;
        return true;
    }

    public bool has_item_count()
    {
        if (recipe.output_type == 0) return false;

        if (recipe.src_type[0] != (ushort)ItemType.None && src_item_count[0] > 0)
        {
            return true;
        }
        if (recipe.src_type[1] != (ushort)ItemType.None && src_item_count[1] > 0)
        {
            return true;
        }
        if (recipe.src_type[2] != (ushort)ItemType.None && src_item_count[2] > 0)
        {
            return true;
        }
        if (recipe.src_type[3] != (ushort)ItemType.None && src_item_count[3] > 0)
        {
            return true;
        }
        
        return false;
    }

    public void consume()
    {
        src_item_count[0] -= recipe.src_count[0];
        src_item_count[1] -= recipe.src_count[1];
        src_item_count[2] -= recipe.src_count[2];
        src_item_count[3] -= recipe.src_count[3];
        //RecalculateFilter();
    }

    //public void cmp(ComponentLookup<AssemblerInputInventory> cd_dict, Entity e0, Entity e1, NativeList<DiffRecord> records)
    //{
    //    AssemblerInputInventory aii0 = cd_dict[e0];
    //    AssemblerInputInventory aii1 = cd_dict[e1];
    //    if(aii0.recipe.output_type != aii1.recipe.output_type)
    //    {
    //        records.Add(new DiffRecord() { diff_value = (int)aii1.recipe.output_type, type = cmp_types.AII, entity0 = e0, entity1 = e1, param0 = (int)aii0.recipe.output_type });
    //    }

    //}
}
public partial struct DiscreteEnergyStates : IComponentData
{
    public const int MaxStack = 10; // for automation interactions
    public ushort cached_fuel; // fuel type
    public ushort in_stock;
    public ushort current;
    public ushort max; // indicates the fuel's energy densities.
    public ushort timeleft; // this replaces assembler time left on fuel-based machines.
    public void consume_fuel()
    {
        if (current > 0)
        {
            current--;
        }
        else if (in_stock > 0)
        {
            in_stock--;
            current = (ushort)(max - 1);
        }
    }

    public bool can_consume()
    {
        return (current > 0 || in_stock > 0);
    }

    public int need_fuel(int item_type)
    {
        if (cached_fuel == item_type)
        {
            return math.max(MaxStack - in_stock, 0);
        }
        else if (cached_fuel == 0 || (in_stock == 0 && current == 0))
        {
            return MaxStack; // just going to assume the in_stock is 0.
        }
        return 0;
    }

    public int need_fuel_manual(int item_type, int count)
    {
        count = math.min(10, count);
        if (cached_fuel == item_type)
        {
            return math.max(count - in_stock, 0);
        }
        else if (cached_fuel == 0 || (in_stock == 0 && current == 0))
        {
            return count; // just going to assume the in_stock is 0.
        }
        return 0;
    }

    public bool is_fuel_empty()
    {
        return cached_fuel == 0 || (in_stock == 0 && current == 0);
    }

    // must be protected by need_fuel, because it does not check for putting in a different fuel.
    public void push_fuels(ushort item_type, ushort count)
    {
        if (cached_fuel == 0 || (in_stock == 0 && current == 0))
        {
            bool require_refresh = cached_fuel != item_type;
            cached_fuel = item_type;
            in_stock = count;
            if (require_refresh)
            {
                refresh_energy_value();
            }
        }
        else
        {
            in_stock += count;
        }
    }
    void refresh_energy_value()
    {
        //for (int i = 0; i < ASMConstants.EnergyValues.Length; ++i)
        //{
        //    if ((ushort)ASMConstants.EnergyItems[i] == cached_fuel)
        //    {
        //        max = ASMConstants.EnergyValues[i];
        //        return;
        //    }
        //}
        max = 0;
    }
}

public struct DiscreteEnergySubTime : IComponentData
{
    public float value;
}
public struct PlayerID_CD:IComponentData
{
    public int value;
}
public partial struct CachedWorkingStates : IComponentData
{
    public half operation_speed;
    public byte value;
}
public enum ItemType : ushort
{
    None = 0,
    CommonMetalOre = 1,
    IronPlate = 2,
    Total,
}

[System.Serializable]
[InternalBufferCapacity(4)]
public struct ResourceNodeOutputStates : IBufferElementData
{
    public ItemType item_type;
    public ushort count_per_second;
}
public partial struct ASMEntityGUID : IComponentData
{
    public uint value; // zero means Entity.Null, valid guids are never zero.
}
public struct ResourceNodeStates : IComponentData
{
    public int level_req;
    public int session_duration;
    public float output_multiplier;
}

public partial struct AssemblerTimeLeft : IComponentData
{
    public int value;
}

public struct ResourceNodeRemaining : IComponentData, IDBGCMP_cd<ResourceNodeRemaining>
{
    public int value;

}

// change this to singular?
[InternalBufferCapacity(4)]
public partial struct ExtractorCoverTargetElement : IBufferElementData
{
    public Entity value;
    //public ASMEntityGUID guid;
    public void set_ref_entity(Entity e) { value = e; }
    public Entity get_ref_entity() { return value; }

}
public partial struct PowerGeneratorStates : IComponentData
{
    public int release_efficiency; // in watt, or joule/second
    public int charging_efficiency; // in watt
    public int unit_efficiency; // in watt, or joule/second
}
public partial struct ProductionModifier : IComponentData
{
    public int actual_joule;
    public int cached_watt_expected; // in watt, or joule/second
    public int idle_consumption; // constant until modified by chips.
    public int full_consumption; // constant until modified by chips, or player settings.
    public const int MaxDiscreteLevel = 4;
    public bool is_at_full()
    {
        return actual_joule == cached_watt_expected;
    }

    public float DiscreteCapacityWatt()
    {
        return (float)actual_joule;
    }
}
// use this on power connectors.
[InternalBufferCapacity(4)]
public partial struct PowerConnectorAdj : IBufferElementData
{
    public Entity adjacent_connector;
    public void set_ref_entity(Entity e) { adjacent_connector = e; }
    public Entity get_ref_entity() { return adjacent_connector; }
   
}
public partial struct PowerConnectorRef : IComponentData
{
    public Entity value;
    public void set_ref_entity(Entity e) { value = e; }
    public Entity get_ref_entity() { return value; }


    public void Deserialize(NativeHashMap<uint, Entity> entity_dict, UnsafeList<byte> buffer, ref int offset)
    {
        uint guid = 0;
        Bursted.ud_struct(buffer, out guid, ref offset);
        entity_dict.TryGetValue(guid, out value);
    }
    public void Serialize(NativeList<byte> buffer, ComponentLookup<ASMEntityGUID> lookup)
    {
        uint guid = 0;
        if (lookup.HasComponent(value))
            guid = lookup[value].value;
        Bursted.ns_generic(buffer, guid);
    }
    public void serialize_ref(NativeList<byte> buffer, ComponentLookup<ASMEntityGUID> lookup)
    {
        if (lookup.HasComponent(value))
            Bursted.ns_generic(buffer, lookup[value]);
        else
            Bursted.ns_generic(buffer, new ASMEntityGUID());
    }
}

// attached to all power units. connectors, generators and users.
public partial struct PowerGridRef : IComponentData, IDBGCMP_cd<PowerGridRef>
{
    public uint value; // grid index.
    //public void cmp(ComponentLookup<PowerGridRef> cd_dict, Entity e0, Entity e1, NativeList<DiffRecord> records)
    //{
    //    var moi0 = cd_dict[e0];
    //    var moi1 = cd_dict[e1];
    //    if (moi0.value != moi1.value)
    //    {
    //        records.Add(new DiffRecord() { diff_value = (int)moi1.value, type = cmp_types.powergrid, entity0 = e0, entity1 = e1, param0 = (int)moi0.value });
    //    }

    //}
}
public struct MeshGORef:IComponentData
{
    public Entity value;
}
public partial struct GalacticType : IComponentData
{
    public GTypes value;
    public byte level;
    public void set_resource_type(int val)
    {

    }
}
public enum GTypes : byte
{
    None = 0, Assembler,
    Balancer,
    Cargoship,
    CombatZone,
    DelayedDamage,
    Extractor,
    HashMachine,
    Job,
    Launcher,
    Notification,
    ObstacleGroup,
    PlayerBase,
    Refinery,
    ResourceNode,
    Router,
    schematics_ghost,
    schematics_root,
    Tower,
    Box,
    PowerStation,
    Chip,
    GZone,
    SolarPanel,
    Generator,
    CRS,  // to be renamed to LDLogisticsStation, 'long-distance logistics station'
    Transporter,
    CommandCenter,
    Total
}