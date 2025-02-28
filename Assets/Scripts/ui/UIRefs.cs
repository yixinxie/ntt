using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIRefs : MonoBehaviour
{
    public static UIRefs self;
    public WeaponPanel weapon;
    public ShortcutsPanel shortcuts;
    public InventoryPanel inventory;
    void Awake() 
    { 
        self = this; 
    }
}
