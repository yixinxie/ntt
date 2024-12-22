using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.AI;

public class NavMeshTest : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform target;
    void Start()
    {
    
        NavMeshQuery nvq = new NavMeshQuery();
        if (target != null)
        {
            var nml = nvq.MapLocation(transform.position, Vector3.one, 0);
            var nml_end = nvq.MapLocation(target.position, Vector3.one, 0);
            var status = nvq.BeginFindPath(nml, nml_end);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
