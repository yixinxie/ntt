using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FluidTestUnit : MonoBehaviour
{
    [HideInInspector]
    public uint guid;
    public uint2 key;
    [HideInInspector]
    public Text txt;
    public bool pump;
    public bool drain;
    public int amount;
    public Entity target;
    [SerializeField]
    public FluidPipeInventory view;
    StringBuilder sbuilder = new StringBuilder();
    void Awake()
    {
        guid = (uint)gameObject.GetInstanceID();
        txt = GetComponentInChildren<Text>();
        target = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntity();
        MachineSimulationSystem.AddFluidPipeComponents(World.DefaultGameObjectInjectionWorld.EntityManager, target);
        
    }
    private void Start()
    {
        if(target.Equals(Entity.Null)== false)
            World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData(target, view);
    }

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        
    }
#endif
    private void Update()
    {
        //FluidInventory view;
        if (target.Equals(Entity.Null))
        {
            //MachineSimulationSystem.self.pipe_states.TryGetValue(key, out view);
        }
        else
        {
            view = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<FluidPipeInventory>(target);
        }
        if (pump)
        {
            pump = false;
            
            view.fs.volumes += (ushort)amount;
            view.fs.pressure += (ushort)amount;
            World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData(target, view);
        }
        if (drain)
        {
            drain = false;
            view.fs.volumes -= (ushort)math.min(view.fs.volumes, amount);
            World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData(target, view);
        }
        sbuilder.Clear();
        sbuilder.AppendFormat("{0}", view.fs.volumes);
        txt.text = sbuilder.ToString();

        if (target.Equals(Entity.Null))
        {
            //MachineSimulationSystem.self.pipe_states[key] = view;
        }
        else
        {
            World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData(target, view);
        }
    }

}
