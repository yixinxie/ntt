using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class ResourceGroupAuthoring : MonoBehaviour
{
    public ResourceArrayType restype;
    public GameObject[] prefab;
    public class Bakery : Baker<ResourceGroupAuthoring>
    {
        public override void Bake(ResourceGroupAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddBuffer<ResourcePrefabEntry>(entity);
            var go_prefabs = authoring.prefab;
            for (int i = 0; i < go_prefabs.Length; ++i)
            {
                Entity baked = Entity.Null;
                if(go_prefabs[i] != null)
                    baked = GetEntity(go_prefabs[i], TransformUsageFlags.Dynamic);
                AppendToBuffer(entity, new ResourcePrefabEntry() {rat = authoring.restype, baked_prefab = baked });
            }
            //Debug.Log("baked " + authoring.restype.ToString());
        }
    }
}
public struct ResourcePrefabEntry:IBufferElementData
{
    public ResourceArrayType rat;
    public Entity baked_prefab;
}