using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Physics;
using Unity.Transforms;

[System.Serializable]
public class BuildPipeControl: IControl
{

    public bool helddown;
    public float3 helddown_pos;
    public int3 previous_built_pos;
    public float3 dbgpos;
    public int pointer_hit_count_debug;
    public bool dragging2build;
    public void cleanup()
    {
        helddown = false;
    }
    public int3 dbgint0;
    public int3 dbgint1;

    public void update(Entity entity, float dt)
    {
     

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (em.HasComponent<LocalTransform>(entity) == false) return;
        //var origin = em.GetComponentData<LocalTransform>(entity).Position;
        //var cteam = em.GetComponentData<CombatTeam>(entity);

        var rcinput = new RaycastInput();
        var cam_ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        rcinput.Start = cam_ray.origin;
        rcinput.End = cam_ray.origin + cam_ray.direction * 100f;
        var phy = SBaseHelpers.self.get_physics();
        //var handle = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<LocalAvoidanceSystem>();
        //var phy = handle.GetSingleton<PhysicsWorldSingleton>();
        rcinput.Filter = new CollisionFilter() 
        { 
            CollidesWith = uint.MaxValue,
            BelongsTo = StructureInteractions.Layer_belt_pipe | StructureInteractions.Layer_structure_scan
            | StructureInteractions.Layer_ground
        };

        NativeList<Unity.Physics.RaycastHit> allHits = new NativeList<Unity.Physics.RaycastHit>(4, Allocator.Temp);
        pointer_hit_count_debug = 0;
        float3 build_hit = float3.zero;
        if (phy.CastRay(rcinput, ref allHits))
        {
            pointer_hit_count_debug = allHits.Length;
            for (int i = 0; i < allHits.Length; ++i)
            {
                var this_hit = allHits[i];
                if(i == 0)
                {
                    build_hit = this_hit.Position;
                }
                

            }
        }
        BuildControl.fullscreenkeys();
        if (Input.GetMouseButtonDown(0))
        {
            helddown = true;
            helddown_pos = build_hit;
        }
        if(helddown && Input.GetMouseButtonUp(0))
        {
            helddown = false;
        }
        dragging2build = false;
        if (helddown)
        {
            if(BuildControl.round2int3(previous_built_pos).Equals(BuildControl.round2int3(build_hit)) == false)
            {
                dragging2build = true;
            }
        }
        dbgint0 = BuildControl.round2int3(helddown_pos);
        dbgint1 = BuildControl.round2int3(build_hit);
        build_hit = BuildControl.position_round(build_hit);

        if (Input.GetMouseButtonDown(0) || dragging2build)
        {
            Debug.DrawLine(build_hit, build_hit + new float3(0f, 1.5f, 0f), Color.red, 2f);
            var bs = em.GetComponentData<BuilderShortcuts>(entity);
            previous_built_pos = BuildControl.round2int3(build_hit);
            float3 half_extents = new float3(1f, 0.1f, 1f);
            short epi = -1;
            switch (bs.currently_selected)
            {
                case ItemType.Belt:
                    epi = (short)EntityPrefabIndices.obstacle_test;
                    half_extents = new float3(1f, 0.5f, 1f);
                    break;

            }

            NativeList<DistanceHit> distance_hits = new NativeList<DistanceHit>(4, Allocator.Temp);
            CollisionFilter cfilter = new CollisionFilter()
            {
                CollidesWith = uint.MaxValue,
                BelongsTo = StructureInteractions.Layer_structure_scan
            };
            if(phy.OverlapBox(build_hit, quaternion.identity, half_extents, ref distance_hits, cfilter))
            {
                //Debug.Log("struct hit count " + distance_hits.Length);
            }
            else
            {
                previous_built_pos = BuildControl.round2int3(build_hit);
                if (epi >= 0)
                {
                    Entity structure_prefab = ResourceRefs.self.get_prefab((EntityPrefabIndices)epi);

                    var new_entity = em.Instantiate(structure_prefab);
                    em.SetComponentData(new_entity, LocalTransform.FromPositionRotation(build_hit, quaternion.identity));
                    var cteam = em.GetComponentData<CombatTeam>(entity);
                    em.SetComponentData(new_entity, cteam);
                }
                else
                {
                    Debug.Log("undefined structure " + bs.currently_selected.ToString());
                }
            }
            
        }
    }
}
