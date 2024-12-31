using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Entities;
using UnityEngine;
using static UnityEditor.ObjectChangeEventStream;

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
        sbuilder.Clear();
        var handle = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<LocalAvoidanceSystem>();
        sbuilder.AppendFormat("frames:{0}", handle.frame_counter);
        sbuilder.AppendLine();
        for (int i = 0; i < handle.codepaths.Length; i++)
        {
            var cp = handle.codepaths[i];
            sbuilder.Append(cp.type.ToString() + ", ");

        }

        tmp.text = sbuilder.ToString();
    }
}
