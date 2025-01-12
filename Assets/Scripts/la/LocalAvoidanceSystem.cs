//#define USE_MULTIPASS

//using System.Numerics;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
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
    public static void regularize_dir_index(ref int test_dir)
    {
        if (test_dir < 0)
        {
            test_dir += 6;
        }
        else if (test_dir >= 6)
        {
            test_dir -= 6;
        }
    }
    public static bool detour_eval(int2 goal_axial, float3 goal_dir, ref MovementInfo mi, NativeArray<byte> occupancies, out bool recalc_destionation)
    {
        recalc_destionation = false;
        int goal_dir_index = HexCoord.offset2dir_index(goal_axial);
        if (mi.blocked_state == 0)
        {
            if (occupancies[goal_dir_index] < 2)
            {
                // left right pick: 1: ccw,
                // 2: cw
                byte left_right_pick = 255;
                int dir_pick = 255;
                //int sign = (left_right_pick == 1) ? 1 : -1;
                
                for(int i = 1; i <= 3; ++i)
                {
                    var test_dir = (goal_dir_index + i);
                    regularize_dir_index(ref test_dir);
                    if (occupancies[test_dir] > 1)
                    {
                        left_right_pick = 1;
                        dir_pick = test_dir;
                        break;
                    }
                    test_dir = (goal_dir_index - i);
                    regularize_dir_index(ref test_dir);
                    if (occupancies[test_dir] > 1)
                    {
                        left_right_pick = 2;
                        dir_pick = test_dir;
                        break;
                    }
                }
                if(left_right_pick == 255)
                {
                    return true;
                }
                mi.blocked_state = left_right_pick;
                mi.detour_dir = (byte)dir_pick;
                mi.refresh_dd(goal_dir);
            }
        }
        else
        {
            var test_dir = mi.detour_dir;
            var rot_dir = (mi.blocked_state == 1) ? 1 : -1;
            if (occupancies[mi.detour_dir] < 2)
            {
                
                // detour
                for(int i = 0; i < 2; ++i)
                {
                    int this_dir = test_dir + rot_dir * i;
                    regularize_dir_index(ref this_dir);
                    if (occupancies[this_dir] > 1)
                    {
                        mi.detour_dir = (byte)this_dir;
                        mi.refresh_dd(goal_dir);
                        break;
                    }
                }
            }
            else 
            {
                // check if we can turn this one dir towards the goal dir
                
                //for (int i = 1; i < 6; ++i)
                int i = 1;
                {
                    int this_dir = test_dir - rot_dir * i;
                    regularize_dir_index(ref this_dir);
                    if (occupancies[this_dir] > 1)
                    {
                        mi.detour_dir = (byte)this_dir;
                        mi.refresh_dd(goal_dir);
                        //break;
                    }
                }

                if (mi.detour_dir == goal_dir_index)
                {
                    mi.blocked_state = 0;
                    mi.detour_dir = 255;
                    mi.refresh_dd(goal_dir);
                    recalc_destionation = true;
                }
            }
        }
        return false;
    }
    public static void horizon_eval_array(LocalTransform c0,
        NativeArray<LAAdjacentEntity> adj_entities,
        ComponentLookup<LocalTransform> adjPositions,
        ComponentLookup<MovementInfo> adj_mi_lookup, NativeArray<byte> occupancies, NativeArray<float> occu_floats)
    {
        float3 self_pos = c0.Position;
        for (int i = 0; i < occupancies.Length; ++i)
        {
            occupancies[i] = byte.MaxValue;
            occu_floats[i] = float.MaxValue;
        }
        for (int i = 0; i < adj_entities.Length; ++i)
        {
            Entity adj_entity = adj_entities[i].value;

            var adj_pos = adjPositions[adj_entity].Position;
            var adj_mi = adj_mi_lookup[adj_entity];

            horizon_eval_single(self_pos, adj_pos, adj_mi, occupancies, occu_floats);
           
        }
    }
    public static void horizon_eval_single(float3 self_pos,
        //NativeArray<LAAdjacentEntity> adj_entities,
        float3 adj_pos, MovementInfo adj_mi, NativeArray<byte> occupancies, NativeArray<float> occu_floats)
    {
        if (adj_mi.move_state >= MovementStates.HoldPosition)
        {
            var to_adj = adj_pos - self_pos;
            var distance = math.distance(0f, to_adj);
            if (distance > float.Epsilon)
            {
                to_adj /= distance;
                var dir_index = HexCoord.offset2dir_index(HexCoord.FromPosition(to_adj));
                byte int_distance = (byte)math.ceil(distance);
                if (int_distance < occupancies[dir_index])
                {
                    occupancies[dir_index] = int_distance;
                    if (distance < 0.667f)
                    {
                        var other1 = dir_index + 1;
                        regularize_dir_index(ref other1);
                        occupancies[other1] = int_distance;
                        other1 = dir_index - 1;
                        regularize_dir_index(ref other1);
                        occupancies[other1] = int_distance;
                    }
                }
                if (occu_floats.IsCreated)
                {
                    if (distance < occu_floats[dir_index])
                    {
                        occu_floats[dir_index] = distance;
                    }
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
    public struct codepath_states : IEquatable<codepath_states>
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
            if (curret_cps.Length > prev_codepaths.Length)
            {
                int sdf = 0;
            }
        }
    }
    public void debug_laadj(Entity entity, NativeList<LAAdjacentEntity> adj_entities, LocalTransform c1, MovementInfo mi)
    {
        var _physics = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        laadj(entity, adj_entities, c1, mi, _physics);
    }
    public static void laadj(Entity entity, NativeList<LAAdjacentEntity> adj_entities, LocalTransform c1, MovementInfo mi, PhysicsWorldSingleton _physics)
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
    protected override void OnUpdate()
    {
        frame_counter++;
        if (BoidsParameters.self != null)
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
        float dt = 0.016f;
        Profiler.BeginSample("LA2 - overlap query");
        var _physics = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        //var _physics = Statics.GetPhysics();
        Entities
            .WithReadOnly(_physics)
            .WithAll<DesiredPosition>().ForEach((Entity entity, DynamicBuffer<LAAdjacentEntity> adj_entities, in LocalTransform c1, in MovementInfo mi) =>
            {
                adj_entities.Clear();
                if (mi.move_state == MovementStates.HoldPosition) return;
                NativeList<LAAdjacentEntity> tmp = new NativeList<LAAdjacentEntity>(8, Allocator.Temp);
                laadj(entity, tmp, c1, mi, _physics);
                adj_entities.AddRange(tmp.AsArray());
            }
        ).ScheduleParallel();
        CompleteDependency();

        Profiler.EndSample();

        Profiler.BeginSample("LA2 - calculate influences");
        var adjPositions = GetComponentLookup<LocalTransform>();
        var adjVelocities = GetComponentLookup<LastFrameVelocity>();
        var adj_mi_lookup = GetComponentLookup<MovementInfo>();
        //return;
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
        if (frame_counter == 53)
        {
            int sdf = 0;
        }
        NativeList<codepath_states> curret_cps = new NativeList<codepath_states>(1024, Allocator.TempJob);
        Entities.WithoutBurst()
        .ForEach((Entity entity, DynamicBuffer<LAAdjacentEntity> adj_entities, ref MovementInfo mi, ref DesiredPosition desired, in BoidsCoeffs boids_coeffs, in LocalTransform c0, in DBGId dbgid) =>
        {
            mi.external_influence = 0f;
            int adj_count = 0;
            float3 self_pos = c0.Position;
            if (mi.move_state == MovementStates.Moving)
            {
                if (mi.debug_index == 3)
                {
                    int sdf = 0;
                }
                float3 prev_velocity = 0f;// adjVelocities[entity].value;


                //influence.value = desired.value - self_pos;
                float3 dir2dp = desired.value - c0.Position;

                float distance2goal = math.distance(desired.value, c0.Position);
                if (distance2goal > float.Epsilon)
                {
                    dir2dp /= distance2goal;
                }
                //influence.value /= distance2goal;

                //float3 goal_dir_normalized = (distance2goal > float.Epsilon) ? mi.current_desired_dir / distance2goal : mi.current_desired_dir;
                //float3 goal_dir_normalized = mi.current_desired_dir;
                var goal_axial = HexCoord.FromPosition(dir2dp);

                float3 adj_position_sum = 0f;
                float3 separation = 0f;
                float3 adj_velocity_sum = 0f; // alignment
                
                //NativeList<int2> blockade_list = new NativeList<int2>(Allocator.Temp);
                var self_axial = HexCoord.FromPosition(self_pos);
                var to_rounded_hcenter = HexCoord.ToPosition(self_axial) - self_pos;
                NativeArray<byte> occupancies = new NativeArray<byte>(6, Allocator.Temp);
                for (int i = 0; i < occupancies.Length; ++i)
                {
                    occupancies[i] = byte.MaxValue;
                    //occu_floats[i] = float.MaxValue;
                }
                for (int i = 0; i < adj_entities.Length; ++i)
                {
                    Entity adj_entity = adj_entities[i].value;

                    //if (adjPositions.HasComponent(adj_entity) == false)
                    //{
                    //    //int sdf = 0;
                    //    continue;
                    //}
                    var adj_pos = adjPositions[adj_entity].Position;

                    //var to_adj_diff = adjpos - self_pos;
                    //to_adj_diff = math.normalizesafe(to_adj_diff, 0f);
                    //var adj_axial = HexCoord.FromPosition(to_adj_diff);

                    var adj_axial = HexCoord.FromPosition(adj_pos + to_rounded_hcenter);
                    //var adj_axial = HexCoord.FromPosition(adjpos);
                    adj_axial -= self_axial;

                    var adj_mi = adj_mi_lookup[adj_entity];
                    if (adj_mi.move_state >= MovementStates.HoldPosition)
                    {
                        horizon_eval_single(self_pos, adj_pos, adj_mi, occupancies, default);

                    }
                    //else
                    {
                        //branches(mi.debug_index, CodePathTypes.Pushable, _codepaths, curret_cps);
                        float surface2surface = adj_entities[i].distance - mi.self_radius;
                        surface2surface = math.clamp(surface2surface, 0.01f, surface2surface);

                        if (surface2surface < mi.self_radius)
                        {
                            var d = math.distance(self_pos, adj_pos);
                            if (d > float.Epsilon)
                            {
                                var repel_dir = (self_pos - adj_pos) / d;
                                float repel_force = 1f / surface2surface;
                                var sep_tmp = repel_dir * repel_force;

                                //Debug.DrawLine(self_pos, self_pos + sep_tmp, Color.red, 0.016f, false);
                                separation += sep_tmp;
                            }
                        }
                        if (adjVelocities.HasComponent(adj_entity))
                        {
                            adj_velocity_sum += adjVelocities[adj_entity].value;
                            adj_position_sum += adj_pos;
                            adj_count++;
                        }
                    }
                }
                bool stuck = detour_eval(HexCoord.FromPosition(dir2dp), dir2dp, ref mi, occupancies, out bool recalc_dest);
                if (recalc_dest)
                {
                    desired.init_finish_line_vec(self_pos);
                }


                //Debug.DrawLine(self_pos, self_pos + mi.current_desired_dir, Color.yellow, 0.016f, false);
                if (adj_count > 0)
                {
                    adj_velocity_sum /= adj_count;
                }
                adj_position_sum = (adj_count > 1) ? adj_position_sum / adj_count : self_pos;

                var sep_length = math.distance(separation, 0f);
                if (sep_length > float.Epsilon)
                {
                    var clampped = math.clamp(sep_length, 0f, 2f);
                    separation = separation / sep_length * clampped;
                }

                Debug.DrawLine(self_pos, self_pos + separation * boids_coeffs.avoid_factor, Color.red, 0.016f, false);
                //Debug.DrawLine(self_pos, self_pos + (adj_position_sum - self_pos) * boids_coeffs.cohesion_factor, Color.green, 0.016f, false);
                //Debug.DrawLine(self_pos, self_pos + (adj_velocity_sum - prev_velocity) * boids_coeffs.speedavg_factor, Color.red, 0.016f, false);
                Debug.DrawLine(self_pos, self_pos + mi.current_desired_dir * boids_coeffs.goal_factor, Color.blue, 0.016f, false);
                float goal_inf = (mi.move_state == MovementStates.Moving) ? boids_coeffs.goal_factor : 0f;
                prev_velocity = prev_velocity +
                    separation * boids_coeffs.avoid_factor +
                    (adj_position_sum - self_pos) * boids_coeffs.cohesion_factor +
                    (adj_velocity_sum - prev_velocity) * boids_coeffs.speedavg_factor +
                    mi.current_desired_dir * goal_inf;
                if (stuck == false)
                {
                    if (mi.move_state == MovementStates.Stuck)
                    {
                        mi.move_state = MovementStates.Moving;
                    }
                    mi.external_influence = prev_velocity;
                    mi.distance2goal = distance2goal;
                }
                else
                {
                    mi.external_influence = 0f;
                    if (mi.move_state == MovementStates.Moving)
                    {
                        mi.move_state = MovementStates.Stuck;
                    }
                }
            }
            else if(mi.move_state == MovementStates.Idle)
            {
                float3 body_repel_aggr = 0f;
                for (int i = 0; i < adj_entities.Length; ++i)
                {
                    Entity adj_entity = adj_entities[i].value;

                    var adj_pos = adjPositions[adj_entity].Position;

                    //var adj_mi = adj_mi_lookup[adj_entity];
                   
                    //branches(mi.debug_index, CodePathTypes.Pushable, _codepaths, curret_cps);
                    var distance = math.distance(self_pos, adj_pos);
                    //float surface2surface = adj_entities[i].distance - mi.s elf_radius;
                    //surface2surface = math.clamp(surface2surface, 0.01f, surface2surface);
                    if (distance < mi.self_radius * 2f && distance > float.Epsilon)
                    {
                        var repel_dir = self_pos - adj_pos;
                        body_repel_aggr += repel_dir / distance;
                        adj_count++;
                    }
                }
                if (adj_count > 0)
                {

                    mi.external_influence = math.normalizesafe(body_repel_aggr, float3.zero);
                }
            }
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
            //, ref RotationChangeLimiter limiter
            in MovementInfo moveinfo,
            in DesiredPosition dp
            //, in Translation selfPosition
            ) =>
            {
                float3 frame_disp_value = 0f;
                if (moveinfo.move_state == MovementStates.Moving)
                {
                    float influence_magnitude = math.distance(moveinfo.external_influence, 0f);
                    float3 up = math.mul(c0.Rotation, new float3(0f, 1f, 0f)); // the actual up direction the unit is facing.
                    float3 forward = math.mul(c0.Rotation, new float3(0f, 0f, 1f)); // the actual direction the unit is facing.
                    float3 normalized_influence = 0f;
                    float speed_scale = 0f;
                    if (influence_magnitude > float.Epsilon && moveinfo.move_state < MovementStates.HoldPosition)
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
                    frame_disp_value = frame_velocity * dt;
                    lfv.value = frame_velocity;
                    //facing.value = adjusted_facing;

                    
                }
                else if (moveinfo.move_state == MovementStates.Idle)
                {
                    frame_disp_value = moveinfo.external_influence * dt;
                }
                c0.Position = c0.Position + frame_disp_value;
            }).Run();
        //}).ScheduleParallel();
        //CompleteDependency();
        //Entities
        //    //.WithNone<DisabledDuration>()
        //    .WithAll<DesiredPosition>()
        //    //.WithAll<LocalAvoidance>()
        //    .ForEach((ref LocalTransform localPosition, in FrameDisplacement disp) =>
        //    {
        //        var newval = localPosition.Position + disp.value;
        //        localPosition.Position = newval;

        //    }).ScheduleParallel();
        //CompleteDependency();
        Profiler.EndSample();

        // update goal coefficient
        Entities
        .ForEach((ref BoidsCoeffs coeffs, in LocalTransform c0, in DesiredPosition dp) =>
        {
            float distance = math.distance(c0.Position, dp.value);
            float goal_coeff = distance / 10f;
            coeffs.goal_factor = math.clamp(goal_coeff, 0f, coeffs.goal_factor_max);

        }).Run();

        //finishers.Clear();
        //var _finishers = finishers;
        Entities.ForEach((Entity entity, ref LocalTransform c0, ref MovementInfo mi, ref LastFrameVelocity prev_vel, in DesiredPosition dp) =>
        {
            if (mi.move_state != MovementStates.Moving) return;
            //float distance = math.distance(c0.Position, dp.value);
            //if (distance < 0.3f)
            //var distance = dp.distance_2_finish_line(c0.Position);
            if(dp.distance_2_finish_line(c0.Position))
            {
                //Debug.Log(entity.ToString() + " reached");
                mi.move_state = MovementStates.Idle;
                mi.blocked_state = 0;
                mi.current_desired_dir = 0f;
                prev_vel = default;
                //_finishers.Add(entity);
            }
        }).Run();
        //if (finishers.Length > 0)
        //{
        //    EntityManager.RemoveComponent<DesiredPosition>(finishers.ToArray(Allocator.Temp));
        //}


    }
}


[InternalBufferCapacity(8)]
public struct LAV2QuantizedOccupancy : IBufferElementData
{
    public float value;
}