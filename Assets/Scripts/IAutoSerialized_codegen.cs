﻿

using Unity.Collections;

using Unity.Networking.Transport;

public partial struct cmpt : IAutoSerialized // auto-generated

{

    public const int type_hash = 258093858;

    public void unpack(NativeList<byte> buffer, ref int offset, Allocator alloc)

    {

        Bursted.ud_struct_partial(buffer, ref this, 8, ref offset);

        Bursted.ud_na(buffer, out na, ref offset, alloc);

        Bursted.ud_na(buffer, out nl_floats, ref offset, alloc);

    }

}

public partial struct cmpt0 : IAutoSerialized // auto-generated

{

    public const int type_hash = 445649330;

    public void unpack(NativeList<byte> buffer, ref int offset, Allocator alloc)

    {

        Bursted.ud_struct_partial(buffer, ref this, 0, ref offset);

        Bursted.ud_na(buffer, out na, ref offset, alloc);

        Bursted.ud_na(buffer, out nl_floats, ref offset, alloc);

    }

}

public partial struct cmpt2 : IAutoSerialized // auto-generated

{

    public const int type_hash = 445649328;

    public void unpack(NativeList<byte> buffer, ref int offset, Allocator alloc)

    {

        Bursted.ud_struct_partial(buffer, ref this, 5, ref offset);

        Bursted.ud_na(buffer, out na, ref offset, alloc);

    }

}



public partial struct cmpt : IAutoSerialized // auto-generated

{

    public NativeList<byte> pack(Allocator alloc)

    {

        NativeList<byte> buffer = new NativeList<byte>(32, alloc);

        Bursted.us_struct(buffer, type_hash);

        Bursted.us_struct_partial(buffer, ref this, 8);

        Bursted.us_na(buffer, na);

        Bursted.us_na(buffer, nl_floats);



        return buffer;

    }

}

public partial struct cmpt0 : IAutoSerialized // auto-generated

{

    public NativeList<byte> pack(Allocator alloc)

    {

        NativeList<byte> buffer = new NativeList<byte>(32, alloc);

        Bursted.us_struct(buffer, type_hash);

        Bursted.us_struct_partial(buffer, ref this, 0);

        Bursted.us_na(buffer, na);

        Bursted.us_na(buffer, nl_floats);



        return buffer;

    }

}

public partial struct cmpt2 : IAutoSerialized // auto-generated

{

    public NativeList<byte> pack(Allocator alloc)

    {

        NativeList<byte> buffer = new NativeList<byte>(32, alloc);

        Bursted.us_struct(buffer, type_hash);

        Bursted.us_struct_partial(buffer, ref this, 5);

        Bursted.us_na(buffer, na);



        return buffer;

    }

}



public partial class BNH // auto-generated

{

    public static void rpc_switch(int type_hash, ref int offset, NativeList<byte> buffer, NetworkConnection sender, NetworkDriver m_Driver, NetworkPipeline pl)

    {

        switch (type_hash)

        {

            case cmpt.type_hash:

                {

                    cmpt _data = default;

                    _data.unpack(buffer, ref offset, Allocator.Temp);

                    _data.callback(m_Driver, sender, pl);

                }

                break;

            case cmpt0.type_hash:

                {

                    cmpt0 _data = default;

                    _data.unpack(buffer, ref offset, Allocator.Temp);

                    _data.callback(m_Driver, sender, pl);

                }

                break;

            case cmpt2.type_hash:

                {

                    cmpt2 _data = default;

                    _data.unpack(buffer, ref offset, Allocator.Temp);

                    _data.callback(m_Driver, sender, pl);

                }

                break;

        }

    }

}

