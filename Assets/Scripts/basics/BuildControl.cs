using Unity.Mathematics;
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
    public float3 dbgpos;
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
    public int pointer_hit_count_debug;
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


        if (Input.GetMouseButton(0))
        {
            Debug.DrawLine(build_hit, build_hit + new float3(0f, 1.5f, 0f), Color.red);
            float3 half_extents = new float3(1f, 0.1f, 1f);
            NativeList<DistanceHit> distance_hits = new NativeList<DistanceHit>(4, Allocator.Temp);
            CollisionFilter cfilter = new CollisionFilter()
            {
                CollidesWith = uint.MaxValue,
                BelongsTo = StructureInteractions.Layer_structure_scan
            };
            if(phy.OverlapBox(build_hit, quaternion.identity, half_extents, ref distance_hits, cfilter))
            {
                Debug.Log("struct hit count " + distance_hits.Length);
            }
            //if (this_weapon.can_autofire(dt))
            //{
            //    //current_ctrl.lmb_down();
            //    //Debug.Log("lmb_clicked()");
            //    Entity hit_entity = default;
            //    allHits.Clear();
            //    if (phy.CastRay(rcinput, ref allHits))
            //    {
            //        for (int i = 0; i < allHits.Length; ++i)
            //        {
            //            var this_hit = allHits[i];
            //            if (this_hit.Entity.Equals(entity) == false)
            //            {
            //                hit_entity = this_hit.Entity;
            //                break;
            //            }
            //        }
            //    }
            //    NativeHashSet<Entity> destroyed = new NativeHashSet<Entity>(8, Allocator.Temp);
            //    WeaponFireSystemV3.process_fire_attempts(ref SBaseHelpers.self.CheckedStateRef, entity, new CombatTarget() { value = hit_entity }, 0, destroyed);
            //    if (destroyed.Count > 0)
            //    {
            //        var destroy_tmp = destroyed.ToNativeArray(Allocator.Temp);
            //        em.DestroyEntity(destroy_tmp);
            //    }
            //}
            //else
            //{
            //    if (this_weapon.has_ammo() == false)
            //    {
            //        Debug.Log("out of ammo!");
            //    }
            //}
        }
    }
}
