using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

public class InventoryPanel : MonoBehaviour
{
    public ItemButton[] buttons;
    // Start is called before the first frame update
    void Start()
    {
        var first_one = buttons[0];
        for (int i = 1; i < buttons.Length; ++i)
        {
            buttons[i] = GameObject.Instantiate(first_one.gameObject, first_one.transform.parent).GetComponent<ItemButton>();
        }
    }
    // Update is called once per frame
    void Update()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        if(ControlBase.self != null && em.HasBuffer<RouterInventory>(ControlBase.self.target_entity))
        {
            var ri_na = em.GetBuffer<RouterInventory>(ControlBase.self.target_entity).ToNativeArray(Allocator.Temp);
            for(int i = 0; i < ri_na.Length; ++i)
            {
                var ri = ri_na[i];
                if (i < buttons.Length)
                {
                    buttons[i].set_as_itemtype((ItemType)ri.item_type);
                }
            }

        }
    }
}
