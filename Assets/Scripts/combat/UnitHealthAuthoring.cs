using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
 
public class UnitHealthAuthoring: MonoBehaviour
{
    public int test_team;
    public bool manual_override;

    [SerializeField]
    public UnitStats stats;

    
    private void OnDrawGizmos()
    {
        //Gizmos.DrawWireSphere(transform.localPosition, weapon.radius);
    }
    public class Bakery : Baker<UnitHealthAuthoring>
    {
        public override void Bake(UnitHealthAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new ComponentTypeSet(new ComponentType[] {
                    typeof(CombatTeam),
                    typeof(UnitStats),
                   
                }));
            SetComponent(entity, authoring.stats);
            SetComponent(entity, new CombatTeam() { value = authoring.test_team});
     
            if (authoring.manual_override)
            {
                AddComponent(entity, new ComponentTypeSet(new ComponentType[] {
                    typeof(ManualMovementCtrl),
                    typeof(ManualFireCtrl),
                }));
            }
        }
    }
}
public struct ManualMovementCtrl : IComponentData 
{ 

}
public struct ManualFireCtrl : IComponentData 
{ 
    // not having this component on any weapon based entities means it will autofire
}
