using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class LAOccuTest : MonoBehaviour
{
    public static LAOccuTest self;
    public Transform desired_location;
    public Entity target;
    
    LocalAvoidanceSystem la_handle;
    [SerializeField]
    public byte[] occu_managed;
    [SerializeField]
    public float[] occu_floats_managed;

    [SerializeField]
    public MovementInfo mi;

    [SerializeField]
    public MovementInfo mi_after;
    public bool copy;
    public bool reset_mi;
    public float self_radius;
    private void Awake()
    {
        self = this;
    }
    void Start()
    {
        la_handle = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LocalAvoidanceSystem>();

    }
    private void OnDestroy()
    {

    }

    // Update is called once per frame
    void Update()
    {

        var self_lt = LocalTransform.FromPositionRotation(transform.position, transform.rotation);
        NativeList<LAAdjacentEntity> adjs = new NativeList<LAAdjacentEntity>(8, Allocator.Temp);
        la_handle.debug_laadj(default, adjs, self_lt, new MovementInfo() { self_radius = self_radius });
        NativeArray<byte> occu = new NativeArray<byte>(6, Allocator.Temp);
        NativeArray<float> occu_floats = new NativeArray<float>(6, Allocator.Temp);
        LocalAvoidanceSystem.horizon_eval_array(self_lt, adjs.AsArray(), la_handle.GetComponentLookup<LocalTransform>(), la_handle.GetComponentLookup<MovementInfo>(), occu, occu_floats);
        occu_managed = occu.ToArray();
        occu_floats_managed = occu_floats.ToArray();
        if (desired_location != null)
        {
            var dp = math.normalize(desired_location.position - transform.position);
            mi_after = mi;
            if (LocalAvoidanceSystem.detour_eval(HexCoord.FromPosition(dp), dp, ref mi_after, occu, out var tmp) == false)
            {
                if (mi_after.blocked_state != 0)
                {
                    Debug.DrawLine(transform.position, transform.position + (Vector3)mi_after.current_desired_dir, Color.yellow);
                }
                else
                {
                    Debug.DrawLine(transform.position, transform.position + (Vector3)dp, Color.green);
                }
            }
        }
        if(copy)
        {
            copy = false;
            mi = mi_after;
        }
        
        if(reset_mi)
        {
            reset_mi = false;
            mi = mi_after = default;
        }
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        
    }
}
