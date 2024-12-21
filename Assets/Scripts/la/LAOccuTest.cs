using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class LAOccuTest : MonoBehaviour
{
    public static LAOccuTest self;
    [SerializeField]
    public MovementInfo mi_before;
    [SerializeField]
    public MovementInfo mi_after;
    public Entity target;
    public bool copy;
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
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (copy)
        {
            copy = false;
            mi_before = mi_after;
            em.SetComponentData(target, mi_before);
        }
        if(em.HasComponent<LocalTransform>(target))
        {
            var c0 = em.GetComponentData<LocalTransform>(target);
            c0.Position = transform.localPosition;
            em.SetComponentData(target, c0);

        }
    }
}
