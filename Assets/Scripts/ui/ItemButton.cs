using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class ItemButton : MonoBehaviour
{
    public TextMeshProUGUI cornertext;
    public Image main;
    public void set_text(string text)
    {
        cornertext.text = text;
    }
    public void set_as_itemtype(ItemType idx)
    {
        var sprite = ImageRefs.self.GetItemIcon((int)idx);
        main.sprite = sprite;
    }
}
