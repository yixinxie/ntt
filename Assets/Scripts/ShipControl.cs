using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ShipControl : MonoBehaviour
{
    public static ShipControl self;
    public float forward_velocity;
    public float acceleration = 3.0f;
    public float cursor_influence = 1f;
    public float roll_degreepersecond = 60f;
    public float mouse_rot_dps = 30f;
    public bool in_control;
    private void Awake()
    {
        self = this;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            in_control = !in_control;
        }
        if (in_control == false) return;
        var dt = Time.deltaTime;
        var pos = transform.localPosition;
        var forward_dir = transform.forward;
        forward_velocity += acceleration * Input.GetAxisRaw("Vertical") * dt;
        //if(forward_velocity < 0f)
        //{
        //    forward_velocity = 0f;
        //}
        pos += forward_dir * forward_velocity * dt;
        transform.localPosition = pos;

        
        
        var tmp_mousepos = Input.mousePosition;
        mouse_pos = new float2(tmp_mousepos.x, tmp_mousepos.y);
        screen_center = new float2(Screen.width / 2f, Screen.height / 2f);

        //var mouse_ray = Camera.main.ScreenPointToRay(tmp_mousepos);
        //var center_ray = Camera.main.ScreenPointToRay(new Vector3(screen_center.x, screen_center.y));
        //Debug.DrawLine(mouse_ray.origin, mouse_ray.origin + mouse_ray.direction, Color.green);
        //Debug.DrawLine(center_ray.origin, center_ray.origin + center_ray.direction, Color.green);
        var current_rot = transform.localRotation;

        var roll = Input.GetAxisRaw("Horizontal") * roll_degreepersecond * dt;
        var roll_rot = Quaternion.AngleAxis(-roll, forward_dir);


        current_rot = roll_rot*current_rot;
        //current_rot = current_rot * roll_rot;
        //if(false)
        {
            var mouse_rot_xy = mouse_pos - screen_center;
            if (math.distance(mouse_rot_xy, 0f) > 50f)
            {
                var pitch_rot = Quaternion.AngleAxis(mouse_rot_dps * dt * -mouse_rot_xy.y, transform.right);
                var yaw_rot = Quaternion.AngleAxis(mouse_rot_dps * dt * mouse_rot_xy.x, transform.up);
                current_rot = pitch_rot * yaw_rot * current_rot;
            }
        }

        transform.localRotation = current_rot;
    }
    public float2 mouse_pos;
    public float2 screen_center;

}
