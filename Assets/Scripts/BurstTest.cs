using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Policy;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;
using UnityEditor;
using UnityEngine;


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

[System.Serializable]
public partial struct cmpt0 : IAutoSerialized, IS2C_RPC
{

    public NativeArray<int> na;
    public NativeList<float> nl_floats;
    public void callback(NetworkConnection sender, ref ClientMainSystem s_world)
    {

    }

}
[System.Serializable]
public partial struct cmpt : IAutoSerialized, IC2S_RPC
{
    public int val;
    public int val2;
    public NativeArray<int> na;
    public NativeList<float> nl_floats;
    public void callback(NetworkConnection sender, ref ServerMainSystem s_world)
    {

    }

}
[System.Serializable]
public partial struct cmpt2 : IAutoSerialized, IC2S_RPC
{
    public float val;
    public byte val2;
    public NativeArray<int2> na;
    public void callback(NetworkConnection sender, ref ServerMainSystem s_world)
    {

    }

}

// bursted network helpers
[BurstCompile]
public partial class BNH
{
    
    public static void managed_rpc_update(NativeArray<NetworkConnection> m_Connections, NetworkDriver m_Driver, NetworkPipeline pl)
    {
        NativeList<byte> buffer = new NativeList<byte>(1024, Allocator.Temp);
        for (int i = 0; i < m_Connections.Length; i++)
        {
            DataStreamReader stream;
            NetworkEvent.Type cmd;
            while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                var conn = m_Connections[i];
                if (cmd == NetworkEvent.Type.Data)
                {
                    NativeArray<byte> tmp = new NativeArray<byte>(stream.Length, Allocator.Temp);
                    stream.ReadBytes(tmp);

                    buffer.Clear();
                    buffer.AddRange(tmp);
                    int offset = 0;
                    Bursted.ud_struct(buffer, out int type_hash, ref offset);

                    //rpc_switch(type_hash, ref offset, buffer, conn, m_Driver, pl);
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    //Debug.Log("Client disconnected from the server.");
                    m_Connections[i] = default;
                    break;
                }
            }
        }
    }

    
    [BurstCompile]
    public static void bursted_rpc_update(ref NativeList<NetworkConnection> m_Connections, ref NetworkDriver m_Driver, ref NetworkPipeline pl, ref SystemState state, ref ServerMainSystem sworld)
    {
        NativeList<byte> buffer = new NativeList<byte>(1024, Allocator.Temp);
        for (int i = 0; i < m_Connections.Length; i++)
        {
            DataStreamReader stream;
            NetworkEvent.Type cmd;
            while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                var conn = m_Connections[i];
                if (cmd == NetworkEvent.Type.Data)
                {
                    NativeArray<byte> tmp = new NativeArray<byte>(stream.Length, Allocator.Temp);
                    stream.ReadBytes(tmp);

                    buffer.Clear();
                    buffer.AddRange(tmp);
                    int offset = 0;
                    Bursted.ud_struct(buffer, out int type_hash, ref offset);

                    rpc_switch(type_hash, ref offset, conn, buffer, ref sworld);
                   
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    //Debug.Log("Client disconnected from the server.");
                    m_Connections[i] = default;
                    break;
                }
            }
        }
    }

}
