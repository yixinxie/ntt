#define ROUNDED_QUANTIZATION
#define PLANE_XY
//#define PLANE_XZ
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
/*
* who you could ignore
all units moving in your same general direction you ignore
all units that are idle you'd be able to push out of the way, you ignore
cuts down the number of units significantly
create a sight horizon, plot all our obstacles on that horizon
aim for the gaps
obviously if the center is clear, we go to the center.
if the center is not clear we go to the gap closest to the center
read the minds of the units traveling towards us to see if they are already decided to avoid me in the direction if so 
prevent hallway dance 
-James Anhalt
*/
public class LATest2 : MonoBehaviour
{
    public bool simulate;
    public float velocity = 3f;
    public float angular_velocity = 90f; // default 180 degrees
    public Transform move_target;
    public float radius; // default 0.5
    
    public float search_radius; // default 1.2
    //public float frame_velocity;
    public UnityEngine.Collider self_col;
    public Vector3 goal_dir;
    // Start is called before the first frame update
    public int adj_count;
    public MovementStates movement_state; // when the unit is in an attacking state or hold position state.
    public byte trapped; // when the unit is surrounded by moving units and units in hold position.
    public byte left1_right2_detour_dir;
    public Vector3 previous_wall_center_dir;
    public Vector3 cached_last_dir;
    public int dbg_self_q;

    public const float uncomfort_zone = 1f; // the repel force generates within this value.
    public const float max_repel_force = 1f;

    public const int MaxQuantization = 8;
    public const float min_distance = 0.01f;
    public const float threshold = 0.4f;// corresponding to 0.5 radius
                                 //const float threshold = 0.65f;// corresponding to 0.5 radius
    public NativeArray<float> quantized_occupations;
    DesiredPosition dp;
    //public NativeArray<float> quantized_occupations_repel;
    //public Vector3 previous_detour_dir;
    //public int is_in_detour;
    void Start()
    {
        quantized_occupations = new NativeArray<float>(MaxQuantization, Allocator.Persistent);
        //World.DefaultGameObjectInjectionWorld.GetExistingSystem<PhysicsWorldSingleton>();
        //var physicsWorld = World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>().;
        //physicsWorld.wo
        //var physicsWorldSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<CollisionWorld>();
        //var physicsWorld = physicsWorldSystem.PhysicsWorld;

        //quantized_occupations_repel = new NativeArray<float>(MaxQuantization, Allocator.Persistent);
    }
    private void OnDestroy()
    {
        quantized_occupations.Dispose();
        //quantized_occupations_repel.Dispose();
    }

    // in world space.
    /* non-rounded
    * z
    * ^
    * |
    * ---> x
    * 
    *  6 5
    * 7   4
    * 0   3
    *  1 2
    */

    /*  rounded
    * z
    * ^
    * |
    * ---> x
    * 
    *   6
    *  7 5
    * 0   4
    *  1 3
    *   2
    */
    public static void occupation_quantize_3(float3 target_minus_self, float distance, NativeArray<float> tmp_quantized_occupations)
    {
        
        int range_idx = occupation_quantize_single(target_minus_self);
        if (distance < threshold)
        {
            int minus = (range_idx - 1);
            minus = (minus < 0) ? minus + MaxQuantization : minus;
            int plus = (range_idx + 1) % MaxQuantization;

            tmp_quantized_occupations[minus] = (distance < tmp_quantized_occupations[minus]) ? distance : tmp_quantized_occupations[minus];
            tmp_quantized_occupations[plus] = (distance < tmp_quantized_occupations[plus]) ? distance : tmp_quantized_occupations[plus];
        }
        tmp_quantized_occupations[range_idx] = (distance < tmp_quantized_occupations[range_idx]) ? distance : tmp_quantized_occupations[range_idx];
    }
    public static void occupation_quantize_3(float3 target_minus_self, float distance, DynamicBuffer<LAV2QuantizedOccupancy> tmp_quantized_occupations)
    {

        int range_idx = occupation_quantize_single(target_minus_self);
        if (distance < threshold)
        {
            int minus = (range_idx - 1);
            minus = (minus < 0) ? minus + MaxQuantization : minus;
            int plus = (range_idx + 1) % MaxQuantization;

            if(distance < tmp_quantized_occupations[minus].value)
            {
                tmp_quantized_occupations[minus] = new LAV2QuantizedOccupancy() { value = distance };
            }
            if (distance < tmp_quantized_occupations[plus].value)
            {
                tmp_quantized_occupations[plus] = new LAV2QuantizedOccupancy() { value = distance };
            }
        }
        if(distance < tmp_quantized_occupations[range_idx].value)
            tmp_quantized_occupations[range_idx] = new LAV2QuantizedOccupancy() { value = distance };
    }


    public static int occupation_quantize_single(float3 diff)
    {
#if PLANE_XY
        var range = (Mathf.Atan2(diff.y, -diff.x) + Mathf.PI) / Mathf.PI / 2f;
#elif PLANE_XZ
        var range = (Mathf.Atan2(diff.z, diff.x) + Mathf.PI) / Mathf.PI / 2f;
#endif

        range = Mathf.Clamp(range, 0f, 1f);
        range *= MaxQuantization;
#if ROUNDED_QUANTIZATION
        int range_idx = Mathf.RoundToInt(range);
        if (range_idx == MaxQuantization) range_idx = 0;
#else
        int range_idx = Mathf.FloorToInt(range);
#endif
        return range_idx;
    }
    

    bool is_dir_empty(Vector3 facing)
    {
        int dir_index = occupation_quantize_single(facing);
        return quantized_occupations[dir_index] == float.MaxValue;
    }
    public static bool is_dir_empty(Vector3 facing, DynamicBuffer<LAV2QuantizedOccupancy> quantized_occupations)
    {
        int dir_index = occupation_quantize_single(facing);
        return quantized_occupations[dir_index].value == float.MaxValue;
    }
    public static Vector3 search_gap_quantized(Vector3 facing, ref byte left1_right2_pref, DynamicBuffer<LAV2QuantizedOccupancy> quantized_occupations)
    {
        int self_dir_index = occupation_quantize_single(facing);
        int loop_count = MaxQuantization / 2;
        int found_index = -1;
        //if (quantized_occupations[self_dir_index] == float.MaxValue)
        //{
        //    return quantized_index2vector(self_dir_index);
        //}

        for (int i = 1; i < loop_count; ++i)
        {
            int left_index = self_dir_index + i;
            int right_index = self_dir_index - i;

            if (left_index >= MaxQuantization) left_index -= MaxQuantization;
            if (quantized_occupations[left_index].value == float.MaxValue)
            {
                found_index = left_index;
                left1_right2_pref = 1;
                //detour_dir = found_index;
                break;
            }
            if (right_index < 0) right_index += MaxQuantization;
            if (quantized_occupations[right_index].value == float.MaxValue)
            {
                found_index = right_index;
                left1_right2_pref = 2;
                //detour_dir = found_index;
                break;
            }

        }
        return quantized_index2vector(found_index);
    }
    public static Vector3 search_gap_quantized_dir(Vector3 facing, byte left1_right2_pref, DynamicBuffer<LAV2QuantizedOccupancy> quantized_occupations)
    {
        int self_dir_index = occupation_quantize_single(facing);
        int loop_count = MaxQuantization;
        int found_index = -1;
        if (left1_right2_pref == 1)
        {
            for (int i = 0; i < loop_count; ++i)
            {
                int left_index = self_dir_index + i;
                if (left_index >= MaxQuantization) left_index -= MaxQuantization;

                if (quantized_occupations[left_index].value == float.MaxValue)
                {
                    found_index = left_index;
                    break;
                }
            }
        }
        else
        {
            // right
            for (int i = 0; i < loop_count; ++i)
            {
                int right_index = self_dir_index - i;
                if (right_index < 0) right_index += MaxQuantization;

                if (quantized_occupations[right_index].value == float.MaxValue)
                {
                    found_index = right_index;
                    break;
                }
            }
        }
        return quantized_index2vector(found_index);
    }
    // use this when the prefered direction is confirmed to be non-empty
    Vector3 search_gap_quantized(Vector3 facing, ref byte left1_right2_pref)
    {
        int self_dir_index = occupation_quantize_single(facing);
        int loop_count = MaxQuantization / 2;
        int found_index = -1;
        //if (quantized_occupations[self_dir_index] == float.MaxValue)
        //{
        //    return quantized_index2vector(self_dir_index);
        //}

        for (int i = 1; i < loop_count; ++i)
        {
            int left_index = self_dir_index + i;
            int right_index = self_dir_index - i;
            
            if (left_index >= MaxQuantization) left_index -= MaxQuantization;
            if (quantized_occupations[left_index] == float.MaxValue)
            {
                found_index = left_index;
                left1_right2_pref = 1;
                //detour_dir = found_index;
                break;
            }
            if (right_index < 0) right_index += MaxQuantization;
            if (quantized_occupations[right_index] == float.MaxValue)
            {
                found_index = right_index;
                left1_right2_pref = 2;
                //detour_dir = found_index;
                break;
            }

        }
        dbg_self_q = found_index;
        return quantized_index2vector(found_index);
    }
    Vector3 search_gap_quantized_dir(Vector3 facing, byte left1_right2_pref)
    {
        int self_dir_index = occupation_quantize_single(facing);
        int loop_count = MaxQuantization;
        int found_index = -1;
        if (left1_right2_pref == 1)
        {
            for (int i = 0; i < loop_count; ++i)
            {
                int left_index = self_dir_index + i;
                if (left_index >= MaxQuantization) left_index -= MaxQuantization;

                if (quantized_occupations[left_index] == float.MaxValue)
                {
                    found_index = left_index;
                    break;
                }
            }
        }
        else
        {
            // right
            for (int i = 0; i < loop_count; ++i)
            {
                int right_index = self_dir_index - i;
                if (right_index < 0) right_index += MaxQuantization;

                if (quantized_occupations[right_index] == float.MaxValue)
                {
                    found_index = right_index;
                    break;
                }
            }
        }
        dbg_self_q = found_index;
        return quantized_index2vector(found_index);
    }
    static Vector3 quantized_index2vector(int found_index)
    {
        Vector3 vec0 = Vector3.zero;
        if (found_index >= 0)
        {
            // found an empty gap.
#if PLANE_XZ
            vec0 = new Vector3(-1f, 0f, 0f);
#elif PLANE_XY
            vec0 = new Vector3(1f, 0f, 0f);
#endif
            float angles = 360f / MaxQuantization;

#if ROUNDED_QUANTIZATION
            float half = 0f;
#else
            float half = angles / 2f;
#endif
#if PLANE_XZ
            vec0 = Quaternion.AngleAxis(-angles * found_index - half, Vector3.up) * vec0;
#elif PLANE_XY
            vec0 = Quaternion.AngleAxis(-angles * found_index - half, Vector3.forward) * vec0;
#endif
        }
        return vec0;
    }

    
    

    // while moving, the return value is 0.
    // while idle, the return value, indicating the push force, is normalized.
    Vector3 obstacle_plotting_and_repels()
    {
        Vector3 repel_force = Vector3.zero;
        if (movement_state == MovementStates.HoldPosition) return repel_force;

        Vector3 self_position = transform.localPosition;
        Vector3 search_center = self_position;// get_forward_raycast_origin();
        UnityEngine.Collider[] cols = Physics.OverlapSphere(search_center, search_radius);
        adj_count = 0;

        float closest_distance = float.MaxValue;
        Vector3 closest_adj_vec = Vector3.zero;
        if (movement_state == MovementStates.Pushable)
        {
            // the unit is in idle state and is subject to being pushed. calculate the push force by finding the closest colliding unit.
            for (int i = 0; i < cols.Length; ++i)
            {
                if (cols[i] == self_col) continue;
                var adj_la = cols[i].GetComponent<LATest2>();
                if (adj_la == null) continue;

                var diff = self_position - adj_la.transform.localPosition;
                float distance2obstacle = diff.magnitude;

                if (distance2obstacle >= min_distance)
                {
                    float distance_surface2surface = distance2obstacle - radius - adj_la.radius;
                    if(distance_surface2surface < closest_distance)
                    {
                        closest_distance = distance_surface2surface;
                        closest_adj_vec = diff;
                    }
                }
                adj_count++;
            }
            
            if (closest_distance < 0.1f)
            {
                repel_force = closest_adj_vec.normalized;
            }
            return repel_force;
        }
        for (int i = 0; i < cols.Length; ++i)
        {
            if (cols[i] == self_col) continue;
            var adj_la = cols[i].GetComponent<LATest2>();
            if (adj_la == null) continue;

            var diff = self_position - adj_la.transform.localPosition;
            float distance2obstacle = diff.magnitude;

            if (distance2obstacle >= min_distance)
            {
                float distance_surface2surface_unclamped = distance2obstacle - radius - adj_la.radius;
                float distance_surface2surface = Mathf.Clamp(distance_surface2surface_unclamped, min_distance, uncomfort_zone);
                if (adj_la.movement_state == MovementStates.HoldPosition)
                {

                    occupation_quantize_3(adj_la.transform.localPosition - self_position, distance_surface2surface, quantized_occupations);
                }
                else if (adj_la.movement_state == MovementStates.Pushable)
                {
                    var similarity = Vector3.Dot(adj_la.cached_last_dir, cached_last_dir);

                    if (similarity < 0.7f || distance_surface2surface_unclamped < 0.1f)
                    {
                        occupation_quantize_3(adj_la.transform.localPosition - self_position, distance_surface2surface, quantized_occupations);
                    }
                }


                //if (adj_la.movement_state != MovementStates.Idle)
                //{
                //    // consider the repel effect.
                //    if (distance_surface2surface_unclamped < closest_distance)
                //    {
                //        closest_distance = distance_surface2surface_unclamped;
                //        closest_adj_vec = diff;
                //    }
                //}

                //if (adj_la.movement_state != MovementStates.Idle)
                //{
                //    // consider the repel effect.
                //    float force_mag = math.remap(min_distance, uncomfort_zone, max_repel_force, 0f, distance_surface2surface);
                //    repel_force += diff / distance2obstacle * force_mag;
                //}

            }

            adj_count++;
        }
        //if (closest_distance < uncomfort_zone)
        //{
        //    //repel_force = closest_adj_vec;
        //    closest_distance = Mathf.Clamp(closest_distance, min_distance, uncomfort_zone);
        //    float force_mag = math.remap(min_distance, uncomfort_zone, max_repel_force, 0f, closest_distance);
        //    //Vector3 repel_dir = diff / distance2obstacle;
        //    repel_force = closest_adj_vec.normalized * force_mag;

        //}

        return repel_force;
    }
    public int frame_index;
    public int frame_cmp;
    void FixedUpdate()
    {
        frame_index++;
        for (int i = 0; i < MaxQuantization; ++i)
        {
            quantized_occupations[i] = float.MaxValue;
        }


        Vector3 self_position = transform.localPosition;
        if (move_target != null)
        {
            goal_dir = move_target.localPosition - self_position;
            goal_dir.Normalize();
            //Debug.DrawLine(self_position, move_target.localPosition, Color.blue);
            //if (Vector3.Distance(transform.localPosition, move_target.localPosition) < 0.3f)
            if (dp.distance_2_finish_line(transform.localPosition))
            {
                simulate = false;
                left1_right2_detour_dir = 0;
                movement_state = MovementStates.Pushable;
                cached_last_dir = Vector3.zero;
            }
        }

        Vector3 idle_repel_force = obstacle_plotting_and_repels();

        Vector3 gap_dir = Vector3.zero;
        if (movement_state == MovementStates.Pushable)
        {
            // check if it is in detour mode
            if (left1_right2_detour_dir == 0)
            {
                // not in detour
                if (is_dir_empty(goal_dir))
                {
                    gap_dir = goal_dir;
                    previous_wall_center_dir = Vector3.zero;

                }
                else
                {
                    // get into detour mode
                    gap_dir = search_gap_quantized(goal_dir, ref left1_right2_detour_dir);
                    var wall_rot = Quaternion.identity;
                    if (left1_right2_detour_dir == 1)
                    {
                        // the result came from left.
                        wall_rot = Quaternion.AngleAxis(45f, Vector3.up);
                    }
                    else if (left1_right2_detour_dir == 2)
                    {
                        wall_rot = Quaternion.AngleAxis(-45f, Vector3.up);
                    }
                    previous_wall_center_dir = wall_rot * gap_dir;
                }
            }
            else
            {
                // in detour
                gap_dir = search_gap_quantized_dir(previous_wall_center_dir, left1_right2_detour_dir);
                var wall_rot = Quaternion.identity;

                if (left1_right2_detour_dir == 1)
                {
                    // the result came from left.
                    wall_rot = Quaternion.AngleAxis(45f, Vector3.up);
                }
                else if (left1_right2_detour_dir == 2)
                {
                    wall_rot = Quaternion.AngleAxis(-45f, Vector3.up);
                }
                previous_wall_center_dir = wall_rot * gap_dir;
                //previous_detour_dir = gap_dir;

                int goal_dir_index = occupation_quantize_single(goal_dir);
                int this_dir_index = occupation_quantize_single(gap_dir);
                if (this_dir_index == goal_dir_index)
                {
                    left1_right2_detour_dir = 0;
                    //Debug.Log("exit detour");
                    //Debug.Break();
                    //simulate = false;
                }
            }
        }

        if (frame_cmp > 0)
        {
            Debug.DrawLine(self_position, self_position + (Vector3)idle_repel_force.normalized * 2f, Color.red, 0.016f, false);
            //Debug.DrawLine(self_position, self_position + (Vector3)previous_wall_center_dir, Color.magenta, 0.016f, false);
            Debug.DrawLine(self_position, self_position + (Vector3)gap_dir * 3f, Color.cyan, 0.016f, false);
        }

        if(simulate)
        {
            simulate = false;
            movement_state = MovementStates.Pushable;
            left1_right2_detour_dir = 0;

            dp = default;
            dp.value = move_target.localPosition;
            dp.init_finish_line_vec(transform.localPosition);
            cached_last_dir = Vector3.zero;
        }
        if (movement_state != MovementStates.HoldPosition)
        {
            var dt = Time.deltaTime;
            if(movement_state == MovementStates.Pushable)
            {
                gap_dir = idle_repel_force;
            }
            else
            {
                if (left1_right2_detour_dir != 0 && gap_dir.magnitude > min_distance)
                {
                    gap_dir += previous_wall_center_dir * 0.2f;
                }
                //if (gap_dir.magnitude < min_distance)left1_right2_detour_dir = 0;
            }
            if (gap_dir.magnitude > min_distance)
            {
                gap_dir.Normalize();
                Quaternion gap_rot = Quaternion.LookRotation(gap_dir, Vector3.up);
                var this_frame_rot = Quaternion.RotateTowards(transform.localRotation, gap_rot, angular_velocity * dt);
                if (movement_state == MovementStates.Pushable)
                {
                    // push vector
                    transform.localPosition += gap_dir * velocity * dt;
                }
                else
                {
                    var this_frame_forward = this_frame_rot * Vector3.forward;
                    var cosine = Vector3.Dot(this_frame_forward, gap_rot * Vector3.forward);
                    cosine = Mathf.Clamp(cosine, 0f, 1f);
                    var velocity_constrained = velocity * cosine;
                    transform.localPosition += this_frame_forward * velocity_constrained * dt;

                    cached_last_dir = this_frame_forward;
                }
                transform.localRotation = this_frame_rot;
            }
        }

    }

    private void OnDrawGizmosSelected()
    {
        //Gizmos.color = Color.yellow;
        //Gizmos.DrawWireSphere(transform.localPosition, search_radius);
    }
}
public enum MovementStates:byte
{
    Pushable, // pushable
    HoldPosition, // not pushable
}
[System.Serializable]
public struct MovementInfo : IComponentData
{
    public float acceleration;
    public float deceleration;
    public float speed; // top speed
    public float angular_speed;
    public float3 current_desired_dir;
    public MovementStates move_state;
    public byte blocked_state; // 0 not blocked, 1 , 2

}

public class StructureInteractions
{
    public const uint Layer_raycast = (uint)1 << 31;
    public const uint Layer_structure = (uint)1 << 30;

    public const uint Layer_vehicle = (uint)1 << 29;
    public const uint Layer_resource_deposit = (uint)1 << 28;

    public const uint Layer_obstacle = (uint)1 << 27;
    //public const uint Layer_ring = (uint)1 << 26;
    public const uint Layer_ground = (uint)1 << 25;
    //public const uint Layer_character = (uint)1 << 24; // bio-swarm, drones
    //public const uint Layer_object_picking = (uint)1 << 23;
    public const uint Layer_ui = (uint)1 << 22;
    //public const uint Layer_projectile = (uint)1 << 21;
    public const uint Layer_galactic = (uint)1 << 20;
}