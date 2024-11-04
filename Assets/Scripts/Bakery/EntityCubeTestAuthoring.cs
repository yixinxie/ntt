using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Rendering;
// for the latest mobile. only support simple item type. no purity. from multiple types, one output type at a time. selectable in UI.
[DisallowMultipleComponent]
public class EntityCubeTestAuthoring : MonoBehaviour
{
   
    public class Bakery : Baker<EntityCubeTestAuthoring>
    {
        public override void Bake(EntityCubeTestAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);



            AddComponent(entity, new ComponentTypeSet(new ComponentType[]
            {
                
                typeof(CubePerInstanceProp),
                //typeof(StorageCellLimit),
            }));
         
        }
    }
}
[MaterialProperty("_Color")]
public struct CubePerInstanceProp:IComponentData
{
    public float4 value;
}