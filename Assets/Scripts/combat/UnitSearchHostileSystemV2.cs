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
[/*UpdateAfter(typeof(ExportPhysicsWorld)), */UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class UnitSearchHostileSystemV2 : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
        Enabled = false;
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
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
    // todo: merge with GetTargets
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

            //case WeaponTypes.Repair_Multi:
            //    pdi.Filter.CollidesWith = team.FriendlyTeamMask();
            //    pdi.Filter.BelongsTo = StructureInteractions.Layer_vehicle;
            //    is_multi_targeting = true;
            //    break;
            //case WeaponTypes.Repair:
            //    pdi.Filter.CollidesWith = team.FriendlyTeamMask();
            //    pdi.Filter.BelongsTo = StructureInteractions.Layer_vehicle | StructureInteractions.Layer_obstacle;
            //    break;
            //case WeaponTypes.Projectile:
            //    pdi.Filter.CollidesWith = team.HostileTeamMask();
            //    pdi.Filter.BelongsTo = StructureInteractions.Layer_vehicle;
            //    break;
            //case WeaponTypes.Projectile_Multi:
            //    pdi.Filter.CollidesWith = team.HostileTeamMask();
            //    pdi.Filter.BelongsTo = StructureInteractions.Layer_vehicle;
            //    is_multi_targeting = true;
            //    break;
            //case WeaponTypes.Cannon_Vehicle:
            //    pdi.Filter.CollidesWith = team.HostileTeamMask();
            //    pdi.Filter.BelongsTo = StructureInteractions.Layer_vehicle;
            //    require_los_check = true;
            //    break;
            //case WeaponTypes.Cannon_Platform:
            //    pdi.Filter.CollidesWith = team.HostileTeamMask();
            //    pdi.Filter.BelongsTo = StructureInteractions.Layer_character;
            //    break;
            ////case WeaponTypes.Cannon_Multi:
            ////    is_multi_targeting = true;
            ////    pdi.Filter.CollidesWith = team.HostileTeamMask();
            ////    pdi.Filter.BelongsTo = StructureInteractions.Layer_vehicle;
            ////    break;

            //case WeaponTypes.Laser_Offensive:
            //case WeaponTypes.Laser_Offensive_Cont:
            //    //case WeaponTypes.Spawn_Hatchling:
            //    //case WeaponTypes.Spawn_Fighter:
            //    //is_multi_targeting = true;
            //    pdi.Filter.CollidesWith = team.HostileTeamMask();
            //    pdi.Filter.BelongsTo = StructureInteractions.Layer_vehicle | StructureInteractions.Layer_character;
            //    require_los_check = true;
            //    break;

            //case WeaponTypes.Spawn_Swarm:
            //    is_multi_targeting = true;
            //    pdi.Filter.CollidesWith = team.HostileTeamMask();
            //    pdi.Filter.BelongsTo = StructureInteractions.Layer_vehicle;
            //    break;
            //case WeaponTypes.Spawn_Fighter_Defensive:
            //    is_multi_targeting = true;
            //    pdi.Filter.CollidesWith = team.HostileTeamMask();
            //    pdi.Filter.BelongsTo = StructureInteractions.Layer_character;
            //    break;
            //case WeaponTypes.Laser_Offensive_Charged:
            //    pdi.Filter.CollidesWith = team.HostileTeamMask();
            //    pdi.Filter.BelongsTo = StructureInteractions.Layer_vehicle;
            //    require_los_check = true;
            //    break;
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
    
    //            if (is_multi_targeting)
    //            {
    //                for (int j = 0; j < hits.Length && j < CombatTargets.MaxCount; ++j)
    //                {
    //                    if (//self_collider.value != Entity.Null && 
    //                        hits[j].Entity == self_collider.value) continue;

    //                    int2 target_coord_axial = HexCoord.FromPosition(hits[j].Position);
    //                    int hex_distance = HexCoord.hex_distance(self_coord_axial, target_coord_axial);
    //                    if (hex_distance <= current_weapon.radius)
    //                    {
    //                        rci.End = hits[j].Position;
    //                        if (require_los_check)
    //                        {
    //                            if (LOSCheck(physics, rci))
    //                            {
    //                                var host = collider_host_ref_array[hits[j].Entity].value;
    //                                combat_targets.append(host);
    //                            }
    //                        }
    //                        else
    //                        {
    //                            var host = collider_host_ref_array[hits[j].Entity].value;
    //                            combat_targets.append(host);
    //                        }
    //                    }
    //                }
    //            }
    //            else
    //            {
    //                // single targeting
    //                int closest = 999;
    //                Entity closest_entity = default;
    //                for (int j = 0; j < hits.Length && j < CombatTargets.MaxCount; ++j)
    //                {
    //                    if (//self_collider.value != Entity.Null && 
    //                        hits[j].Entity == self_collider.value) continue;

    //                    int2 target_coord_axial = HexCoord.FromPosition(hits[j].Position);
    //                    int hex_distance = HexCoord.hex_distance(self_coord_axial, target_coord_axial);
    //                    if (hex_distance <= current_weapon.radius && hex_distance < closest)
    //                    {
    //                        rci.End = hits[j].Position;
    //                        //if (LOSCheck(physics, rci, self_collider.picking_collider, hits[j].Entity))
    //                        if (require_los_check)
    //                        {
    //                            if (LOSCheck(physics, rci))
    //                            {
    //                                closest_entity = collider_host_ref_array[hits[j].Entity].value;
    //                                closest = hex_distance;
    //                            }
    //                        }
    //                        else
    //                        {
    //                            if (collider_host_ref_array.HasComponent(hits[j].Entity))
    //                            {
    //                                closest_entity = collider_host_ref_array[hits[j].Entity].value;
    //                                closest = hex_distance;
    //                            }
    //                            else
    //                            {
    //                                int sdf = 0;
    //                            }
    //                        }
    //                    }
    //                }
    //                if (closest_entity != Entity.Null)
    //                {
    //                    combat_targets.append(closest_entity);
    //                }
    //            }
    //        }
    //        else
    //        {
    //            //Debug.Log(entity.ToString() + " targets nothing @" + i);
    //        }
    //    }
    //}
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
   
    protected override void OnUpdate()
    {
        //var incombat_array = GetComponentLookup<InCombat>();
        //var ecb = new EntityCommandBuffer(Allocator.TempJob);
        //Entities//.WithAll<RefreshCombatTargetsCmd>()
        //.ForEach((Entity entity, //DynamicBuffer<WeaponInfoV2> weapons, 
        //DynamicBuffer<CombatTargets> targets,
        //DynamicBuffer<BeamVisualEntity> visuals
        //) =>
        //{
        //    if (visuals.Length == 0)
        //    {
        //        for (int i = 0; i < targets.Length; ++i)
        //        {
        //            visuals.Add(default);
        //        }
        //    }
        //    for (int i = 0; i < targets.Length; ++i)
        //    {
        //        var current_targets = targets[i];
        //        for (int j = 0; j < current_targets.count; ++j)
        //        {
        //            var target = current_targets.value_at(j);
        //            if (target == Entity.Null) break;
        //            //if (incombat_array.HasComponent(target) == false)
        //            {
        //                // temporary logic for the healing beam cancel.
        //                if (visuals[i].value != Entity.Null)
        //                {
        //                    ecb.DestroyEntity(visuals[i].value);
        //                    visuals[i] = default;
        //                }
        //                current_targets.assign_at(default, j);

        //            }
        //        }
        //    }
        //}).Run();
        //ecb.Playback(EntityManager);
        //ecb.Dispose();
        var physics = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        var c0_array = GetComponentLookup<LocalTransform>();
        // turrets
        Entities//.WithAll<InCombat>()
        .ForEach((Entity entity, DynamicBuffer<WeaponInfoV2> weapons, DynamicBuffer<CombatTarget> targets,
        in LocalTransform lt, in CombatTeam team) =>
        {
            var c0 = CachedTurretTransform.from_localtransform(lt);
            if (weapons.Length != targets.Length) { return; }
            for(int i = 0; i < weapons.Length; ++i)
            {
                bool should_get_targets = false;
                var target = targets[i];
                if (c0_array.HasComponent(target.value) == false)
                {
                    if(target.value.Equals(Entity.Null))
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
                    if(GetTargets_ICD(physics, weapons[i], out CombatTarget _target, c0_array, c0, team, entity))
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
            
        }).Run();


        //NativeList<> 
        // passive cc detect pass
        Entities//.WithAll<InCombat>()
        .ForEach((Entity entity, ref CmdCntrModes ccstates,
        in LocalTransform c0, in CombatTeam team) =>
        {

            if (ccstates.mode == CmdCntrModeTypes.Passive && c0_array.HasComponent(ccstates.invader) == false)
            {
                var cc_wi = new WeaponInfoV2();
                cc_wi.radius = 150f;
                cc_wi.weapon_type = WeaponTypes.CC_Passive_Scan;
                var clt = CachedTurretTransform.from_localtransform(c0);
                if (GetTargets_ICD(physics, cc_wi, out CombatTarget _target, c0_array, clt, team, entity))
                {
                    //Debug.Log("cc gains target " + _target.value);
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

        }).Run();

        //Entities.WithAll<InCombat>()
        //    .WithNone<CachedTurretTransform>()
        //.ForEach((Entity entity, DynamicBuffer<WeaponInfoV2> weapons, DynamicBuffer<CombatTargets> targets,
        //in LocalTransform c0, in Rotation c1, in CombatTeam team
        ////, in ColliderRef self_collider
        //)=>
        //{
        //    var ctt = new CachedTurretTransform() { c0 = c0.Position, c1 = c1.Value };
        //    bool should_get_targets = false;
        //    if (targets[0].count == 0)
        //    {
        //        should_get_targets = true;
        //    }
        //    else
        //    {
        //        var target_entity = targets[0].value_at(0);
        //        if (c0_array.HasComponent(target_entity) == false || IsTargetInRange(c0_array[target_entity].Value, ctt, weapons[0].attack_radians) == false)
        //        {
        //            should_get_targets = false;
        //        }

        //    }
        //    if (should_get_targets)
        //    {
        //        GetTargets_ICD(physics, incombat_array, weapons[0], out CombatTargets _targets, ctt, team, entity);
        //        targets[0] = _targets;
        //    }
        //}).Run();

        ////var movestate_array = GetComponentLookup<LAV2MovementStates>();
        //ecb = new EntityCommandBuffer(Allocator.TempJob);
        //Entities.WithAll<InCombat, DesiredPosition>()
        //    .WithNone<CachedTurretTransform>()
        //.ForEach((Entity entity, DynamicBuffer<CombatTargets> targets,
        ////in ColliderRef self_collider, 
        //ref LAV2MovementStates mstates,
        //in TypeCached types) =>
        //{
        //    var _targets = targets[0];
        //    if (_targets.count > 0)
        //    {
        //        if (types.structure_type != StructureType.Banshee)
        //        {
        //            mstates.hold_position();
        //            //ecb.RemoveComponent<DesiredPosition>(entity);
        //        }
        //    }
        //}).Run();
        //ecb.Playback(EntityManager);
        //ecb.Dispose();
        ////AttackAndMoveCheck3();
        ////Statics.RemoveComponentByType<RefreshCombatTargetsCmd>(EntityManager);

        ////turrets(physics);
        //bio_search_platform();
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
   
   
    //void AttackAndMoveCheck2()
    //{
    //    // check if ships have valid targets.
    //    var _findlist = find_move2target;
    //    var c0_array = GetComponentLookup<LocalTransform>();
    //    var ecb = new EntityCommandBuffer(Allocator.TempJob);
    //    Entities.WithAll<DroneTag>()
    //    .ForEach((Entity entity, DynamicBuffer<CombatTargets> targets, in ColliderRef collider_ref, in DroneTargetPosition ship_move_to) =>
    //    {
    //        bool has_combat_target = false;
    //        for (int i = 0; i < targets.Length; ++i)
    //        {
    //            var weapon_targets = targets[i];
    //            for (int j = 0; j < weapon_targets.count; ++j)
    //            {
    //                Entity target = weapon_targets.value_at(j);
    //                if (target == Entity.Null) break;

    //                if (c0_array.HasComponent(target))
    //                {
    //                    has_combat_target = true;
    //                    break;
    //                }

    //            }
    //        }
    //        if (has_combat_target)
    //        {
    //            ecb.RemoveComponent<DesiredPosition>(collider_ref.value);
    //        }
    //        else
    //        {
    //            var dp = new DesiredPosition();
    //            dp.value = ship_move_to.value;
    //            var self_position = c0_array[entity].Value;
    //            dp.init_finish_line_vec(self_position);

    //            ecb.AddComponent(collider_ref.value, dp);
    //        }
    //    }).Run();
    //    ecb.Playback(EntityManager);
    //    ecb.Dispose();
    //}
    //void AttackAndMoveCheck3()
    //{
    //    // check if ships have valid targets.
    //    var _findlist = find_move2target;
    //    var c0_array = GetComponentLookup<LocalTransform>();
    //    var ecb = new EntityCommandBuffer(Allocator.TempJob);
    //    Entities.WithAll<DroneTag>()
    //    .ForEach((Entity entity, DynamicBuffer<CombatTargets> targets, in ColliderRef collider_ref, in DroneTargetPosition ship_move_to) =>
    //    {
    //        Entity combat_target = default;
    //        if (collider_ref.value == Entity.Null)
    //        {
    //            return;
    //        }
    //        for (int i = 0; i < targets.Length; ++i)
    //        {
    //            var weapon_targets = targets[i];
    //            for (int j = 0; j < weapon_targets.count; ++j)
    //            {
    //                Entity target = weapon_targets.value_at(j);
    //                if (target == Entity.Null) break;

    //                if (c0_array.HasComponent(target))
    //                {
    //                    combat_target = target;
    //                    break;
    //                }

    //            }
    //        }
    //        if (combat_target != Entity.Null)
    //        {
    //            var average = (c0_array[combat_target].Value + c0_array[entity].Value) / 2f;
    //            var dp = new DesiredPosition();
    //            //dp.value = c0_array[combat_target].Value;
    //            dp.value = average;

    //            //dp.init_finish_line_vec(self_position);

    //            ecb.AddComponent(collider_ref.value, dp);
    //            var self_position = c0_array[entity].Value;
    //            Debug.DrawLine(self_position, dp.value, Color.yellow, 0.5f);
    //        }
    //        else
    //        {
    //            var dp = new DesiredPosition();
    //            dp.value = ship_move_to.value;
    //            //var self_position = c0_array[entity].Value;
    //            //dp.init_finish_line_vec(self_position);

    //            ecb.AddComponent(collider_ref.value, dp);
    //            //Debug.DrawLine(self_position, dp.value, Color.white);
    //        }
    //    }).Run();
    //    ecb.Playback(EntityManager);
    //    ecb.Dispose();
    //}

}
