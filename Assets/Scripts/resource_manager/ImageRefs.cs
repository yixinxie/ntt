using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.Video;

public class ImageRefs : MonoBehaviour
{
    public static ImageRefs self;
    // Start is called before the first frame update
    public Sprite[] item_icons;
    public Sprite[] recipe_icons;
    public SpriteAtlas item_icons_sa;
    public Sprite[] extended_icons;
    public Sprite[] currency_icons;
    public Sprite[] other_icons;
    public Sprite[] structure_icons;
    public Sprite[] tech_icons;
    public Sprite[] chip_icons;
    public Sprite[] chip_clique_icons;
    public Sprite[] chip_effect_up_icons;
    public Sprite[] chip_effect_down_icons;
    public Sprite[] instruction_icons;
    public VideoClip[] instruction_videos;
    private void Awake()
    {
        if (self != null)
        {
            //GameObject.Destroy(gameObject);
            return;
        }
        
        //GameObject.DontDestroyOnLoad(gameObject);
        self = this;
        //for(int i = 0; i < item_icons.Length; ++i)
        //{
        //    item_icons[i] = null;
        //}
        //item_icons = new Sprite[item_icons_sa.spriteCount];
        //item_icons_sa.GetSprites(item_icons);
        if(use_atlas)
        {
            //rehash_icons_from_atlas();
        }
    }
    public bool use_atlas;
    void rehash_icons_from_atlas()
    {
        var tmp_item_icons = new Sprite[item_icons_sa.spriteCount];
        item_icons = new Sprite[item_icons_sa.spriteCount];
        item_icons_sa.GetSprites(tmp_item_icons);
        for (int i = 0; i < (int)ItemType.Total; ++i)
        {
            var itype = (ItemType)i;
            string type_string = itype.ToString();
            bool found = false;
            for (int j = 0; j < tmp_item_icons.Length; ++j)
            {
                if (tmp_item_icons[j].name.StartsWith(type_string))
                {
                    item_icons[i] = tmp_item_icons[j];
                    found = true;
                    break;
                }

            }
            if (found == false)
            {
                Debug.Log(type_string + " icon is not found!");
            }

        }
    }
    //public bool refresh_item_icons_from_atlas;
    private void OnDrawGizmos()
    {
        //if(refresh_item_icons_from_atlas)
        //{
        //    refresh_item_icons_from_atlas = false;
        //    rehash_icons_from_atlas();
        //}
    }
    public Sprite GetItemIcon(int currency_idx)
    {
        if (currency_idx < 0 || currency_idx >= item_icons.Length)
        {
            //Debug.LogWarning("invalid currency sprite " + index + " in length " + item_icons.Length);
            return null;
        }
        return item_icons[currency_idx];
    }

    public Sprite GetOtherIcon(int index)
    {
        if (index < 0 || index >= other_icons.Length)
        {
            //Debug.LogWarning("invalid sprite " + index + " in length " + other_icons.Length);
            return null;
        }
        return other_icons[index];
    }

    public Sprite GetRecipeIcon(int index)
    {
        if (index < 0 || index >= recipe_icons.Length - 1)
        {
            //Debug.LogWarning("invalid sprite " + index + " in length " + item_icons.Length);
            return recipe_icons[0];
        }

        return recipe_icons[index + 1];
    }

    //public Sprite GetTechIcon(Technologies tech)
    //{
    //    if (SORefs.self.so0.GetTechSM(tech, out TechResearchDef def) == false) 
    //        return null;

    //    int index = (int)def.key;
    //    if (index < 0 || index >= tech_icons.Length)
    //    {
    //        //Debug.LogWarning("invalid sprite " + index + " in length " + item_icons.Length);
    //        return null;
    //    }

    //    return tech_icons[index];
    //}

    //public Sprite GetMachineIcon(GalacticType gtype)
    //{
    //    if (SORefs.self.so0.GetGalaxyStructureDef(gtype, out GalaxyStructureDef def))
    //    {
    //        //if (gtype.value == GTypes.Assembler /*&& gtype.machine_get_type() != def.subtype*/)
    //        //    return null;

    //        var s_type = def.icon_type;
    //        return structure_icons[(int)s_type];
    //    }

    //    return null;
    //}

    //public Sprite GetChipIcon(ScientistTypes type)
    //{
    //    int index = (int)type;
    //    if (index < 0 || index >= chip_icons.Length)
    //    {
    //        return null;
    //    }

    //    return chip_icons[index];
    //}
    //public Sprite GetChipCliqueIcon(ChipCardDefV2.ChipClique type)
    //{
    //    int index = (int)type;
    //    if (index < 0 || index >= chip_clique_icons.Length)
    //    {
    //        return null;
    //    }

    //    return chip_clique_icons[index];
    //}
    //public Sprite GetChipEffectIcon(BonusEffectTypes type, bool up)
    //{
    //    if (up)
    //    {
    //        int index = (int)type;
    //        if (index < 0 || index >= chip_effect_up_icons.Length)
    //        {
    //            return null;
    //        }

    //        return chip_effect_up_icons[index];
    //    }
    //    else
    //    {
    //        int index = (int)type;
    //        if (index < 0 || index >= chip_effect_down_icons.Length)
    //        {
    //            return null;
    //        }

    //        return chip_effect_down_icons[index];
    //    }
    //}

    //public Sprite GetInstructionIcon(string name)
    //{
    //    foreach(var icon in instruction_icons)
    //    {
    //        if (icon.name.Equals(name))
    //            return icon;
    //    }

    //    return null;
    //}
    //public VideoClip GetInstructionVideo(string name)
    //{
    //    foreach (var video in instruction_videos)
    //    {
    //        if (video.name.Equals(name))
    //            return video;
    //    }

    //    return null;
    //}
#if UNITY_EDITOR
    public bool rescan;
    private void OnDrawGizmosSelected()
    {
        if(rescan)
        {
            rescan = false;
            //var refs = GetComponent<SORefs>();
            {
                item_icons = new Sprite[(int)ItemType.Total];
                rescan_icon(typeof(ItemType), "Assets/art/icons/", (int)ItemType.Total, ItemType.None.ToString(), out item_icons);
                //for (int i = 0; i < (int)ItemType.Total; ++i)
                //{
                //    string path = "Assets/art_asm_temp/icons/" + ((ItemType)i).ToString() + ".png";
                //    var tmp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                //    if (tmp == null)
                //    {
                //        string default_path = "Assets/art_asm_temp/icons/" + default_icon.ToString() + ".png";
                //        item_icons[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(default_path);
                //    }
                //    else
                //    {
                //        item_icons[i] = tmp;
                //    }
                //}
            }

            //{
            //    ExtendedItemTypes default_icon_ex = ExtendedItemTypes.None;
            //    int total = (int)ExtendedItemTypes.Total - (int)ExtendedItemTypes.None;
            //    extended_icons = new Sprite[total];
            //    for (int i = 0; i < total; ++i)
            //    {
            //        string path = "Assets/art_asm_temp/ex_icons/" + ((ExtendedItemTypes)i + (int)ExtendedItemTypes.None).ToString() + ".png";
            //        var tmp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            //        if (tmp == null)
            //        {
            //            string default_path = "Assets/art_asm_temp/ex_icons/" + default_icon_ex.ToString() + ".png";
            //            extended_icons[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(default_path);
            //        }
            //        else
            //        {
            //            extended_icons[i] = tmp;
            //        }
            //    }
            //}
            //{
            //    CurrencyTypes default_currency_icon = CurrencyTypes.None;
            //    int total = (int)CurrencyTypes.Total - (int)CurrencyTypes.None;
            //    currency_icons = new Sprite[total];
            //    for (int i = 0; i < total; ++i)
            //    {
            //        string path = "Assets/art_asm_temp/currency_icons/" + ((CurrencyTypes)i + (int)CurrencyTypes.None).ToString() + ".png";
            //        var tmp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            //        if (tmp == null)
            //        {
            //            string default_path = "Assets/art_asm_temp/currency_icons/" + default_currency_icon.ToString() + ".png";
            //            currency_icons[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(default_path);
            //        }
            //        else
            //        {
            //            currency_icons[i] = tmp;
            //        }
            //    }
            //}
        }
    }
    void rescan_icon(System.Type enum_type, string scan_dir, int total, string default_icon, out Sprite[] item_icons)
    {
        item_icons = new Sprite[total];
        string[] enum_names = enum_type.GetEnumNames();
        for (int i = 0; i < total; ++i)
        {
            Debug.Log(enum_names[i].ToString());
            
            //string path = "Assets/art_asm_temp/icons/" + ((ItemType)i).ToString() + ".png";
            string path = scan_dir + i.ToString() + "-" + enum_names[i] + ".png";
            var tmp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (tmp == null) // use default
            {
                string default_path = scan_dir + default_icon + ".png";
                item_icons[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(default_path);
            }
            else
            {
                item_icons[i] = tmp;
            }
        }
    }
#endif
}
