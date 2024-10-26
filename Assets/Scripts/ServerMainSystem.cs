using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Entities;
using Unity.Burst;

[BurstCompile]
public partial struct ServerMainSystem : ISystem
{
    public NetworkDriver m_Driver;
    public NativeList<NetworkConnection> m_Connections;
    public NetworkPipeline pl;
    void OnCreate(ref SystemState state)
    {
        //NetworkSettings ns = new NetworkSettings();
#if TRANSPORT_TEST
        m_Driver = NetworkDriver.Create();
        //NativeArray<NetworkPipelineStageId> stages = new NativeArray<NetworkPipelineStageId>(1, Allocator.Temp);
        //stages[0] = new NetworkPipelineStageId() { }
        pl = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
        var endpoint = NetworkEndpoint.AnyIpv4.WithPort(7777);
        if (m_Driver.Bind(endpoint) != 0)
        {
            Debug.LogError("Failed to bind to port 7777.");
            return;
        }
        m_Driver.Listen();
#else
        state.Enabled = false;
#endif
    }

    void OnDestroy(ref SystemState state)
    {
        if (m_Driver.IsCreated)
        {
            m_Driver.Dispose();
            m_Connections.Dispose();
        }
    }
    //[BurstCompile]
    void OnUpdate(ref SystemState state)
    {
        m_Driver.ScheduleUpdate().Complete();
        // Clean up connections.
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {
                m_Connections.RemoveAtSwapBack(i);
                i--;
            }
        }

        // Accept new connections.
        NetworkConnection c;
        while ((c = m_Driver.Accept()) != default)
        {
            m_Connections.Add(c);
                
            //Debug.Log("Accepted a connection.");
        }

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

                    //ServerRPCs.switcher(type_hash, ref offset, ref conn, ref buffer, ref this, ref state);

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
