using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

struct LastFrameVelocity : IComponentData {
    public float3 value;
}
struct ExternalInfluence : IComponentData {
    public float3 value;
    public float distance2goal;
}

public struct DesiredPosition : IComponentData {
    public float3 value;
    public float3 finish_line_right;
    //public float goal_scale;
    //public float is_end;
    //public float3 GetNext()
    //{
    //    return finish_line_vec;
    //}
    //public void SetNext(float3 next)
    //{
    //    finish_line_vec = next;
    //}
    public void init_finish_line_vec(float3 start_pos)
    {
        float3 goal_vec = value - start_pos;
        goal_vec = math.normalize(goal_vec);
        finish_line_right = Vector3.Cross(Vector3.forward, goal_vec).normalized;
    }
    public bool distance_2_finish_line(float3 current_position)
    {
        var from_goal = current_position - value;
        var distance = math.distance(0f, from_goal);
        if(distance < float.Epsilon)
        {
            return true;
        }
        from_goal /= distance;
        return Vector3.Cross(from_goal, finish_line_right).z > 0f;

        //Vector3.Cross()
    }
}
struct FrameDisplacement : IComponentData {
    public float3 value;
}


[InternalBufferCapacity(8)]
public struct LAAdjacentEntity : IBufferElementData
{
    public Entity value;
    public float distance;
}
//struct LocalAvoidance : IComponentData
//{
//}

//public struct MovementInfo : IComponentData
//{
//    public float angular_speed; // in radians
//    public float speed;
//}
struct LA_Radius : IComponentData
{
    public float value;
}