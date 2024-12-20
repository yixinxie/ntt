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
        float dt = World.Time.DeltaTime;
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
            .WithAll<DesiredPosition>().ForEach((Entity entity, DynamicBuffer<AdjacentEntities> c0, in LocalTransform c1, in LA_Radius radius) =>
            {
                PointDistanceInput inp = new PointDistanceInput();
                inp.Position = c1.Position;
                inp.MaxDistance = radiusOfInterest_Multiplier * radius.value;
                inp.Filter.BelongsTo = StructureInteractions.Layer_raycast;
                inp.Filter.CollidesWith = StructureInteractions.Layer_vehicle | StructureInteractions.Layer_obstacle;
                inp.Filter.GroupIndex = 0;

                NativeList<DistanceHit> hitsArray = new NativeList<DistanceHit>(Allocator.Temp);
                _physics.CalculateDistance(inp, ref hitsArray);
                c0.Clear();
                for (int i = 0; i < hitsArray.Length; ++i)
                {
                    if (hitsArray[i].Entity == entity) continue;
                    c0.Add(new AdjacentEntities() { value = hitsArray[i].Entity, distance = hitsArray[i].Distance });
                }
                hitsArray.Dispose();
            }
        ).ScheduleParallel();
        CompleteDependency();

        Profiler.EndSample();

        Profiler.BeginSample("LA2 - calculate influences");

        //var adjRadiuses = GetComponentDataFromEntity<LA_Radius>(true);
        //var dp_array = GetComponentLookup<DesiredPosition>();
        var adjPositions = GetComponentLookup<LocalTransform>();
        var adjVelocities = GetComponentLookup<LastFrameVelocity>();
        var adj_mi_lookup = GetComponentLookup<MovementInfo>();

        Entities
        .ForEach((Entity entity, DynamicBuffer<AdjacentEntities> adjIndices, ref ExternalInfluence influence, ref MovementInfo mi, in DesiredPosition desired, 
        in LA_Radius self_radius, in BoidsCoeffs boids_coeffs, in LocalTransform c1) =>
        {

            float3 self_pos = adjPositions[entity].Position;
            float3 prev_velocity = adjVelocities[entity].value;
            float3 to_goal = mi.current_desired_dir;

            //influence.value = desired.value - self_pos;
            float distance2goal = math.distance(to_goal, 0f);
            //influence.value /= distance2goal;

            float3 goal_dir_normalized = (distance2goal > float.Epsilon) ? to_goal / distance2goal : to_goal;
            var goal_axial = HexCoord.FromPosition(goal_dir_normalized);

            float3 adj_position_sum = 0f;
            float3 separation = 0f;
            float3 adj_velocity_sum = 0f; // alignment
            int adj_count = 0;
            bool block_checked = false;
            //NativeList<int2> blockade_list = new NativeList<int2>(Allocator.Temp);
            var self_axial = HexCoord.FromPosition(self_pos);
            var to_rounded_hcenter = HexCoord.ToPosition(self_axial) - self_pos;
            for (int i = 0; i < adjIndices.Length; ++i)
            {
                Entity adj_entity = adjIndices[i].value;

                if (adjPositions.HasComponent(adj_entity) == false)
                {
                    //int sdf = 0;
                    continue;
                }
                var adjpos = adjPositions[adj_entity].Position;

                if (block_checked == false)
                {
                    var adj_axial = HexCoord.FromPosition(adjpos + to_rounded_hcenter);
                    adj_axial -= self_axial;
                    if (goal_axial.Equals(adj_axial))
                    {
                        var adj_mi = adj_mi_lookup[adj_entity];
                        if(mi.move_state == MovementStates.HoldPosition)
                        {
                            block_checked = true;
                            
                            if(mi.blocked_state == 0)
                            {
                                quaternion q = quaternion.AxisAngle(new float3(0f, 1f, 0f), math.radians(60f));
                                // rotate goal_axial
                                mi.current_desired_dir = math.mul(q, HexCoord.ToPosition(goal_axial));
                                mi.blocked_state = 1;
                            }
                            else
                            {
                                float sign = (mi.blocked_state == 1) ? -1f : 1f;
                                quaternion q = quaternion.AxisAngle(new float3(0f, 1f, 0f), math.radians(60f) * sign);
                                mi.current_desired_dir = math.mul(q, mi.current_desired_dir);
                                if(math.dot(mi.current_desired_dir, desired.value) < 0.1f)
                                {
                                    mi.blocked_state = 0;
                                }
                            }

                            // decide which direction to take for this detour


                            block_checked = true;
                        }
                    }
                }

                float surface2surface = adjIndices[i].distance - self_radius.value;
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
            
            //Debug.DrawLine(self_pos, self_pos + around_blockade_vector, Color.yellow, 0.016f, false);
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