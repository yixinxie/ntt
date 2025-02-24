using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
 
public class InventoryAuthoring: MonoBehaviour
{
    [SerializeField]
    public RouterInventory storage;
    private void OnDrawGizmos()
    {
    }
    public class Bakery : Baker<InventoryAuthoring>
    {
        public override void Bake(InventoryAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new ComponentTypeSet(new ComponentType[] {
                    typeof(BuilderTag),
                    typeof(RouterInventory),
                }));

            var scell = SetBuffer<RouterInventory>(entity);
            scell.Add(authoring.storage);
        }
    }
}
public struct BuilderTag : IComponentData { }
