using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

public class ShortcutsPanel : MonoBehaviour
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
        if(ControlBase.self != null && em.HasComponent<BuilderShortcuts>(ControlBase.self.target_entity))
        {
            var bshortcuts = em.GetComponentData<BuilderShortcuts>(ControlBase.self.target_entity);
            for(int i = 0; i < BuilderShortcuts.column_count; ++i)
            {
                var itype = bshortcuts.get_item(i);
                buttons[i].set_as_itemtype(itype);
            }

        }
    }
}
