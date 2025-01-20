/**
 * Copyright 2021-2022 Chongqing Centauri Technology LLC.
 * All Rights Reserved.
 * 
 */
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Systems;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.GraphicsBuffer;
// UWP unitsearch, weaponfire, projectilemotion
//[/*UpdateAfter(typeof(ExportPhysicsWorld)), */UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct UnitSearchHostileSystem : ISystem
{
    ComponentLookup<LocalTransform> c0_array;
    WeaponFireSystemV3 weapon_fires;
    void OnCreate(ref SystemState sstate)
    {
        c0_array = sstate.GetComponentLookup<LocalTransform>();
        var s_e = sstate.EntityManager.CreateEntity();
        sstate.EntityManager.AddComponentData(s_e, new ASMSysStates());
        //sstate.EntityManager.<ASMSysStates>(SystemAPI.GetSingletonEntity<ASMSysStates>());
        weapon_fires.init(ref sstate);
    }
    void OnDestroy(ref SystemState sstate)
    {
        weapon_fires.dispose(ref sstate);
    }
    public static bool IsTargetInCone(float3 target_position, CachedTurretTransform c0c1, WeaponInfoV2 weaponinfo)
    {
        //var this_dist = math.distance(target_position, c0c1.c0);

        var fwd_worldspace = math.mul(c0c1.c1, new float3(0f, 0f, 1f));
        var cosine = math.dot(fwd_worldspace, math.normalize(target_position - c0c1.c0));
        //Debug.DrawLine(c0c1.c0, c0c1.c0 + fwd_worldspace * 50f, Color.green, 0.016f);

        cosine = math.clamp(cosine, -1f, 1f);
        bool in_cone = weaponinfo.attack_radians < float.Epsilon || cosine > math.cos(weaponinfo.attack_radians / 2f);
        bool in_range = math.distance(target_position, c0c1.c0) < weaponinfo.radius;
        return in_cone && in_range;
    }
    public static bool GetTargets_ICD(PhysicsWorld physics, 
        WeaponInfoV2 current_weapon, out CombatTarget combat_target, ComponentLookup<LocalTransform> c0_lookup,
        in CachedTurretTransform c0c1, in CombatTeam team, Entity self_entity)//, in ColliderRef self_collider)
    {
        combat_target = default;
        //int2 self_coord_axial = HexCoord.FromPosition(c0c1.c0, HexagonMap.unit_length);
        //for (int i = 0; i < weapons.Length; ++i)
        //var current_weapon = weapons[i];
        bool is_multi_targeting = false;
        bool require_los_check = false;
        bool search_furthest = false;
        PointDistanceInput pdi = new PointDistanceInput();
        pdi.Position = c0c1.c0;
        pdi.MaxDistance = current_weapon.radius;
        pdi.Filter = default;
        //pdi.Filter.CollidesWith |= StructureInteractions.Layer_obstacle;
        //pdi.Filter.BelongsTo |= StructureInteractions.Layer_obstacle;
        switch (current_weapon.weapon_type)
        {
            case WeaponTypes.Cannon:
                pdi.Filter.CollidesWith = team.HostileTeamMask();
                pdi.Filter.BelongsTo = StructureInteractions.Layer_ground_vehicle_scan;
                break;
            case WeaponTypes.CC_Passive_Scan:
                pdi.Filter.CollidesWith = team.HostileTeamMask();
                pdi.Filter.BelongsTo = StructureInteractions.Layer_ground_vehicle_scan;
                break;

        }


        //NativeList<DistanceHit> hits = default;// = new NativeList<DistanceHit>(Allocator.Temp);
        NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
        var rci = new RaycastInput();
        rci.Filter = pdi.Filter;
        rci.Start = pdi.Position;
        if (physics.CalculateDistance(pdi, ref hits))
        {
            float distance_cmp = (search_furthest) ? 0f : float.MaxValue;
            Entity found_entity = default;
            for (int j = 0; j < hits.Length; ++j)
            {
                if (hits[j].Entity == self_entity) continue;
                var target_pos = c0_lookup[hits[j].Entity].Position;
                var this_dist = math.distancesq(target_pos, c0c1.c0);
                if ((search_furthest && this_dist > distance_cmp) ||
                    (search_furthest == false && this_dist < distance_cmp))
                {
                    //if (require_los_check)
                    //{
                    //    rci.End = hits[j].Position;
                    //    if (UnitSearchHostileSystemV2.LOSCheck(physics, rci) && incombat_array.HasComponent(hits[j].Entity))
                    //    {
                    //        //closest_entity = collider_host_ref_array[hits[j].Entity].value;
                    //        //Debug.DrawLine(c0c1.c0, hits[j].Position, Color.red, 0.016f);
                    //        closest_entity = hits[j].Entity;
                    //        closest = this_dist;
                    //    }
                    //}
                    //else
                    if (IsTargetInCone(target_pos, c0c1, current_weapon))
                    {
                        //if (incombat_array.HasComponent(hits[j].Entity))
                            //closest_entity = collider_host_ref_array[hits[j].Entity].value;
                            found_entity = hits[j].Entity;
                            //Debug.DrawLine(c0c1.c0, hits[j].Position, Color.red, 0.016f);
                            distance_cmp = this_dist;
                    }
                }
                //else if (cosine <= math.cos(current_weapon.attack_radians / 2f))
                //{
                //    Debug.DrawLine(c0c1.c0, hits[j].Position, Color.red, 0.016f);
                //}
            }
            if (found_entity != Entity.Null)
            {
                //Debug.Log(c0c1.c0.ToString() + " targets " + found_entity.ToString());
                combat_target.value = found_entity;
                return true;
                //combat_targets.append(found_entity);
            }
        }
        else
        {
        }
        return false;
    }
    
    
    public static bool LOSCheck(PhysicsWorld physics, RaycastInput rci)
    {
        rci.Filter.CollidesWith = StructureInteractions.Layer_obstacle;
        rci.Filter.BelongsTo = StructureInteractions.Layer_obstacle;
        // default up vec
        float3 default_up = new float3(0f, 1f, 0f);
        var dir = math.normalize(rci.End - rci.Start);
        var right = math.cross(default_up, dir);
        var rci_tmp = rci;
        rci_tmp.Start += right;
        rci_tmp.End += right;
        if (LOSCheck_Single(physics, rci_tmp) == false)
        {
            return false;
        }

        rci_tmp = rci;
        if (LOSCheck_Single(physics, rci_tmp) == false)
        {
            return false;
        }

        rci_tmp = rci;
        rci_tmp.Start -= right;
        rci_tmp.End -= right;
        if (LOSCheck_Single(physics, rci_tmp) == false)
        {
            return false;
        }

        return true;
    }

    static bool LOSCheck_Single(PhysicsWorld physics, RaycastInput rci)
    {
        NativeList<Unity.Physics.RaycastHit> rh_obstacles = new NativeList<Unity.Physics.RaycastHit>(Allocator.Temp);
        return physics.CastRay(rci, ref rh_obstacles) == false;
    }
    partial struct search_target_job:IJobEntity
    {
        public PhysicsWorld physics;
        public ComponentLookup<LocalTransform> c0_array;
        void Execute(Entity entity, DynamicBuffer<WeaponInfoV2> weapons, DynamicBuffer<CombatTarget> targets, in CombatTeam team)
        {
            var c0 = CachedTurretTransform.from_localtransform(c0_array[entity]);
            if (weapons.Length != targets.Length) { return; }
            for (int i = 0; i < weapons.Length; ++i)
            {
                bool should_get_targets = false;
                var target = targets[i];
                if (c0_array.HasComponent(target.value) == false)
                {
                    if (target.value.Equals(Entity.Null))
                    {
                        // this unit has no target to begin with.
                    }
                    else
                    {
                        // this unit's target has been destroyed.
                    }
                    should_get_targets = true;
                }
                else
                {
                    if (IsTargetInCone(c0_array[target.value].Position, c0, weapons[i]) == false)
                    {
                        // target out of range.
                        //Debug.Log(entity.ToString() + " detargets.");
                        targets[i] = default;
                        should_get_targets = true;
                    }
                }
                if (should_get_targets)
                {
                    if (GetTargets_ICD(physics, weapons[i], out CombatTarget _target, c0_array, c0, team, entity))
                    {
                        //Debug.Log(entity.ToString() + " gains target " + _target.value);
                        //target.value = _target;
                        targets[i] = _target;
                    }
                    else
                    {
                        targets[i] = default;
                    }
                }
            }
        }
    }
    partial struct set_combatteam_job : IJobEntity
    {
        public void Execute(Entity target, ref PhysicsCollider pcol, in CombatTeam cteam)
        {
            unsafe
            {
                var cfilter = pcol.ColliderPtr->GetCollisionFilter();
                cfilter.BelongsTo = cfilter.BelongsTo | cteam.FriendlyTeamMask();
                pcol.ColliderPtr->SetCollisionFilter(cfilter);
            }
        }
    }
    void OnUpdate(ref SystemState sstate)
    {
        var ass = SystemAPI.GetSingletonRW<ASMSysStates>();
        ass.ValueRW.elapsed += Time.deltaTime;
        
        sstate.CompleteDependency();
        c0_array.Update(ref sstate);

        var job0 = new set_combatteam_job();
        job0.Run();
        // turrets
        var job1 = new search_target_job();
        job1.physics = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        job1.c0_array = c0_array;
        job1.Run();

        var fire0 = new WeaponFireSystemV3.weapon_fire_0();
        fire0.c0_array = c0_array;
        fire0.dt = Time.deltaTime;
        fire0._weapon_fire_attempts = new NativeList<WeaponFireAttemptInfo>(8, Allocator.TempJob);
        fire0.Run();
        weapon_fires.OnUpdate(ref sstate, fire0._weapon_fire_attempts);
        fire0._weapon_fire_attempts.Dispose();
        //NativeList<> 
        // passive cc detect pass
        //Entities//.WithAll<InCombat>()
        //.ForEach((Entity entity, ref CmdCntrModes ccstates,
        //in LocalTransform c0, in CombatTeam team) =>
        //{

        //    if (ccstates.mode == CmdCntrModeTypes.Passive && c0_array.HasComponent(ccstates.invader) == false)
        //    {
        //        var cc_wi = new WeaponInfoV2();
        //        cc_wi.radius = 150f;
        //        cc_wi.weapon_type = WeaponTypes.CC_Passive_Scan;
        //        var clt = CachedTurretTransform.from_localtransform(c0);
        //        if (GetTargets_ICD(physics, cc_wi, out CombatTarget _target, c0_array, clt, team, entity))
        //        {
        //            //Debug.Log("cc gains target " + _target.value);
        //            //target.value = _target;
        //            ccstates.invader = _target.value;
        //            //var refs = sstate.EntityManager.GetBuffer<CmdCntrUnitRef>(entity).ToNativeArray(Allocator.Temp);
        //            //for(int i = 0; i < refs.Length; ++i)
        //            //{
        //            //    dispatch_cc_unit(ref sstate, refs[i].value, _target.value);
        //            //}

        //        }
        //        else
        //        {
        //            ccstates.invader = default;
        //        }
        //    }

        //}).Run();

    }
    static void dispatch_cc_unit(ref SystemState sstate, Entity entity, Entity target)
    {
        var target_position = sstate.EntityManager.GetComponentData<LocalTransform>(target).Position;
        var unit_position = sstate.EntityManager.GetComponentData<LocalTransform>(entity).Position;
        var dp = sstate.EntityManager.GetComponentData<DesiredPosition>(entity);
        dp.init_finish_line_vec(unit_position);
        dp.target = target;
        sstate.EntityManager.SetComponentData(entity, dp);
    }
   
   

}
[InternalBufferCapacity(4), System.Serializable]
public struct WeaponInfoV2 : IBufferElementData
{
    public WeaponTypes weapon_type;
    public ushort ammo_type;

    //public StructureType projectile_type;
    public float radius;
    public half attack_radians;

    //public byte burst_index; // changing
    public half attack_time_left; // changing
    public half attack_duration; // constant
    public byte attacks_left;
    public byte attacks_total;

    public half weapon_cooldown_left; // 0 means ready to fire, 
    public half weapon_cooldown_total;

    public half base_damage;

    public byte damamge_type;
    //public uint param0;
}

[InternalBufferCapacity(4)]
unsafe public struct CombatTargets:IBufferElementData
{
    public const int MaxCount = 4;
    unsafe public fixed int indices[MaxCount];
    unsafe public fixed int versions[MaxCount];
    public int count;
    public Entity value_at(int idx)
    {
        return new Entity() { Index = indices[idx], Version = versions[idx] };
    }
    public void set_null(int idx)
    {
        if (count > 0)
        {
            count--;
            for (int i = idx; i < MaxCount - 1; ++i)
            {
                indices[i] = indices[i + 1];
                versions[i] = versions[i + 1];
            }
        }
    }
    public void assign_at(Entity entity, int idx)
    {
        indices[idx] = entity.Index;
        versions[idx] = entity.Version;
    }
    public void append(Entity entity)
    {
        if(count < 4)
        {
            assign_at(entity, count);
            count++;
        }

    }
    
}


//unsafe public struct CombatTargets_ICD : IComponentData
//{
//    public const int MaxCount = 4;
//    unsafe public fixed int indices[4];
//    unsafe public fixed int versions[4];
//    public int count;
//    public Entity value_at(int idx)
//    {
//        return new Entity() { Index = indices[idx], Version = versions[idx] };
//    }
//    public CombatTargets AsCombatTargets()
//    {
//        CombatTargets ret = new CombatTargets();
//        for (int i = 0; i < count; ++i)
//        {
//            ret.append(value_at(0));
//        }
//        return ret;
//    }
//    public void set_null(int idx)
//    {
//        if (count > 0)
//        {
//            count--;
//            for (int i = idx; i < MaxCount - 1; ++i)
//            {
//                indices[i] = indices[i + 1];
//                versions[i] = versions[i + 1];
//            }
//        }
//    }
//    public void assign_at(Entity entity, int idx)
//    {
//        indices[idx] = entity.Index;
//        versions[idx] = entity.Version;
//    }
//    public void append(Entity entity)
//    {
//        if (count < 4)
//        {
//            assign_at(entity, count);
//            count++;
//        }

//    }

//}
[InternalBufferCapacity(4)]
public struct CombatTarget : IBufferElementData
{
    public Entity value;
}
public struct DroneTargetPosition : IComponentData 
{
    public float3 value;
}
public struct ASMSysStates : IComponentData
{
    public float elapsed;
}