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
    [MenuItem("Codegen/Serialization")]
    public static void codgen()
    {
        _GenerateNetworkingCode("", typeof(IAutoSerialized), "");

    }
    public static void _GenerateNetworkingCode(string generatedScriptPath, Type baseType, string template)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        List<Type> types = new List<Type>();
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
        types.Sort((x, y) => x.Name.CompareTo(y.Name));
        StringBuilder stringBuilder = new StringBuilder();
        Debug.Log(types.Count + " found");
        for (int i = 0; i < types.Count; ++i)
        {
            // one class.
            Type thisType = types[i];
            string fullClassName = thisType.ToString(); // with namespace
            string className = fullClassName;
            Debug.Log(thisType.Name + " generated");
            generateCSCode(types[i], stringBuilder);

        }
        Debug.Log(stringBuilder);

        AssetDatabase.Refresh();
    }
    static void generateCSCode(Type type, StringBuilder stringBuilder)
    {
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
        stringBuilder.AppendFormat("Bursted.us_struct_partial(buffer, val, {0});", accumulated_size);
        stringBuilder.Append(Environment.NewLine);

        for (int i = collection_start; i < fields.Length; ++i)
        {
            FieldInfo fieldInfo = fields[i];
            int field_size = Marshal.SizeOf(fieldInfo.FieldType);
            accumulated_size += field_size;
            var ttypes = fieldInfo.FieldType.GetGenericArguments();
            if (ttypes.Length > 0)
            {
                stringBuilder.AppendFormat("Bursted.us_na(raw, {0});", fieldInfo.Name);
            }
            else
            {
                stringBuilder.AppendFormat("Debug.LogWarning(\"not right!\"):" + fieldInfo.Name);
                //break;
            }
            stringBuilder.Append(Environment.NewLine);
        }
    }

    [MenuItem("Codegen/test")]
    public static void codgen2()
    {
        Debug.Log(generateCSCode2());

    }

    static string generateCSCode2()
    {
        var lines = t_super.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return for_each(lines.ToList(), 0,  new Func<int, List<object>>[]{
            (int tmp) =>
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
            } },
        new Func<object, Dictionary<string, string>>[] {
            (object o) =>
            {
                Type type = (Type)o;
                Dictionary<string, string> ret = new Dictionary<string, string>();
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

                ret.Add("%offset%", accumulated_size.ToString());

                for (int i = collection_start; i < fields.Length; ++i)
                {
                    FieldInfo fieldInfo = fields[i];
                    int field_size = Marshal.SizeOf(fieldInfo.FieldType);
                    accumulated_size += field_size;
                    var ttypes = fieldInfo.FieldType.GetGenericArguments();
                    if (ttypes.Length > 0)
                    {
                        //stringBuilder.AppendFormat("Bursted.us_na(raw, {0});", fieldInfo.Name);
                    }
                    else
                    {
                        //stringBuilder.AppendFormat("Debug.LogWarning(\"not right!\"):" + fieldInfo.Name);
                        //break;
                    }
                    
                }
                return ret;
            } , 


        });
    }
    static string proc3(List<string> strs, int ptr, object key, Func<object, int>[] controllers, Func<object, Dictionary<string, string>>[] funcs)
    {
        string ret = "";
        int count = controllers[ptr](key);
        var dict = funcs[ptr](key);
        for (int k = 0; k < count; ++k)
        {
            for (int i = 0; i < strs.Count; ++i)
            {
                var cur = strs[i];
                if (cur.StartsWith("%for"))
                {
                    List<string> next = new List<string>();
                    int end_idx = strs.Count - 1;
                    for (; end_idx >= i + 1; --end_idx)
                    {
                        if (strs[end_idx].StartsWith("%end")) break;
                    }
                    for (int j = i + 1; j < end_idx; ++j)
                    {
                        next.Add(strs[j]);
                    }
                    if (ptr + 1 < funcs.Length)
                    {
                        ret += proc3(next, ptr + 1, key, controllers, funcs) + Environment.NewLine;

                    }
                    strs.RemoveRange(i, end_idx);
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

    static string for_each(List<string> strs, int ptr, Func<int, List<object>>[] controllers, Func<object, Dictionary<string, string>>[] funcs)
    {
        string ret = "";
        var listobj = controllers[ptr](0);
        Debug.Log(listobj.Count);
        for (int k = 0; k < listobj.Count; ++k)
        {
            var dict = funcs[ptr](listobj[k]);
            for (int i = 0; i < strs.Count; ++i)
            {
                var cur = strs[i];
                if (cur.StartsWith("%for"))
                {
                    List<string> next = new List<string>();
                    int end_idx = strs.Count - 1;
                    for (; end_idx >= i + 1; --end_idx)
                    {
                        if (strs[end_idx].StartsWith("%end")) break;
                    }
                    for (int j = i + 1; j < end_idx; ++j)
                    {
                        next.Add(strs[j]);
                    }
                    if (ptr + 1 < funcs.Length)
                    {
                        ret += for_each(next, ptr + 1, controllers, funcs) + Environment.NewLine;

                    }
                    strs.RemoveRange(i, end_idx);
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
%for%
public partial struct %name% : IAutoSerialized
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
%end%
";

    static string tp1 = @"
        Bursted.ud_na(buffer, out %rp0%, ref offset, alloc);";

    static string tp2 = @"
        Bursted.us_na(buffer, %rp0%);";
    static string tp0 = @"
public partial struct %name% : IAutoSerialized
{
    public const int type_hash = %hash%;

    public NativeList<byte> pack(Allocator alloc)
    {
        NativeList<byte> buffer = new NativeList<byte>(32, alloc);
        Bursted.us_struct(buffer, type_hash);
        Bursted.us_struct_partial(buffer, ref this, %offset%);
        %rc1%
        //Bursted.us_na(buffer, na);
        //Bursted.us_na(buffer, nl_floats);

        return buffer;
    }
    public void unpack(NativeList<byte> buffer, ref int offset, Allocator alloc)
    {
        Bursted.ud_struct_partial(buffer, ref this, %offset%, ref offset);
        %rc0%
    }
}
";
//    string tp0 = @"
//public partial struct %rp0% : IAutoSerialized
//{
//    public const int type_hash = %rp1%;
//    // managed
//    public void send(NetworkDriver nd, NetworkConnection target, NetworkPipeline np)
//    {
//        NativeList<byte> buffer = pack(Allocator.Temp);

//        nd.BeginSend(np, target, out var writer);
//        writer.WriteBytes(buffer.AsArray());
//        nd.EndSend(writer);
//    }
//    public NativeList<byte> pack(Allocator alloc)
//    {
//        NativeList<byte> buffer = new NativeList<byte>(32, alloc);
//        Bursted.us_struct(buffer, type_hash);
//        Bursted.us_struct_partial(buffer, ref this, %rp2%);
//        %tp1%
//        return buffer;
//    }

//    public void unpack(NativeList<byte> buffer, ref int offset, Allocator alloc)
//    {
//        Bursted.ud_struct_partial(buffer, ref this, %rp2%, ref offset);
//        %tp2%
//    }
//}
//";
}
