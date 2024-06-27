using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using System.Text;

public class TestArchetype : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        NativeList<Entity> entities = new NativeList<Entity>(8, Allocator.Temp);
        for (int i = 0; i < 8; ++i)
        {
            entities.Add(em.CreateEntity());
        }
        em.AddComponent<Cmpt_Alpha>(entities.AsArray());
        print_once(em);

        em.AddComponent<Cmpt_Beta>(entities[1]);
        em.AddComponent<Cmpt_Beta>(entities[3]);
        em.AddComponent<Cmpt_Beta>(entities[5]);
        em.AddComponent<Cmpt_Beta>(entities[7]);

        print_once(em);

        em.AddComponent<Cmpt_Gamma>(entities[3]);
        em.AddComponent<Cmpt_Gamma>(entities[7]);

        print_once(em);
    }
    void print_once(EntityManager em)
    {
        var q = em.CreateEntityQuery(typeof(Cmpt_Alpha));
        var tmp = q.ToEntityArray(Allocator.Temp);
        StringBuilder sbuilder = new StringBuilder();
        for (int i = 0; i < tmp.Length; ++i)
        {
            sbuilder.Append(tmp[i].ToString());
        }
        Debug.Log(sbuilder.ToString());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
public struct Cmpt_Alpha : IComponentData
{
    public int value;
}
public struct Cmpt_Beta : IComponentData
{
    public int value;
}
public struct Cmpt_Gamma : IComponentData
{
    public int value;
}
public struct Cmpt_Delta : IComponentData
{
    public int value;
}