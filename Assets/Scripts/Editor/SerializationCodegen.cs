using Codice.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities.UniversalDelegates;
using Unity.Networking.Transport;
using UnityEditor;
using UnityEngine;

public class SerializationCodegen : MonoBehaviour
{
    //[MenuItem("Codegen/Serialization")]
    //public static void codgen()
    //{
    //    _GenerateNetworkingCode("", typeof(IAutoSerialized), "");

    //}
    //public static void _GenerateNetworkingCode(string generatedScriptPath, Type baseType, string template)
    //{
    //    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
    //    List<Type> types = new List<Type>();
    //    for (int i = 0; i < assemblies.Length; ++i)
    //    {
    //        Type[] assemblyTypes = assemblies[i].GetTypes();
    //        for (int j = 0; j < assemblyTypes.Length; ++j)
    //        {
    //            if (assemblyTypes[j].IsInterface) continue;
    //            if (baseType.IsAssignableFrom(assemblyTypes[j]))
    //            {
    //                types.Add(assemblyTypes[j]);
    //            }
    //        }
    //    }
    //    types.Sort((x, y) => x.Name.CompareTo(y.Name));
    //    StringBuilder stringBuilder = new StringBuilder();
    //    Debug.Log(types.Count + " found");
    //    for (int i = 0; i < types.Count; ++i)
    //    {
    //        // one class.
    //        Type thisType = types[i];
    //        string fullClassName = thisType.ToString(); // with namespace
    //        string className = fullClassName;
    //        Debug.Log(thisType.Name + " generated");
    //        generateCSCode(types[i], stringBuilder);

    //    }
    //    Debug.Log(stringBuilder);

    //    AssetDatabase.Refresh();
    //}
    //static void generateCSCode(Type type, StringBuilder stringBuilder)
    //{
    //    const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public |
    //        BindingFlags.Instance;

    //    FieldInfo[] fields = type.GetFields(flags);
    //    // reorder
    //    int accumulated_size = 0;
    //    int collection_start = 0;
    //    for (int i = 0; i < fields.Length; ++i)
    //    {
    //        FieldInfo fieldInfo = fields[i];
    //        int field_size = Marshal.SizeOf(fieldInfo.FieldType);
            
    //        var ttypes = fieldInfo.FieldType.GetGenericArguments();
    //        if (ttypes.Length > 0)
    //        {
    //            collection_start = i;
    //            break;
    //        }
    //        accumulated_size += field_size;

    //    }
    //    stringBuilder.AppendFormat("Bursted.us_struct_partial(buffer, val, {0});", accumulated_size);
    //    stringBuilder.Append(Environment.NewLine);

    //    for (int i = collection_start; i < fields.Length; ++i)
    //    {
    //        FieldInfo fieldInfo = fields[i];
    //        int field_size = Marshal.SizeOf(fieldInfo.FieldType);
    //        accumulated_size += field_size;
    //        var ttypes = fieldInfo.FieldType.GetGenericArguments();
    //        if (ttypes.Length > 0)
    //        {
    //            stringBuilder.AppendFormat("Bursted.us_na(raw, {0});", fieldInfo.Name);
    //        }
    //        else
    //        {
    //            stringBuilder.AppendFormat("Debug.LogWarning(\"not right!\"):" + fieldInfo.Name);
    //            //break;
    //        }
    //        stringBuilder.Append(Environment.NewLine);
    //    }
    //}

    [MenuItem("Codegen/test")]
    public static void codgen2()
    {
        generateCSCode2();

    }
    static Dictionary<string, string> type2three(object o)
    {
        if (o == null) return new Dictionary<string, string>();
        Type type = (Type)o;
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public |
            BindingFlags.Instance;

        FieldInfo[] fields = type.GetFields(flags);
        // reorder
        int accumulated_size = 0;
        //int collection_start = 0;
        for (int i = 0; i < fields.Length; ++i)
        {
            FieldInfo fieldInfo = fields[i];
            int field_size = Marshal.SizeOf(fieldInfo.FieldType);

            var ttypes = fieldInfo.FieldType.GetGenericArguments();
            if (ttypes.Length > 0)
            {
                //collection_start = i;
                break;
            }
            accumulated_size += field_size;

        }
        var ret = new Dictionary<string, string>();
        ret.Add("%name%", type.Name);
        ret.Add("%hash%", type.Name.GetHashCode().ToString());
        ret.Add("%offset%", accumulated_size.ToString());
        return ret;
    }
    
    static Dictionary<string, string> type2name(object o)
    {
        Type type = (Type)o;
        //Debug.Log(type.Name);
        Dictionary<string, string> ret = new Dictionary<string, string>();

        ret.Add("%mbr%", type.Name);

        return ret;
    }

    static Dictionary<string, string> name2name(object o)
    {
        Dictionary<string, string> ret = new Dictionary<string, string>();

        ret.Add("%mbr%", (string)o);

        return ret;
    }
    static List<object> get_all_ias_types(object key)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var baseType = typeof(IAutoSerialized);
        List<object> types = new List<object>();
        for (int i = 0; i < assemblies.Length; ++i)
        {
            Type[] assemblyTypes = assemblies[i].GetTypes();
            for (int j = 0; j < assemblyTypes.Length; ++j)
            {
                if (assemblyTypes[j].IsInterface) continue;
                if (baseType.IsAssignableFrom(assemblyTypes[j]))
                {
                    types.Add(assemblyTypes[j]);
                }
            }
        }
        types.Sort((x, y) => ((Type)x).Name.CompareTo(((Type)y).Name));
        return types;
    }
    static List<object> controllers1(object key)
    {
        List<object> types = new List<object>();
        types.Add(key);
        return types;
    }
    static List<object> get_ias_collections(object key)
    {
        Type type = (Type)key;
        List<object> types = new List<object>();

        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public |
        BindingFlags.Instance;

        FieldInfo[] fields = type.GetFields(flags);
        // reorder
        int accumulated_size = 0;
        int collection_start = 0;
        for (int i = 0; i < fields.Length; ++i)
        {
            FieldInfo fieldInfo = fields[i];
            int field_size = Marshal.SizeOf(fieldInfo.FieldType);

            var ttypes = fieldInfo.FieldType.GetGenericArguments();
            if (ttypes.Length > 0)
            {
                collection_start = i;
                break;
            }
            accumulated_size += field_size;

        }


        for (int i = collection_start; i < fields.Length; ++i)
        {
            FieldInfo fieldInfo = fields[i];
            //int field_size = Marshal.SizeOf(fieldInfo.FieldType);
            var ttypes = fieldInfo.FieldType.GetGenericArguments();
            if (ttypes.Length > 0)
            {
                types.Add(fieldInfo.Name);
                //stringBuilder.AppendFormat("Bursted.us_na(raw, {0});", fieldInfo.Name);
            }
            else
            {
                types.Add("incorrect");
                //stringBuilder.AppendFormat("Debug.LogWarning(\"not right!\"):" + fieldInfo.Name);
                //break;
            }

        }
        return types;
    }
    static void generateCSCode2()
    {
        string s0 = "";
        string[] lines;
        lines = t_super.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        s0 += for_each(lines.ToList(), 0, null,
            (int tmp, object key) =>
            {
                if (tmp == 0)
                    return get_all_ias_types(key);
                else if (tmp == 1)
                    return get_ias_collections(key);
                //else if (tmp == 2)
                //    return controllers2(key);
                return null;
            },

            (object o, int idx) =>
            {
                if (idx == 1)
                    return type2three(o);
                else if (idx == 2)
                    return name2name(o);
                //else if (idx == 3)
                //    return get_dict3(o);
                return new Dictionary<string, string>();
            }
        );
        lines = t_super2.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        s0 += for_each(lines.ToList(), 0, null,
            (int tmp, object key) =>
            {
                if (tmp == 0)
                    return get_all_ias_types(key);
                else if (tmp == 1)
                    return get_ias_collections(key);
                //else if (tmp == 2)
                //    return controllers2(key);
                return null;
            },

            (object o, int idx) =>
            {
                if (idx == 1)
                    return type2three(o);
                else if (idx == 2)
                    return name2name(o);
                return new Dictionary<string, string>(0);
            }
        );


        lines = template_switch.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        s0 += for_each(lines.ToList(), 0, null,
            (int tmp, object key) =>
            {
                if (tmp == 0)
                    return get_all_ias_types(key);
                else if (tmp == 1)
                    return controllers1(key);
                //else if (tmp == 2)
                //    return controllers2(key);
                return null;
            },

            (object o, int idx) =>
            {
                if (idx == 1)
                    return type2three(o);
                else if (idx == 2)
                    return type2name(o);
                //else if (idx == 3)
                //    return get_dict3(o);
                return new Dictionary<string, string>(0);
            }
        );
        Debug.Log(s0);
    }

    static string for_each(List<string> strs, int ptr, object key, Func<int, object, List<object>> controllers, Func<object, int, Dictionary<string, string>> funcs)
    {
        string ret = "";
        //if (ptr >= controllers.Length) return ret;
        
        //Debug.Log(listobj.Count);
        
        {
            var dict = funcs(key, ptr);
            for (int i = 0; i < strs.Count; ++i)
            {
                var cur = strs[i];
                if (cur.TrimStart(new char[] { '\t', ' '}).StartsWith("%for"))
                {
                    List<string> next = new List<string>();
                    int end_idx = strs.Count - 1;
                    for (; end_idx >= i + 1; --end_idx)
                    {
                        if (strs[end_idx].TrimStart('\t', ' ').StartsWith("%end")) break;
                    }
                    for (int j = i + 1; j < end_idx; ++j)
                    {
                        next.Add(strs[j]);
                    }
                    var listobj = controllers(ptr, key);
                    if (listobj == null)
                    {
                        return ret;
                    }
                    for (int k = 0; k < listobj.Count; ++k)
                    {
                        ret += for_each(next, ptr + 1, listobj[k], controllers, funcs);
                    }
                    i = end_idx;
                }
                else
                {
                    foreach (var kvp in dict)
                    {
                        cur = cur.Replace(kvp.Key, kvp.Value);
                    }
                    ret += cur + Environment.NewLine;
                }
            }
        }
        return ret;
    }



    static string t_super = @"
using Unity.Collections;
using Unity.Networking.Transport;
%for%
public partial struct %name% : IAutoSerialized // auto-generated
{
    public const int type_hash = %hash%;
    public void unpack(NativeList<byte> buffer, ref int offset, Allocator alloc)
    {
        Bursted.ud_struct_partial(buffer, ref this, %offset%, ref offset);
		%for%
        Bursted.ud_na(buffer, out %mbr%, ref offset, alloc);
		%end%
    }
}
%end%";

    static string t_super2 = @"
%for%
public partial struct %name% : IAutoSerialized // auto-generated
{
    public NativeList<byte> pack(Allocator alloc)
    {
        NativeList<byte> buffer = new NativeList<byte>(32, alloc);
        Bursted.us_struct(buffer, type_hash);
        Bursted.us_struct_partial(buffer, ref this, %offset%);
%for%
        Bursted.us_na(buffer, %mbr%);
%end%

        return buffer;
    }
}
%end%";


    static string template_switch = @"
public partial class BNH // auto-generated
{
    public static void rpc_switch(int type_hash, ref int offset, NativeList<byte> buffer, NetworkConnection sender, NetworkDriver m_Driver, NetworkPipeline pl)
    {
        switch (type_hash)
        {
%for%
            case %name%.type_hash:
                {
                    %name% _data = default;
                    _data.unpack(buffer, ref offset, Allocator.Temp);
                    _data.callback(m_Driver, sender, pl);
                }
                break;
%end%
        }
    }
}";
}
