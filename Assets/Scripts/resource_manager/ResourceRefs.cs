using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class ResourceRefs : MonoBehaviour
{
    public static ResourceRefs self;
    public EntityPrefabs entity_prefabs;
    private void Awake()
    {
        self = this;
    }
    public Entity get_prefab(EntityPrefabIndices idx)
    {
        return entity_prefabs.entity_prefabs_0[(int)idx];
    }
}
public struct EntityPrefabs
{
    public NativeArray<Entity> entity_prefabs_0;
}
public enum EntityPrefabIndices: ushort
{
    friendly,
    hostile,
    obstacle,
    total
}