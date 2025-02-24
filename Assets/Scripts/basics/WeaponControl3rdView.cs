using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Physics;
using Unity.Mathematics;

[System.Serializable]
public class WeaponControl3rdView : IControl
{
    public void cleanup()
    {
    }

    void continuous_weapon()
    {

    }
    public void update(Entity entity, float dt)
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (em.HasComponent<LocalTransform>(entity) == false) return;
        var origin = em.GetComponentData<LocalTransform>(entity).Position;
        var weapons = em.GetBuffer<WeaponInfoV2>(entity).ToNativeArray(Allocator.Temp);
        if(weapons.Length == 0)
        {
            return;
        }
        var this_weapon = weapons[0];
        var cteam = em.GetComponentData<CombatTeam>(entity);

        if (this_weapon.weapon_type > WeaponTypes.Channeled_Start && this_weapon.weapon_type < WeaponTypes.Projectile_Start)
        {
        }
        var rcinput = new RaycastInput();
        //var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float3 hit_ray_y_offset = new float3(0f, 0.25f, 0f);
        rcinput.Start = origin;
        //rcinput.End = ray.direction * 100f;
        var hit_xz = BuildControl.HitOnXZPlane(Camera.main);
        var hit_dir = math.normalize(hit_xz - origin);
        rcinput.End = origin + hit_dir * this_weapon.radius;
        rcinput.Start += hit_ray_y_offset;
        rcinput.End += hit_ray_y_offset;
        var phy = SBaseHelpers.self.get_physics();
        UnitSearchHostileSystem.initialize_query_cfilter(this_weapon.weapon_type, cteam, ref rcinput.Filter);

        NativeList<Unity.Physics.RaycastHit> allHits = new NativeList<Unity.Physics.RaycastHit>(4, Allocator.Temp);
        if (phy.CastRay(rcinput, ref allHits))
        {
            
            for (int i = 0; i < allHits.Length; ++i)
            {
                var this_hit = allHits[i];
                Debug.DrawLine(this_hit.Position, this_hit.Position + new float3(0f, 1.5f, 0f), Color.red);
                
            }
        }

            
        if (Input.GetMouseButton(0))
        {
            if (this_weapon.can_autofire(dt))
            {
                //current_ctrl.lmb_down();
                //Debug.Log("lmb_clicked()");
                Entity hit_entity = default;
                allHits.Clear();
                if (phy.CastRay(rcinput, ref allHits))
                {
                    for (int i = 0; i < allHits.Length; ++i)
                    {
                        var this_hit = allHits[i];
                        if (this_hit.Entity.Equals(entity) == false)
                        {
                            hit_entity = this_hit.Entity;
                            break;
                        }
                    }
                }
                NativeHashSet<Entity> destroyed = new NativeHashSet<Entity>(8, Allocator.Temp);
                WeaponFireSystemV3.process_fire_attempts(ref SBaseHelpers.self.CheckedStateRef, entity, new CombatTarget() { value = hit_entity }, 0, destroyed);
                if (destroyed.Count > 0)
                {
                    var destroy_tmp = destroyed.ToNativeArray(Allocator.Temp);
                    em.DestroyEntity(destroy_tmp);
                }
            }
            else
            {
                if(this_weapon.has_ammo() == false)
                {
                    Debug.Log("out of ammo!");
                }
            }
        }
        
    }

}