using Unity.Collections;
using Unity.Networking.Transport;

public interface IAutoSerialized
{
    //public void callback(NetworkConnection sender, ref ServerMainSystem s_world);    
}
public interface IC2S_RPC
{
    public void callback(NetworkConnection sender, ref ServerMainSystem s_world);

}
public interface IS2C_RPC
{
    public void callback(NetworkConnection sender, ref ClientMainSystem c_world);
}
