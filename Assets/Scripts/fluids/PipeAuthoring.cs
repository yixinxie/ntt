using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public class PipeAuthoring: MonoBehaviour
{

    public class Bakery : Baker<PipeAuthoring>
    {
        public override void Bake(PipeAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            //AddCommonMachineComponents(this, entity);
            //AssemblerAuthoring.AddPowerConsumerCD(this, entity);
            //TileRouterAuthoring.AddDirectTransportComponents(this, entity, authoring.is_client);
            AddComponent(entity, new ComponentTypeSet(new ComponentType[]{
                typeof(FluidFlowBDTarget),
                typeof(FluidPipeInventory),
                typeof(PipeStates),
                typeof(PipeMesh),
            }));

        }
    }
}
[ChunkSerializable]
unsafe public struct PipeStates:IComponentData
{
    public UnsafeHashMap<int3, byte> map;
}
public struct PipeMesh:IBufferElementData
{
    public Entity value;
}
