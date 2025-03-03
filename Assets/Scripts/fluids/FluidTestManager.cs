using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
#if UNITY_EDITOR
using UnityEditor;
#endif

// in the scene fluidtest
public class FluidTestManager : MonoBehaviour
{
    
    // Start is called before the first frame update
    public List<FluidTestUnit> pairs;
    public float flow_coef = 1.0f;
    public float pressure_coef = 1.0f;
    public static FluidTestManager self;
    private void Awake()
    {
        self = this;
    }
    void Start()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;

        //for (int i = 0; i < pairs.Count; i += 2)
        //{
        //    var ftu0 = pairs[i];
        //    var ftu1 = pairs[i + 1];
        //    var key = new uint2(ftu0.guid, ftu1.guid);
        //    if (key.x > key.y) key = new uint2(key.y, key.x);
        //    if(FluidUpdateSystem.self.pipe_states.TryGetValue(key, out FluidInventory fi) == false)
        //    {
        //        fi.max_volume = 100;
        //        FluidUpdateSystem.self.pipe_states.Add(key, fi);
        //    }

        //    var fpt_db = em.GetBuffer<FluidPipeTarget>(ftu0.target);
        //    fpt_db.Add(new FluidPipeTarget() { value = key });

        //    fpt_db = em.GetBuffer<FluidPipeTarget>(ftu1.target);
        //    fpt_db.Add(new FluidPipeTarget() { value = key });
        //    var pipego = GameObject.Instantiate(pairs[0].gameObject, transform);
        //    var ftu = pipego.GetComponent<FluidTestUnit>();
        //    ftu.key = key;
        //    ftu.target = default;
        //    ftu.view = new FluidInventory() { max_volume= 100 };
        //    //var rt0 = ftu0.GetComponent<RectTransform>();
        //    //var rt1 = ftu1.GetComponent<RectTransform>();
        //    //var rt = pipego.GetComponent<RectTransform>();
        //    pipego.transform.localPosition = (ftu0.transform.localPosition + ftu1.transform.localPosition) / 2f;
        //    pipego.name = "pipe";
        //}
    }
    public bool tick;
#if UNITY_EDITOR
    public bool connect;
    private void OnDrawGizmos()
    {
        if(pairs != null)
        {
            for(int i = 0; i < pairs.Count; i += 2)
            {
                Gizmos.DrawLine(pairs[i].transform.position, pairs[i + 1].transform.position);
            }
        }
        if (connect)
        {
            connect = false;
            if (Selection.gameObjects.Length == 2)
            {
                var ftu0 = Selection.gameObjects[0].GetComponent<FluidTestUnit>();
                var ftu1 = Selection.gameObjects[1].GetComponent<FluidTestUnit>();

                var ftm = this;
                for (int i = 0; i < ftm.pairs.Count; i += 2)
                {
                    if (ftm.pairs[i].guid == ftu0.guid && ftm.pairs[i + 1].guid == ftu1.guid)
                    {
                        ftm.pairs.RemoveAt(i);
                        ftm.pairs.RemoveAt(i);
                        return;
                    }
                    if (ftm.pairs[i].guid == ftu1.guid && ftm.pairs[i + 1].guid == ftu0.guid)
                    {
                        ftm.pairs.RemoveAt(i);
                        ftm.pairs.RemoveAt(i);
                        return;
                    }
                }

                ftm.pairs.Add(ftu0);
                ftm.pairs.Add(ftu1);
            }
        }
    }
#endif
    float elapsed;
    public float update_interval = 0.016f;
    // Update is called once per frame
    void Update()
    {
        elapsed += Time.deltaTime;
        //if(elapsed > update_interval)
        //{
        //    FluidUpdateSystem.self.update();
        //    elapsed -= update_interval;
        //}
        //if(tick)
        //{
        //    tick = false;
        //    FluidUpdateSystem.self.update();
        //}
        
    }
}
