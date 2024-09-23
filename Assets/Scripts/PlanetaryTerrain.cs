using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class PlanetaryTerrain : MonoBehaviour
{
    // Start is called before the first frame update

    public Transform[] three;
    void Start()
    {
        
    }
    private void OnDrawGizmos()
    {
        if(three != null && three.Length == 3)
        {
            for(int i = 0; i < three.Length; i++)
            {
                if (three[i] == null)
                {
                    return;
                }
            }
            // clockwise winding
            triangle_divide(three[0].position, three[1].position, three[2].position, default, 2);
        }
    }
    static void triangle_divide(float3 p0, float3 p1, float3 p2, NativeList<float3> points, int level)
    {
       // float height = 1f;
       // var p0_nml = math.normalize(p0);
       // //var height = NoisesTest.mesh0.octaves(p0_nml, 4);
       // p0 = p0_nml * height;

       // var p1_nml = math.normalize(p1);
       //// height = NoisesTest.mesh0.octaves(p1_nml, 4);
       // p1 = p1_nml * height;

       // var p2_nml = math.normalize(p2);
       // //height = NoisesTest.mesh0.octaves(p2_nml, 4);
       // p2 = p2_nml * height;


        Gizmos.DrawLine(p0, p1);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p0, p2);
        if (level == 0) return;
        float3 q0 = (p0 + p1) / 2f;
        float3 q1 = (p1 + p2) / 2f;
        float3 q2 = (p0 + p2) / 2f;

        //var height = NoisesTest.mesh0.octaves(q0, 4);
        //q0 = math.normalize(q0) * height;

        //height = NoisesTest.mesh0.octaves(q1, 4);
        //q1 = math.normalize(q1) * height;

        //height = NoisesTest.mesh0.octaves(q2, 4);
        //q2 = math.normalize(q2) * height;


        triangle_divide(p0, q0, q2, points, level - 1);
        triangle_divide(q0, p1, q1, points, level - 1);
        triangle_divide(q0, q1, q2, points, level - 1);
        triangle_divide(q2, q1, p2, points, level - 1);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
