using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Physics;
using static UnityEditor.ObjectChangeEventStream;
using Unity.Mathematics;

[System.Serializable]
public class WeaponControl3rdView : IControl
{
    public void cleanup()
    {
    }

    
    public void update(Entity entity, float dt)
    {
        if (World.DefaultGameObjectInjectionWorld.EntityManager.HasComponent<LocalTransform>(entity) == false) return;
        if (Input.GetMouseButtonDown(0))
        {
            //current_ctrl.lmb_down();
            //Debug.Log("lmb_clicked()");
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var origin = em.GetComponentData<LocalTransform>(entity).Position;
            var cteam = em.GetComponentData<CombatTeam>(entity);
            var weapons = em.GetBuffer<WeaponInfoV2>(entity).ToNativeArray(Allocator.Temp);
            if(weapons.Length == 0)
            {
                return;
            }
            var using_weapon = weapons[0];
            Entity hit_entity = default;
            {
                var handle = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<LocalAvoidanceSystem>();
                var phy = handle.GetSingleton<PhysicsWorldSingleton>();
                var rcinput = new RaycastInput();

                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                float3 hit_ray_y_offset = new float3(0f, 0.5f, 0f);
                rcinput.Start = origin;
                //rcinput.End = ray.direction * 100f;
                var hit_xz = BuildControl.HitOnXZPlane(Camera.main);
                var hit_dir = math.normalize(hit_xz - origin);
                rcinput.End = origin + hit_dir + using_weapon.radius;
                rcinput.Start += hit_ray_y_offset;
                rcinput.End += hit_ray_y_offset;
                UnitSearchHostileSystem.initialize_cfilter(using_weapon.weapon_type, cteam, ref rcinput.Filter);
                //var rcfilter = new CollisionFilter();
                //rcfilter.CollidesWith = uint.MaxValue;
                //rcfilter.BelongsTo = uint.MaxValue;
                //rcinput.Filter = rcfilter;

                if (phy.CastRay(rcinput, out var rchit))
                {
                    hit_entity = rchit.Entity;
                }
            }
            SystemState sstate = default;
            NativeHashSet<Entity> destroyed = new NativeHashSet<Entity>(8, Allocator.Temp);
            WeaponFireSystemV3.process_fire_attempts(ref sstate, entity, new CombatTarget() { value = hit_entity }, 0, destroyed);
        }
        
    }

    public void lmb_up()
    {
        Debug.Log("lmb_up");
    }
}