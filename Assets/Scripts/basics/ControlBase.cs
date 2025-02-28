using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Entities.UniversalDelegates;

public class ControlBase : MonoBehaviour
{
    public static ControlBase self;
    public float move_speed = 3f;
    public MouseModes mouse_mode;
    public IControl current_ctrl;
    [SerializeField]
    public WeaponControl3rdView weapon_ctrl;
    [SerializeField]
    public BuildControl build_ctrl;
    public Entity target_entity; // entity being controlled
    // Start is called before the first frame update
    private void Awake()
    {
        self = this;
    }
    void Start()
    {
        weapon_ctrl = new WeaponControl3rdView();
        build_ctrl = new BuildControl();
        current_ctrl = weapon_ctrl;
    }
    public void sync2unit()
    {

    }
    // Update is called once per frame
    void Update()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (target_entity.Equals(Entity.Null))
        {
            var mo_q = em.CreateEntityQuery(typeof(ManualMovementCtrl));
            var mo_entities = mo_q.ToEntityArray(Allocator.Temp);
            if (mo_entities.Length > 0)
            {
                target_entity = mo_entities[0];
                Debug.Log("manual control acquires " + target_entity.ToString());
                //em.RemoveComponent<ManualMovementCtrl>(target_entity);
            }
            mo_q.Dispose();
        }
        var horizontal = Input.GetAxisRaw("Horizontal");
        var vertical = Input.GetAxisRaw("Vertical");
        var cam_t = Camera.main.transform;
        var cam_fwd = cam_t.forward;

        
        float3 disp = float3.zero;
        if (cam_fwd.y < 0f && (math.abs(horizontal) > float.Epsilon || math.abs(vertical) > float.Epsilon))
        {
            Vector3 cam_v = cam_fwd;
            cam_v.y = 0f;
            cam_v.Normalize();
            Vector3 cam_h = cam_t.right;
            cam_h.y = 0f;
            cam_h.Normalize();
            disp = move_speed * Time.deltaTime * math.normalize(vertical * cam_v + horizontal * cam_h);
            
        }
        if (em.HasComponent<LocalTransform>(target_entity))
        {
            var target_c0 = em.GetComponentData<LocalTransform>(target_entity);
            target_c0.Position += disp;
            em.SetComponentData(target_entity, target_c0);
            transform.localPosition = target_c0.Position;
        }
        

        current_ctrl.update(target_entity, Time.deltaTime);
        if (em.HasComponent<BuilderShortcuts>(target_entity))
        {
            var bs = em.GetComponentData<BuilderShortcuts>(target_entity);
            for (int i = 0; i < 10; ++i)
            {
                var key_index = i;

                if(i == 9)
                {
                    key_index = -1;
                }
                var testkey = KeyCode.Alpha1 + key_index;

                if (Input.GetKeyDown(testkey))
                {
                    Debug.Log(testkey.ToString());
                    

                    item2control(i, ref bs);
                    em.SetComponentData(target_entity, bs);
                }
            }
        }

    }
    public void item2control(int key, ref BuilderShortcuts states)
    {
        var idx = states.get_item_index(key);
        var selected_item = states.get_item(idx);
        if (selected_item == states.currently_selected) return;
        current_ctrl.cleanup();
        switch (selected_item)
        {
            case ItemType.Command_Center:
            case ItemType.Extractor:
            
                //current_ctrl.cleanup();
                mouse_mode = MouseModes.Build;
                current_ctrl = build_ctrl;
                break;
            case ItemType.Belt:
                break;
            case ItemType.Pistol:
                //current_ctrl.cleanup();
                mouse_mode = MouseModes.Weapon;
                current_ctrl = weapon_ctrl;
                break;
        }
        states.currently_selected = selected_item;
    }    
}
public enum MouseModes : byte
{
    Weapon,
    Build,
}
public interface IControl
{
    public void update(Entity entity, float dt);
    public void cleanup();
}

