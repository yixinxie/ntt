using Unity.Burst;
using Unity.Entities;
using Unity.Physics;

partial class SBaseHelpers : SystemBase
{
    public static SBaseHelpers self;
    protected override void OnCreate()
    {
        base.OnCreate();
        self = this;
        Enabled = false;
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
