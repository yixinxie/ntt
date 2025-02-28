using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class WeaponPanel : MonoBehaviour
{
    public TextMeshProUGUI text0;
    StringBuilder textsb = new StringBuilder();
    // Start is called before the first frame update
    void Start()
    {
        
    }
    // Update is called once per frame
    void Update()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        if(ControlBase.self != null && em.HasBuffer<WeaponInfoV2>(ControlBase.self.target_entity))
        {
            var weapons = em.GetBuffer<WeaponInfoV2>(ControlBase.self.target_entity).ToNativeArray(Allocator.Temp);
            textsb.Clear();
            for (int i = 0; i < weapons.Length; i++)
            {
                textsb.AppendFormat("{0} {1}", weapons[i].weapon_type.ToString(), weapons[i].ammo_left);
                textsb.AppendLine();

            }
            text0.text = textsb.ToString();
        }
    }
}
