using Unity.Burst;
using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public partial class BioNodeUpdateSystem : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
		Enabled = false;
	}
    protected override void OnUpdate()
    {
		
		

	}
}

