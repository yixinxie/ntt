using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[DisallowMultipleComponent]
 
public class CommandCenterAuthoring : MonoBehaviour
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

    
    public class Bakery : Baker<CommandCenterAuthoring>
    {
        public override void Bake(CommandCenterAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            TileAssemblerAuthoring.AddCommonMachineComponents(this, entity, authoring.is_client);
            TileExtractorAuthoring.AddPowerConsumerCD(this, entity);
            //TileRouterAuthoring.AddDirectTransportComponents(this, entity, authoring.is_client);
            AddComponent(entity, new ComponentTypeSet(new ComponentType[]{
                typeof(CmdCntrModes),
                typeof(CmdCntrUnitRef),
                typeof(CmdCntrPathNode),
            }
                ));


            var gtype = new GalacticType();
            gtype.value = GTypes.CommandCenter;
            //gtype.machine_init(authoring.machine_subtype);
            SetComponent(entity, gtype);
        }
    }
}
[InternalBufferCapacity(8)]
public struct CmdCntrUnitRef:IBufferElementData
{
    public Entity value;
}
public enum CmdCntrModeTypes : byte
{
    Passive,
    Aggressive,
    Manual, // follow the player.
}
public struct CmdCntrModes:IComponentData
{
    public CmdCntrModeTypes mode;
    public Entity invader;
}
[InternalBufferCapacity(8)]
public struct CmdCntrPathNode:IBufferElementData
{
    public float3 value;
}