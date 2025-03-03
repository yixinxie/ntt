﻿using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Physics;
using Unity.Transforms;

[System.Serializable]
public class BuildControl : IControl
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
    public static float3 HitOnXZPlane(Camera cam)
    {
        if (Input.mousePosition.x < 0 || Input.mousePosition.x > Screen.width || Input.mousePosition.y < 0 || Input.mousePosition.y > Screen.height)
            return 0f;

        var ray = cam.ScreenPointToRay(Input.mousePosition);
        float y_diff = (-ray.origin.y);
        if (Mathf.Abs(ray.direction.y) > 0.01f)
        {
            float factor = y_diff / ray.direction.y;
            return ray.origin + ray.direction * factor;
        }
        return 0f;
    }
    public static float3 position_round(float3 inp)
    {
        return new float3(math.round(inp.x), inp.y, math.round(inp.z));
    }
    public static int3 round2int3(float3 inp)
    {
        return new int3((int)math.round(inp.x), (int)math.round(inp.y), (int)math.round(inp.z));
    }
    public int3 dbgint0;
    public int3 dbgint1;
    public static void fullscreenkeys()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            UIRefs.self.inventory.toggle();
        }
    }
    public void update(Entity entity, float dt)
    {
        //if (Input.GetMouseButtonDown(0))
        //{
        //    //Debug.Log("lmb_down()");
        //    helddown = true;
        //    helddown_pos = HitOnXZPlane(Camera.main);
        //}
        //if (Input.GetMouseButtonUp(0))
        //{
        //    //Debug.Log("lmb_up()");
        //    helddown = false;
        //}
        //if(helddown)
        //{
        //    var this_hit_pos = HitOnXZPlane(Camera.main);
        //    Debug.DrawLine(helddown_pos, this_hit_pos, Color.yellow);
        //    dbgpos = this_hit_pos;
        //}

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
            BelongsTo = StructureInteractions.Layer_ground_vehicle_scan | StructureInteractions.Layer_structure_scan
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
        fullscreenkeys();
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
            if(round2int3(previous_built_pos).Equals(round2int3(build_hit)) == false)
            {
                dragging2build = true;
            }
        }
        dbgint0 = round2int3(helddown_pos);
        dbgint1 = round2int3(build_hit);
        build_hit = position_round(build_hit);

        if (Input.GetMouseButtonDown(0) || dragging2build)
        {
            Debug.DrawLine(build_hit, build_hit + new float3(0f, 1.5f, 0f), Color.red, 2f);
            var bs = em.GetComponentData<BuilderShortcuts>(entity);
            previous_built_pos = round2int3(build_hit);
            float3 half_extents = new float3(1f, 0.1f, 1f);
            short epi = -1;
            switch (bs.currently_selected)
            {
                case ItemType.Extractor:
                    epi = (short)EntityPrefabIndices.extractor_test;
                    half_extents = new float3(1f, 0.5f, 1f);
                    break;
                case ItemType.Command_Center:
                    epi = (short)EntityPrefabIndices.command_center;
                    half_extents = new float3(1.5f, 0.5f, 1.5f);
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
                previous_built_pos = round2int3(build_hit);
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
