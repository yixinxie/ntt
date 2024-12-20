using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class HexagonGridGizmo : MonoBehaviour
{
    public int2 target_axial_coord;
    public int2 target_diff_coord;
    public Transform coord_test_target;
    public bool draw_adj;
    public int draw_hex_radius = 3;
    public Color draw_color = Color.white;
    public float scale = 1.0f;
    public Transform offset_t;
    //public bool generate_tiered_coords;
    //public int tier_range = 8;
    //public int hex_dir = 0;
    //public int hex_dir_calculated;
    void Start()
    {
        
    }
    public bool test_gawr;
    public bool select_and_round;
    //public float atan_value;

    private void OnDrawGizmos()
    {
        //if(generate_tiered_coords)
        //{
        //    var self_axial = HexCoord.FromPosition(transform.localPosition);
        //    var next_hex_dir = (hex_dir + 1) % 6;
        //    var target_axial = self_axial + HexCoord.offsets(hex_dir] * tier_range + HexCoord.offsets(next_hex_dir] * (tier_range + 1);
        //    Gizmos.color = Color.magenta;
        //    var target_pos = HexCoord.ToPosition(target_axial);
        //    Gizmos.DrawLine(transform.localPosition, target_pos);

        //    //hex_dir_calculated = TileToGridSystem.offset2dir_index(target_axial - self_axial);

        //    var diff = (Vector3)target_pos - transform.localPosition;
        //    atan_value = math.atan2(diff.z, diff.x) * Mathf.Rad2Deg;
        //    hex_dir_calculated = (int)math.round((atan_value - 30f) / 60f);
        //    if (hex_dir_calculated < 0) hex_dir_calculated += 6;


        //}
        
        //if(draw_adj)
        //{
        //    var self_axial = HexCoord.FromPosition(transform.position);
        //    for(int i = 0; i < HexCoord._offsets_Length; ++i)
        //    {
        //        var tmp = self_axial + HexCoord.offsets(i);
        //        //Debug.DrawLine(transform.position, HexCoord.ToPosition(tmp), Color.yellow);
        //        TextGizmo.Draw(HexCoord.ToPosition(tmp), i.ToString());
        //    }
        //}

        if (transform.parent != null)
        {
            //var abs_hex = HexCoord.round2hexcenter_custom(transform.position, HexagonMap.HexSideLength * scale);
            //transform.localPosition = transform.parent.InverseTransformPoint(abs_hex);
        }
        else
        {
            //transform.localPosition = HexCoord.round2hexcenter_custom(transform.localPosition, HexagonMap.HexSideLength * scale);
        }
        if (draw_hex_radius > 0 && disable_grid_drawing == false)
        {
            DrawHexGrid(draw_hex_radius, transform.position, draw_color, (offset_t != null) ? offset_t.position - transform.position:0f, scale);
        }

        if(coord_test_target != null)
        {
            target_axial_coord = HexCoord.FromPosition(coord_test_target.position);
            target_diff_coord = target_axial_coord - HexCoord.FromPosition(transform.position);
        }
        //dir_index = GZoneExpansionStates.diff2gzone_hex_dir(transform.rotation);
    }
    public int dir_index;
    public bool disable_grid_drawing;
    private void OnDrawGizmosSelected()
    {
        if(select_and_round)
        {
            select_and_round = false;
            var ts = GetComponentsInChildren<Transform>();
            for(int i = 0; i < ts.Length; ++i)
            {
                var rounded = HexCoord.ToPosition_scaled(HexCoord.FromPosition_scaled(ts[i].position, scale),scale);
                ts[i].position = rounded;
            }
        }
    }
    public static void DrawHexGrid(int half_size, Vector3 pos, Color draw_color, float3 offset, float scale = 1.0f)
    {
        float hsl_scaled = HexagonMap.HexSideLength * scale;
        float cs_scaled = HexagonMap.CellSize * scale;
        float ulh_scaled = HexagonMap.unit_length_half * scale;
        float y_bk = pos.y;
        var center_axial = HexCoord.FromPosition_scaled(pos, scale);
        Gizmos.color = draw_color;

        for (int y = -half_size; y <= half_size; ++y)
        {
            for (int x = -half_size; x <= half_size; ++x)
            {
                var this_axial = new int2(x, y) + center_axial;
                Vector3 center = HexCoord.ToPosition_legacy(this_axial, hsl_scaled) + offset + new float3(0f, y_bk, 0f);
                if (HexCoord.hex_distance(center_axial, this_axial) > half_size) continue;

                Gizmos.DrawLine(center + new Vector3(0f, 0f, hsl_scaled), center + new Vector3(cs_scaled * 0.5f, 0f, ulh_scaled));

                Gizmos.DrawLine(center + new Vector3(cs_scaled * 0.5f, 0f, ulh_scaled),
                    center + new Vector3(cs_scaled * 0.5f, 0f, -ulh_scaled));

                Gizmos.DrawLine(center + new Vector3(cs_scaled * 0.5f, 0f, -ulh_scaled),
                    center + new Vector3(0f, 0f, -hsl_scaled));

                //

                Gizmos.DrawLine(center + new Vector3(0f, 0f, hsl_scaled),
                    center + new Vector3(-cs_scaled * 0.5f, 0f, ulh_scaled));

                Gizmos.DrawLine(center + new Vector3(-cs_scaled * 0.5f, 0f, ulh_scaled),
                    center + new Vector3(-cs_scaled * 0.5f, 0f, -ulh_scaled));

                Gizmos.DrawLine(center + new Vector3(-cs_scaled * 0.5f, 0f, -ulh_scaled),
                    center + new Vector3(0f, 0f, -hsl_scaled));



            }
        }
    }
}
