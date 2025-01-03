using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
 
public class LAOverlapAuthoring : MonoBehaviour
{
    public Transform movetarget;
    public float movement_speed = 10f;
    public float turn_speed_degrees = 15f;
    public float la_radius = 15f;
    public bool skip_goal_position;
    public bool pushable;
    public byte debug_index;
    public bool use_pathfinding = true;
    public string dbgid;

    private void OnDrawGizmos()
    {
        //if(movetarget!=null)
        //    Gizmos.DrawLine(transform.localPosition, movetarget.localPosition);
    }
    public class Bakery : Baker<LAOverlapAuthoring>
    {
        public override void Bake(LAOverlapAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new ComponentTypeSet(new ComponentType[] {
                    typeof(DBGId),
                }));
            if (string.IsNullOrEmpty(authoring.dbgid) == false)
            {
                SetComponent(entity, new DBGId() { str32 = new FixedString32Bytes(authoring.dbgid) });
            }

            AddLAComponentsStatic(this, entity);
            SetComponent(entity, new BoidsCoeffs() { avoid_factor = 0.1f, goal_factor = 0.1f, cohesion_factor = 0.1f, speedavg_factor = 0.1f,goal_factor_max = 0.2f });

            var mi = new MovementInfo()
            {
                speed = authoring.movement_speed,
                angular_speed = Mathf.Deg2Rad * authoring.turn_speed_degrees,
                move_state = (authoring.pushable) ? MovementStates.Moving : MovementStates.HoldPosition,
                blocked_state = 0,
                self_radius = authoring.la_radius,
                debug_index = authoring.debug_index

            };
            if (authoring.movetarget != null)
            {
                var dp = new DesiredPosition();
                dp.value = authoring.movetarget.position;
                dp.init_finish_line_vec(authoring.transform.position);
                SetComponent(entity, dp);
                var goal_dir = math.normalize(authoring.movetarget.position - authoring.transform.position);
                mi.current_desired_dir = goal_dir;
            }
            SetComponent(entity, mi);

            SetComponent(entity, new BoidsCoeffs()
            {
                avoid_factor = BoidsParameters.const_avoid_factor,
                cohesion_factor = BoidsParameters.const_cohesion_factor,
                speedavg_factor = BoidsParameters.const_speedavg_factor,
                goal_factor_max = BoidsParameters.const_goal_factor,
                goal_factor = BoidsParameters.const_goal_factor
            });
        }
    }
    public static void AddLAComponentsStatic<T>(Baker<T> em, Entity entity) where T:UnityEngine.Component
    {
        em.AddComponent(entity, new ComponentTypeSet(new ComponentType[] {
                    typeof(LAAdjacentEntity),
                    typeof(DesiredPosition),
                    typeof(MovementInfo),
                    typeof(LastFrameVelocity),
                    typeof(BoidsCoeffs),
                }));
    }
    //public static void AddLAV2ComponentsStatic(EntityManager em, Entity entity)
    //{
    //    em.AddComponent(entity, new ComponentTypeSet(new ComponentType[] {
    //                typeof(AdjacentEntities),
    //                typeof(DesiredPosition),
    //                typeof(MovementInfo),
    //                typeof(LA_Radius),

    //                typeof(LAV2MovementStates),
    //                typeof(LAV2QuantizedOccupancy),
    //            }));
    //    var buffer = em.GetBuffer<LAV2QuantizedOccupancy>(entity);
    //    for(int i = 0; i < LATest2.MaxQuantization; ++i)
    //        buffer.Add(default);
    //}
    public void AddLAComponent<T>(Baker<T> dstManager, Entity entity) where T : UnityEngine.Component
    {
        dstManager.AddComponent(entity, new ComponentTypeSet(new ComponentType[] {
                    typeof(LAAdjacentEntity),
                    typeof(LastFrameVelocity),
                    //typeof(FrameDisplacement),
                    //typeof(LocalAvoidance),

                    //typeof(DesiredPosition),
                    typeof(MovementInfo),
                    typeof(BoidsCoeffs),
                }));
        if (use_pathfinding)
        {
            //dstManager.AddComponent(entity, new ComponentTypeSet(new ComponentType[] {
            //        typeof(PathPoint),
            //        typeof(PathPointPtr),
            //    }));
        }
        
        //dstManager.SetComponent(entity, new MovementInfo() { speed = movement_speed, angular_speed = turn_speed_degrees });
    }
}

public struct BoidsCoeffs:IComponentData
{
    public float avoid_factor;
    public float cohesion_factor;
    public float speedavg_factor;
    public float goal_factor;
    public float goal_factor_max; // not used
}
public struct DBGId:IComponentData
{
    public FixedString32Bytes str32;
}