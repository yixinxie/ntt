using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[System.Serializable]
public struct HexCoord
{
    //https://www.redblobgames.com/grids/hexagons/#hex-to-pixel
    //public int2 value; // axial
    public const float one_third = 1f / 3f;
    //public void from_offset(int2 offset)
    //{
    //    value = new int2(offset.x - (offset.y - (offset.y & 1)) / 2, offset.y);
    //}
    //public int2 to_offset()
    //{
    //    var col = value.x + (value.y - (value.y & 1)) / 2;
    //    var row = value.y;

    //    return new int2(col, row);
    //}
    //// not actively used
    //public int2 to_axial()
    //{
    //    int q = value.x - (value.y - (value.y & 1)) / 2;
    //    int r = value.y;
    //    return new int2(q, r);
    //}
    // not actively used
    //public void from_axial(int2 axial)
    //{
    //    var col = axial.x + (axial.y - (axial.y & 1)) / 2;
    //    var row = axial.y;

    //    value = new int2(col, row);
    //}
    public static int hex_distance(int2 a, int2 b)
    {
        //function axial_distance(a, b):
        return (math.abs(a.x - b.x)
          + math.abs(a.x + a.y - b.x - b.y)
          + math.abs(a.y - b.y)) / 2;
    }

    //public void from_position(float3 point)
    //{
    //    value = FromPosition(point);
    //}

    // legacy, not recommended to use
    public static Vector3 round2hexcenter_custom(Vector3 worldpos, float scale)
    {
        int2 tmp = FromPosition_legacy(worldpos, scale);
        return ToPosition_legacy(tmp, scale);
    }

    public static Vector3 round2hexcenter_scaled(Vector3 worldpos, float scale)
    {
        int2 tmp = FromPosition_scaled(worldpos, scale);
        return ToPosition_scaled(tmp, scale);
    }

    // legacy, not recommended to use
    public static float3 ToPosition_legacy(int2 axial, float size)
    {
        var x = (sqrt3 * axial.x + sqrt3 / 2f * axial.y) * size;
        var z = 1.5f * axial.y * size;
        return new float3(x, 0f, z);
    }
    static readonly float sqrt3 = math.sqrt(3.0f);
    public static int2 FromPosition(float3 point)
    {
        const float size = HexagonMap.HexSideLength;
        var q = (sqrt3 / 3f * point.x - 1f / 3f * point.z) / size;
        var r = (2f / 3f * point.z) / size;
        return axis_round(new float3(q, r, -q - r)).xy;
    }
    // legacy, not recommended to use
    public static int2 FromPosition_legacy(float3 point, float scale)
    {
        var q = (sqrt3 / 3f * point.x - 1f / 3f * point.z) / scale;
        var r = (2f / 3f * point.z) / scale;
        return axis_round(new float3(q, r, -q - r)).xy;
    }
    public static int2 FromPosition_scaled(float3 point, float scale)
    {
        float size = HexagonMap.HexSideLength * scale;
        var q = (sqrt3 / 3f * point.x - 1f / 3f * point.z) / size;
        var r = (2f / 3f * point.z) / size;
        return axis_round(new float3(q, r, -q - r)).xy;
    }
    // clockwise
    public static int2 rotate(int2 axial)
    {
        int3 cube = new int3(axial.x, axial.y, -axial.x - axial.y);
        return new int2(-cube.z, -cube.x);
    }
    // counter-clockwise
    public static int2 rotate_cc(int2 axial)
    {
        int3 cube = new int3(axial.x, axial.y, -axial.x - axial.y);
        return new int2(-cube.y, -cube.z);
    }
    public static Vector3 round2hexcenter(Vector3 worldpos)
    {
        int2 tmp = FromPosition(worldpos);
        return ToPosition(tmp);
    }
    public static float3 round2hexcenter(float3 worldpos)
    {
        int2 tmp = FromPosition(worldpos);
        return ToPosition(tmp);
    }
    public static Vector3 round2hexcenter_multi(Vector3 worldpos)
    {
        int2 tmp = FromPosition(worldpos);
        return ToPosition(tmp);
    }

    public static float3 round2hexcenter_f3(float3 worldpos)
    {
        int2 tmp = FromPosition(worldpos);
        return ToPosition(tmp);
    }
    public static int3 axis_round(float3 frac)
    {
        var q = math.round(frac.x);
        var r = math.round(frac.y);
        var s = math.round(frac.z);

        var q_diff = math.abs(q - frac.x);
        var r_diff = math.abs(r - frac.y);
        var s_diff = math.abs(s - frac.z);

        if (q_diff > r_diff && q_diff > s_diff)
            q = -r - s;
        else if (r_diff > s_diff)
            r = -q - s;
        else
            s = -q - r;

        return new int3((int)math.round(q), (int)math.round(r), (int)math.round(s));
    }

    //public void to_position(out float3 point)
    //{
    //    //var x = (sqrt3 * value.x + sqrt3 / 2f * value.y) * size;
    //    //var z = 1.5f * value.y * size;
    //    //point = new float3(x, 0f, z);
    //    point = ToPosition(value);
    //}

    //public void to_position(out float3 point, float size_override)
    //{
    //    var x = (sqrt3 * value.x + sqrt3 / 2f * value.y) * size_override;
    //    var z = 1.5f * value.y * size_override;
    //    point = new float3(x, 0f, z);
    //}

    // pointy top implementation, z-aligned
    public static float3 ToPosition(int2 axial)
    {
        const float size = HexagonMap.HexSideLength;
        var x = (sqrt3 * axial.x + sqrt3 / 2f * axial.y) * size;
        var z = 1.5f * axial.y * size;
        return new float3(x, 0f, z);
    }

    public static float3 ToPosition_scaled(int2 axial, float scale = 1f)
    {
        float size = HexagonMap.HexSideLength * scale;
        var x = (sqrt3 * axial.x + sqrt3 / 2f * axial.y) * size;
        var z = 1.5f * axial.y * size;
        return new float3(x, 0f, z);
    }


    public static readonly int[] _offsets = new int[] {
                1, 0, // -> east, +x
                0, 1, // northeast
                -1, 1, // northwest
                -1, 0, // west, -x
                0, -1, // southwest
                1, -1, // southeast
                };
    public static int offset2dir_index(int2 offset)
    {
        if (offset.x > 0)
        {
            return (offset.y < 0) ? 5 : 0;
        }
        else if (offset.x < 0)
        {
            return (offset.y > 0) ? 2 : 3;
        }
        return (offset.y > 0) ? 1 : 4;
    }

    public static readonly int _offsets_Length = 6;
    public static int2 offsets(int idx)
    {
        return new int2(_offsets[idx * 2], _offsets[idx * 2 + 1]);
    }



}

public class HexagonMap : MonoBehaviour
{
    //public const float CellSize = 16f; // distance from the center of one cell to that of its adjacent cell. server map version
    public const float CellSize = 1f; // distance from the center of one cell to that of its adjacent cell. server map version
    //public const float unit_length_c = 0.57735f;
    public const float CellSize2HexagonSideLength = 0.5773502692f;
    //[HideInInspector]
    public const float HexSideLength = CellSize2HexagonSideLength * CellSize;// length of a hexagon's one side. used to be unit length
    //public const float QuadWidth = 3f * CellSize2HexagonSideLength * CellSize;// length of a hexagon's one side.
    //public const float QuadHeight = 2f * CellSize;// length of a hexagon's one side.

    // the width(x) of a hex quad is 3 x CS2HSL x CellSize, height(y) is 2 x CellSize
    // scale x: 3 x CS2HSL is 1.7320508076, or 1.732051f
    // scale y: 2
    // when the cellsize is 1
    //public float unit_length; 
    public const float unit_length_half = HexSideLength / 2f;
}