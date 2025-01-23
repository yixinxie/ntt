/**
 * Copyright 2021-2022 Chongqing Centauri Technology LLC.
 * All Rights Reserved.
 * 
 */
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;

public partial struct WeaponFireSystemV3
{
    public NativeList<Entity> drones2destroy;

    public NativeList<SpawnInstance> spawn_instances;
    public NativeList<WeaponFireAttemptInfo> weapon_fire_attempts;
    public const int legacy_count_per_batch = 8;
    public NativeArray<float3> spawn_offsets;
    public NativeArray<float> rn_sequence;
    public ComponentLookup<LocalTransform> c0_array;
    int rng_iterator;
    public Unity.Mathematics.Random rng;
    public void init(ref SystemState sstate)
    {
        this = default;
        c0_array = sstate.GetComponentLookup<LocalTransform>();
        rng = Unity.Mathematics.Random.CreateFromIndex(1);
        rng.InitState(3);
        drones2destroy = new NativeList<Entity>(Allocator.Persistent);
        spawn_instances = new NativeList<SpawnInstance>(Allocator.Persistent);
        weapon_fire_attempts = new NativeList<WeaponFireAttemptInfo>(Allocator.Persistent);
        rn_sequence = new NativeArray<float>(1024, Allocator.Persistent);
        for(int i = 0; i < rn_sequence.Length; ++i)
        {
            rn_sequence[i] = UnityEngine.Random.Range(0f, 1f);
        }
        spawn_offsets = new NativeArray<float3>(legacy_count_per_batch, Allocator.Persistent);
        spawn_offsets[0] = math.normalize(new float3(-1f, 0.2f, 0.5f));
        for(int i = 1; i < 4; ++i)
        {
            spawn_offsets[i] = math.normalize(spawn_offsets[0] + new float3(0f, 0f, -0.2f) * i);
        }

        for (int i = 4; i < 8; ++i)
        {
            var tmp = spawn_offsets[i - 4];
            tmp.x *= -1f;
            spawn_offsets[i] = tmp;
        }
        
        //Enabled = false;

    }
    
    public void dispose(ref SystemState sstate)
    {
        rn_sequence.Dispose();
        spawn_instances.Dispose();
        weapon_fire_attempts.Dispose();
        spawn_offsets.Dispose();
        drones2destroy.Dispose();
    }
    //public ComponentLookup<T> GetComponentDataFromEntity_helper<T>() where T: unmanaged, IComponentData
    //{
    //    return GetComponentLookup<T>();
    //}
    //public BufferLookup<T> GetBufferDataFromEntity_helper<T>() where T : unmanaged, IBufferElementData
    //{
    //    return GetBufferLookup<T>();
    //}
    public static bool should_heal(Entity target2heal, WeaponInfoV2 weapon, StorageCell ammo, UnitStats stats)
    {
        //bool should_heal = false;
        float healable_amount = stats.max_health - stats.health;

        if (healable_amount > Mathf.Abs(weapon.base_damage))
        {
            return ammo.item_count > 0;
        }
        return false;
    }
    public partial struct weapon_fire_0:IJobEntity
    {
        public NativeList<WeaponFireAttemptInfo> _weapon_fire_attempts;
        public ComponentLookup<LocalTransform> c0_array;
        public float dt;
        public void Execute(Entity entity, DynamicBuffer<WeaponInfoV2> weapons, DynamicBuffer<StorageCell> ammos, DynamicBuffer<CombatTarget> com_targets)
        {
#if UNITY_EDITOR
            int diff = weapons.Length - com_targets.Length;
            for (int i = 0; i < diff; ++i)
            {
                com_targets.Add(default);
                //Debug.LogWarning("com add default");
            }
#endif

            for (int i = 0; i < weapons.Length; ++i)
            {
                WeaponInfoV2 current_weapon = weapons[i];
                //if (current_weapon.weapon_type == WeaponTypes.Spawn_Swarm
                //|| current_weapon.weapon_type == WeaponTypes.Spawn_Fighter_Defensive) continue;
                if (current_weapon.weapon_cooldown_left <= dt)
                {
                    if (ammos[i].item_count > 0)
                    {
                        //for (int j = 0; j < targets.count; ++j)
                        var target = com_targets[i];

                        if (target.value.Equals(Entity.Null) == false && c0_array.HasComponent(target.value))
                        {
                            com_targets[i] = default;
                            _weapon_fire_attempts.Add(new WeaponFireAttemptInfo() { initiator = entity, combat_target = target, weapon_index = i });
                        }
                    }
                }
                else
                {
                    current_weapon.weapon_cooldown_left -= (half)dt;
                    weapons[i] = current_weapon;
                }
            }
        }
    }
    //public void OnUpdate(ref SystemState sstate, NativeList<WeaponFireAttemptInfo> _weapon_fire_attempts)
    //{
    //    //c0_array.Update(ref sstate);
        
    //    NativeHashSet<Entity> destroyed = new NativeHashSet<Entity>(8, Allocator.Temp);
    //    for (int i = 0; i < _weapon_fire_attempts.Length; ++i)
    //    {
    //        var attempt = _weapon_fire_attempts[i];
    //        process_fire_attempts(ref sstate, attempt.initiator, attempt.combat_target, attempt.weapon_index, destroyed);
    //    }
    //    //_weapon_fire_attempts.Dispose();
    //    var destroy_tmp = destroyed.ToNativeArray(Allocator.Temp);
    //    //for (int i = 0; i < destroy_tmp.Length; ++i)
    //    //    sstate.EntityManager.DestroyEntity(destroy_tmp[i]);
    //    sstate.EntityManager.DestroyEntity(destroy_tmp);
    //    //        spawn_update();
    //}
    public static void adjacent_biolink_remove(EntityManager em, Entity entity, Entity subject)
    {
        //var tmp_links = em.GetBuffer<BioNodeLinkRef>(entity);
        //for (int j = 0; j < tmp_links.Length; ++j)
        //{
        //    if (tmp_links[j].node_entity == subject)
        //    {
        //        if (tmp_links[j].positive == 1)
        //        {
        //            var go = GameObjectLink.self.GetMeshGo(tmp_links[j].link_id);
        //            if (go != null)
        //                GameObject.Destroy(go);

        //            GameObjectLink.self.RemoveLink(tmp_links[j].link_id);
        //        }
        //        tmp_links.RemoveAt(j);
        //        break;
        //    }
        //}
    }

    public static void self_link_remove(EntityManager em, Entity entity)
    {
        //var tmp_links = em.GetBuffer<BioNodeLinkRef>(entity);
        //for (int j = 0; j < tmp_links.Length; ++j)
        //{
        //    if (tmp_links[j].positive == 1)
        //    {
        //        var go = GameObjectLink.self.GetMeshGo(tmp_links[j].link_id);
        //        if (go != null)
        //            GameObject.Destroy(go);

        //        GameObjectLink.self.RemoveLink(tmp_links[j].link_id);
        //    }
        //}
    }
    public static void destroy_biolinks(EntityManager em, Entity entity)
    {
        //var types = em.GetComponentData<TypeCached>(entity);
        //if (types.prefab_index == PrefabType2Index.BioNode)
        //{
        //    var links = em.GetBuffer<BioNodeLinkRef>(entity).ToNativeArray(Allocator.Temp);
        //    for (int i = 0; i < links.Length; ++i)
        //    {
        //        adjacent_biolink_remove(em, links[i].node_entity, entity);
        //    }
        //    self_link_remove(em, entity);
        //}
    }
    //void drone_detarget()
    //{
    //    var ecb = new EntityCommandBuffer(Allocator.TempJob);
    //    Entities.WithAll<RefreshCombatTargetsCmd, DroneTag>()
    //    .ForEach((Entity entity, DynamicBuffer<CombatTargets> targets, in ColliderRef col_ref) =>
    //    {
    //        //UnityEngine.Debug.Log(entity.ToString() + " refresh combattargets");
    //        for (int i = 0; i < targets.Length; ++i)
    //        {
    //            CombatTargets combatTargets = targets[i];
    //            if (combatTargets.count > 0 && col_ref.value != Entity.Null)
    //            {
    //                ecb.RemoveComponent<DesiredPosition>(col_ref.value);
    //            }
    //        }

    //    }).Run();
    //    ecb.Playback(EntityManager);
    //    ecb.Dispose();
    //}
    
    void spawn_update()
    {
        //var _spawn_offsets = spawn_offsets;
        //float dt = Time.DeltaTime;
        //var _spawns = spawn_instances;
        ////var ends_array = GetComponentLookup<ChannelEnds>();
        //var c0_array = GetComponentLookup<LocalTransform>();
        //var c1_array = GetComponentLookup<Rotation>();
        //if (spawn_paused == false)
        //{
        //    Entities
        //        .WithAll<InCombat>()
        //    .ForEach((Entity entity
        //    //, DynamicBuffer<CombatDroneRef> drone_refs
        //    //ref DroneSpawnCooldown spawn_cd, 
        //    , DynamicBuffer<DroneSpawnType> spawn_types
        //    , DynamicBuffer<DroneSpawnDelay> spawn_delays
        //    , ref DroneSpawnStates spawn_states
        //    //, in CombatTeam team
        //    //, in DroneCount max_drone_count
        //    ) =>
        //    {
        //        if (spawn_states.ptr == spawn_delays.Length) return;
        //        spawn_states.elapsed += dt;
        //        if (spawn_states.elapsed >= spawn_delays[spawn_states.ptr].value)
        //        {
        //            for (int j = 0; j < spawn_states.count_per_batch; ++j)
        //            {
        //                var spawnpos = _spawn_offsets[j];
        //            //var diff = math.normalize(spawnpos - c0_array[origin].Value);
        //            var diff = spawnpos;
        //                var origin_rot = c1_array[entity].Value;
        //                spawnpos = c0_array[entity].Value + math.mul(origin_rot, spawnpos * 5f);

        //            //int sp_type = (team.value == 0) ? (int)StructureType.Fighter : (int)StructureType.Hatchling;
        //            int sp_type = spawn_types[spawn_states.ptr * spawn_states.count_per_batch + j].value;

        //                var si = new SpawnInstance()
        //                {
        //                    host = entity,
        //                //initial_target = destination,
        //                //channel = current_channel,
        //                spawn_position = spawnpos,
        //                    spawn_rotation = math.mul(origin_rot, quaternion.LookRotation(diff, new float3(0f, 1f, 0f))),
        //                    spawn_type = sp_type
        //                };
        //                _spawns.Add(si);
        //            }
        //            spawn_states.elapsed = 0f;
        //            spawn_states.ptr++;
        //        }
        //    }).Run();

        //    for (int i = 0; i < spawn_instances.Length; ++i)
        //    {
        //        process_spawn_attempts(spawn_instances[i]);
        //    }
        //    spawn_instances.Clear();
        //}
        ////drone_detarget();
        ////drone_dp_set();
        
    }
    void drone_dp_set()
    {
        //// drone command issue pass, todo: make this one time only!
        //var incombat_array = GetComponentLookup<InCombat>();
        //var dtp_array = GetComponentLookup<DroneTargetPosition>();
        //var dp_array = GetComponentLookup<DesiredPosition>();
        //var c0_array = GetComponentLookup<LocalTransform>();

        //Entities
        //    .WithAll<InCombat>()
        //.ForEach((Entity entity,
        //DynamicBuffer<CombatDroneRef> drone_refs, DynamicBuffer<CombatTargets> combat_targets_array
        ////,DynamicBuffer<StorageCell> ammos, DynamicBuffer<WeaponInfoV2> weapons
        ////,ref OutputAlternate oa
        //) =>
        //{
        //    //if (weapons.Length == 0 || weapons[0].weapon_type != WeaponTypes.Spawn_Swarm || drone_refs.Length == 0) return;
        //    int valid_target_count = 0;
        //    var ctargets = combat_targets_array[0];
        //    for (int i = 0; i < ctargets.count; ++i)
        //    {
        //        if (incombat_array.HasComponent(ctargets.value_at(i)))
        //        {
        //            valid_target_count++;
        //        }
        //        else
        //        {
        //            break;
        //        }
        //    }
        //    //var carrier_position = c0_array[entity].Value;

        //    int drone_count = drone_refs.Length;

        //    int each = Mathf.CeilToInt((float)drone_count / valid_target_count);  // 7 / 2 = 4
        //    //int remainder = drone_count % valid_target_count; // 17 % 5 = 2
        //    //int full_batches = (drone_count - remainder) / each; // 17 - 2 = 15, 15 / 3 = 5(full batches)
        //    int batches = valid_target_count;
        //    int drone_cmd_incre = 0;
        //    int target_index = 0;

        //    while (batches > 0)
        //    {
        //        var target_entity = ctargets.value_at(target_index);
        //        //if (c0_array.HasComponent(target_entity) == false) Debug.LogWarning("ctargets.value_at(target_index)");
        //        if (c0_array.HasComponent(target_entity) == false) continue;
        //        var target_c0 = c0_array[target_entity].Value;
        //        for (int j = 0; j < each && drone_cmd_incre + j < drone_count; ++j)
        //        {
        //            var drone = drone_refs[drone_cmd_incre + j].value;
        //            //if (dtp_array.HasComponent(drone) == false) Debug.LogWarning("dtp_array.HasComponent(drone)");
        //            if (dtp_array.HasComponent(drone))
        //                dtp_array[drone] = new DroneTargetPosition() { value = target_c0 };

        //            if (dp_array.HasComponent(drone))
        //            {
        //                var dp = new DesiredPosition();
        //                dp.value = target_c0;
        //                dp.init_finish_line_vec(c0_array[drone].Value);
        //                dp_array[drone] = dp;
        //            }

        //            //dp.init_finish_line_vec(c0.Position);

        //        }
        //        //Debug.DrawLine(carrier_position, target_c0, Color.green, 0.1f);
        //        drone_cmd_incre += each;
        //        target_index++;
        //        batches--;
        //    }
        //}).Run();
    }

    public static void process_fire_attempts(ref SystemState sstate, Entity entity, CombatTarget targets, int weapon_index, NativeHashSet<Entity> unit_destroyed)
    {
        if (sstate.EntityManager.HasComponent<LocalTransform>(entity) == false) return;
        if (sstate.EntityManager.HasComponent<LocalTransform>(targets.value) == false) return;
        var host_weapons = sstate.EntityManager.GetBuffer<WeaponInfoV2>(entity);
        if (weapon_index >= host_weapons.Length) return;
        WeaponInfoV2 current_weapon = host_weapons[weapon_index];
        //var hashed_entities = ResourceRefs.self.hashed_entities;
        //var clasers = LaserRenderSystem.self.continuous_laser_states;
        //var lasers = LaserRenderSystem.self.impulse_laser_states;
        //var clasers_enemy = LaserRenderSystem.self.continuous_laser_states_enemy;



        //var _rng = rng;
        //_rng.InitState(rng.NextUInt());

        ////var bio_swarm = ResourceRefs.self.GetStructureEntity(PrefabType2Index.Swarm);
        //bool create_meshes = MainSingleton.Instance.current_scene == SceneType.Combat || MainSingleton.Instance.current_scene == SceneType.PlatformCombat;
        bool fired_once = false;
        //Entity tmp_entity;

        var c0c1 = new CachedTurretTransform();
        if (sstate.EntityManager.HasComponent<CachedTurretTransform>(entity) == false)
        {
            //Debug.Log(entity.ToString() + " has no CachedTurretTransform");
            var lt = sstate.EntityManager.GetComponentData<LocalTransform>(entity);
            c0c1.c0 = lt.Position;
            c0c1.c1 = lt.Rotation;
        }
        else
        {
            c0c1 = sstate.EntityManager.GetComponentData<CachedTurretTransform>(entity);
        }
        var team = sstate.EntityManager.GetComponentData<CombatTeam>(entity);

        switch (current_weapon.weapon_type)
        {
            //case WeaponTypes.Laser_Offensive_Charged:
            //    if (targets.count > 0)
            //    {
            //        var target = targets.value_at(0);
            //        var weapon_target_position = EntityManager.GetComponentData<LocalTransform>(target).Value;
            //        var claser = new ContinuousLaserStates();
            //        claser.from_entity = entity;
            //        claser.to_entity = target;
            //        claser.def_from_position = c0c1.c0;
            //        claser.def_to_position = weapon_target_position;
            //        claser.Init((byte)team.value);
            //        //lis.Init(c0c1.c0, weapon_target_position, (byte)team.value);
            //        clasers.Add(claser);
            //        float3 hit_dir = math.normalize(weapon_target_position - c0c1.c0);
            //        CombatDamageCalculateSystem.self.damage_instances.Add(new DamageInstance()
            //        {
            //            initiator = entity,
            //            receiver = target,
            //            damage = current_weapon.base_damage,
            //            damage_types = current_weapon.damamge_types,
            //            hit_position = weapon_target_position, // todo?
            //            hit_dir = hit_dir
            //        });
            //        fired_once = true;
            //        //Debug.DrawLine(c0c1.c0, weapon_target_position, Color.yellow, 2f);
            //    }
            //    break;

            //case WeaponTypes.Laser_Offensive:
            //    //Debug.Log("laser!");
            //    if (targets.count > 0)
            //    {
            //        var target = targets.value_at(0);
            //        var weapon_target_position = EntityManager.GetComponentData<LocalTransform>(target).Value;
            //        var lis = new ImpulseLaserStates();

            //        lis.Init(c0c1.c0, weapon_target_position, (byte)team.value);
            //        lasers.Add(lis);
            //        float3 hit_dir = math.normalize(weapon_target_position - c0c1.c0);

            //        CombatDamageCalculateSystem.self.damage_instances.Add(new DamageInstance()
            //        {
            //            initiator = entity,
            //            receiver = target,
            //            damage = current_weapon.base_damage,
            //            damage_types = current_weapon.damamge_types,
            //            hit_position = weapon_target_position, // todo?
            //            hit_dir = hit_dir
            //        });
            //        fired_once = true;
            //    }
            //    break;
            //case WeaponTypes.Laser_Offensive_Cont:
            //    if (targets.count > 0)
            //    {
            //        var target = targets.value_at(0);
            //        var weapon_target_position = EntityManager.GetComponentData<LocalTransform>(target).Value;
            //        var claser = new ContinuousLaserStates();
            //        claser.from_entity = entity;
            //        claser.to_entity = target;
            //        claser.def_from_position = c0c1.c0;
            //        claser.def_to_position = weapon_target_position;
            //        claser.Init((byte)team.value);
            //        //lis.Init(c0c1.c0, weapon_target_position, (byte)team.value);
            //        clasers_enemy.Add(claser);
            //        float3 hit_dir = math.normalize(weapon_target_position - c0c1.c0);
            //        CombatDamageCalculateSystem.self.damage_instances.Add(new DamageInstance()
            //        {
            //            initiator = entity,
            //            receiver = target,
            //            damage = current_weapon.base_damage,
            //            damage_types = current_weapon.damamge_types,
            //            hit_position = weapon_target_position, // todo?
            //            hit_dir = hit_dir
            //        });
            //        fired_once = true;
            //    }
            //    break;

            case WeaponTypes.Cannon:
                var single_target = targets.value;
                if (single_target != Entity.Null)
                {
                    if(sstate.EntityManager.HasComponent<UnitStats>(single_target))
                    {
                        fired_once = true;
                        var us = sstate.EntityManager.GetComponentData<UnitStats>(single_target);
                        var instance_damage = (float)current_weapon.base_damage - us.defense;
                        us.health -= instance_damage;
                        Debug.Log(entity.ToString() + " hits " + single_target.ToString() + " for " + instance_damage);
                        if(us.health <= 0f)
                        {
                            us.health = 0f;
                            unit_destroyed.Add(single_target);
                            Debug.Log(single_target.ToString() + " dies.");
                        }
                        sstate.EntityManager.SetComponentData(single_target, us);
                        float3 weapon_target_position = sstate.EntityManager.GetComponentData<LocalTransform>(single_target).Position;
                        Debug.DrawLine(c0c1.c0, weapon_target_position, Color.red, 0.5f);

                    }
                    //
                    //var cannon_shot_spawn_offset = math.mul(c0c1.c1, new float3(0f, 0f, 4.3f));
                    //

                    //tmp_entity = EntityManager.Instantiate(hashed_entities[(int)current_weapon.projectile_type]);
                    ////Debug.DrawLine(c0c1.c0, weapon_target_position, Color.red, 0.5f);
                    //init_cannon_timed_scattered(EntityManager, entity, tmp_entity, c0c1.c0 + cannon_shot_spawn_offset, single_target, weapon_target_position, team, _rng);
                    //EntityManager.SetComponentData(tmp_entity, new ProjectilePropertiesFromLauncher()
                    //{ damage = current_weapon.base_damage, damage_types = current_weapon.damamge_types });

                    //Unity.Physics.CollisionFilter cfilter = default;
                    //cfilter.CollidesWith = team.HostileTeamMask();
                    //cfilter.BelongsTo = StructureInteractions.Layer_vehicle | StructureInteractions.Layer_character;
                    ////cfilter.BelongsTo = StructureInteractions.Layer_vehicle;

                    //EntityManager.SetComponentData(tmp_entity, new ProjectileCollisionProperties() { cfilter = cfilter });
                    //if (create_meshes)
                    //    EntityManager.AddComponent<MeshCreateCmd>(tmp_entity);

                    //CombatSoundRefs.self.Play(0, c0c1.c0);

                    //// muzzle flash fx
                    //var muzzle_flash = EntityManager.Instantiate(ResourceRefs.self.misc_entities[14]);
                    //EntityManager.SetComponentData(muzzle_flash, new LocalTransform() { Value = c0c1.c0 + cannon_shot_spawn_offset });
                    //EntityManager.SetComponentData(muzzle_flash, new Rotation() { Value = CamControl.self.transform.rotation });
                    //EntityManager.SetComponentData(muzzle_flash, new MuzzleFlashNoise() { value = _rng.NextFloat(1000f) });

                }
                break;
            
            //case WeaponTypes.Projectile:
            //    for (int j = 0; j < targets.count; ++j)
            //    {
            //        var target = targets.value_at(j);
            //        if (target == Entity.Null) continue;

            //        fired_once = true;
            //        float3 weapon_target_position = EntityManager.GetComponentData<LocalTransform>(target).Value;

            //        tmp_entity = EntityManager.Instantiate(hashed_entities[(int)current_weapon.projectile_type]);
            //        init_projectile(EntityManager, entity, tmp_entity, c0c1.c0, c0c1.c1, team, target, weapon_target_position);
            //        EntityManager.SetComponentData(tmp_entity, new ProjectilePropertiesFromLauncher()
            //        { damage = current_weapon.base_damage, damage_types = current_weapon.damamge_types });
            //        if (create_meshes)
            //        {
            //            EntityManager.AddComponent<MeshCreateCmd>(tmp_entity);
            //            GameObjectLink.self.PlayAnim(entity, 1); // required by bionode's animation playback.
            //        }

            //        CombatSoundRefs.self.Play(1, c0c1.c0);
            //        break;
            //    }
            //    break;
            //case WeaponTypes.Projectile_Platform:
            //    for (int j = 0; j < targets.count; ++j)
            //    {
            //        var target = targets.value_at(j);
            //        if (target == Entity.Null) continue;

            //        fired_once = true;
            //        float3 weapon_target_position = EntityManager.GetComponentData<LocalTransform>(target).Value;

            //        tmp_entity = EntityManager.Instantiate(hashed_entities[(int)current_weapon.projectile_type]);
            //        init_projectile_guided(EntityManager, entity, tmp_entity, c0c1.c0, c0c1.c1, team, target, weapon_target_position);
            //        EntityManager.SetComponentData(tmp_entity, new ProjectilePropertiesFromLauncher()
            //        { damage = current_weapon.base_damage, damage_types = current_weapon.damamge_types });

            //        EntityManager.AddComponent<MeshCreateCmd>(tmp_entity);

            //        CombatSoundRefs.self.Play(1, c0c1.c0);
            //        break;
            //    }
            //    break;

            case WeaponTypes.Repair_Multi: // channeled
            case WeaponTypes.Repair: // channeled
                {
                    //fired_once = process_beam(entity, current_weapon, targets, ResourceRefs.self.misc_entities[5], create_meshes);
                }
                break;
            //case WeaponTypes.Spawn_Fighter:
            //case WeaponTypes.Spawn_Hatchling:
            //case WeaponTypes.Spawn_Swarm:

            //    var _hatchery = EntityManager.GetComponentData<DroneCount>(entity);
            //    for (int j = 0; j < targets.count; ++j)
            //    {
            //        var target = targets.value_at(j);
            //        if (target == Entity.Null) continue;
            //        int drone_count = EntityManager.GetBuffer<CombatDroneRef>(entity).Length;
            //        if (drone_count < _hatchery.max_count)
            //        {
            //            //float rng = _rn_sequence[_rng_it];
            //            //_rng_it++;
            //            //_rng_it = _rng_it % _rn_sequence.Length;
            //            //Debug.Log("WeaponModes.Spawn " + weapons[i].projectile_type.ToString());
            //            if (hashed_entities.ContainsKey((int)current_weapon.projectile_type))
            //            {
            //                tmp_entity = EntityManager.Instantiate(hashed_entities[(int)current_weapon.projectile_type]);
            //                float3 weapon_target_position = EntityManager.GetComponentData<LocalTransform>(target).Value;
            //                //var new_spawn = create_spawn(EntityManager, tmp_entity, entity, c0, weapon_target_position + (rng.NextFloat() - 0.5f) * new float3(0f, 2f, 0f), team);
            //                init_spawn(EntityManager, tmp_entity, entity, c0, weapon_target_position, team);
            //                //EntityManager.AddComponentData(new_spawn, new CB_Assignment() { owner_entity = entity });
            //                //Debug.DrawLine(c0c1.c0, weapon_target_position, Color.green, 0.5f);
            //                var combat_drones = EntityManager.GetBuffer<CombatDroneRef>(entity);
            //                combat_drones.Add(new CombatDroneRef() { value = tmp_entity });
            //                fired_once = true;
            //            }
            //        }
            //    }
            //    break;
            default:
                Debug.LogWarning("unexpected weapon type:" + current_weapon.weapon_type.ToString());
                break;
        }
        if (fired_once)
        {
            //if (current_weapon.burst_max > 0)
            //{
            //    current_weapon.burst_index++;
            //    if (current_weapon.burst_index < current_weapon.burst_max)
            //    {
            //        current_weapon.cooldown_left = current_weapon.burst_cooldown;
            //    }
            //    else
            //    {
            //        current_weapon.cooldown_left = current_weapon.cooldown_max;
            //        current_weapon.burst_index = 0;
            //    }

            //}
            //else
            {
                current_weapon.weapon_cooldown_left = current_weapon.weapon_cooldown_total;
            }

            var weapon_db = sstate.EntityManager.GetBuffer<WeaponInfoV2>(entity);
            weapon_db[weapon_index] = current_weapon;

            // update ammunitions
            var ammos = sstate.EntityManager.GetBuffer<StorageCell>(entity);
            var tmp = ammos[weapon_index];
            tmp.item_count--;
            ammos[weapon_index] = tmp;
        }
    }

    void process_spawn_attempts(SpawnInstance sp_inst)
    {
        //Entity entity = sp_inst.host;
        ////Entity target = sp_inst.initial_target;//, Entity channel;
        //var hashed_entities = ResourceRefs.self.hashed_entities;

        //var _rng = rng;
        //_rng.InitState(rng.NextUInt());

        ////bool fired_once = false;

        ////var c0 = EntityManager.GetComponentData<LocalTransform>(entity);
        ////var c1 = EntityManager.GetComponentData<Rotation>(entity);


        ////float rng = _rn_sequence[_rng_it];
        ////_rng_it++;
        ////_rng_it = _rng_it % _rn_sequence.Length;
        ////Debug.Log("WeaponModes.Spawn " + weapons[i].projectile_type.ToString());
        //if (hashed_entities.ContainsKey(sp_inst.spawn_type) == false)
        //    return;

        //var team = EntityManager.GetComponentData<CombatTeam>(entity);
        ////float3 weapon_target_position = EntityManager.GetComponentData<LocalTransform>(sp_inst.initial_target).Value;
        //Entity drone_entity = EntityManager.Instantiate(hashed_entities[sp_inst.spawn_type]);

        //EntityManager.SetComponentData(drone_entity, new LocalTransform() { Value = sp_inst.spawn_position });
        //EntityManager.SetComponentData(drone_entity, new Rotation() { Value = sp_inst.spawn_rotation });
        ////var new_spawn = create_spawn(EntityManager, tmp_entity, entity, c0, weapon_target_position + (rng.NextFloat() - 0.5f) * new float3(0f, 2f, 0f), team);
        //init_spawn(EntityManager, drone_entity, entity, default, team);
        ////EntityManager.AddComponentData(new_spawn, new CB_Assignment() { owner_entity = entity });

        //var collider_prefab = ResourceRefs.self.colliders[12];
        //AddPhysicsComponents(EntityManager, drone_entity, collider_prefab);
        //LAOverlapAuthoring.AddLAV2ComponentsStatic(EntityManager, drone_entity);
        //EntityManager.SetComponentData(drone_entity, new LA_Radius() { value = 5 } );
        //EntityManager.SetComponentData(drone_entity, new MovementInfo() { speed = 25, angular_speed = 180 });


        ////Debug.DrawLine(c0c1.c0, weapon_target_position, Color.green, 0.5f);
        //var combat_drones = EntityManager.GetBuffer<CombatDroneRef>(entity);
        //combat_drones.Add(new CombatDroneRef() { value = drone_entity });

        //var comtargets = EntityManager.GetBuffer<CombatTargets>(sp_inst.host);
        //var target_c0 = EntityManager.GetComponentData<LocalTransform>(comtargets[0].value_at(0)).Value;

        //EntityManager.SetComponentData(drone_entity, new DroneTargetPosition() { value = target_c0 });
        //var dp = new DesiredPosition();
        //dp.value = target_c0;
        //dp.init_finish_line_vec(sp_inst.spawn_position);
        //EntityManager.SetComponentData(drone_entity, dp);
        //var move_states = new LAV2MovementStates();
        //move_states.move_state = MovementStates.Moving;
        //EntityManager.SetComponentData(drone_entity, move_states);

    }
    public static void AddPhysicsComponents(EntityManager em, Entity target, Entity physics_prefab)
    {
        var pc = em.GetComponentData<PhysicsCollider>(physics_prefab);
        em.AddComponentData(target, pc);

        // only kinematic colliders will have the following.
        var pm = em.GetComponentData<PhysicsMass>(physics_prefab);
        em.AddComponentData(target, pm);

        var pv = em.GetComponentData<PhysicsVelocity>(physics_prefab);
        em.AddComponentData(target, pv);

        var pgf = em.GetComponentData<PhysicsGravityFactor>(physics_prefab);
        em.AddComponentData(target, pgf);
    }
    public static void RemovePhysicsComponents(EntityManager em, Entity target)
    {
        em.RemoveComponent(target, new ComponentTypeSet(
            typeof(PhysicsCollider), typeof(PhysicsMass),
            typeof(PhysicsVelocity), typeof(PhysicsGravityFactor)
            ));
    }
    public static void BeamScaling(EntityManager em, Entity new_entity, float3 c0, float3 target_position)
    {
        var diff = math.normalize(target_position - c0);
        var distance = math.distance(target_position, c0);
        var beam_rot = Quaternion.FromToRotation(Vector3.forward, diff);

        em.SetComponentData(new_entity, LocalTransform.FromPositionRotation((target_position + c0) / 2f, beam_rot));
        //em.SetComponentData(new_entity, new PartialFacingTag() { facing = diff });
        var nuscale = em.GetComponentData<PostTransformMatrix>(new_entity);
        var tmp = nuscale.Value;
        tmp = float4x4.Scale(distance);
        nuscale.Value = tmp;
        em.SetComponentData(new_entity, nuscale);
    }
    public static void BeamScaling(EntityCommandBuffer em, Entity new_entity, float3 c0, float3 target_position)
    {
        var diff = math.normalize(target_position - c0);
        var distance = math.distance(target_position, c0);
        var beam_rot = Quaternion.FromToRotation(Vector3.forward, diff);

        em.SetComponent(new_entity, LocalTransform.FromPositionRotation((target_position + c0) / 2f, beam_rot));
        //em.SetComponent(new_entity, new PartialFacingTag() { facing = diff });
        em.SetComponent(new_entity, new PostTransformMatrix() { Value = float4x4.Scale(0.05f, 0.05f, distance) });
    }
    //bool process_beam(Entity initiator, WeaponInfoV2 weapon, CombatTargets comtargets, Entity laser_entity, bool create_meshes)
    //{
    //    bool fired_once = false;
    //    var c0 = EntityManager.GetComponentData<LocalTransform>(initiator).Position;
    //    //var contacts = EntityManager.GetBuffer<BeamContacts>(initiator).ToNativeArray(Allocator.Temp);
    //    var ammos = EntityManager.GetBuffer<StorageCell>(initiator).ToNativeArray(Allocator.Temp);
    //    var visuals_db = EntityManager.GetBuffer<BeamVisualEntity>(initiator);
    //    if (visuals_db.Length == 0)
    //    {
    //        for(int i = 0; i < comtargets.count; ++i)
    //        {
    //            visuals_db.Add(default);
    //        }
    //    }
    //    var visuals = visuals_db.ToNativeArray(Allocator.Temp);
        
    //    for (int i = 0; i < comtargets.count; ++i)
    //    {
    //        var target2heal = comtargets.value_at(i);
    //        //if (target2heal == Entity.Null) break;
    //        var stats = EntityManager.GetComponentData<UnitStats>(target2heal);
    //        bool should_heal = WeaponFireSystemV3.should_heal(target2heal, weapon, ammos[0], stats);

    //        if (should_heal)
    //        {
    //            fired_once = true;
    //            //CombatDamageCalculateSystem.self.damage_instances.Add(new DamageInstance()
    //            //{
    //            //    initiator = initiator,
    //            //    receiver = target2heal,
    //            //    damage = weapon.base_damage,
    //            //    damage_types = weapon.damamge_types,
    //            //    //hit_position = c0c1.c0,
    //            //    //hit_dir = math.mul(c1.Value, new float3(0f, 0f, 1f))

    //            //});
    //            if (visuals[i].value == Entity.Null && create_meshes)
    //            {
    //                // create laser
    //                Entity new_entity = EntityManager.Instantiate(laser_entity);

    //                var target_position = EntityManager.GetComponentData<LocalTransform>(target2heal).Position;
    //                //Debug.DrawLine(target_position, target_position + new float3(0f, 30f, 0f), Color.green);
    //                //Debug.DrawLine(c0c1.c0, c0c1.c0 + new float3(0f, 30f, 0f), Color.green);
    //                BeamScaling(EntityManager, new_entity, c0, target_position);

    //                var tmp_visuals = EntityManager.GetBuffer<BeamVisualEntity>(initiator);
    //                tmp_visuals[i] = new BeamVisualEntity() { value = new_entity };
    //            }
    //        }
    //        else
    //        {
    //            if (visuals[i].value != Entity.Null)
    //            {
    //                //Debug.Log(visuals[i].value.ToString() + " beam visual destroyed");
    //                // destroy laser
    //                EntityManager.DestroyEntity(visuals[i].value);
    //                //var tmp = visuals[i];
    //                //tmp.value = default;
    //                //visuals[i] = tmp;

    //                var tmp_visuals = EntityManager.GetBuffer<BeamVisualEntity>(initiator);
    //                tmp_visuals[i] = default;
    //            }
    //        }
    //    }
    //    return fired_once;
    //}
    struct ParticleInitialVectorJob : IJob
    {
        
        //[ReadOnly]
        //public NativeArray<float3> hit_positions;
        //[ReadOnly]
        //public NativeArray<float3> hit_dirs;

        [ReadOnly]
        public float3 hit_position;
        [ReadOnly]
        public float3 hit_dir;

        [ReadOnly]
        public float3 ship_pos;

        //[ReadOnly]
        //public NativeArray<float3> sphere_centers;
        [ReadOnly]
        public float3 sphere_center;
        [ReadOnly]
        public NativeArray<Entity> entities;
        public EntityCommandBuffer ecb;

        public Unity.Mathematics.Random rng;
        public void Execute()
        {
            for (int i = 0; i < entities.Length; ++i)
            {
                //var hit_position = hit_positions[i];
                //var hit_dir = hit_dirs[i];
                //var sphere_center = sphere_centers[i];

                float3 hit_normal = hit_position - sphere_center;
                hit_normal = math.normalize(hit_normal);
                float3 tangent = Vector3.Cross(hit_dir, hit_normal);
                float angle0_jitter = rng.NextFloat(-1f, 1f) * Mathf.PI / 2f * 0.5f; // 45 degrees
                var q0 = quaternion.AxisAngle(tangent, angle0_jitter);

                Entity particle_entity = entities[i];
                ecb.SetComponent(particle_entity, LocalTransform.FromPosition(hit_position));
                //ecb.SetComponent(particle_entity, new DNAParticleShipTarget() { target_position = ship_pos });
                //ecb.SetComponent(particle_entity, new DNAParticleMotion()
                //{
                //    from = hit_position,
                //    dir = math.mul(q0, hit_normal),
                //    distance = rng.NextFloat(5f, 8f),
                //    total = 2f
                //});
            }
        }
    }
    public void spawn_particles(int instances, float3 sphere_center, float3 _hit_dir, float3 _hit_position, float3 ship_position, uint rng_seed)
    {
        //Entity particle_prefab = ResourceRefs.self.misc_entities[3];
        //NativeArray<Entity> particle_entities = new NativeArray<Entity>(instances, Allocator.TempJob);
        //EntityManager.Instantiate(particle_prefab, particle_entities);

        //var initjob = new ParticleInitialVectorJob();
        //initjob.rng = Unity.Mathematics.Random.CreateFromIndex(rng_seed);
        //initjob.ship_pos = ship_position;
        ////initjob.hit_dirs = new NativeArray<float3>(instances, Allocator.TempJob);
        ////initjob.hit_positions = new NativeArray<float3>(instances, Allocator.TempJob);
        //initjob.hit_dir = _hit_dir;
        //initjob.hit_position = _hit_position;
        //initjob.sphere_center = sphere_center;
        //initjob.ecb = new EntityCommandBuffer(Allocator.TempJob);
        //initjob.entities = particle_entities;

        //initjob.Run();

        ////initjob.hit_dirs.Dispose();
        ////initjob.hit_positions.Dispose();
        //initjob.ecb.Playback(EntityManager);
        //initjob.ecb.Dispose();


        //particle_entities.Dispose();
    }
    public static float calc_damage(float incoming, byte damage_types, byte armor_types)
    {
        if (damage_types == 0 || armor_types == 0)
            return incoming;
        bool found = (damage_types & armor_types) > 0;

        //incoming = (found) ? incoming * 999f : incoming * 999f; // cheat
        //incoming = (found) ? incoming * 1.3f : incoming * 0.7f;
        incoming = (found) ? incoming * 1f : incoming * 1f;
        return incoming;
    }
    //public static float get_damage_from_level_influence(EntityManager em, Entity initiator)
    //{
    //    int initiator_level = em.GetComponentData<UnitLevel>(initiator).value;
    //    return 1f + ((float)initiator_level - 1f) * 0.1f;
    //}
    public static void process_damages(EntityManager em, NativeList<DamageInstance> damage_instances, NativeList<Entity> mark4destroy, uint rng_seed)
    {
        //bool create_meshes = true; // MainPanel.self.current_scene == SceneType.Combat;
        //for (int i = 0; i < damage_instances.Length; ++i)
        //{ 
        //    var current = damage_instances[i];
        //    //var weapons = em.GetBuffer<WeaponInfoV2>(current.initiator);
        //    //var weapon = weapons[current.intiator_weapon_index];
            
        //    if (em.Exists(current.receiver) == false || em.HasComponent<UnitStats>(current.receiver) == false) continue;
        //    var receiver_unitstats = em.GetComponentData<UnitStats>(current.receiver);
        //    float prior_health = receiver_unitstats.health;

        //    float damage_prior2level = current.damage;
        //    if (em.HasComponent<UnitLevel>(current.initiator))
        //    {
        //        damage_prior2level *= get_damage_from_level_influence(em, current.initiator);
        //    }

        //    float damage_calculated = calc_damage(damage_prior2level, current.damage_types, receiver_unitstats.armor_types);
        //    receiver_unitstats.health -= damage_calculated;
        //    if (em.HasComponent<WeaponInfoV2>(current.initiator))
        //    {
        //        var weapons = em.GetBuffer<WeaponInfoV2>(current.initiator).ToNativeArray(Allocator.Temp);
        //        if (weapons[current.intiator_weapon_index].projectile_type == StructureType.Cannon)
        //        {
        //            // play cannon shot hit fx.
        //            var hit_fx = em.Instantiate(ResourceRefs.self.misc_entities[15]);
        //            //Debug.DrawLine(current.hit_position, current.hit_position + current.hit_dir * 5f, Color.red, 1f);
        //            em.SetComponentData(hit_fx, LocalTransform.FromPositionRotation(current.hit_position, CamControl.self.transform.rotation));
        //            //em.SetComponentData(hit_fx, new Rotation() { Value = quaternion.LookRotation(current.hit_dir, new float3(0f, 1f, 0f)) });
        //            var rng_noise = Unity.Mathematics.Random.CreateFromIndex(rng_seed).NextFloat(1000f);
        //            em.SetComponentData(hit_fx, new MuzzleFlashNoise() { value = rng_noise });
        //        }
        //    }

            

        //    //Debug.Log(current.initiator.ToString() + " hits " + current.receiver.ToString() + " for " + damage_calculated);
        //    if (receiver_unitstats.health > receiver_unitstats.max_health)
        //    {
        //        receiver_unitstats.health = receiver_unitstats.max_health;
        //    }
        //    receiver_unitstats.health = math.clamp(receiver_unitstats.health, 0f, receiver_unitstats.max_health);
        //    float actual_damage_done = prior_health - receiver_unitstats.health;
        //    if (em.HasComponent<ReportInterval>(current.initiator))
        //    {
        //        //actual_damage_done
        //        var dps = em.GetComponentData<ReportInterval>(current.initiator);
        //        var db = em.GetBuffer<Report_DPSecond>(current.initiator);
        //        var tmp = db[dps.ptr];
        //        tmp.accumulative += actual_damage_done;
        //        db[dps.ptr] = tmp;

        //    }
        //    em.SetComponentData(current.receiver, receiver_unitstats);
            
        //    if (receiver_unitstats.health < float.Epsilon)
        //    {
        //        //if (em.GetComponentData<CombatTeam>(current.receiver).value == 1)
        //        //    Debug.Log(current.receiver.ToString() + " dies.");
        //        mark4destroy.Add(current.receiver);

        //        //if (em.HasComponent<CombatDroneRef>(current.receiver))
        //        //{
        //        //    var drones = em.GetBuffer<CombatDroneRef>(current.receiver);
        //        //    for(int j = 0; j < drones.Length; ++j)
        //        //    {
        //        //        mark4destroy.Add(drones[i].value);
        //        //    }
        //        //}
        //    }

        //    if (actual_damage_done < float.Epsilon) continue;
        //    if(em.HasComponent<ZoneTag>(current.receiver))
        //    {
        //        var zone_index = em.GetComponentData<ZoneTag>(current.receiver).index;
        //        self.platform_hit_elapsed[zone_index] = 1f;
        //    }

        //    if (em.Exists(current.initiator) && em.HasComponent<MultiOutInventory>(current.initiator))
        //    {
        //        // receiver plays animation
        //        if (create_meshes)
        //            GameObjectLink.self.PlayAnim(current.receiver, 0);

        //        // credit the initiator, disabled for now. 2022-5-30, x1x
        //        //if (em.HasComponent<BioResource>(current.receiver))
        //        //{
        //        //    var hit_resource = em.GetComponentData<BioResource>(current.receiver).item;
        //        //    var harvest = em.GetComponentData<MultiOutInventory>(current.initiator);
        //        //    int dep_idx = harvest.can_deposit(hit_resource, 1);
        //        //    if (dep_idx >= 0)
        //        //    {
        //        //        harvest.deposit_nocheck(dep_idx, hit_resource, 1);
        //        //        if (create_meshes)
        //        //        {
        //        //            var sphere_center = Statics.em.GetComponentData<LocalTransform>(current.receiver).Value;
        //        //            var ship_pos = Statics.em.GetComponentData<LocalTransform>(current.initiator).Value;
        //        //            int particle_amount = Mathf.CeilToInt(actual_damage_done / 10f);
        //        //            WeaponFireSystemV3.self.spawn_particles(particle_amount, sphere_center, current.hit_dir, current.hit_position, ship_pos, rng_seed);
        //        //        }
        //        //    }
        //        //    em.SetComponentData(current.initiator, harvest);
        //        //}
        //    }
        //    else if(create_meshes && em.HasComponent<ShipCombatShieldEntityRef>(current.receiver))
        //    {
        //        var shield_ref = em.GetComponentData<ShipCombatShieldEntityRef>(current.receiver);
        //        Entity shield_entity = shield_ref.value;
        //        if (shield_ref.value == Entity.Null)
        //        {
        //            shield_entity = em.Instantiate(ResourceRefs.self.misc_entities[7]);
        //            shield_ref.value = shield_entity;
        //            em.SetComponentData(current.receiver, shield_ref);

                        
        //        }

        //        var hit_sphere_center = Statics.em.GetComponentData<LocalTransform>(current.receiver).Value;
        //        em.SetComponentData(shield_entity, LocalTransform.FromPosition(hit_sphere_center));

        //        var hit_states = em.GetComponentData<ShipShieldHitStates>(shield_entity);
        //        int new_idx = hit_states.append_one();
        //        em.SetComponentData(shield_entity, hit_states);

                    
        //        //var hit_normal = current.hit_position - sphere_center;
        //        var hit_normal = current.hit_dir;
        //        //Debug.Log("shield hit normal " + hit_normal.ToString());
        //        //Debug.DrawLine(hit_sphere_center, hit_sphere_center + hit_normal * 50f, Color.red, 3f);
        //        ///hit_normal = math.normalize(hit_normal);
        //        const float fx_duration = 0.6f; // this corresponds to the duration value in the material
        //        switch (new_idx)
        //        {
        //            case 0:
        //                var hit_vec_0 = em.GetComponentData<ShipShieldHitVector_0>(shield_entity);
        //                hit_vec_0.value = -hit_normal * fx_duration;
        //                em.SetComponentData(shield_entity, hit_vec_0);
        //                break;

        //            case 1:
        //                var hit_vec_1 = em.GetComponentData<ShipShieldHitVector_1>(shield_entity);
        //                hit_vec_1.value = -hit_normal * fx_duration;
        //                em.SetComponentData(shield_entity, hit_vec_1);
        //                break;
        //            case 2:
        //                var hit_vec_2 = em.GetComponentData<ShipShieldHitVector_2>(shield_entity);
        //                hit_vec_2.value = -hit_normal * fx_duration;
        //                em.SetComponentData(shield_entity, hit_vec_2);
        //                break;
        //            case 3:
        //                var hit_vec_3 = em.GetComponentData<ShipShieldHitVector_3>(shield_entity);
        //                hit_vec_3.value = -hit_normal * fx_duration;
        //                em.SetComponentData(shield_entity, hit_vec_3);
        //                break;
        //        }
        //    }
        //}
        //damage_instances.Clear();
        //ProjectileMotionUpdateSystem.DestroyCombatants(em, mark4destroy);
        
    }
    public static Entity create_spawn(EntityCommandBuffer ecb, Entity tmp_entity, Entity _host, float3 c0, float3 target_c0, CombatTeam team)
    {
        var diff = target_c0 - c0;
        diff = math.normalize(diff);
        var quat = quaternion.LookRotation(diff, new float3(0f, 1f, 0f));
        ecb.SetComponent(tmp_entity, LocalTransform.FromPositionRotation(c0, quat));
        ecb.SetComponent(tmp_entity, team);
        //ecb.AddComponent(tmp_entity, new DesiredPosition() { value = target_c0 });
        //ecb.SetComponent(tmp_entity, new DroneTag() { host_carrier = _host });
        ecb.SetComponent(tmp_entity, new DroneTargetPosition() { value = target_c0 });
        return tmp_entity;
    }
    public static void init_spawn(EntityManager em, Entity tmp_entity, Entity _host, Entity _channel, CombatTeam team)
    {
        em.SetComponentData(tmp_entity, team);
        //var dp = new DesiredPosition();
        //dp.value = target_c0;
        //dp.goal_scale = 1f;
        //dp.init_finish_line_vec(c0c1.c0);
        //em.SetComponentData(tmp_entity, new DroneTargetPosition() { value = target_c0 });
        //em.SetComponentData(tmp_entity, new DroneTag() { host_carrier = _host,channel = _channel });
        //em.SetComponentData(tmp_entity, new ColliderRef() { picking_collider = tmp_entity });
    }
    //public static Entity create_bio_spawn(EntityCommandBuffer ecb, Entity tmp_entity, float3 c0, float3 target_c0, CombatTeam team)
    //{
    //    ecb.SetComponent(tmp_entity, new LocalTransform() { Value = c0 });
    //    var diff = target_c0 - c0;
    //    diff = math.normalize(diff);
    //    var quat = quaternion.LookRotation(diff, new float3(0f, 1f, 0f));
    //    ecb.SetComponent(tmp_entity, new Rotation() { Value = quat });
    //    ecb.SetComponent(tmp_entity, team);
    //    //ecb.SetComponent(tmp_entity, new DroneTag() { host = _host });
    //    return tmp_entity;
    //}
    public const float ParabolicControlPointHeightOffset = 15f;
    public static void init_projectile(EntityManager em, Entity _launcher, Entity tmp_entity, float3 c0, quaternion c1, CombatTeam team, Entity target, float3 target_position)
    {
        //// launching a projectile initially going upwards from the launcher, e.g. bionodes
        ////Entity tmp_entity = ecb.Instantiate(prototype_entity);

        //var hit_radius = em.GetComponentData<BioNodeSphereRadius>(target).value;

        //em.SetComponentData(tmp_entity, LocalTransform.FromPosition(c0));
        //float3 dir = target_position - c0;
        //dir = math.normalize(dir);

        //float3 right = math.cross(new float3(0f, 1f, 0f), dir); // right at the view of the launcher

        //quaternion rotated_by = quaternion.AxisAngle(right, -90f * Mathf.Deg2Rad);
        
        //var rotated_forward = math.mul(rotated_by, dir);
        //var rev_hit_dir = (c0 + rotated_forward * ParabolicControlPointHeightOffset) - target_position;
        //rev_hit_dir = math.normalize(rev_hit_dir);
        //var corrected_hit_position = target_position + rev_hit_dir * hit_radius;

        //var rotated_up = math.mul(rotated_by, new float3(0f, 1f, 0f));
        //var quat = quaternion.LookRotation(rotated_forward, rotated_up);
        //em.SetComponentData(tmp_entity, new Rotation() { Value = quat });
        //em.SetComponentData(tmp_entity, team);

        //em.SetComponentData(tmp_entity, new ProjectileEntities() { target = target, launcher = _launcher });
        //em.SetComponentData(tmp_entity, new ProjectileTimeToKill() { total_duration = 4f, end_position = corrected_hit_position, start_position = c0 });

        //var bezier = guided_bezier(c0, c1, target_position);
        //em.AddComponentData(tmp_entity, bezier);
        //var pmms = em.GetComponentData<ProjectileMissileMotionStates>(tmp_entity);
        //pmms.last_position = c0;
        //em.SetComponentData(tmp_entity, pmms);
    }

    public static void init_projectile_guided(EntityManager em, Entity _launcher, Entity tmp_entity, float3 c0, quaternion c1, CombatTeam team, Entity target, float3 target_position)
    {
        //em.SetComponentData(tmp_entity, new LocalTransform() { Value = c0 });
        //var quat = quaternion.LookRotation(math.mul(c1, new float3(0f, 1f, 0f)), math.mul(c1, new float3(0f, 0f, 1f)));
        //em.SetComponentData(tmp_entity, new Rotation() { Value = quat });
        //em.SetComponentData(tmp_entity, team);

        //em.SetComponentData(tmp_entity, new ProjectileEntities() { target = target, launcher = _launcher });
        //em.SetComponentData(tmp_entity, new ProjectileTimeToKill() { total_duration = 3f, end_position = target_position, start_position = c0 });

        //var bezier = guided_platform_bezier(c0, c1, target_position);
        //em.AddComponentData(tmp_entity, bezier);
        //var pmms = em.GetComponentData<ProjectileMissileMotionStates>(tmp_entity);
        //pmms.last_position = c0;
        //em.SetComponentData(tmp_entity, pmms);
    }

    public static BezierControls guided_bezier(float3 c0, quaternion c1, float3 target)
    {
        BezierControls ret = default;
        var localup = math.mul(c1, new float3(0f, 1f, 0f));

        var ctrl0 = c0 + localup * 15f;
        var ctrl1 = ctrl0;
        Debug.DrawLine(c0, ctrl0, Color.yellow, 1f);
        Debug.DrawLine(target, ctrl1, Color.yellow, 1f);
        ret.start = c0;
        ret.end = target;
        ret.start_ctrl = ctrl0;
        ret.end_ctrl = ctrl1;
        return ret;
    }
    //public static void init_cannon(EntityCommandBuffer ecb, Entity _launcher, Entity tmp_entity, LocalTransform c0, Entity target_entity, float3 target_c0, CombatTeam team)
    //{
    //    ecb.SetComponent(tmp_entity, c0);
    //    var diff = target_c0 - c0c1.c0;
    //    diff = math.normalize(diff);
    //    var quat = quaternion.LookRotation(diff, new float3(0f, 1f, 0f));
    //    ecb.SetComponent(tmp_entity, new Rotation() { Value = quat });
    //    ecb.SetComponent(tmp_entity, team);
    //    ecb.SetComponent(tmp_entity, new ProjectileEntities() { target = target_entity, launcher = _launcher });
    //}

    public static void init_cannon_timed_scattered(EntityManager em, Entity _launcher, Entity tmp_entity, float3 cannon_spawn_position, Entity target_entity, float3 target_c0, CombatTeam team, Unity.Mathematics.Random rng)
    {
        //em.SetComponentData(tmp_entity, new LocalTransform() { Value = cannon_spawn_position });
        //float3 offset = rng.NextFloat3();
        //offset.x *= rng.NextFloat(-1f, 1f);
        //offset.y *= rng.NextFloat(-1f, 1f);
        //offset.z *= rng.NextFloat(-1f, 1f);
        //offset = math.normalize(offset) * 3.7f;
        //var heading_target = target_c0 + offset;
        //var diff = heading_target - cannon_spawn_position;
        //diff = math.normalize(diff);
        //var quat = quaternion.LookRotation(diff, new float3(0f, 1f, 0f));
        ////Debug.DrawLine(cannon_spawn_position, heading_target, Color.green, 1f);
        //em.SetComponentData(tmp_entity, new Rotation() { Value = quat });

        //float distance = math.distance(heading_target, cannon_spawn_position);
        //float speed = 12.0f;
        //float total_travel_time = distance / speed;
        //em.SetComponentData(tmp_entity, new ProjectileTimeToKill() { total_duration = total_travel_time });

        //em.SetComponentData(tmp_entity, team);
        //em.SetComponentData(tmp_entity, new ProjectileEntities() { target = target_entity, launcher = _launcher });
    }

}
public struct RefreshChannelCmd : IComponentData
{

}
public struct DamageInstance
{
    public Entity initiator;
    public Entity receiver;
    public float3 hit_position;
    public float3 hit_dir;
    public int intiator_weapon_index;
    public float damage;
    public byte damage_types;

}

public struct SpawnInstance
{
    public Entity host;
    //public Entity initial_target;
    //public Entity channel;
    public float3 spawn_position;
    public quaternion spawn_rotation;
    public int spawn_type;
}
public struct BezierControls:IComponentData
{
    public float3 start;
    public float3 end;
    public float3 start_ctrl;
    public float3 end_ctrl;
    //public float3 last_position;
}

public struct WeaponFireAttemptInfo
{
    public Entity initiator;
    public CombatTarget combat_target;
    public int weapon_index;
}
public struct InstantDamageAttempt // instance weapons
{
    public Entity initiator_fleet;
    public Entity target_fleet;
    public short ship_index; // index in the initiator fleet
    public short index_in_target_fleet;
}
[InternalBufferCapacity(8)]
public struct DelayedDamageStates:IBufferElementData
{
    //public Entity initiator_fleet;
    //public Entity target_fleet;
    //public WeaponTypes cached_weapon_type;
    public WeaponTypes_V2 cached_weapon_type;
    public byte ship_index; // index in the initiator fleet
    public byte index_in_target_fleet;
    public int duration;
    public long start_time;

    public int HashCode(uint guid)
    {
        int hash = 4;

        hash = (hash << 4) ^ (hash >> 28) ^ (int)(guid);
        hash = (hash << 4) ^ (hash >> 28) ^ (int)(cached_weapon_type);
        hash = (hash << 4) ^ (hash >> 28) ^ (int)(ship_index);
        hash = (hash << 4) ^ (hash >> 28) ^ (int)(index_in_target_fleet);
        hash = (hash << 4) ^ (hash >> 28) ^ duration;
        hash = (hash << 4) ^ (hash >> 28) ^ (int)(start_time);

        return hash;

    }

}

//public struct CombatProjectileVisualCreateInfo
//{
//    public WeaponTypes_V2 cached_weapon_type;
//    public Entity from_entity;
//    public Entity to_entity;
//    public float3 from_position;
//    public quaternion from_rotation;
//    public float3 to_position;
//    public byte ship_index; // index in the initiator fleet
//    public byte index_in_target_fleet;
//    public short timeleft;
//    public short duration;
//}