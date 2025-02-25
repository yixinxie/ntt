using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

public class DebugStats : MonoBehaviour
{
    public TextMeshProUGUI tmp;
    StringBuilder sbuilder = new StringBuilder();
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        var handle = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<LocalAvoidanceSystem>();
        var phy = handle.GetSingleton<PhysicsWorldSingleton>();
        var rcinput = new RaycastInput();

        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        rcinput.Start = ray.origin;
        rcinput.End = ray.origin + ray.direction * 100f;
        var rcfilter = new CollisionFilter();
        rcfilter.CollidesWith = uint.MaxValue;
        rcfilter.BelongsTo = uint.MaxValue;
        rcinput.Filter = rcfilter;

        sbuilder.Clear();
        sbuilder.AppendFormat("frames:{0}", handle.frame_counter);
        sbuilder.AppendLine();

        if(phy.CastRay(rcinput, out var rchit))
        {
            var entityname = World.DefaultGameObjectInjectionWorld.EntityManager.GetName(rchit.Entity);
            sbuilder.AppendLine(entityname);

        }
        for (int i = 0; i < handle.codepaths.Length; i++)
        {
            var cp = handle.codepaths[i];
            sbuilder.Append(cp.type.ToString() + ", ");

        }

        tmp.text = sbuilder.ToString();
    }
}
