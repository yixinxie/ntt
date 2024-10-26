using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Entities;
using Unity.Burst;

[BurstCompile]
public partial struct ClientMainSystem : ISystem
{
    public NetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public NetworkPipeline pl;
    void OnCreate(ref SystemState state)
    {
        //NetworkSettings ns = new NetworkSettings();
#if TRANSPORT_TEST
        m_Driver = NetworkDriver.Create();

        pl = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
        var endpoint = NetworkEndpoint.LoopbackIpv4.WithPort(7777);
        m_Connection = m_Driver.Connect(endpoint);
        //var endpoint = NetworkEndpoint.AnyIpv4.WithPort(7777);
        //if (m_Driver.Bind(endpoint) != 0)
        //{
        //    Debug.LogError("Failed to bind to port 7777.");
        //    return;
        //}
        //m_Driver.Listen();
#else
        state.Enabled = false;
#endif
    }

    void OnDestroy(ref SystemState state)
    {
        if (m_Driver.IsCreated)
        {
            m_Connection.Disconnect(m_Driver);
            m_Driver.Dispose();
        }
    }
    //[BurstCompile]
    void OnUpdate(ref SystemState state)
    {
        m_Driver.ScheduleUpdate().Complete();
        if (!m_Connection.IsCreated)
        {
            return;
        }

        NativeList<byte> buffer = new NativeList<byte>(1024, Allocator.Temp);
        Unity.Collections.DataStreamReader stream;
        NetworkEvent.Type cmd;
        while ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                Debug.Log("We are now connected to the server.");


                cmpt a = default;
                a.val = 123;
                //a.send(m_Connection, pl, m_Driver);

            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                NativeArray<byte> tmp = new NativeArray<byte>(stream.Length, Allocator.Temp);
                stream.ReadBytes(tmp);

                buffer.Clear();
                buffer.AddRange(tmp);
                int offset = 0;
                Bursted.ud_struct(buffer, out int type_hash, ref offset);

                //ClientRPCs.switcher(type_hash, ref offset, ref m_Connection, ref buffer, ref this, ref state);


            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Client got disconnected from server.");
                m_Connection = default;
            }
        }


    }
}
