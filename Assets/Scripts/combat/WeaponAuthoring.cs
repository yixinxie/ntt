using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
 
public class WeaponAuthoring : MonoBehaviour
{
    public int test_team;
    [SerializeField]
    public WeaponInfoV2 weapon;

    [SerializeField]
    public UnitStats stats;

    [SerializeField]
    public StorageCell storage;
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.localPosition, weapon.radius);
    }
    public class Bakery : Baker<WeaponAuthoring>
    {
        public override void Bake(WeaponAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new ComponentTypeSet(new ComponentType[] {
                    typeof(CombatTarget),
                    typeof(CombatTeam),
                    typeof(WeaponInfoV2),
                    typeof(UnitStats),
                    typeof(StorageCell),
                   
                }));
            var weapons = SetBuffer<WeaponInfoV2>(entity);
            weapons.Add(authoring.weapon);
            var ctargets = SetBuffer<CombatTarget>(entity);
            ctargets.Add(default);
            
            SetComponent(entity, authoring.stats);

            SetComponent(entity, new CombatTeam() { value = authoring.test_team});
                       
            var scell = SetBuffer<StorageCell>(entity);
            scell.Add(authoring.storage);
        }
    }
}
