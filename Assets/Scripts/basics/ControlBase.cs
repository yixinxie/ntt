using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;

public class ControlBase : MonoBehaviour
{
    public float move_speed = 3f;
    public MouseModes mouse_mode;
    public IControl current_ctrl;
    [SerializeField]
    public WeaponControl3rdView weapon_ctrl;
    [SerializeField]
    public BuildControl build_ctrl;
    public Entity target_entity;
    // Start is called before the first frame update
    void Start()
    {
        weapon_ctrl = new WeaponControl3rdView();
        build_ctrl = new BuildControl();
        current_ctrl = weapon_ctrl;
    }
    
    // Update is called once per frame
    void Update()
    {
        if (target_entity == null)
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var mo_q = em.CreateEntityQuery(typeof(ManualMovementCtrl));
            var mo_entities = mo_q.ToEntityArray(Allocator.Temp);
            if (mo_entities.Length > 0)
            {
                target_entity = mo_entities[0];
                em.RemoveComponent<ManualMovementCtrl>(target_entity);
            }
            mo_q.Dispose();
        }
        var horizontal = Input.GetAxisRaw("Horizontal");
        var vertical = Input.GetAxisRaw("Vertical");
        var cam_t = Camera.main.transform;
        var cam_fwd = cam_t.forward;

        if(cam_fwd.y < 0f && (math.abs(horizontal) > float.Epsilon || math.abs(vertical) > float.Epsilon))
        {
            Vector3 cam_v = cam_fwd;
            cam_v.y = 0f;
            cam_v.Normalize();
            Vector3 cam_h = cam_t.right;
            cam_h.y = 0f;
            cam_h.Normalize();
            var disp = move_speed * Time.deltaTime * (Vector3)math.normalize(vertical * cam_v + horizontal * cam_h);
            transform.localPosition += disp;
        }
        if(target_entity.Equals(Entity.Null))
        {
            return;
        }
        current_ctrl.update(target_entity, Time.deltaTime);

        for (int i = 0; i < 9; ++i)
        {
            var testkey = KeyCode.Alpha1 + i;
            
            if (Input.GetKeyDown(testkey)) 
            {
                if (mouse_mode == MouseModes.Weapon)
                {
                    current_ctrl.cleanup();
                    mouse_mode = MouseModes.Build;
                    current_ctrl = build_ctrl;
                }
                else if (mouse_mode == MouseModes.Build)
                {
                    current_ctrl.cleanup();
                    mouse_mode = MouseModes.Weapon;
                    current_ctrl = weapon_ctrl;
                }
            }
        }

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
[System.Serializable]
public class BuildControl : IControl
{
    public void cleanup()
    {
        helddown = false;
    }

    public void lmb_down()
    {
    }
    public bool helddown;
    public float3 helddown_pos;
    public float3 dbgpos;
    public static float3 HitOnXZPlane(Camera cam)
    {
        if (Input.mousePosition.x < 0 || Input.mousePosition.x > Screen.width || Input.mousePosition.y < 0 || Input.mousePosition.y > Screen.height)
            return 0f;

        var ray = cam.ScreenPointToRay(Input.mousePosition);
        float y_diff = (-ray.origin.y);
        if (Mathf.Abs(ray.direction.y) > 0.01f)
        {
            float factor = y_diff / ray.direction.y;
            return ray.origin + ray.direction * factor;
        }
        return 0f;
    }
    public void update(Entity entity, float dt)
    {
        if (Input.GetMouseButtonDown(0))
        {
            //Debug.Log("lmb_down()");
            helddown = true;
            helddown_pos = HitOnXZPlane(Camera.main);
        }
        if (Input.GetMouseButtonUp(0))
        {
            //Debug.Log("lmb_up()");
            helddown = false;
        }
        if(helddown)
        {
            var this_hit_pos = HitOnXZPlane(Camera.main);
            Debug.DrawLine(helddown_pos, this_hit_pos, Color.yellow);
            dbgpos = this_hit_pos;
        }
    }

    public void lmb_up()
    {
    }
}
