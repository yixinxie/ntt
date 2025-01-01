//#define USE_MULTIPASS

//using System.Numerics;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
//using static Unity.Mathematics.math;

// changed on 2022-5-30, to fix the huge simulation discrepancy between pc and android
//[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
//[UpdateAfter(typeof(FighterLaunchSystem))]
public partial class LocalAvoidanceSystem : SystemBase
{
    public const float radiusOfInterest_Multiplier = 3f;
    public int frame_counter;
    NativeList<Entity> finishers;
    public NativeList<codepath_states> codepaths;
    // for character's goal check.

    protected override void OnCreate()
    {
        base.OnCreate();
        finishers = new NativeList<Entity>(Allocator.Persistent);
        codepaths = new NativeList<codepath_states>(1024, Allocator.Persistent);
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        finishers.Dispose();
        codepaths.Dispose();
    }
    public static void detour_eval(LocalTransform c0, ref MovementInfo mi, 
        NativeArray<LAAdjacentEntity> adj_entities, DesiredPosition desired,
        ComponentLookup<LocalTransform> adjPositions,
        ComponentLookup<MovementInfo> adj_mi_lookup)
    {
        float3 self_pos = c0.Position;


        //float3 goal_dir_normalized = (distance2goal > float.Epsilon) ? mi.current_desired_dir / distance2goal : mi.current_desired_dir;
        float3 goal_dir_normalized = mi.current_desired_dir;
        var goal_axial = HexCoord.FromPosition(mi.current_desired_dir);

        float3 adj_position_sum = 0f;
        float3 separation = 0f;
        float3 adj_velocity_sum = 0f; // alignment
        bool block_checked = false;
        //NativeList<int2> blockade_list = new NativeList<int2>(Allocator.Temp);
        var self_axial = HexCoord.FromPosition(self_pos);
        var to_rounded_hcenter = HexCoord.ToPosition(self_axial) - self_pos;
        NativeArray<byte> occupancies = new NativeArray<byte>(6, Allocator.Temp);
        for (int i = 0; i < adj_entities.Length; ++i)
        {
            Entity adj_entity = adj_entities[i].value;

            if (adjPositions.HasComponent(adj_entity) == false)
            {
                //int sdf = 0;
                continue;
            }
            var adjpos = adjPositions[adj_entity].Position;

            //var to_adj_diff = adjpos - self_pos;
            //to_adj_diff = math.normalizesafe(to_adj_diff, 0f);
            //var adj_axial = HexCoord.FromPosition(to_adj_diff);

            var adj_axial = HexCoord.FromPosition(adjpos + to_rounded_hcenter);
            //var adj_axial = HexCoord.FromPosition(adjpos);
            adj_axial -= self_axial;

            var adj_mi = adj_mi_lookup[adj_entity];
            if (adj_mi.move_state == MovementStates.HoldPosition && HexCoord.hex_distance(0, adj_axial) == 1)
            {
                if (goal_axial.Equals(adj_axial))
                {
                    Debug.DrawLine(adjpos, adjpos + new float3(0f, 1f, 0f), Color.red);
                    if (block_checked == false)
                    {
                        float sign = 0f;
                        if (mi.blocked_state == 0)
                        {
                            if (mi.move_state == MovementStates.Pushable)
                            {
                                int sdf = 0;
                            }
                            // decide which direction to take for this detour
                            mi.blocked_state = 1;

                            sign = (mi.blocked_state == 1) ? 1f : -1f;
                            var q = quaternion.AxisAngle(new float3(0f, 1f, 0f), math.radians(60f) * sign);
                            // rotate goal_axial
                            mi.current_desired_dir = math.mul(q, HexCoord.ToPosition(adj_axial));
                            //Debug.Log();
                        }
                        else
                        {
                            if (mi.move_state == MovementStates.Pushable)
                            {
                                int sdf = 0;
                            }

                            sign = (mi.blocked_state == 1) ? 1f : -1f;
                            var q = quaternion.AxisAngle(new float3(0f, 1f, 0f), math.radians(60f) * sign);
                            mi.current_desired_dir = math.mul(q, mi.current_desired_dir);

                        }

                        block_checked = true;
                    }

                }
                var occupied_dir_index = HexCoord.offset2dir_index(adj_axial);
                occupancies[occupied_dir_index] = 1;
            }

        }
        if (block_checked == false && mi.blocked_state != 0)
        {
            if (mi.move_state == MovementStates.Pushable)
            {
                int sdf = 0;
            }
            // reverse rotate 60 degrees and check
            float sign = (mi.blocked_state == 1) ? 1f : -1f;
            quaternion q = quaternion.AxisAngle(new float3(0f, 1f, 0f), math.radians(60f) * -sign);
            var test_dir = math.mul(q, mi.current_desired_dir);
            var test_offset = HexCoord.FromPosition(test_dir);
            var test_dir_index = HexCoord.offset2dir_index(test_offset);
            if (occupancies[test_dir_index] == 0)
            {
                mi.current_desired_dir = test_dir;
                var goal_dir_index = HexCoord.offset2dir_index(goal_axial);
                if (goal_dir_index == test_dir_index)
                {
                    mi.blocked_state = 0;
                    mi.current_desired_dir = math.normalize(desired.value - c0.Position);
                    Debug.DrawLine(self_pos, self_pos + mi.current_desired_dir, Color.yellow, 0.5f, false);
                }
            }

        }
    }
    public enum CodePathTypes : ushort
    {
        None = 0,
        HoldPosition0,
        HoldPosition1,
        HoldPosition2,
        HoldPosition3_0,
        HoldPosition3_1,
        HexDistance3,
        Pushable,
        NonBlocking0,
        Relax,
        Ret2Goal,

    }
    public struct codepath_states:IEquatable<codepath_states>
    {
        public CodePathTypes type;
        public bool Equals(codepath_states other)
        {
            return type == other.type;
        }
    }
    void branches(byte debug_index, CodePathTypes _type, NativeList<codepath_states> prev_codepaths, NativeList<codepath_states> curret_cps)
    {
        if (debug_index == 1)
        {
            var cps = new codepath_states() { type = _type };
            var ptr = curret_cps.Length;
            if (ptr < prev_codepaths.Length && prev_codepaths[ptr].Equals(cps) == false)
            {
                // debug state changes
                int sdf = 0;
            }
            //_codepaths.Add(new codepath_states() { type = CodePathTypes.HoldPosition0 });
            curret_cps.Add(cps);
            if(curret_cps.Length > prev_codepaths.Length)
            {
                int sdf = 0;
            }
        }
    }
    protected override void OnUpdate()
    {
        frame_counter++;
        if(BoidsParameters.self != null)
        {
            float avoid_factor = BoidsParameters.self.avoid_factor;
            float cohesion_factor = BoidsParameters.self.cohesion_factor;
            float speedavg_factor = BoidsParameters.self.speedavg_factor;
            float goal_factor = BoidsParameters.self.goal_factor;
            Entities.ForEach((ref BoidsCoeffs bc) =>
            {
                bc.avoid_factor = avoid_factor;
                bc.cohesion_factor = cohesion_factor;
                bc.speedavg_factor = speedavg_factor;
                bc.goal_factor = goal_factor;
            }).Run();
        }
        //float dt = World.Time.DeltaTime;
        float dt = 0.008f;
        Profiler.BeginSample("LA2 - overlap query");
        var _physics = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        //var _physics = Statics.GetPhysics();
        Entities
            .WithReadOnly(_physics)
            .WithAll<DesiredPosition>().ForEach((Entity entity, DynamicBuffer<LAAdjacentEntity> adj_entities, in LocalTransform c1, in MovementInfo mi) =>
            {
                PointDistanceInput inp = new PointDistanceInput();
                inp.Position = c1.Position;
                inp.MaxDistance = radiusOfInterest_Multiplier * mi.self_radius;
                inp.Filter.BelongsTo = StructureInteractions.Layer_ground_vehicle_scan | StructureInteractions.Layer_obstacle;
                inp.Filter.CollidesWith = uint.MaxValue;
                inp.Filter.GroupIndex = 0;

                NativeList<DistanceHit> hitsArray = new NativeList<DistanceHit>(Allocator.Temp);
                _physics.CalculateDistance(inp, ref hitsArray);
                adj_entities.Clear();
                for (int i = 0; i < hitsArray.Length; ++i)
                {
                    if (hitsArray[i].Entity == entity) continue;
                    adj_entities.Add(new LAAdjacentEntity() { value = hitsArray[i].Entity, distance = hitsArray[i].Distance });
                }
                hitsArray.Dispose();
            }
        ).ScheduleParallel();
        CompleteDependency();

        Profiler.EndSample();

        Profiler.BeginSample("LA2 - calculate influences");
        var adjPositions = GetComponentLookup<LocalTransform>();
        var adjVelocities = GetComponentLookup<LastFrameVelocity>();
        var adj_mi_lookup = GetComponentLookup<MovementInfo>();
        //bool early = false;
        //if (early)
        //{
        //    Entities
        //    .WithoutBurst()
        //    .ForEach((Entity entity, DynamicBuffer<LAAdjacentEntity> adj_entities, ref MovementInfo mi, in DesiredPosition desired,
        //    in BoidsCoeffs boids_coeffs, in LocalTransform c0) =>
        //    {
        //        if (mi.move_state == MovementStates.HoldPosition) return;
        //        var mi_after = mi;
        //        LAOccuTest.self.mi_before = mi; 
        //        detour_eval(c0, ref mi_after, adj_entities.ToNativeArray(Allocator.Temp), desired, adjPositions, adj_mi_lookup);
        //        LAOccuTest.self.mi_after = mi_after;
        //        LAOccuTest.self.target = entity;

        //        Debug.DrawLine(c0.Position, c0.Position + mi.current_desired_dir, Color.gray, 0.016f, false);
        //        Debug.DrawLine(c0.Position, c0.Position + mi_after.current_desired_dir, Color.white, 0.016f, false);
        //    }).Run();
        //    return;
        //}
        var fc = frame_counter;
        var _codepaths = codepaths;
        if(frame_counter == 53)
        {
            int sdf = 0;
        }
        NativeList<codepath_states> curret_cps = new NativeList<codepath_states>(1024, Allocator.TempJob);
        Entities.WithoutBurst()
        .ForEach((Entity entity, DynamicBuffer<LAAdjacentEntity> adj_entities, ref MovementInfo mi, in DesiredPosition desired, in BoidsCoeffs boids_coeffs, in LocalTransform c0) =>
        {
            if(mi.debug_index == 1)
            {
                int sdf = 0;
            }
            float3 self_pos = c0.Position;
            float3 prev_velocity = adjVelocities[entity].value;


            //influence.value = desired.value - self_pos;
            float distance2goal = math.distance(desired.value, c0.Position);
            //influence.value /= distance2goal;

            //float3 goal_dir_normalized = (distance2goal > float.Epsilon) ? mi.current_desired_dir / distance2goal : mi.current_desired_dir;
            float3 goal_dir_normalized = mi.current_desired_dir;
            var goal_axial = HexCoord.FromPosition(mi.current_desired_dir);

            float3 adj_position_sum = 0f;
            float3 separation = 0f;
            float3 adj_velocity_sum = 0f; // alignment
            int adj_count = 0;
            bool block_checked = false;
            //NativeList<int2> blockade_list = new NativeList<int2>(Allocator.Temp);
            var self_axial = HexCoord.FromPosition(self_pos);
            var to_rounded_hcenter = HexCoord.ToPosition(self_axial) - self_pos;
            NativeArray<byte> occupancies = new NativeArray<byte>(6, Allocator.Temp);
            for (int i = 0; i < adj_entities.Length; ++i)
            {
                Entity adj_entity = adj_entities[i].value;

                //if (adjPositions.HasComponent(adj_entity) == false)
                //{
                //    //int sdf = 0;
                //    continue;
                //}
                var adjpos = adjPositions[adj_entity].Position;

                //var to_adj_diff = adjpos - self_pos;
                //to_adj_diff = math.normalizesafe(to_adj_diff, 0f);
                //var adj_axial = HexCoord.FromPosition(to_adj_diff);

                var adj_axial = HexCoord.FromPosition(adjpos + to_rounded_hcenter);
                //var adj_axial = HexCoord.FromPosition(adjpos);
                adj_axial -= self_axial;

                var adj_mi = adj_mi_lookup[adj_entity];
                if (adj_mi.move_state == MovementStates.HoldPosition)
                {
                    
                    branches(mi.debug_index, CodePathTypes.HoldPosition0, _codepaths, curret_cps);
                    if (goal_axial.Equals(adj_axial) && HexCoord.hex_distance(0, adj_axial) == 1)
                    {
                        branches(mi.debug_index, CodePathTypes.HoldPosition1, _codepaths, curret_cps);
                        if (block_checked == false)
                        {
                            block_checked = true;

                            branches(mi.debug_index, CodePathTypes.HoldPosition2, _codepaths, curret_cps);
                            float sign = 0f;
                            if (mi.blocked_state == 0)
                            {
                                branches(mi.debug_index, CodePathTypes.HoldPosition3_0, _codepaths, curret_cps);
                                //if (mi.move_state == MovementStates.Pushable)
                                //{
                                //    int sdf = 0;
                                //}
                                // decide which direction to take for this detour
                                mi.blocked_state = 1;

                                sign = (mi.blocked_state == 1) ? 1f : -1f;
                                var q = quaternion.AxisAngle(new float3(0f, 1f, 0f), math.radians(60f) * sign);
                                // rotate goal_axial
                                mi.current_desired_dir = math.mul(q, HexCoord.ToPosition(adj_axial));
                                //Debug.Log();
                            }
                            else
                            {
                                branches(mi.debug_index, CodePathTypes.HoldPosition3_1, _codepaths, curret_cps);
                                //if (mi.move_state == MovementStates.Pushable)
                                //{
                                //    int sdf = 0;
                                //}

                                sign = (mi.blocked_state == 1) ? 1f : -1f;
                                var q = quaternion.AxisAngle(new float3(0f, 1f, 0f), math.radians(60f) * sign);
                                mi.current_desired_dir = math.mul(q, mi.current_desired_dir);

                            }

                            
                        }

                    }
                    if (HexCoord.hex_distance(0, adj_axial) == 1)
                    {
                        branches(mi.debug_index, CodePathTypes.HexDistance3, _codepaths, curret_cps);
                        var occupied_dir_index = HexCoord.offset2dir_index(adj_axial);
                        occupancies[occupied_dir_index] = 1;
                    }
                }
                else
                {
                    branches(mi.debug_index, CodePathTypes.Pushable, _codepaths, curret_cps);
                    float surface2surface = adj_entities[i].distance - mi.self_radius;
                    surface2surface = math.clamp(surface2surface, 0.01f, surface2surface);

                    if (surface2surface < mi.self_radius)
                    {
                        var d = math.distance(self_pos, adjpos);
                        if (d > float.Epsilon)
                        {
                            var repel_dir = (self_pos - adjpos) / d;
                            float repel_force = 1f / surface2surface;
                            var sep_tmp = repel_dir * repel_force;

                            //Debug.DrawLine(self_pos, self_pos + sep_tmp, Color.red, 0.016f, false);
                            separation += sep_tmp;
                        }
                    }
                    if (adjVelocities.HasComponent(adj_entity))
                    {
                        adj_velocity_sum += adjVelocities[adj_entity].value;
                        adj_position_sum += adjpos;
                        adj_count++;
                    }
                }
            }
            if (block_checked == false && mi.blocked_state != 0)
            {
                branches(mi.debug_index, CodePathTypes.NonBlocking0, _codepaths, curret_cps);

                // reverse rotate 60 degrees and check
                float sign = (mi.blocked_state == 1) ? 1f : -1f;
                quaternion q = quaternion.AxisAngle(new float3(0f, 1f, 0f), math.radians(60f) * -sign);
                var test_dir = math.mul(q, mi.current_desired_dir);
                var test_offset = HexCoord.FromPosition(test_dir);
                var test_dir_index = HexCoord.offset2dir_index(test_offset);
                if (occupancies[test_dir_index] == 0)
                {
                    branches(mi.debug_index, CodePathTypes.Relax, _codepaths, curret_cps);
                    mi.current_desired_dir = test_dir;
                    var new_to_goal = math.normalize(desired.value - c0.Position);
                    var goal_dir_index = HexCoord.offset2dir_index(HexCoord.FromPosition(new_to_goal));
                    if (goal_dir_index == test_dir_index)
                    {
                        branches(mi.debug_index, CodePathTypes.Ret2Goal, _codepaths, curret_cps);
                        mi.blocked_state = 0;
                        mi.current_desired_dir = new_to_goal;
                        Debug.DrawLine(self_pos, self_pos + mi.current_desired_dir, Color.yellow, 0.5f, false);
                    }
                }
            }
            //Debug.DrawLine(self_pos, self_pos + mi.current_desired_dir, Color.yellow, 0.016f, false);
            if (adj_count > 0)
            {
                adj_velocity_sum /= adj_count;
            }
            adj_position_sum = (adj_count > 1) ? adj_position_sum / adj_count : self_pos;
            //Debug.DrawLine(self_pos, self_pos + separation * boids_coeffs.avoid_factor, Color.red, 0.016f, false);
            //Debug.DrawLine(self_pos, self_pos + (adj_position_sum - self_pos) * boids_coeffs.cohesion_factor, Color.green, 0.016f, false);
            //Debug.DrawLine(self_pos, self_pos + (adj_velocity_sum - prev_velocity) * boids_coeffs.speedavg_factor, Color.red, 0.016f, false);
            //Debug.DrawLine(self_pos, self_pos + (goaldir) * boids_coeffs.goal_factor, Color.blue, 0.016f, false);
            prev_velocity = prev_velocity +
                separation * boids_coeffs.avoid_factor +
                (adj_position_sum - self_pos) * boids_coeffs.cohesion_factor +
                (adj_velocity_sum - prev_velocity) * boids_coeffs.speedavg_factor +
                goal_dir_normalized * boids_coeffs.goal_factor;

            mi.external_influence = prev_velocity;
            mi.distance2goal = distance2goal;
            //externalInfluence.value.y = 0f;
            //externalInfluence.value = influence;
            //}).ScheduleParallel();
            //CompleteDependency();
        }).Run();
        Profiler.EndSample();
        codepaths.Clear();
        codepaths.AddRange(curret_cps.AsArray());
        curret_cps.Dispose();

        Profiler.BeginSample("LA2 - apply influence");
        Entities
            //.WithNone<DisabledDuration>()
            //.WithoutBurst().WithStructuralChanges() // debug only
            //.WithAll<LocalAvoidance>()
            .ForEach((Entity entity,

            ref LastFrameVelocity lfv,
            ref LocalTransform c0,
            ref FrameDisplacement frame_disp,
            //, ref RotationChangeLimiter limiter
            in MovementInfo moveinfo,
            in DesiredPosition dp
            //, in Translation selfPosition
            ) =>
            {
                float influence_magnitude = math.distance(moveinfo.external_influence, 0f);
                float3 up = math.mul(c0.Rotation, new float3(0f, 1f, 0f)); // the actual up direction the unit is facing.
                float3 forward = math.mul(c0.Rotation, new float3(0f, 0f, 1f)); // the actual direction the unit is facing.
                float3 normalized_influence = 0f;
                float speed_scale = 0f;
                if (influence_magnitude > float.Epsilon && moveinfo.move_state != MovementStates.HoldPosition)
                {
                    normalized_influence = moveinfo.external_influence / influence_magnitude;
                    float3 adjusted_facing = Vector3.RotateTowards(forward, normalized_influence, moveinfo.angular_speed * dt, 0f);
                    // speedScale is a scalar that measures how much the adjusted_facing aligns with the direction it wants to go. apply this as a factor in the final velocity.
                    float scale_from2goal = 1f;

                    //if (dp.goal_scale == 1.0f)
                    //{
                    //    scale_from2goal = clamp(influence.distance2goal / 20f, 0f, 1f);
                    //}

                    speed_scale = math.clamp(math.dot(adjusted_facing, normalized_influence) * scale_from2goal, 0f, 1f);
                    c0.Rotation = Quaternion.LookRotation(adjusted_facing, up);
                }
                //float3 frame_velocity = adjusted_facing * moveinfo.movement_speed * speedScale * slow_coeff;
                float3 frame_velocity = normalized_influence * moveinfo.speed * speed_scale;
                frame_disp.value = frame_velocity * dt;
                lfv.value = frame_velocity;
                //facing.value = adjusted_facing;
            }).Run();
        //}).ScheduleParallel();
        //CompleteDependency();
        Entities
            //.WithNone<DisabledDuration>()
            .WithAll<DesiredPosition>()
            //.WithAll<LocalAvoidance>()
            .ForEach((ref LocalTransform localPosition, in FrameDisplacement disp) =>
            {
                var newval = localPosition.Position + disp.value;
                localPosition.Position = newval;

            }).ScheduleParallel();
        CompleteDependency();
        Profiler.EndSample();

        // update goal coefficient
        Entities
        .ForEach((ref BoidsCoeffs coeffs, in LocalTransform c0, in DesiredPosition dp) =>
        {
            float distance = math.distance(c0.Position, dp.value);
            float goal_coeff = distance / 10f;
            if (goal_coeff > coeffs.goal_factor_max) goal_coeff = coeffs.goal_factor_max;
            coeffs.goal_factor = goal_coeff;

        }).Run();

        //finishers.Clear();
        //var _finishers = finishers;
        //Entities.WithNone<DroneTag>()
        //.WithAll<LocalAvoidance>().ForEach((Entity entity, in Translation c0, in DesiredPosition dp) =>
        //{
        //    float distance = math.distance(c0.Value, dp.value);
        //    if(distance < 0.3f)
        //    {
        //        _finishers.Add(entity);
        //    }
        //}).Run();
        //if (finishers.Length > 0)
        //{
        //    EntityManager.RemoveComponent<DesiredPosition>(finishers.ToArray(Allocator.Temp));
        //}


    }
}
public struct LAV2MovementStates:IComponentData
{
    public MovementStates move_state;
    public float3 cached_last_dir;
    public float3 previous_wall_center_dir;
    public byte left1_right2_detour_dir;
    public float3 tmp_goal_dir;
    public float3 tmp_gap_dir;
    public float3 tmp_repel_force;
    public void idle()
    {
        left1_right2_detour_dir = 0;
        move_state = MovementStates.Pushable;
        cached_last_dir = Vector3.zero;
    }
    public void hold_position()
    {
        left1_right2_detour_dir = 0;
        move_state = MovementStates.HoldPosition;
        cached_last_dir = Vector3.zero;
    }
}

[InternalBufferCapacity(8)]
public struct LAV2QuantizedOccupancy : IBufferElementData
{
    public float value;
}