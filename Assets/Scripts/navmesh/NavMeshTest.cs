using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using Unity.AI.Navigation;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.AI;

public class NavMeshTest : MonoBehaviour
{
    // Start is called before the first frame update
    public NavMeshSurface surface;
    public Transform target;
    [SerializeField]
    public NavMeshQuery nvq;
    [SerializeField]
    public NavMeshLocation nml;
    [SerializeField]
    public NavMeshLocation nml_end;
    [SerializeField]
    public PathQueryStatus pqs;
    public int pathsize;
    public int len2;
    [SerializeField]
    NavMeshWorld nworld;
    public bool refresh_navmesh;
    public int it_p;
    private void OnDestroy()
    {
        nvq.Dispose();
    }
    void Start()
    {
        surface.BuildNavMesh();
        
        nworld = NavMeshWorld.GetDefaultWorld();
        nvq = new NavMeshQuery(nworld, Allocator.Persistent, 100);
        if (target != null)
        {
           
            //PathUtils.FindStraightPath(nvq, transform.position, target.position,)
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(refresh_navmesh)
        {
            refresh_navmesh = false;
            var st = surface.GetBuildSettings();
        }
        nml = nvq.MapLocation(transform.position, Vector3.one, 0);
        nml_end = nvq.MapLocation(target.position, Vector3.one, 0);
        pqs = nvq.BeginFindPath(nml, nml_end);
        if(pqs == PathQueryStatus.InProgress)
        {
            pqs = nvq.UpdateFindPath(100, out it_p);
            if(pqs == PathQueryStatus.Success)
            {
                pqs = nvq.EndFindPath(out pathsize);

                int max_path_size = pathsize * 10;
                NativeArray<NavMeshLocation> results = new NativeArray<NavMeshLocation>(pathsize + 1, Allocator.Temp);
                NativeArray<StraightPathFlags> st_flags = new NativeArray<StraightPathFlags>(max_path_size, Allocator.Temp);
                NativeArray<float> vertex_sides = new NativeArray<float>(max_path_size, Allocator.Temp);
                NativeArray<PolygonId> poly_ids = new NativeArray<PolygonId>(pathsize + 1, Allocator.Temp);
                //NativeArray<PolygonId> tmp = new NativeArray<PolygonId>(1024, Allocator.Temp);
                //NativeSlice<PolygonId> polygonIds = new NativeSlice<PolygonId>(tmp);
                len2 = nvq.GetPathResult(poly_ids);

                int st_path_count = 0;
                var rs = PathUtils.FindStraightPath(nvq, transform.position, target.position, poly_ids, pathsize, ref results, ref st_flags, ref vertex_sides, ref st_path_count, max_path_size);

                if (rs == PathQueryStatus.Success)
                {
                    for (int i = 0; i < results.Length; ++i)
                    {

                        var pos3f = results[i].position;
                        Debug.DrawLine(pos3f, pos3f + Vector3.up, Color.green);
                    }
                }
            }
        }

        //pqs == PathQueryStatus.

    }
}
