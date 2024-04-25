using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

public class BurstTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        TestBursted.test_em(ref em);
        Debug.Log(TestBursted.test_nastna());
    }
    public int dbg;
    // Update is called once per frame
    void Update()
    {
        UnsafeList<byte> list = new UnsafeList<byte>(8, Allocator.TempJob);
        dbg = TestBursted.bursted(ref list);
        list.Dispose();
    }

    
}
[BurstCompile]
public class TestBursted
{
    struct teststruct
    {
        public NativeArray<int> vals;
    }
    [BurstCompile]
    public static int test_nastna()
    {
        NativeArray<teststruct> structs = new NativeArray<teststruct>(3, Allocator.Temp);
        var tt = new teststruct();
        tt.vals = new NativeArray<int>(4, Allocator.Temp);
        tt.vals[0] = 1;
        structs[0] = tt;
        return structs[0].vals[0];
    }
    [BurstCompile]
    public static int bursted(ref UnsafeList<byte> buffer)
    {
        us_basic(ref buffer, 3);
        int offset = 0;
        ud_struct(buffer, out int val, ref offset);
        return val;
    }
    [BurstCompile]
    public static void test_em(ref EntityManager em)
    {
        var new_entity = em.CreateEntity();
        em.AddComponentData(new_entity, new TestICD() { value = 5 });
    }
    public static void ud_struct<T>(UnsafeList<byte> buffer, out T val, ref int offset) where T : unmanaged
    {
        unsafe
        {
            UnsafeUtility.CopyPtrToStructure(buffer.Ptr + offset, out val);
            offset += sizeof(T);
        }
    }
    public static void us_basic<T>(ref UnsafeList<byte> buffer, T val) where T : unmanaged
    {
        T tmp = val;
        us_struct(ref buffer, ref tmp);
    }
    public static void us_struct<T>(ref UnsafeList<byte> buffer, ref T val) where T : unmanaged
    {
        unsafe
        {
            int offset = buffer.Length;
            buffer.AddReplicate(0, sizeof(T));
            UnsafeUtility.CopyStructureToPtr(ref val, buffer.Ptr + offset);
        }
    }
}
public struct TestICD : IComponentData
{
    public int value;
}
