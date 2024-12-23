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
    public NavMeshQuery nvq;
    public NavMeshLocation nml;
    public NavMeshLocation nml_end;
    public PathQueryStatus pqs;
    public int len;
    private void OnDestroy()
    {
        nvq.Dispose();
    }
    void Start()
    {
        nvq = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.Persistent, 100);

        if (target != null)
        {
            nml = nvq.MapLocation(transform.position, Vector3.one, 0);
            nml_end = nvq.MapLocation(target.position, Vector3.one, 0);
            pqs = nvq.BeginFindPath(nml, nml_end);
            //PathUtils.FindStraightPath(nvq, transform.position, target.position,)
        }
    }

    // Update is called once per frame
    void Update()
    {
        //pqs = nvq.UpdateFindPath(1, out int it_performed);
        pqs = nvq.EndFindPath(out len);
        //pqs == PathQueryStatus.

    }
}
