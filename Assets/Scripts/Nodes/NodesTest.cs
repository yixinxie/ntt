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
    //public NodesTest[] outputs;
    public int outputcount;
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
            unsafe
            {
                NodeGraph.eval(temp_buffer, sizeof(NodeValue4));
            }
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
    //static void getbyoptype<T>(NodeOpTypes optype, out T out_val) where T : unmanaged, IRTNode
    //{
    //    var input_count = NodeGraph.optype2inputcount[(ushort)optype];

    //    if (input_count == 1)
    //    {
    //        out_val = (T)new RTNode1();
    //        //return new RTNode1();
    //    }
    //    else if (input_count == 2)
    //    {
    //        return new RTNode2(); 
    //    }
    //    else if (input_count == 3)
    //    {
    //        return new RTNode3();
    //    }
    //    else if (input_count == 4)
    //    {
    //        return new RTNode4();
    //    }
    //    return new RTNode1();
    //}
    // depth first
    unsafe int serialize_recursive(NativeList<byte> temp_buffer, NodesTest node_states, int output_offset)
    {
        var input_count = NodeGraph.optype2inputcount[(ushort)node_states.node_type];
        RTNode1 n1 = default;
        RTNode2 n2 = default;
        RTNode3 n3 = default;
        RTNode4 n4 = default;
        int* parent_indices = null;
        int* input_offsets = null;
        int* output_offsets = null;
        bool create_new = (node_states.allocated_index == -1);
        if (create_new)
        {
            Bursted.ns_generic(temp_buffer, node_states.node_type);
            node_states.allocated_index = temp_buffer.Length;
            
            if (input_count == 1)
            {
                n1.init();
                Bursted.ns_generic(temp_buffer, n1);
                parent_indices = n1.parent_indices;
                input_offsets = n1.input_offsets;
                output_offsets = n1.output_offsets;
            }
            else if (input_count == 2)
            {
                n2.init();
                Bursted.ns_generic(temp_buffer, n2);
                parent_indices = n2.parent_indices;
                input_offsets = n2.input_offsets;
                output_offsets = n2.output_offsets;
            }
            else if (input_count == 3)
            {
                n3.init();
                Bursted.ns_generic(temp_buffer, n3);
                parent_indices = n3.parent_indices;
                input_offsets = n3.input_offsets;
                output_offsets = n3.output_offsets;
            }
            else if (input_count == 4)
            {
                n4.init();
                Bursted.ns_generic(temp_buffer, n4);
                parent_indices = n4.parent_indices;
                input_offsets = n4.input_offsets;
                output_offsets = n4.output_offsets;
            }


            // operation specific
            switch(node_states.node_type)
            {
                case NodeOpTypes.Constant:
                    {
                        input_offsets[0] = temp_buffer.Length;
                        var val0 = new NodeValue4();
                        val0 = node_states.val4;
                        Bursted.ns_generic(temp_buffer, val0);
                    }
                    break;
                case NodeOpTypes.Addition:
                    {
                        input_offsets[0] = temp_buffer.Length;
                        Bursted.ns_generic(temp_buffer, new NodeValue4());
                        input_offsets[1] = temp_buffer.Length;
                        Bursted.ns_generic(temp_buffer, new NodeValue4());
                    }
                    break;
            }
            if (input_count == 1)
            {
                Bursted.us_struct_offset(temp_buffer, n1, node_states.allocated_index);
            }
            else if (input_count == 2)
            {
                Bursted.us_struct_offset(temp_buffer, n2, node_states.allocated_index);
            }
            else if (input_count == 3)
            {
                Bursted.us_struct_offset(temp_buffer, n3, node_states.allocated_index);
            }
            else if (input_count == 4)
            {
                Bursted.us_struct_offset(temp_buffer, n4, node_states.allocated_index);
            }
        }
        else
        {
            int offset = node_states.allocated_index;
            if (input_count == 1)
            {
                Bursted.nd_generic(temp_buffer, out n1, ref offset, Allocator.Temp);
                parent_indices = n1.parent_indices;
                input_offsets = n1.input_offsets;
                output_offsets = n1.output_offsets;
            }
            else if (input_count == 2)
            {
                Bursted.nd_generic(temp_buffer, out n2, ref offset, Allocator.Temp);
                parent_indices = n2.parent_indices;
                input_offsets = n2.input_offsets;
                output_offsets = n2.output_offsets;
            }
            else if (input_count == 3)
            {
                Bursted.nd_generic(temp_buffer, out n3, ref offset, Allocator.Temp);
                parent_indices = n3.parent_indices;
                input_offsets = n3.input_offsets;
                output_offsets = n3.output_offsets;
            }
            else if (input_count == 4)
            {
                Bursted.nd_generic(temp_buffer, out n4, ref offset, Allocator.Temp);
                parent_indices = n4.parent_indices;
                input_offsets = n4.input_offsets;
                output_offsets = n4.output_offsets;
            }
        }


        // end of op-specific
        for (int i = 0; i < RTNode8.MaxOutputs; i++)
        {
            if (output_offsets[i] == -1)
            {
                output_offsets[i] = output_offset;
                break;
            }
        }

        if (create_new)
        {
            for (int i = 0; i < node_states.input_refs.Length; ++i)
            {
                parent_indices[i] = serialize_recursive(temp_buffer, node_states.input_refs[i], input_offsets[i]);
            }
        }
        if (input_count == 1)
        {
            Bursted.us_struct_offset(temp_buffer, n1, node_states.allocated_index);
        }
        else if (input_count == 2)
        {
            Bursted.us_struct_offset(temp_buffer, n2, node_states.allocated_index);
        }
        else if (input_count == 3)
        {
            Bursted.us_struct_offset(temp_buffer, n3, node_states.allocated_index);
        }
        else if (input_count == 4)
        {
            Bursted.us_struct_offset(temp_buffer, n4, node_states.allocated_index);
        }
        return node_states.allocated_index - 2;
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
    public static readonly int[] optype2inputcount = new int[] { 0, 1, 2, 2, 1, 2, 0};
    unsafe public static void eval(NativeList<byte> buffer, int index)
    {
        int offset = index;
        Bursted.nd_generic(buffer, out NodeOpTypes optype, ref offset, Allocator.Temp);
        var input_count = optype2inputcount[(ushort)optype];
        int* parent_indices = null;
        int* input_offsets = null;
        int* output_offsets = null;
        if (input_count == 1)
        {
            Bursted.nd_generic(buffer, out RTNode1 current, ref offset, Allocator.Temp);
            parent_indices = current.parent_indices;
            input_offsets = current.input_offsets;
            output_offsets = current.output_offsets;
        }
        else if (input_count == 2)
        {
            Bursted.nd_generic(buffer, out RTNode2 current, ref offset, Allocator.Temp);
            parent_indices = current.parent_indices;
            input_offsets = current.input_offsets;
            output_offsets = current.output_offsets;
        }
        else if (input_count == 3)
        {
            Bursted.nd_generic(buffer, out RTNode3 current, ref offset, Allocator.Temp);
            parent_indices = current.parent_indices;
            input_offsets = current.input_offsets;
            output_offsets = current.output_offsets;
        }
        else if (input_count == 4)
        {
            Bursted.nd_generic(buffer, out RTNode4 current, ref offset, Allocator.Temp);
            parent_indices = current.parent_indices;
            input_offsets = current.input_offsets;
            output_offsets = current.output_offsets;
        }
        if (parent_indices != null)
        {
            for (int i = 0; i < input_count; ++i)
            {
                var idx = parent_indices[i];
                if (idx >= 0)
                {
                    eval(buffer, idx);
                }
            }
        }
        switch (optype)
        {
            case NodeOpTypes.Addition:
                {
                    offset = input_offsets[0];
                    Bursted.ud_struct(buffer, out NodeValue4 left, ref offset);
                    offset = input_offsets[1];
                    Bursted.ud_struct(buffer, out NodeValue4 right, ref offset);
                    RTNode8.float_add(left, right, out var output);
                    for (int i = 0; i < RTNode8.MaxOutputs; ++i)
                    {
                        var idx = output_offsets[i];
                        if (idx < 0)
                            break;
                        Bursted.us_struct_offset(buffer, output, idx);
                    }
                }

                break;
            case NodeOpTypes.Constant:
                {
                    offset = input_offsets[0];
                    Bursted.ud_struct(buffer, out NodeValue4 left, ref offset);

                    for (int i = 0; i < RTNode8.MaxOutputs; ++i)
                    {
                        var idx = output_offsets[i];
                        if (idx < 0)
                            break;
                        Bursted.us_struct_offset(buffer, left, idx);
                    }
                }
                break;

        }
    }
}
public interface IRTNode
{
    public int get_input_count();
    unsafe public int* get_parent_indices();
    unsafe public int* get_input_offsets();
    unsafe public int* get_output_offsets();
}
unsafe public struct RTNode8 : IRTNode
{
    public const int MaxInputs = 8;
    public const int MaxOutputs = 4;
    public fixed int parent_indices[MaxInputs];
    public fixed int input_offsets[MaxInputs];
    public fixed int output_offsets[MaxOutputs];

    public int get_input_count()
    {
        return MaxInputs;
    }
    unsafe public int* get_parent_indices()
    {
        fixed(int* p = parent_indices)
        {
            return p;
        }
    }
    unsafe public int* get_input_offsets()
    {
        fixed (int* p = input_offsets)
        {
            return p;
        }
    }
    unsafe public int* get_output_offsets()
    {
        fixed (int* p = output_offsets)
        {
            return p;
        }
    }
    //public NodeOpTypes optype;
    public void init()
    {
        //optype = 0;
        for (int i = 0; i < MaxInputs; ++i)
        {
            parent_indices[i] = -1;
            input_offsets[i] = -1;
            output_offsets[i] = -1;
        }
    }

    public static void float_add(NodeValue4 left, NodeValue4 right, out NodeValue4 output)
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
unsafe public struct RTNode4 :IRTNode
{
    public const int MaxInputs = 4;
    public const int MaxOutputs = 4;
    public fixed int parent_indices[MaxInputs];
    public fixed int input_offsets[MaxInputs];
    public fixed int output_offsets[MaxOutputs];
    public int get_input_count()
    {
        return MaxInputs;
    }
    unsafe public int* get_parent_indices()
    {
        fixed (int* p = parent_indices)
        {
            return p;
        }
    }
    unsafe public int* get_input_offsets()
    {
        fixed (int* p = input_offsets)
        {
            return p;
        }
    }
    unsafe public int* get_output_offsets()
    {
        fixed (int* p = output_offsets)
        {
            return p;
        }
    }
    public void init()
    {
        //optype = 0;
        for (int i = 0; i < MaxInputs; ++i)
        {
            parent_indices[i] = -1;
            input_offsets[i] = -1;
            output_offsets[i] = -1;
        }
    }
}
unsafe public struct RTNode3 : IRTNode
{
    public const int MaxInputs = 3;
    public const int MaxOutputs = 4;
    public fixed int parent_indices[MaxInputs];
    public fixed int input_offsets[MaxInputs];
    public fixed int output_offsets[MaxOutputs];
    public int get_input_count()
    {
        return MaxInputs;
    }
    unsafe public int* get_parent_indices()
    {
        fixed (int* p = parent_indices)
        {
            return p;
        }
    }
    unsafe public int* get_input_offsets()
    {
        fixed (int* p = input_offsets)
        {
            return p;
        }
    }
    unsafe public int* get_output_offsets()
    {
        fixed (int* p = output_offsets)
        {
            return p;
        }
    }
    public void init()
    {
        //optype = 0;
        for (int i = 0; i < MaxInputs; ++i)
        {
            parent_indices[i] = -1;
            input_offsets[i] = -1;
            output_offsets[i] = -1;
        }
    }
}
unsafe public struct RTNode2 : IRTNode
{
    public const int MaxInputs = 2;
    public const int MaxOutputs = 4;
    public fixed int parent_indices[MaxInputs];
    public fixed int input_offsets[MaxInputs];
    public fixed int output_offsets[MaxOutputs];
    public int get_input_count()
    {
        return MaxInputs;
    }
    unsafe public int* get_parent_indices()
    {
        fixed (int* p = parent_indices)
        {
            return p;
        }
    }
    unsafe public int* get_input_offsets()
    {
        fixed (int* p = input_offsets)
        {
            return p;
        }
    }
    unsafe public int* get_output_offsets()
    {
        fixed (int* p = output_offsets)
        {
            return p;
        }
    }
    public void init()
    {
        //optype = 0;
        for (int i = 0; i < MaxInputs; ++i)
        {
            parent_indices[i] = -1;
            input_offsets[i] = -1;
            output_offsets[i] = -1;
        }
    }
}
unsafe public struct RTNode1:IRTNode
{
    public const int MaxInputs = 1;
    public const int MaxOutputs = 4;
    public fixed int parent_indices[MaxInputs];
    public fixed int input_offsets[MaxInputs];
    public fixed int output_offsets[MaxOutputs];
    public int get_input_count()
    {
        return MaxInputs;
    }
    unsafe public int* get_parent_indices()
    {
        fixed (int* p = parent_indices)
        {
            return p;
        }
    }
    unsafe public int* get_input_offsets()
    {
        fixed (int* p = input_offsets)
        {
            return p;
        }
    }
    unsafe public int* get_output_offsets()
    {
        fixed (int* p = output_offsets)
        {
            return p;
        }
    }
    public void init()
    {
        //optype = 0;
        for (int i = 0; i < MaxInputs; ++i)
        {
            parent_indices[i] = -1;
            input_offsets[i] = -1;
            output_offsets[i] = -1;
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