using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[DisallowMultipleComponent]
 
public class RoboFactoryAuthoring: MonoBehaviour
{
    // Add fields to your component here. Remember that:
    //
    // * The purpose of this class is to store data for authoring purposes - it is not for use while the game is
    //   running.
    // 
    // * Traditional Unity serialization rules apply: fields must be public or marked with [SerializeField], and
    //   must be one of the supported types.
    //
    // For example,
    //    public float scale;
    public Entity[] rot_parts;
    public bool is_client;
    public int def_player_id;
    public ASMRecipe_Ntv initial_recipe;
    //public MachineSubTypes machine_subtype;

    
    public class Bakery : Baker<RoboFactoryAuthoring>
    {
        public override void Bake(RoboFactoryAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            TileAssemblerAuthoring.AddCommonMachineComponents(this, entity, authoring.is_client);
            TileExtractorAuthoring.AddPowerConsumerCD(this, entity);
            //TileRouterAuthoring.AddDirectTransportComponents(this, entity, authoring.is_client);
            AddComponent(entity, new ComponentTypeSet(new ComponentType[]{
                typeof(MachineOutputInventory),
                typeof(AssemblerInputInventory),
                typeof(ExtraProductionStates),
                typeof(CachedWorkingStates),
                //typeof(MachineDirectTransportDirTargetPositions),
            }
                ));

            //SetComponent(entity, new StructureMeshInfo() { category = prefab_category, index = (int)prefab_index });

            //SetComponent(entity, new PowerConsumption() { value = structure_info.power_req });
            //SetComponent(entity, new StorageCellLimit() { value = structure_info.storage_limit });


            var gtype = new GalacticType();
            gtype.value = GTypes.Assembler;
            //gtype.machine_init(authoring.machine_subtype);
            SetComponent(entity, gtype);
            SetComponent(entity, new PlayerID_CD() { value = authoring.def_player_id });

            AddComponent(entity, new ComponentTypeSet(new ComponentType[]
                {
                //typeof(AssemblerInputInventory),
                //typeof(MachineOutputInventory),
                typeof(AssemblerTimeLeft),
                //typeof(OrientationInGrid),
                //typeof(PowerConsumption),
                    //typeof(StorageCellLimit),
                }));
            //SetComponent(entity, new StorageCellLimit() { value = 50 });

            var aii = new AssemblerInputInventory();
            aii.recipe = authoring.initial_recipe;
            SetComponent(entity, new AssemblerInputInventory() { recipe = authoring.initial_recipe });
            SetComponent(entity, new MachineOutputInventory() { item_type = (ushort)authoring.initial_recipe.output_type });

#if UNITY_EDITOR
            //AddComponent(entity, typeof(AssemblerInputInventoryDebug));
#endif
            //if (authoring.is_client)
            //{

            //    SetComponent(entity, new AnimationInterpolation() { cycle_length = 10f });

            //    if (authoring.rot_parts != null && authoring.rot_parts.Length > 0)
            //    {
            //        AddComponent(entity, new RotatingPiece() { value = authoring.rot_parts[0] });
            //        if (authoring.rot_parts.Length > 1)
            //            AddComponent(entity, new RotatingPiece2() { value = authoring.rot_parts[1] });
            //        else
            //            AddComponent(entity, new RotatingPiece2());
            //    }
            //}
        }
    }
}