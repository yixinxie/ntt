using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using System.Security.Cryptography;

public class SimpleECSMono:MonoBehaviour
{
    public NTTManager world;
    void Start()
    {
        //unsafe
        {
            //SimpleECS* pworld = &world;
            world.init();
            Ntt e0 = world.create_ntt();
            world.ACD<testcd>(e0);
            world.SCD(e0, new testcd() { value = 321 });
            var t = world.GCD<testcd>(e0);
            Debug.Log(t.value);
            Debug.Log("arch count:" + world.get_archetype_count());
            world.ACD<testcd2>(e0);
            world.SCD(e0, new testcd2() { value = 12f });

            var t2 = world.GCD<testcd2>(e0);
            Debug.Log(t2.value);
            t = world.GCD<testcd>(e0);
            Debug.Log(t.value);
            Debug.Log("arch count:" + world.get_archetype_count());
            world.DestroyNtt(e0);
            
        }
    }
    private void OnDestroy()
    {
        world.dispose();
    }

}
public struct Ntt
{
    public int Index;
    public int Version;
}
unsafe public struct NTTManager
{
    //NativeList<int> Ntt_indices;
    UnsafeList<int> Ntt_versions;
    UnsafeList<NttArchetypeInfo> archetypes_by_ntt; // each entity has one
    UnsafeList<int> removed_ids;

    
    UnsafeHashMap<int, ArchetypeDef> archetype_hashes2defs; // archetype hash keys, archetype def

    UnsafeHashMap<int, DataBlock> data_blocks; // key: archetype hash key, value: data block
    UnsafeHashMap<ushort, int> cached_component_sizes; // key: component typeid, value: component sizeof
    // caching
    UnsafeHashMap<ushort, UnsafeHashSet<int>> component_types2archetype_map; // key: component typeid, value: hash set of archetype hash keys.
    int cursor;
    public int get_archetype_count()
    {
        return archetype_hashes2defs.Count;
    }
    ArchetypeDef create_archetype(ArchetypeDef previous, UnsafeList<ushort> typeids)
    {
        //var hash = NttArchetypeDef.hash(previous.component_ids);

        var new_hash = ArchetypeDef.hash(previous, typeids);

        if (archetype_hashes2defs.TryGetValue(new_hash, out ArchetypeDef newdef) == false)
        {
            newdef = new ArchetypeDef();
            newdef.component_ids = new UnsafeList<ushort>(4, Allocator.Persistent);

            if(previous.component_ids.IsCreated)
                newdef.component_ids.AddRange(previous.component_ids);
            newdef.component_ids.AddRange(typeids);
            newdef.refresh(cached_component_sizes);

            archetype_hashes2defs.Add(new_hash, newdef);

            // type registration for query caching.
            for (int i = 0; i < newdef.component_ids.Length;++i)
            {
                var cid = newdef.component_ids[i];
                if(component_types2archetype_map.TryGetValue(cid, out UnsafeHashSet<int> archetype_hashes))
                {
                    archetype_hashes.Add(new_hash);
                    component_types2archetype_map[cid] = archetype_hashes;
                }
                else
                {
                    archetype_hashes = new UnsafeHashSet<int>(4, Allocator.Persistent);
                    archetype_hashes.Add(new_hash);
                    component_types2archetype_map.Add(cid, archetype_hashes);
                }
            }
        }
        
        return newdef;
    }
    void internal_component_data_copy(ArchetypeDef source, int index_in_source, ArchetypeDef target, int index_in_target)
    {
        var kvarray = source.component_offsets.GetKeyValueArrays(Allocator.Temp);
        var datablock_from = data_blocks[source.cached_hash];
        var datablock_target = data_blocks[target.cached_hash];
        for (int i = 0; i < kvarray.Length; ++i)
        {
            
            var key = kvarray.Keys[i];
            var val = kvarray.Values[i];
            ushort typeid = key;
            int component_offset = val;

            if (target.component_offsets.TryGetValue(typeid, out int component_offset_in_target) == false)
            {
                continue;
            }

            if(cached_component_sizes.TryGetValue(typeid, out int type_sizeof) == false)
            {
                continue;
            }
            void* from_ptr = datablock_from.raw_data.Ptr + index_in_source * datablock_from.stride + component_offset;
            void* target_ptr = datablock_target.raw_data.Ptr + index_in_target * datablock_target.stride + component_offset_in_target;
            UnsafeUtility.MemCpy(target_ptr, from_ptr, type_sizeof);
            //UnsafeUtility.CopyPtrToStructure(ptr, out data_out);
        }
    }
    public void init()
    {
        Ntt_versions = new UnsafeList<int>(1024, Allocator.Persistent);
        removed_ids = new UnsafeList<int>(1024, Allocator.Persistent);
        archetypes_by_ntt = new UnsafeList<NttArchetypeInfo>(1024, Allocator.Persistent);
        data_blocks = new UnsafeHashMap<int, DataBlock>(64, Allocator.Persistent);
        cached_component_sizes = new UnsafeHashMap<ushort, int>(64, Allocator.Persistent);
        archetype_hashes2defs = new UnsafeHashMap<int, ArchetypeDef>(64, Allocator.Persistent);
        component_types2archetype_map = new UnsafeHashMap<ushort, UnsafeHashSet<int>>(64, Allocator.Persistent);
        // generated registrations
        cached_component_sizes.Add(new testcd().typeid(), sizeof(testcd));
        cached_component_sizes.Add(new testcd2().typeid(), sizeof(testcd2));
    }
    public void dispose()
    {
        Ntt_versions.Dispose();
        removed_ids.Dispose();
        archetypes_by_ntt.Dispose();
        {
            var values = data_blocks.GetValueArray(Allocator.Temp);
            for (int i = 0; i < values.Length; ++i)
            {
                values[i].dispose();
            }
        }
        data_blocks.Dispose();
        cached_component_sizes.Dispose();
        {
            var values = archetype_hashes2defs.GetValueArray(Allocator.Temp);
            for(int i = 0; i < values.Length; ++i)
            {
                values[i].dispose();
            }
        }
        archetype_hashes2defs.Dispose();
        var value_array = component_types2archetype_map.GetValueArray(Allocator.Temp);
        for(int i = 0; i < value_array.Length; ++i)
        {
            value_array[i].Dispose();
        }
        component_types2archetype_map.Dispose();
    }
    public Ntt create_ntt()
    {
        Ntt ret;
        int id2use;
        if (removed_ids.Length > 0)
        {
            id2use = removed_ids[removed_ids.Length - 1];
            removed_ids.RemoveAtSwapBack(removed_ids.Length - 1);
            
        }
        else
        {
            id2use = ++cursor;
            int count2add = cursor - Ntt_versions.Length + 1;
            if (count2add > 0)
            {
                Ntt_versions.AddReplicate(1, count2add);
                archetypes_by_ntt.AddReplicate(default, count2add);
            }
        }
        
        //Ntt_versions[id2use]++;
        
        ret.Index = id2use;
        ret.Version = Ntt_versions[id2use];

        return ret;
    }
    public void ACD<T>(Ntt ntt) where T : unmanaged, INttCD
    {
        UnsafeList<ushort> tmp = new UnsafeList<ushort>(1, Allocator.Temp);
        tmp.Add(new T().typeid());
        AddComponents(ntt, tmp);
    }
    public void AddComponents(Ntt ntt, UnsafeList<ushort> typeids)
    {
        // get existing archetype
        var per_ntt = archetypes_by_ntt[ntt.Index];

        archetype_hashes2defs.TryGetValue(per_ntt.archetype_hash, out ArchetypeDef existing_archetype);

        var new_type = create_archetype(existing_archetype, typeids);
        
        var index_in_prev = per_ntt.index_in_data_block;
        
        if (data_blocks.TryGetValue(new_type.cached_hash, out DataBlock target_block) == false)
        {
            target_block = new DataBlock();

            var component_ids = new_type.component_ids;
            int stride = 0;
            for (int i = 0; i < component_ids.Length; ++i)
            {
                var typeid = component_ids[i];
                if (cached_component_sizes.TryGetValue(typeid, out int component_size))
                {
                    stride += component_size;
                }
            }

            target_block.init(stride);
            data_blocks.Add(new_type.cached_hash, target_block);
        }
        int index_in_new_block = target_block.alloc();
        if(data_blocks.TryGetValue(existing_archetype.cached_hash, out DataBlock previous_block))
        {
            internal_component_data_copy(existing_archetype, index_in_prev, new_type, index_in_new_block);
            previous_block.remove(index_in_prev);
            data_blocks[new_type.cached_hash] = previous_block;
        }

        per_ntt.index_in_data_block = index_in_new_block;
        //per_ntt.archetype = &new_type;
        //per_ntt.data_block = &target_block;

        per_ntt.archetype_hash = new_type.cached_hash;

        archetypes_by_ntt[ntt.Index] = per_ntt;

    }
    public void DestroyNtt(Ntt ntt)
    {
        removed_ids.Add(ntt.Index);
        Ntt_versions[ntt.Index]++;

        var per_ntt = archetypes_by_ntt[ntt.Index];
        if(data_blocks.TryGetValue(per_ntt.archetype_hash, out DataBlock dblock))
        {
            dblock.remove(per_ntt.index_in_data_block);
            data_blocks[per_ntt.archetype_hash] = dblock;
        }
        archetypes_by_ntt[ntt.Index] = default;
    }
    public int GetComponentOffset(Ntt ntt, ushort typeid)
    {
        var archetype_info = archetypes_by_ntt[ntt.Index];
        if (archetype_hashes2defs.TryGetValue(archetype_info.archetype_hash, out ArchetypeDef def))
        {
            if (def.component_offsets.TryGetValue(typeid, out int offset))
                return offset;
        }
        return -1;
    }
    public int GetComponentOffset<T>(Ntt ntt) where T: unmanaged, INttCD
    {
        T tmp = default;
        var archetype_info = archetypes_by_ntt[ntt.Index];
        if (archetype_hashes2defs.TryGetValue(archetype_info.archetype_hash, out ArchetypeDef def))
        {
            if (def.component_offsets.TryGetValue(tmp.typeid(), out int offset))
                return offset;
        }
        return -1;
    }

    public T GCD<T>(Ntt ntt) where T:unmanaged, INttCD
    {
        T data_out = default;
        ushort typeid = data_out.typeid();
        int component_offset = GetComponentOffset(ntt, typeid);
        if (component_offset >= 0)
        {
            //var data = data_store[typeid];
            {
                var archetype = archetypes_by_ntt[ntt.Index];
                if (data_blocks.TryGetValue(archetype.archetype_hash, out DataBlock data_block))
                {
                    //var index_in_cgd = icg.indices_in_component_group;
                    void* ptr = data_block.raw_data.Ptr + archetype.index_in_data_block * data_block.stride + component_offset;
                    UnsafeUtility.CopyPtrToStructure(ptr, out data_out);
                }
            }
        }
        return data_out;
    }

    public void SCD<T>(Ntt ntt, T data_in) where T : unmanaged, INttCD
    {
        ushort typeid = data_in.typeid();
        var archetype = archetypes_by_ntt[ntt.Index];
        if (data_blocks.TryGetValue(archetype.archetype_hash, out DataBlock data_block))
        {
            int component_offset = GetComponentOffset(ntt, typeid);
            if (component_offset >= 0)
            {
                void* ptr = data_block.raw_data.Ptr + archetype.index_in_data_block * data_block.stride + component_offset;
                UnsafeUtility.CopyStructureToPtr(ref data_in, ptr);
                data_blocks[archetype.archetype_hash] = data_block;
            }
        }
    }
}
unsafe public struct NttArchetypeInfo // one per Ntt
{
    //public ArchetypeDef* archetype;
    //public DataBlock* data_block;
    public int archetype_hash;
    //public int datablock_hash;
    public int index_in_data_block;
}

unsafe public struct ArchetypeDef // multiple entities can share one archetypedef.
{
    public UnsafeList<ushort> component_ids;
    public UnsafeHashMap<ushort, int> component_offsets; // component typeid, offset
    public int cached_hash;
    public void dispose()
    {
        component_ids.Dispose();
        component_offsets.Dispose();
    }
    public void refresh(UnsafeHashMap<ushort, int> typeids2sizes)
    {
        component_ids.Sort();
        if(component_offsets.IsCreated)
        {
            component_offsets.Dispose();
        }
        component_offsets = new UnsafeHashMap<ushort, int>(component_ids.Length, Allocator.Persistent);
        int offset_incre = 0;
        for(int i = 0; i < component_ids.Length; ++i)
        {
            var typeid = component_ids[i];
            if(typeids2sizes.TryGetValue(typeid, out int component_size))
            {
                component_offsets.Add(typeid, offset_incre);
                offset_incre += component_size;
            }
        }
        cached_hash = hash(component_ids);
    }
    public static int hash(UnsafeList<ushort> component_ids)
    {
        unchecked
        {
            const int p = 16777619;
            int hash = (int)2166136261;

            for (int i = 0; i < component_ids.Length; i++)
                hash = (hash ^ component_ids[i]) * p;

            return hash;
        }
    }
    public static int hash(ArchetypeDef def, UnsafeList<ushort> more)
    {
        UnsafeList<ushort> tmp;
        if (def.component_ids.IsCreated == false)
        {
            tmp = new UnsafeList<ushort>(1, Allocator.Temp);
        }
        else
        {
            tmp = new UnsafeList<ushort>(def.component_ids.Length + 1, Allocator.Temp);
            tmp.AddRange(def.component_ids.Ptr, def.component_ids.Length);
        }
        tmp.AddRange(more);
        tmp.Sort();
        unchecked
        {
            const int p = 16777619;
            int hash = (int)2166136261;

            for (int i = 0; i < tmp.Length; i++)
                hash = (hash ^ tmp[i]) * p;

            return hash;
        }
    }
}

unsafe public struct DataBlock
{
    public int stride;
    public int cursor;
    public UnsafeList<byte> raw_data;
    public UnsafeList<int> removed_ids;
    public void init(int _stride)
    {
        stride = _stride;
        raw_data = new UnsafeList<byte>(1024, Allocator.Persistent);
        removed_ids = new UnsafeList<int>(64, Allocator.Persistent);
    }
    public void dispose()
    {
        raw_data.Dispose();
        removed_ids.Dispose();
    }
    public void remove(int idx)
    {
        removed_ids.Add(idx);
    }
    public int alloc()
    {
        int id2use;
        if(removed_ids.Length > 0)
        {
            id2use = removed_ids[removed_ids.Length - 1];
            removed_ids.RemoveAtSwapBack(removed_ids.Length - 1);
        }
        else
        {
            id2use = cursor++;

            //raw_data.Resize(cursor, NativeArrayOptions.ClearMemory);
            raw_data.AddReplicate(0, cursor * stride - raw_data.Length);
            //while (raw_data.Length <= id2use)
            //{
            //}
        }
        UnsafeUtility.MemSet(raw_data.Ptr + id2use * stride, 0, stride);
        
        return id2use;
    }

}
public interface INttCD
{
    public ushort typeid();
}
public partial struct testcd : INttCD
{
    public ushort typeid()
    {
        return 123;
    }
}
public partial struct testcd
{
    public int value;
}

public partial struct testcd2
{
    public ushort typeid()
    {
        return 456;
    }
}
public partial struct testcd2 : INttCD
{
    public float value;
}



public struct asm_cell
{
    const uint ITEMTYPE_MASK = 1023;
    const uint ITEMCOUNT_MASK = 2047;
    const uint ITEMMAX_MASK = 2047;

    const int ITEMTYPE_SHIFT = 0;
    const int ITEMCOUNT_SHIFT = 9;
    const int ITEMMAX_SHIFT = 20;
    public static int itype(uint cell)
    {
        return (int)(cell & ITEMTYPE_MASK);
    }
    public static void itype_set(ref uint cell, int itemtype)
    {
        uint_mark(ref cell, 0, (uint)itemtype, ITEMTYPE_MASK);
    }
    static void uint_mark(ref uint current, int position, uint val, uint mask)
    {
        uint _mask = ~mask;
        current = (current & _mask) + (val << position);
    }

    public static int icount(uint cell)
    {
        return (int)((cell >> ITEMCOUNT_SHIFT) & ITEMCOUNT_MASK);
    }
    public static void icount_set(ref uint cell, int icount)
    {
        uint_mark(ref cell, ITEMCOUNT_SHIFT, (uint)icount, ITEMCOUNT_MASK);
    }

    public static int imax(uint cell)
    {
        return (int)((cell >> ITEMMAX_SHIFT) & ITEMMAX_MASK);
    }
    public static void imax_set(ref uint cell, int icount)
    {
        uint_mark(ref cell, ITEMMAX_SHIFT, (uint)icount, ITEMMAX_MASK);
    }
}