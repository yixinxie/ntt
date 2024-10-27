unsafe public struct RTNode8 : IRTNode
{
    public const int MaxInputs = 8;
    public fixed int parent_indices[MaxInputs];
    public fixed int input_offsets[MaxInputs];
    public fixed int output_offsets[RTNode4.MaxOutputs];

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
            output_offsets[i] = input_offsets[i] = parent_indices[i] = 0;
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
public interface IRTNode
{
    public int get_input_count();
    unsafe public int* get_parent_indices();
    unsafe public int* get_input_offsets();
    unsafe public int* get_output_offsets();
}
unsafe public struct RTNode4 : IRTNode
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
            output_offsets[i] = input_offsets[i] = parent_indices[i] = 0;
        }
    }
}
unsafe public struct RTNode3 : IRTNode
{
    public const int MaxInputs = 3;
    public fixed int parent_indices[MaxInputs];
    public fixed int input_offsets[MaxInputs];
    public fixed int output_offsets[RTNode4.MaxOutputs];
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
            output_offsets[i] = input_offsets[i] = parent_indices[i] = 0;
        }
    }
}
unsafe public struct RTNode2 : IRTNode
{
    public const int MaxInputs = 2;
    public fixed int parent_indices[MaxInputs];
    public fixed int input_offsets[MaxInputs];
    public fixed int output_offsets[RTNode4.MaxOutputs];
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
            output_offsets[i] = input_offsets[i] = parent_indices[i] = 0;
        }
    }
}
unsafe public struct RTNode1 : IRTNode
{
    public const int MaxInputs = 1;
    public fixed int parent_indices[MaxInputs];
    public fixed int input_offsets[MaxInputs];
    public fixed int output_offsets[RTNode4.MaxOutputs];
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
            output_offsets[i] = input_offsets[i] = parent_indices[i] = 0;
        }
    }
}
