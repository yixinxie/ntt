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
                    typeof(BuilderShortcuts),
                    typeof(RouterInventory),
                }));
            var bs = new BuilderShortcuts();
            //bs.init(ItemType.Extractor, ItemType.Command_Center, ItemType.Pistol, 0);
            bs.enabled_rows = 1;
            SetComponent(entity, bs);
            var scell = SetBuffer<RouterInventory>(entity);
            scell.Add(authoring.storage);
        }
    }
}
unsafe public struct BuilderShortcuts : IComponentData 
{
    public const int max_row_count = 3;
    public const int column_count = 10;

    public byte enabled_rows; // max number of shortcut rows
    public byte activated_row;
    public ItemType currently_selected;
    public fixed ushort shortcuts[max_row_count * column_count];
    public void init(ItemType it0, ItemType it1, ItemType it2, ItemType it3)
    {
        shortcuts[0] = (ushort)it0;
        shortcuts[1] = (ushort)it1;
        shortcuts[2] = (ushort)it2;
        shortcuts[3] = (ushort)it3;
    }
    public ItemType get_item(int idx)
    {
        return (ItemType)shortcuts[idx];
    }

    public int get_item_index(int ofs)
    {
        var tmp = activated_row * column_count + ofs;
        return tmp;
    }

}
