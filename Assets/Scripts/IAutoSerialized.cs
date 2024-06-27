using Unity.Collections;
using Unity.Networking.Transport;

public interface IAutoSerialized
{
    //public void serialize(NativeList<byte> raw);
    //public int recv(DataStreamReader stream, Allocator alloc = Allocator.Temp);
    //public NativeList<byte> pack(Allocator alloc);
    //public void unpack(NativeList<byte> buffer, ref int offset, Allocator alloc);
    public void callback(NetworkDriver nd, NetworkConnection sender, NetworkPipeline np);

    
}
// codegen
public partial class BNH
{
    public static void rpc_switch(int type_hash, ref int offset, NativeList<byte> buffer, NetworkConnection sender, NetworkDriver m_Driver, NetworkPipeline pl)
    {
        switch (type_hash)
        {
            case cmpt.type_hash:
                {
                    cmpt _data = default;
                    _data.unpack(buffer, ref offset, Allocator.Temp);
                    _data.callback(m_Driver, sender, pl);
                }
                break;
        }
    }

}
// codegen
public partial struct cmpt : IAutoSerialized
{
    public const int type_hash = 123;
    // managed
    public void send(NetworkDriver nd, NetworkConnection target, NetworkPipeline np)
    {
        NativeList<byte> buffer = pack(Allocator.Temp);

        nd.BeginSend(np, target, out var writer);
        writer.WriteBytes(buffer.AsArray());
        nd.EndSend(writer);
    }
    public NativeList<byte> pack(Allocator alloc)
    {
        NativeList<byte> buffer = new NativeList<byte>(32, alloc);
        Bursted.us_struct(buffer, type_hash);
        Bursted.us_struct_partial(buffer, ref this, 8);
        Bursted.us_na(buffer, na);
        Bursted.us_na(buffer, nl_floats);

        return buffer;
    }

    public void unpack(NativeList<byte> buffer, ref int offset, Allocator alloc)
    {
        Bursted.ud_struct_partial(buffer, ref this, 8, ref offset);
        //Bursted.ud_struct_partial(buffer, val, 56);
        Bursted.ud_na(buffer, out na, ref offset, alloc);
        Bursted.ud_na(buffer, out nl_floats, ref offset, alloc);
    }
}