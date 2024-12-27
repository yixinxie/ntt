using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
[DisallowMultipleComponent]
 
public class TileResourceNodeAuthoring : MonoBehaviour
{
    public int remaining;
    public List<ResourceNodeOutputStates> outputs;
    public bool is_client;
    public class Bakery : Baker<TileResourceNodeAuthoring>
    {
        public override void Bake(TileResourceNodeAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            //Debug.Log("SMResourceNodeAuthoring");
            AddComponent(entity, new ComponentTypeSet(new ComponentType[]{
            typeof(ResourceNodeOutputStates),
            typeof(ResourceNodeRemaining),
            typeof(GalacticType),
            typeof(ASMEntityGUID),
            typeof(ParentTileZoneRef),
            typeof(SimulationGroup), // this probably should not be set to Planned
            typeof(PlayerID_CD), // not necessary
        }
        ));

            SetComponent(entity, new ResourceNodeRemaining()
            {
                value = authoring.remaining
            });
            var gtype = new GalacticType() { value = GTypes.ResourceNode };
            if (authoring.outputs.Count > 0)
                gtype.set_resource_type((int)authoring.outputs[0].item_type);
            SetComponent(entity, gtype);

            var atb = authoring.outputs.ToNativeArray(Allocator.Temp);
            for (int i = 0; i < atb.Length; ++i)
                AppendToBuffer(entity, atb[i]);

            if (authoring.is_client)
            {
                AddComponent(entity, new ComponentTypeSet(new ComponentType[]{
                typeof(MeshGORef),
                typeof(ColliderRef),
                //typeof(MachineItemGizmos),
                }
                ));
                SetSharedComponent(entity, SimulationGroup.Client());
                
            }
            else
            {
                //AddComponent(entity, new ComponentTypes(new ComponentType[]{
                //typeof(ExtractorCoverTargetElement),
                //}
                //));
                SetSharedComponent(entity, SimulationGroup.Server());
            }
        }
    }
}
public struct ParentTileZoneRef:IComponentData
{
    public Entity value;
}
