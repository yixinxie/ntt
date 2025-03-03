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
    ComponentLookup<ManualFireCtrl> manualfire_lookup;
    WeaponFireSystemV3 weapon_fires;
    EntityQuery query_without_manual_fire;
    void OnCreate(ref SystemState sstate)
    {
        c0_array = sstate.GetComponentLookup<LocalTransform>();
        manualfire_lookup = sstate.GetComponentLookup<ManualFireCtrl>();
        var s_e = sstate.EntityManager.CreateEntity();
        sstate.EntityManager.AddComponentData(s_e, new ASMSysStates());
        //sstate.EntityManager.<ASMSysStates>(SystemAPI.GetSingletonEntity<ASMSysStates>());
        weapon_fires.init(ref sstate);
        var eq_desc = new EntityQueryDesc()
        {
            All = new ComponentType[] { typeof(WeaponInfoV2), typeof(CombatTarget), typeof(MovementInfo), typeof(CombatTeam)},
            None = new ComponentType[] { typeof(ManualFireCtrl) },
        };
        query_without_manual_fire = sstate.EntityManager.CreateEntityQuery(eq_desc);
    }
    void OnDestroy(ref SystemState sstate)
    {
        query_without_manual_fire.Dispose();
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
    public static void initialize_query_cfilter(WeaponTypes weapon_type, CombatTeam team, ref CollisionFilter filter)
    {
        switch (weapon_type)
        {
            case WeaponTypes.Cannon:
                filter.CollidesWith = team.HostileTeamMask();
                filter.BelongsTo = StructureInteractions.Layer_ground_vehicle_scan;
                break;
            case WeaponTypes.CC_Passive_Scan:
                filter.CollidesWith = team.HostileTeamMask();
                filter.BelongsTo = StructureInteractions.Layer_ground_vehicle_scan;
                break;

        }
    }
    public static bool GetTargets_ICD(PhysicsWorld physics, 
        WeaponInfoV2 current_weapon, out CombatTarget combat_target, ComponentLookup<LocalTransform> c0_lookup,
        in CachedTurretTransform c0c1, in CombatTeam team, Entity self_entity)//, in ColliderRef self_collider)
    {
        combat_target = default;
        //int2 self_coord_axial = HexCoord.FromPosition(c0c1.c0, HexagonMap.unit_length);
        //for (int i = 0; i < weapons.Length; ++i)
        //var current_weapon = weapons[i];
        //bool is_multi_targeting = false;
        //bool require_los_check = false;
        bool search_furthest = false;
        PointDistanceInput pdi = new PointDistanceInput();
        pdi.Position = c0c1.c0;
        pdi.MaxDistance = current_weapon.radius;
        pdi.Filter = default;
        //pdi.Filter.CollidesWith |= StructureInteractions.Layer_obstacle;
        //pdi.Filter.BelongsTo |= StructureInteractions.Layer_obstacle;
        initialize_query_cfilter(current_weapon.weapon_type, team, ref pdi.Filter);

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
                var hit_entity = hits[j].Entity;
                if (c0_lookup.HasComponent(hit_entity) == false)
                {
                    continue;
                }
                var target_pos = c0_lookup[hit_entity].Position;
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
        //public ComponentLookup<ManualFireCtrl> manualFireCtrlLookup;
        public ComponentLookup<LocalTransform> c0_array;
        void Execute(Entity entity, DynamicBuffer<WeaponInfoV2> weapons, DynamicBuffer<CombatTarget> targets, ref MovementInfo mi, in CombatTeam team)
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
                        Debug.Log(entity.ToString() + " detargets.");
                        if(mi.uctype == UnitCommandTypes.AttackMove)
                        {
                            mi.move_state = MovementStates.Moving;
                        }
                        else if (mi.uctype == UnitCommandTypes.Standby)
                        {
                            mi.move_state = MovementStates.Idle;
                        }
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
                        if (_target.Equals(Entity.Null) == false)
                        {
                            mi.move_state = MovementStates.HoldPosition;
                        }
                    }
                    else
                    {
                        targets[i] = default;
                    }
                }
            }
        }
    }
    public partial struct set_combatteam_job : IJobEntity
    {
        public static void sync_managed(Entity target, EntityManager em )
        {
            var cteam = em.GetComponentData<CombatTeam>(target);
            var pcol = em.GetComponentData<PhysicsCollider>(target);
            unsafe
            {
                var cfilter = pcol.ColliderPtr->GetCollisionFilter();
                cfilter.BelongsTo = cfilter.BelongsTo | cteam.FriendlyTeamMask();
                pcol.ColliderPtr->SetCollisionFilter(cfilter);
            }
        }
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

    partial struct cc_search : IJobEntity
    {
        public PhysicsWorld physics;
        [ReadOnly]
        public ComponentLookup<LocalTransform> c0_array;

        public void Execute(Entity entity, ref CmdCntrModes ccstates, in LocalTransform c0, in CombatTeam team)
        {
            if (ccstates.mode == CmdCntrModeTypes.Passive && c0_array.HasComponent(ccstates.invader) == false)
            {
                var cc_wi = new WeaponInfoV2();
                cc_wi.radius = 150f;
                cc_wi.weapon_type = WeaponTypes.CC_Passive_Scan;
                var clt = CachedTurretTransform.from_localtransform(c0);
                if (GetTargets_ICD(physics, cc_wi, out CombatTarget _target, c0_array, clt, team, entity)) // ccsearch
                {
                    Debug.Log("cc gains target " + _target.value);
                    //target.value = _target;
                    ccstates.invader = _target.value;
                    //var refs = sstate.EntityManager.GetBuffer<CmdCntrUnitRef>(entity).ToNativeArray(Allocator.Temp);
                    //for(int i = 0; i < refs.Length; ++i)
                    //{
                    //    dispatch_cc_unit(ref sstate, refs[i].value, _target.value);
                    //}

                }
                else
                {
                    ccstates.invader = default;
                }
            }
        }
    }

    void OnUpdate(ref SystemState sstate)
    {
        var ass = SystemAPI.GetSingletonRW<ASMSysStates>();
        ass.ValueRW.elapsed += Time.deltaTime;
        
        sstate.CompleteDependency();
        c0_array.Update(ref sstate);
        var _physics = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        var job0 = new set_combatteam_job();
        job0.Run();
        // turrets

        manualfire_lookup.Update(ref sstate);
        var job1 = new search_target_job();
        job1.physics = _physics;
        job1.c0_array = c0_array;
        //job1.manualFireCtrlLookup = manualfire_lookup;
        job1.Run(query_without_manual_fire);

        c0_array.Update(ref sstate);
        // passive cc detect pass
        var job2 = new cc_search();
        job2.c0_array = c0_array;
        job2.physics = _physics;
        job2.Run();

        c0_array.Update(ref sstate);
        var fire0 = new WeaponFireSystemV3.weapon_fire_0();
        fire0.c0_array = c0_array;
        fire0.dt = Time.deltaTime;
        fire0._weapon_fire_attempts = new NativeList<WeaponFireAttemptInfo>(8, Allocator.TempJob);
        fire0.Run();
        //weapon_fires.OnUpdate(ref sstate, fire0._weapon_fire_attempts);
        c0_array.Update(ref sstate);
        NativeHashSet<Entity> destroyed = new NativeHashSet<Entity>(8, Allocator.Temp);
        for (int i = 0; i < fire0._weapon_fire_attempts.Length; ++i)
        {
            var attempt = fire0._weapon_fire_attempts[i];
            WeaponFireSystemV3.process_fire_attempts(ref sstate, attempt.initiator, attempt.combat_target, attempt.weapon_index, destroyed);
        }
        //_weapon_fire_attempts.Dispose();
        var destroy_tmp = destroyed.ToNativeArray(Allocator.Temp);
        //for (int i = 0; i < destroy_tmp.Length; ++i)
        //    sstate.EntityManager.DestroyEntity(destroy_tmp[i]);
        sstate.EntityManager.DestroyEntity(destroy_tmp);

        fire0._weapon_fire_attempts.Dispose();

        var load_ammo_job = new WeaponFireSystemV3.weapon_load_ammo();
        load_ammo_job.Run();
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
    public float attack_radians;

    //public byte burst_index; // changing
    public float attack_time_left; // changing
    public float attack_duration; // constant
    public byte attacks_left;
    public byte attacks_total;

    public float weapon_cooldown_left; // 0 means ready to fire, 
    public float weapon_cooldown_total;

    public float base_damage;

    public byte damage_type;
    public byte ammo_left;
    public bool can_autofire(float dt)
    {
        return weapon_ready(dt) && has_ammo();
    }

    public bool has_ammo()
    {
        return ammo_left > 0;
    }
    public bool weapon_ready(float dt)
    {
        return weapon_cooldown_left <= dt;
    }
    //public uint param0;
}

public struct ProjectileStates : IComponentData
{
    public ProjectileAttackTypes damage_type;
    public float timed;
    public float radius; // aoe radius
    public Entity target;
    public float3 target_position;

    public float base_damage;

    public byte weapon_damage_type;
  
    public void init(WeaponInfoV2 weapon_info)
    {
        base_damage = weapon_info.base_damage;
        weapon_damage_type = weapon_info.damage_type;

        switch (weapon_info.weapon_type)
        {
            case WeaponTypes.Projectile:
                damage_type = ProjectileAttackTypes.Timed_Single;
                break;
        }
    }
}
public enum ProjectileAttackTypes:byte
{
    Timed_Single,
    Timed_AOE,
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