using System.Collections;
using System.Collections.Generic;
using Unity.Physics;
using UnityEngine;
using UnityEngine.Windows;

public class CFilterTest : MonoBehaviour
{
    public int setteam_0;
    public int setteam_1;
    
    public bool res;
    // Start is called before the first frame update
    void Start()
    {
        
    }
    private void OnDrawGizmos()
    {
        CollisionFilter cf_query = default;
        CollisionFilter cf1 = default;
        var team0 = new CombatTeam() { value = setteam_0 };
        //cf_query.BelongsTo = cf_query.BelongsTo | team0.FriendlyTeamMask();
        //cf_query.CollidesWith = StructureInteractions.Layer_ground_vehicle_scan;

        UnitSearchHostileSystem.initialize_query_cfilter(WeaponTypes.Cannon, team0, ref cf_query);

        var team1 = new CombatTeam() { value = setteam_1 };
        cf1.BelongsTo = cf1.BelongsTo | team1.FriendlyTeamMask();
        cf1.CollidesWith = StructureInteractions.Layer_ground_vehicle_scan;
        res = CollisionFilter.IsCollisionEnabled(cf1, cf_query);
    }
}
