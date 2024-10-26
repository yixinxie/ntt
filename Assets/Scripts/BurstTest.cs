using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;
using UnityEditor;
using UnityEngine;


[BurstCompile]
public class BurstTest
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

public partial struct cmpt0 : IAutoSerialized, IS2C_RPC
{
    public float f_val;
    public NativeArray<int> na;
    public NativeList<float> nl_floats;
    public void callback(NetworkConnection sender, ref ClientMainSystem s_world, ref SystemState sstate)
    {
        //Debug.Log("client receives " + f_val + ", " + nl_floats.Length);
    }
}
public partial struct cmpt : IAutoSerialized, IC2S_RPC
{
    public int val;
    public int val2;
    public NativeArray<int> na;
    public NativeList<float> nl_floats;
    public void callback(NetworkConnection sender, ref ServerMainSystem ctx, ref SystemState sstate)
    {
        //Debug.Log("server receives" + val);
        cmpt0 resp = default;
        resp.f_val = 1.235f;
        //resp.send(sender, ctx.pl, ctx.m_Driver);
    }

}
public partial struct cmpt2 : IAutoSerialized, IC2S_RPC
{
    public float val;
    public byte val2;
    public NativeArray<int2> na;
    public void callback(NetworkConnection sender, ref ServerMainSystem s_world, ref SystemState sstate)
    {
    }

}
[StructLayout(LayoutKind.Sequential)]
public partial struct c_cmpt0
{
    byte a;
    public double fval;
    public int ival;
    public NativeArray<float> fl_na;
    unsafe public void serialize(NativeList<byte> buffer)
    {
        fixed (void* pp2 = &fl_na)
        {
            fixed (void* pp = &a)
            {
                //IntPtr ip = (IntPtr*)pp;
                var aa = (int)pp2 - (int)pp;
                Debug.Log("addr diff " + aa);
            }
        }
        fixed (void* pp = &a)
        {


            int offset = buffer.Length;
            int len = 24;
            buffer.AddReplicate(0, len);
            //UnsafeUtility.copy(p, buffer.GetUnsafePtr() + offset);
            UnsafeUtility.MemCpy(buffer.GetUnsafePtr() + offset, pp, len);
        }
        //var ptr = Marshal.unsafe(val, 0);
    }

    unsafe public void deserialize(NativeList<byte> buffer, ref int offset)
    {
        fixed (void* pp = &a)
        {
            int len = 24;
            //UnsafeUtility.copy(p, buffer.GetUnsafePtr() + offset);
            UnsafeUtility.MemCpy(pp, buffer.GetUnsafePtr() + offset,  len);
            offset += len;
        }

        //fixed (void* pp = &val.fval)
        //{


        //    int offset = buffer.Length;
        //    int len = 8;
        //    buffer.AddReplicate(0, len);
        //    //UnsafeUtility.copy(p, buffer.GetUnsafePtr() + offset);
        //    UnsafeUtility.MemCpy(pp, buffer.GetUnsafePtr() + offset, len);
        //}
        //var ptr = Marshal.unsafe(val, 0);
    }
}

public partial struct RouterInventory : IBufferElementData
{
    public ushort item_type;
    public const int Stacking = 50;
    
    public ushort item_count {
        get {
            var tmp = ((item_blocks) * Stacking) + last_count;
            return (ushort)tmp;
        }
        set {
            item_blocks = (byte)(value / Stacking);
            last_count = (byte)(value % Stacking);
        }
    }
    public byte item_blocks;
    public byte last_count;
}