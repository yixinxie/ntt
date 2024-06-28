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
