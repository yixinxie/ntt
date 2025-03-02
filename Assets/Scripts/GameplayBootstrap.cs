using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;

public class GameplayBootstrap : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(ResourceRefs.self != null)
        {
            var prefabs = ResourceRefs.self.entity_prefabs.entity_prefabs_0;
            if(prefabs.IsCreated)
            {
                var em = World.DefaultGameObjectInjectionWorld.EntityManager;
                var friendly = em.Instantiate(ResourceRefs.self.get_prefab(EntityPrefabIndices.friendly_turret_test));
                var hostile = em.Instantiate(ResourceRefs.self.get_prefab(EntityPrefabIndices.enemy));

#if UNITY_EDITOR
                em.SetName(friendly, "friendly");
                em.SetName(hostile, "hostile");
#endif
                em.SetComponentData(friendly, LocalTransform.FromPosition(new float3(1f, 0f, 1f)));
                em.SetComponentData(hostile, LocalTransform.FromPosition(new float3(8f, 0f, 1f)));

                var ri_db = em.GetBuffer<RouterInventory>(friendly);
                ri_db.Add(new RouterInventory() { item_type = (ushort)ItemType.Pistol, item_count = 1 });
                ri_db.Add(new RouterInventory() { item_type = (ushort)ItemType.Command_Center, item_count = 2 });
                ri_db.Add(new RouterInventory() { item_type = (ushort)ItemType.Extractor, item_count = 3 });
                enabled = false;
            }
        }
    }
}
