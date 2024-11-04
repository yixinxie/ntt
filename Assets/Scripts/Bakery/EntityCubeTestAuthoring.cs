using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Rendering;
// for the latest mobile. only support simple item type. no purity. from multiple types, one output type at a time. selectable in UI.
[DisallowMultipleComponent]
public class EntityCubeTestAuthoring : MonoBehaviour
{
    public Color initial_color;
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
            float4 c = new float4(authoring.initial_color.r, authoring.initial_color.g, authoring.initial_color.b, 1.0f);
            SetComponent(entity, new CubePerInstanceProp() { value = c });
         
        }
    }
}
[MaterialProperty("_Color")]
public struct CubePerInstanceProp:IComponentData
{
    public float4 value;
}