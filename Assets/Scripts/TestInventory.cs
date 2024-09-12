using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestInventory : MonoBehaviour
{
    [SerializeField]
    public RouterInventory ri;
    public int cached_ri_count;
    public int cached_ri_block_count;
    public int cached_ri_last_count;
    public int adjust;
    // Start is called before the first frame update
    void Start()
    {
        
    }
    private void OnDrawGizmos()
    {
        if(adjust != 0)
        {
            ri.item_count = (ushort)(ri.item_count + adjust);
            adjust = 0;
        }
        cached_ri_count = ri.item_count;
        cached_ri_block_count = ri.item_blocks;
        cached_ri_last_count = ri.last_count;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
