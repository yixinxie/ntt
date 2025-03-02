using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
public class enum_gens
{
    static string prefabtypes_header = @"
public enum EntityPrefabIndices:short
{";
    static string prefabtypes_footer = @"
total};";
    [MenuItem("Codegen/prefab types refresh")]
    public static void GenerateCodeFile()
    {
        StringBuilder sbuilder = new StringBuilder(prefabtypes_header);
        
        var asset_guids = AssetDatabase.FindAssets("t:prefab", new string[] { "Assets/prefabs/common" });
        for (int i = 0; i < asset_guids.Length; ++i)
        {
            var asset_path = AssetDatabase.GUIDToAssetPath(asset_guids[i]);
            var sprites = AssetDatabase.LoadMainAssetAtPath(asset_path);
            sbuilder.AppendLine(sprites.name + ", ");
        }
        sbuilder.Append(prefabtypes_footer);
        Debug.Log(asset_guids.Length + " prefab enums generated.");
        File.WriteAllText("Assets/Scripts/generated/prefab_types.cs", sbuilder.ToString());
        AssetDatabase.Refresh();
    }
}