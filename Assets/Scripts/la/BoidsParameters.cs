using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidsParameters : MonoBehaviour
{
    public static BoidsParameters self;
    public float avoid_factor = 0.1f;
    public float cohesion_factor = 0.0f;
    public float speedavg_factor = 0.0f;
    public float goal_factor = 0.3f;

    public const float const_avoid_factor = 0.1f;
    public const float const_cohesion_factor = 0.02f;
    public const float const_speedavg_factor = 0.02f;
    public const float const_goal_factor = 0.5f;
    void Awake(){
        self = this;

    }

}
