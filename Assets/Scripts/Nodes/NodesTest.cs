using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class NodesTest : MonoBehaviour
{
    // Start is called before the first frame update
    public NodeOpTypes node_type;
    public NodesTest[] input_refs;
    public NodesTest[] outputs;
    public int allocated_index;
    public NodeValue4 val4;
    void Start()
    {
        
    }
    public bool serialize;
    private void OnDrawGizmos()
    {

        if (serialize)
        {
            NativeList<byte> temp_buffer = new NativeList<byte>(256, Allocator.Temp);
            serialize = false;
            var visited = new NativeHashSet<int>(8, Allocator.Temp);
            reset_recursive(this, visited);
            Bursted.ns_generic(temp_buffer, new NodeValue4());
            serialize_recursive(temp_buffer, this, 0);
            NodeGraph.eval(temp_buffer, 8);
            int ofs = 0;
            Bursted.nd_generic(temp_buffer, out NodeValue4 testval, ref ofs, Allocator.Temp);
            Debug.Log(testval.val_float + ", " + testval.val_int);

        }
    }
    void reset_recursive(NodesTest current, NativeHashSet<int> visited)
    {
        current.allocated_index = -1;
        for (int i = 0; i < current.input_refs.Length; i++)
        {
            var node = current.input_refs[i];
            if (node != null && visited.Contains(node.gameObject.GetInstanceID()) == false)
            {
                visited.Add(node.gameObject.GetInstanceID());
                reset_recursive(current.input_refs[i], visited);
            }
        }
    }
    // depth first
    unsafe int serialize_recursive(NativeList<byte> temp_buffer, NodesTest node_states, int output_offset)
    {
        RTNode n = default;
        bool create_new = (node_states.allocated_index == -1);
        if (create_new)
        {
            node_states.allocated_index = temp_buffer.Length;
            n.init();
            n.optype = node_states.node_type;
            Bursted.ns_generic(temp_buffer, n);


            // operation specific
            switch(node_states.node_type)
            {
                case NodeOpTypes.Constant:
                    {
                        n.input_offsets[0] = temp_buffer.Length;
                        var val0 = new NodeValue4();
                        val0 = node_states.val4;
                        Bursted.ns_generic(temp_buffer, val0);
                    }
                    break;
                case NodeOpTypes.Addition:
                    {
                        n.input_offsets[0] = temp_buffer.Length;
                        Bursted.ns_generic(temp_buffer, new NodeValue4());
                        n.input_offsets[1] = temp_buffer.Length;
                        Bursted.ns_generic(temp_buffer, new NodeValue4());
                    }
                    break;
            }
            Bursted.us_struct_offset(temp_buffer, n, node_states.allocated_index);
        }
        else
        {
            int offset = node_states.allocated_index;
            Bursted.nd_generic(temp_buffer, out n, ref offset, Allocator.Temp);
        }


        // end of op-specific
        for (int i = 0; i < RTNode.MaxOutputs; i++)
        {
            if (n.output_offsets[i] == -1)
            {
                n.output_offsets[i] = output_offset;
                break;
            }
        }

        if (create_new)
        {
            for (int i = 0; i < node_states.input_refs.Length; ++i)
            {
                n.parent_indices[i] = serialize_recursive(temp_buffer, node_states.input_refs[i], n.input_offsets[i]);
            }
        }
        Bursted.us_struct_offset(temp_buffer, n, node_states.allocated_index);
        return node_states.allocated_index;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
public enum NodeOpTypes:ushort
{
    None = 0,
    Constant,
    Addition,
    Multiplication,
    Cosine,
    Compare,
    Branching,
    Total
}

public enum NodeVarTypes : byte
{
    None = 0,
    Int, // 4 bytes
    Float, // 4 bytes
    Float2, // 8 bytes
    Float3, // 12 bytes
    Float4, // 16 bytes
    Total
}
public struct NodeGraph
{
    unsafe public static void eval(NativeList<byte> buffer, int index)
    {
        int offset = index;
        Bursted.nd_generic(buffer, out RTNode current, ref offset, Allocator.Temp);
        for (int i = 0; i < RTNode.MaxInputs; ++i)
        {
            var idx = current.parent_indices[i];
            if (idx >= 0)
            {
                eval(buffer, idx);
            }
        }
        switch (current.optype)
        {
            case NodeOpTypes.Addition:
                {
                    offset = current.input_offsets[0];
                    Bursted.ud_struct(buffer, out NodeValue4 left, ref offset);
                    offset = current.input_offsets[1];
                    Bursted.ud_struct(buffer, out NodeValue4 right, ref offset);
                    RTNode.float_add(buffer, left, right, out var output);
                    for (int i = 0; i < RTNode.MaxInputs; ++i)
                    {
                        var idx = current.output_offsets[i];
                        if (idx < 0)
                            break;
                        Bursted.us_struct_offset(buffer, output, idx);
                    }
                }

                break;
            case NodeOpTypes.Constant:
                {
                    offset = current.input_offsets[0];
                    Bursted.ud_struct(buffer, out NodeValue4 left, ref offset);

                    for (int i = 0; i < RTNode.MaxInputs; ++i)
                    {
                        var idx = current.output_offsets[i];
                        if (idx < 0)
                            break;
                        Bursted.us_struct_offset(buffer, left, idx);
                    }
                }
                break;

        }
    }
}
unsafe public struct RTNode
{
    public const int MaxInputs = 8;
    public const int MaxOutputs = 4;
    public fixed int parent_indices[MaxInputs];
    public fixed int input_offsets[MaxInputs];
    public fixed int output_offsets[MaxOutputs];
    public NodeOpTypes optype;
    public void init()
    {
        optype = 0;
        for (int i = 0; i < MaxInputs; ++i)
        {
            parent_indices[i] = -1;
            input_offsets[i] = -1;
            output_offsets[i] = -1;
        }
    }

    public static void float_add(NativeList<byte> buffer, NodeValue4 left, NodeValue4 right, out NodeValue4 output)
    {

        output = default;

        if (left.var_type == NodeVarTypes.Float)
        {
            if (right.var_type == NodeVarTypes.Float)
            {
                output.var_type = NodeVarTypes.Float;
                output.val_float = left.val_float + right.val_float;
            }
            else if (right.var_type == NodeVarTypes.Int)
            {
                output.var_type = NodeVarTypes.Float;
                output.val_float = left.val_float + right.val_int;
            }
        }
        else if (left.var_type == NodeVarTypes.Int)
        {
            if (right.var_type == NodeVarTypes.Int)
            {
                output.var_type = NodeVarTypes.Int;
                output.val_int = left.val_int + right.val_int;

            }
            else if (right.var_type == NodeVarTypes.Float)
            {
                output.var_type = NodeVarTypes.Float;
                output.val_float = left.val_int + right.val_float;
            }
        }
                
    }


}

[StructLayout(LayoutKind.Explicit), System.Serializable]
public struct NodeValue4
{
    [FieldOffset(0)]
    public NodeVarTypes var_type;
    [FieldOffset(4)]
    public float val_float;
    [FieldOffset(4)]
    public int val_int;
}