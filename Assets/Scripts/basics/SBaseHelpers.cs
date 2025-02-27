using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

partial class SBaseHelpers : SystemBase
{
    public static SBaseHelpers self;
    protected override void OnCreate()
    {
        base.OnCreate();
        self = this;
        Enabled = false;
        var group = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
        Application.targetFrameRate = 60;
        
        group.RateManager = new RateUtils.VariableRateManager(60);

    }
    public PhysicsWorldSingleton get_physics()
    {
        return GetSingleton<PhysicsWorldSingleton>();
    }
    // get checkedstateref
    protected override void OnUpdate()
    {
    }
}
