/**
 * Copyright 2021-2022 Chongqing Centauri Technology LLC.
 * All Rights Reserved.
 * 
 */
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Systems;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.GraphicsBuffer;
// UWP unitsearch, weaponfire, projectilemotion
[UpdateAfter(typeof(UnitSearchHostileSystem))]
public partial struct MachineSimulationSystem : ISystem
{
    ComponentLookup<LocalTransform> c0_array;
    void OnCreate(ref SystemState sstate)
    {
        c0_array = sstate.GetComponentLookup<LocalTransform>();
    }
    void OnDestroy(ref SystemState sstate)
    {
    }
    
    void OnUpdate(ref SystemState sstate)
    {
        var extractor_pass0 = new extractor_pass0();
        extractor_pass0.Run();
        var extractor_pass1 = new extractor_pass1();
        extractor_pass1.dt = SystemAPI.Time.DeltaTime;
        extractor_pass1.Run();

    }

    partial struct extractor_pass0 : IJobEntity
    {
        //var remaining_array = GetComponentLookup<ResourceNodeRemaining>();
        void Execute(DynamicBuffer<ExtractorCoverTargetElement> res_nodes,
        ref CachedWorkingStates working,
        ref ExtractorProductionStates prod_states, ref MachineOutputInventory moi, ref ExtraProductionStates eps/*, in ProductionModifier modif,*/)
        {
            if (prod_states.left <= 0f)
            {
                bool atleast_one = false;

                if (moi.count < ASMConstants.MachineOutputInventoryItemLimit)
                {
                    //for (int i = res_nodes.Length - 1; i >= 0; --i)
                    {
                        //int attempt_output_index = oa.attempt_index(res_nodes.Length);
                        //var res_node = res_nodes[i].value;
                        //if (remaining_array.HasComponent(res_node))
                        {
                            //var remaining = remaining_array[res_node];
                            //if (remaining.value > 0)
                            {
                                atleast_one = true;
                                prod_states.left = prod_states.total;
                                Debug.Log("extractor starts");
                            }
                        }
                    }
                }
                //return atleast_one;
            }
            //return true;
        }

    }
    partial struct extractor_pass1:IJobEntity
    {
        public float dt;
        //var remaining_array = GetComponentLookup<ResourceNodeRemaining>();
        void Execute(DynamicBuffer<ExtractorCoverTargetElement> res_nodes,
        ref CachedWorkingStates working,
        ref ExtractorProductionStates prod_states, ref MachineOutputInventory moi, ref ExtraProductionStates eps,/* in ProductionModifier modif,*/ in PlayerID_CD pid)
        {
        //    extractor_execution_pass(res_nodes, ref moi, ref working, modif, ref prod_states, ref eps, pid, default/*, ppfs*/);
        //}
        //public static void extractor_execution_pass(DynamicBuffer<ExtractorCoverTargetElement> res_nodes, ref MachineOutputInventory moi,
        //ref CachedWorkingStates working, ProductionModifier modif,
        //    ref ExtractorProductionStates prod_states, ref ExtraProductionStates eps, in PlayerID_CD pid,
        //    ComponentLookup<ResourceNodeRemaining> remaining_array, /*NativeHashMap<int, PlayerProductionFrameStats> ppfs*/)
        //{

            //if (machine_pm_common_execution(modif) == false)
            //{
            //    working.value = 0;
            //    return 0;
            //}
            working.value = 1;
            if (prod_states.left <= 0f)
            {
                //extractor_check2start(res_nodes, remaining_array, moi, ref prod_states, ref working);
                return;
            }
            int aggregate = 0;
            prod_states.left -= dt;
            if (prod_states.left <= 0f)
            {
                prod_states.left = 0f;

                //for (int i = res_nodes.Length - 1; i >= 0; --i)
                {
                    //var res_node = res_nodes[i].value;
                    //if (remaining_array.HasComponent(res_node))
                    {
                        //var remaining = remaining_array[res_node];
                        //if (remaining.value <= 0)
                        //    continue;

                        var available_capacity = ASMConstants.MachineOutputInventoryItemLimit - moi.count;
                        //var actual_amount = min_three(remaining.value, prod_states.batch_count, available_capacity);
                        var actual_amount = min_three(9999, prod_states.batch_count, available_capacity);
                        if (actual_amount > 0)
                        {
                            //remaining.value -= actual_amount;
                            //remaining_array[res_node] = remaining;
                            moi.count += (short)actual_amount;
                            aggregate += actual_amount;
                            //moi.item_type2 = (int)ItemType.Matrix_Blue;
                            //moi.count2++;
                        }
                    }
                }
                if (aggregate > 0)
                {
                    var prev_aggr = aggregate;
                    var bonus = eps.aggre(aggregate) - aggregate;
                    moi.count += (short)bonus;

                    if (moi.item_type2 != 0 && eps.get_hit())
                    {
                        moi.count2 += 1;
                    }
                    /*
                    int tmp_bonus_progress = eps.bonus_progress + eps.bonus_increment;
                    if (tmp_bonus_progress >= 100)
                    {
                        int tmp_rate = tmp_bonus_progress / 100;
                        int tmp_val = eps.batch_count * tmp_rate;
                        tmp_bonus_progress = tmp_bonus_progress % 100;
                        var bonus_amount = tmp_val;
                        moi.count += bonus_amount;
                        aggregate += bonus_amount;
                    }
                    eps.bonus_progress = (byte)tmp_bonus_progress;
                    */
                    //ProductionStatsManaged.submit_collection_pass(moi.item_type, prev_aggr + bonus, pid.value, ppfs);
                }
            }
        }

    }
    
    public static int min_three(int v0, int v1, int v2)
    {
        return math.min(v0, math.min(v1, v2));
    }
    public static float min_three(float v0, float v1, float v2)
    {
        return math.min(v0, math.min(v1, v2));
    }


}

public class ASMConstants
{
    public const int MachineOutputInventoryItemLimit = 10;

}
