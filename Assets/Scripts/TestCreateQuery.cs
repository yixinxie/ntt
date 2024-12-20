using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class TestCreateQuery : MonoBehaviour
{
    // Start is called before the first frame update
    EntityManager cached_em;
    void Start()
    {
        cached_em = World.DefaultGameObjectInjectionWorld.EntityManager;
    }
    // Update is called once per frame
    public void OnValueChanged(bool bo)
    {
        enabled = bo;
    }

    void Update()
    {
        for(int i = 0; i < 1000; ++i)
        {
            EntityQuery q = cached_em.CreateEntityQuery(typeof(TestQuerySpam));

        }
    }
}
public struct TestQuerySpam : IComponentData { }