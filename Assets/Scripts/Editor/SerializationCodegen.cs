using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;
using System.Security.Policy;
using System.Text;
using Unity.Burst;
using Unity.Collections;
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
            generateCSCode(types[i], stringBuilder, template, fullClassName, className, baseType);

        }
        Debug.Log(stringBuilder);

        AssetDatabase.Refresh();
    }
    static void generateCSCode(Type type, StringBuilder stringBuilder, string template, string fullClassName, string className, Type baseType)
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
}
