using Unity.Collections;
using Unity.Networking.Transport;

public interface IAutoSerialized
{
    public void callback(NetworkConnection sender, ref ServerMainSystem s_world);    
}
