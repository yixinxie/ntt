//#define USE_MULTIPASS

//using System.Numerics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
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

    NativeList<Entity> finishers;
    // for character's goal check.

    protected override void OnCreate()
    {
        base.OnCreate();
        finishers = new NativeList<Entity>(Allocator.Persistent);
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        finishers.Dispose();
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
    protected override void OnUpdate()
    {
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
        //physics = Statics.GetPhysics();
        //Entities.ForEach((ref TranslationCopy c1, in Translation c0) =>
        //{
        //    c1.value = c0.Value;
        //}).ScheduleParallel();
        //CompleteDependency();

        Profiler.BeginSample("LA2 - overlap query");
        var _physics = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        //var _physics = Statics.GetPhysics();
        Entities
            //.WithAll<LocalAvoidance>()
            //.WithNone<DisabledDuration>()
            .WithReadOnly(_physics)
            .WithAll<DesiredPosition>().ForEach((Entity entity, DynamicBuffer<LAAdjacentEntity> adj_entities, in LocalTransform c1, in LA_Radius radius) =>
            {
                PointDistanceInput inp = new PointDistanceInput();
                inp.Position = c1.Position;
                inp.MaxDistance = radiusOfInterest_Multiplier * radius.value;
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
        bool early = false;
        if (early)
        {
            Entities
            .WithoutBurst()
            .ForEach((Entity entity, DynamicBuffer<LAAdjacentEntity> adj_entities, ref ExternalInfluence influence, ref MovementInfo mi, in DesiredPosition desired,
            in LA_Radius self_radius, in BoidsCoeffs boids_coeffs, in LocalTransform c0) =>
            {
                if (mi.move_state == MovementStates.HoldPosition) return;
                var mi_after = mi;
                LAOccuTest.self.mi_before = mi; 
                detour_eval(c0, ref mi_after, adj_entities.ToNativeArray(Allocator.Temp), desired, adjPositions, adj_mi_lookup);
                LAOccuTest.self.mi_after = mi_after;
                LAOccuTest.self.target = entity;

                Debug.DrawLine(c0.Position, c0.Position + mi.current_desired_dir, Color.gray, 0.016f, false);
                Debug.DrawLine(c0.Position, c0.Position + mi_after.current_desired_dir, Color.white, 0.016f, false);
            }).Run();
            return;
        }

        //var adjRadiuses = GetComponentDataFromEntity<LA_Radius>(true);
        //var dp_array = GetComponentLookup<DesiredPosition>();
        

        Entities
            .WithoutBurst()
        .ForEach((Entity entity, DynamicBuffer<LAAdjacentEntity> adj_entities, ref ExternalInfluence influence, ref MovementInfo mi, in DesiredPosition desired, 
        in LA_Radius self_radius, in BoidsCoeffs boids_coeffs, in LocalTransform c0) =>
        {

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
                if (adj_mi.move_state == MovementStates.HoldPosition)
                {
                    if (goal_axial.Equals(adj_axial) && HexCoord.hex_distance(0, adj_axial) == 1)
                    {
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
                    if (HexCoord.hex_distance(0, adj_axial) < 3)
                    {

                        var occupied_dir_index = HexCoord.offset2dir_index(adj_axial);
                        occupancies[occupied_dir_index] = 1;
                    }
                }
                else
                {

                    float surface2surface = adj_entities[i].distance - self_radius.value;
                    surface2surface = math.clamp(surface2surface, 0.01f, surface2surface);

                    if (surface2surface < self_radius.value)
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
                    var new_to_goal = math.normalize(desired.value - c0.Position);
                    var goal_dir_index = HexCoord.offset2dir_index(HexCoord.FromPosition(new_to_goal));
                    if (goal_dir_index == test_dir_index)
                    {
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


            influence.value = prev_velocity;
            influence.distance2goal = distance2goal;
            //externalInfluence.value.y = 0f;
            //externalInfluence.value = influence;
            //}).ScheduleParallel();
            //CompleteDependency();
        }).Run();
        Profiler.EndSample();

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
            in ExternalInfluence influence,
            in DesiredPosition dp
            //, in Translation selfPosition
            ) =>
            {
                float influence_magnitude = math.distance(influence.value, 0f);
                float3 up = math.mul(c0.Rotation, new float3(0f, 1f, 0f)); // the actual up direction the unit is facing.
                float3 forward = math.mul(c0.Rotation, new float3(0f, 0f, 1f)); // the actual direction the unit is facing.
                float3 normalized_influence = 0f;
                float speed_scale = 0f;
                if (influence_magnitude > float.Epsilon && moveinfo.move_state != MovementStates.HoldPosition)
                {
                    normalized_influence = influence.value / influence_magnitude;
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
        //.WithAll<LocalAvoidance>()
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