using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;

//[BurstCompile]
//[UpdateAfter(typeof(VariableRateSimulationSystemGroup))]
public partial struct ResourceIndexingSystem : ISystem
{
    Entity load_c_scene;
    Entity load_s_scene;
    bool is_scene_loaded;
    bool initialized;
    //partial struct ToResourceArray:IJob
    //{
    //    //public ComponentLookup<ResourcePrefab> resourcetype_dict;
    //    //public NativeParallelMultiHashMap<int, ResourcePrefabInsertInfo> _resource_dict;
    //    //public NativeArray<int> _index_incrementals;

    //    public NativeArray<Entity> targets;
    //    public void Execute()
    //    {
    //        for (int i = 0; i < targets.Length; ++i)
    //        {
    //            var rp = resourcetype_dict[targets[i]];
    //            var res_type = (int)rp.array_type;
    //            //int index = _index_incrementals[(int)res_type];
    //            if (rp.baked_prefab != Entity.Null)
    //            {
    //                _resource_dict.Add(res_type, new ResourcePrefabInsertInfo() { entity = rp.baked_prefab, array_index = rp.array_index });
    //            }
    //            else
    //            {
    //                _resource_dict.Add(res_type, new ResourcePrefabInsertInfo() { entity = targets[i], array_index = rp.array_index });
    //            }
    //            //_index_incrementals[(int)res_type]++;
    //        }
    //    }
    //}
    public void OnCreate(ref SystemState state)
    {
        //resource_dict = new NativeParallelMultiHashMap<int, ResourcePrefabInsertInfo>(256, Allocator.Persistent);
        //index_incrementals = new NativeArray<int>((int)ResourceArrayType.Total, Allocator.Persistent);
        //state.RequireForUpdate<ResourcePrefab>();
        //state.RequireForUpdate<ResourcePrefabEntry>();
        load_c_scene = Entity.Null;
        load_s_scene = Entity.Null;
        is_scene_loaded = false;
    }
    public void OnDestroy(ref SystemState state)
    {
    }

    public struct ResourcePrefabInsertInfo
    {
        public int array_index;
        public Entity entity;

    }
    void addcmpt_recursive(Entity target, ref SystemState state, NativeList<Entity> all_children)
    {
        all_children.Add(target);
        if (state.EntityManager.HasBuffer<Child>(target) == false) return;
        var children = state.EntityManager.GetBuffer<Child>(target).ToNativeArray(Allocator.Temp);
        for(int i = 0; i < children.Length; ++i)
        {
            addcmpt_recursive(children[i].Value, ref state, all_children);
        }
    }
    public void OnUpdate(ref SystemState state)
    {
        //var q = state.EntityManager.CreateEntityQuery(typeof(ResourcePrefab));
        //if(q.CalculateEntityCount() > 0)
        //{
        //    Debug.Log(q.CalculateEntityCount() + " entities to be added to resource array.");


        //    var job = new ToResourceArray();
        //    job.resourcetype_dict = state.GetComponentLookup<ResourcePrefab>();
        //    job._resource_dict = resource_dict;
        //    //job._index_incrementals = index_incrementals;
        //    job.targets = q.ToEntityArray(Allocator.TempJob);
        //    job.Run();
        //    state.EntityManager.RemoveComponent<ResourcePrefab>(job.targets);

        //    //NativeList<Entity> to_deprefab = new NativeList<Entity>(Allocator.Temp);
        //    //for(int i = 0; i < job.targets.Length; ++i)
        //    //{
        //    //    addcmpt_recursive(job.targets[i], ref state, to_deprefab);
        //    //}
        //    //state.EntityManager.AddComponent<Prefab>(to_deprefab.ToArray(Allocator.Temp));
        //    state.EntityManager.AddComponent<Prefab>(job.targets);
        //    job.targets.Dispose();

        //    refill_prefab_array(resource_dict, ref ResourceRefs.self.structures, ResourceArrayType.ClientCommon);
        //    refill_prefab_array(resource_dict, ref ResourceRefs.self.entity_meshes, ResourceArrayType.ClientOnly);
        //    refill_prefab_array(resource_dict, ref ResourceRefs.self.item_icon_entities, ResourceArrayType.ItemIcon);
        //    refill_prefab_array(resource_dict, ref ResourceRefs.self.colliders, ResourceArrayType.Collider);

        //    refill_prefab_array(resource_dict, ref ResourceRefsServer.self.structures, ResourceArrayType.ServerCommon);
        //    refill_prefab_array(resource_dict, ref ResourceRefsServer.self.server_only_prefab_entities, ResourceArrayType.ServerOnly);
        //    if(initialized == false)
        //    {
        //        initialized = true;
        //        ServerMapDataLoader_Start();
        //        ResourceRefs.self.init_material_override();
        //    }

        //}

        if (!is_scene_loaded)
        {
            //EntitySceneReference client_scene_ref = default;
            //if (ResourceRefs.self != null)
            //{
            //    client_scene_ref = ResourceRefs.self.client_scene_ref;
            //    if (client_scene_ref.IsReferenceValid && load_c_scene == Entity.Null)
            //    {
            //        load_c_scene = SceneSystem.LoadSceneAsync(state.WorldUnmanaged, client_scene_ref);
            //    }
            //}

            //EntitySceneReference server_scene_ref = default;
            //if (ResourceRefsServer.self != null)
            //{
            //    server_scene_ref = ResourceRefsServer.self.server_scene_ref;
            //    if (server_scene_ref.IsReferenceValid && load_s_scene == Entity.Null)
            //    {
            //        load_s_scene = SceneSystem.LoadSceneAsync(state.WorldUnmanaged, server_scene_ref);
            //    }
            //}

            //if (client_scene_ref.IsReferenceValid && load_c_scene != Entity.Null && !server_scene_ref.IsReferenceValid)
            //{
            //    if (SceneSystem.IsSceneLoaded(state.WorldUnmanaged, load_c_scene))
            //    {
            //        is_scene_loaded = true;
            //        Debug.Log("client sub scene loaded!");
            //    }
            //}
            //else if (server_scene_ref.IsReferenceValid && load_s_scene != Entity.Null && !client_scene_ref.IsReferenceValid)
            //{
            //    if (SceneSystem.IsSceneLoaded(state.WorldUnmanaged, load_s_scene))
            //    {
            //        is_scene_loaded = true;
            //        Debug.Log("server sub scene loaded!");
            //    }
            //}
            //else if (client_scene_ref.IsReferenceValid && load_c_scene != Entity.Null && server_scene_ref.IsReferenceValid && load_s_scene != Entity.Null)
            //{
            //    if (SceneSystem.IsSceneLoaded(state.WorldUnmanaged, load_c_scene) && SceneSystem.IsSceneLoaded(state.WorldUnmanaged, load_s_scene))
            //    {
            //        is_scene_loaded = true;
            //        Debug.Log("sub scene loaded!");
            //    }
            //}
        }

        //if (is_scene_loaded)
        {
            var q = state.EntityManager.CreateEntityQuery(typeof(ResourcePrefabEntry));//fixed
            if (q.CalculateEntityCount() > 0)
            {
                var targets = q.ToEntityArray(Allocator.TempJob);
               
                for (int i = 0; i < targets.Length; ++i)
                {
                    var rpe_db = state.EntityManager.GetBuffer<ResourcePrefabEntry>(targets[i]).ToNativeArray(Allocator.Temp);

                    if (rpe_db.Length == 0) continue;

                    ResourceArrayType rat = rpe_db[0].rat;
                    NativeList<Entity> ref_list = new NativeList<Entity>(Allocator.Temp);
                    for (int j = 0; j < rpe_db.Length; ++j)
                    {
                        ref_list.Add(rpe_db[j].baked_prefab);
                    }
                    var array2assign = ref_list.ToArray(Allocator.Persistent);
                    //state.EntityManager.AddComponent<Prefab>(array2assign);
                    switch (rat)
                    {
                        case ResourceArrayType.Common:
                            var ep = ResourceRefs.self.entity_prefabs;
                            clear_and_assign(ref ep.entity_prefabs_0, array2assign);
                            ResourceRefs.self.entity_prefabs = ep;
                            //ResourceRefs.self.initialize_count++;
                            Debug.Log("array2assign " + rat.ToString() + ":" + array2assign.Length);
                            break;
                        //case ResourceArrayType.Collider:
                        //    clear_and_assign(ref ResourceRefs.self.colliders, array2assign);
                        //    ResourceRefs.self.initialize_count++;
                        //    break;
                    }


                }
                q.Dispose();
                //q = state.EntityManager.CreateEntityQuery(typeof(AddMDTTTag), typeof(Prefab));//fixed

                //Debug.Log(targets.Length + " rpe processed" + "q AddMDTTTag count = " + q.CalculateChunkCount());

                //state.EntityManager.AddComponent<MachineDirectTransportTarget>(q);
                //state.EntityManager.RemoveComponent<AddMDTTTag>(q);
                //q.Dispose();
                //q = state.EntityManager.CreateEntityQuery(typeof(AddMPTTag), typeof(Prefab));//fixed

                //Debug.Log(targets.Length + " rpe processed" + "q AddMPTTag count = " + q.CalculateChunkCount());

                //state.EntityManager.AddComponent<ManualProductionTotal>(q);
                //state.EntityManager.RemoveComponent<AddMPTTag>(q);
                //q.Dispose();
                state.EntityManager.DestroyEntity(targets);

                targets.Dispose();

                // lobby test registration
                /*
                var self_reg = new C2SGameServerRegistration();
                self_reg.ip = "127.0.0.1"; // game server ip
                self_reg.port = LGServer.self.listenPort;
                self_reg.game_mode = (byte)ExplorationModes.WanderingPlanet;
                //self_reg.season_duration = 3600 * 24 * 14; // 14 days
                self_reg.season_duration = 5;
                self_reg.waiting_period = (int)math.round(self_reg.season_duration * 0.25f);
                self_reg.name = "Gaia";
                LGServer_Lobby.self.SendUnconnected(self_reg, "127.0.0.1", 3030); // lobby server ip
                */


//                if (ResourceRefs.self != null && ResourceRefs.self.initialized == false)
//                {
//                    if (ResourceRefs.self.initialize_count == 5)
//                    {
//#if UNITY_EDITOR
//                        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
//                        if (scene.name == "client_asm")
//                        {
//                            ResourceRefs.self.init_material_override();
//                        }
//                        else
//                        {
//#endif
//                            if (ServerConfigMgr.Instance.CLIENT_STANDALONE)
//                            {
//                                ResourceRefs.self.init_material_override();
//                            }
//                            else if (!ServerConfigMgr.Instance.CLIENT_STANDALONE && !ServerConfigMgr.Instance.SERVER_STANDALONE)
//                            {
//                                ResourceRefs.self.init_material_override();
//                            }
//#if UNITY_EDITOR
//                        }
//#endif
//                        ResourceRefs.self.initialized = true;
//                    }
//                }

            }
        }
    }
    void clear_and_assign(ref NativeArray<Entity> src, NativeArray<Entity> target)
    {
        if (src.IsCreated) src.Dispose();
        src = target;
    }
   
    //void refill_prefab_array(NativeParallelMultiHashMap<int, ResourcePrefabInsertInfo> map, ref NativeArray<Entity> cc_array,  ResourceArrayType rat)
    //{
    //    if (cc_array.IsCreated)
    //    {
    //        cc_array.Dispose();

    //    }
    //    NativeList<ResourcePrefabInsertInfo> prefab_list = new NativeList<ResourcePrefabInsertInfo>(Allocator.Temp);
    //    if(map.TryGetFirstValue((int)rat, out ResourcePrefabInsertInfo target, out NativeParallelMultiHashMapIterator<int> it))
    //    {
    //        do
    //        {
    //            prefab_list.Add(target);
    //        }
    //        while (map.TryGetNextValue(out target, ref it));
    //    }

    //    cc_array = new NativeArray<Entity>(prefab_list.Length, Allocator.Persistent);
    //    for(int i = 0; i < prefab_list.Length; ++i)
    //    {
    //        cc_array[prefab_list[i].array_index] = prefab_list[i].entity;
    //    }
    //    //cc_array = prefab_list.ToArray(Allocator.Persistent);
    //}
    
    //protected override void OnUpdate()
    //{
    //    //throw new System.NotImplementedException();
    //}
}

public enum ResourceArrayType:short
{
    Common,
    ItemIcon,
    Collider,
    Total,
}