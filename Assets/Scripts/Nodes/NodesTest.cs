using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

public class NodesTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
public enum NodeOpTypes:ushort
{
    None = 0,
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
    Int,
    Float,
    Float2,
    Float3,
    Total
}
public struct NodeGraph
{
    unsafe public void eval(NativeList<RTNode> nodes, NativeList<byte> buffer, int index)
    {
        var current = nodes[index];
        for (int i = 0; i < RTNode.Max; ++i)
        {
            var idx = current.parent_indices[i];
            if (idx >= 0)
            {
                eval(nodes, buffer, idx);
            }
        }
        int offset = 0;
        switch (current.optype)
        {
            case NodeOpTypes.Addition:
                offset = current.input_offsets[0];
                Bursted.ud_struct(buffer, out NodeParam4 left, ref offset);
                offset = current.input_offsets[1];
                Bursted.ud_struct(buffer, out NodeParam4 right, ref offset);
                RTNode.float_add(buffer, left, right, out var output);
                for (int i = 0; i < RTNode.Max; ++i)
                {
                    var idx = current.output_offsets[i];
                    Bursted.us_struct(buffer, output);
                }
                
                break;
        }
    }
}
unsafe public struct RTNode
{
    public const int Max = 8;
    public fixed int parent_indices[8];
    public fixed int input_offsets[8];
    public fixed int output_offsets[8];
    public NodeOpTypes optype;

    public static void float_add(NativeList<byte> buffer, NodeParam4 left, NodeParam4 right, out NodeParam4 output)
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

[StructLayout(LayoutKind.Explicit)]
public struct NodeParam4
{
    [FieldOffset(0)]
    public NodeVarTypes var_type;
    [FieldOffset(4)]
    public float val_float;
    [FieldOffset(4)]
    public int val_int;
}