using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
 
public class WeaponAuthoring : MonoBehaviour
{
    [SerializeField]
    public WeaponInfoV2 weapon;

    //[SerializeField]
    //public UnitStats stats;

    
    private void OnDrawGizmos()
    {
        //Gizmos.DrawWireSphere(transform.localPosition, weapon.radius);
    }
    public class Bakery : Baker<WeaponAuthoring>
    {
        public override void Bake(WeaponAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new ComponentTypeSet(new ComponentType[] {
                    typeof(CombatTarget),
                    typeof(WeaponInfoV2),
                   
                }));
            var weapons = SetBuffer<WeaponInfoV2>(entity);
            weapons.Add(authoring.weapon);
            var ctargets = SetBuffer<CombatTarget>(entity);
            ctargets.Add(default);
            
        }
    }
}
