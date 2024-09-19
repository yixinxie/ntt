using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class NoisesTest : MonoBehaviour
{
    public float dbg_radius = 1f;
    public float square_length = 1f;
    public int step_count = 10;
    public float noise_strength = 1f;
    public bool refresh = true;
    public bool auto_refresh = false;
    public MeshFilter mfilter;
    public MeshRenderer mr;
    public float unit_length = 1f;
    public int dim = 100;
    public float starting_freq = 1f;
    public float intensity = 1f;

    // Start is called before the first frame update
    void Start()
    {
        
    }
    private void OnDrawGizmos()
    {
        //float3 basepos = transform.localPosition;
        //float3 min_corner = new float3(-1f, -1f, -1f) * square_length / 2f;
        //float step_size = 1f / (float)step_count;
        //for (int y = 0; y < step_count; ++y)
        //{
        //    for (int x = 0; x < step_count; ++x)
        //    {
        //        var point = min_corner + new float3(x, 0f, y) * step_size;
        //        var key = math.normalize(point);
        //        var noise_val = NoiseStatics.cnoise(key);
        //        var p2 = key + key * noise_val * noise_strength;
        //        Gizmos.color = (noise_val > 0f) ? Color.red : Color.green;
        //        Gizmos.DrawLine(basepos + key, basepos + p2);
        //    }
        //}
    }

    // Update is called once per frame
    void Update()
    {
        if(refresh)
        {
            if(auto_refresh == false)
                refresh = false;
            var mesh = mfilter.mesh;
            mesh.Clear();
            //mesh.SetVertices()
            var job0 = new mesh0();
            job0.verts = new NativeList<float3>(32768, Allocator.TempJob);
            job0.indices = new NativeList<int>(32768, Allocator.TempJob);
            job0.min = transform.localPosition;/* - new Vector3(50f, 0f ,50f);*/
            job0.max = transform.localPosition + new Vector3(100f, 0f, 100f);
            job0.resolution = dim;
            job0.starting_freq = starting_freq;
            job0.intensity = intensity;
            job0.Run();
            mesh.SetVertices(job0.verts.AsArray());
            mesh.SetIndices(job0.indices.AsArray(), MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();
            
            job0.verts.Dispose();
            job0.indices.Dispose();



        }
    }
    [BurstCompile]
    struct mesh0 : IJob
    {
        public NativeList<float3> verts;
        public NativeList<int> indices;
        public float3 min;
        public float3 max;
        public int resolution;
        public float starting_freq;
        public float intensity;
        public static float octaves(float3 key, int count)
        {
            float sum = 0f;
            float strength = 1f;
            float freq = 1f;
            for(int i = 0; i < count; ++i)
            {
                //sum += NoiseStatics.cnoise(key * freq) * strength;
                //sum += noise.cnoise(key * freq) * strength;
                sum += noise.cnoise(key * freq) * strength;
                strength *= 0.5f;
                freq *= 2f;
            }
            return sum;
        }

        public void Execute()
        {
            var diff3 = max - min;
            // inclusive
            var r0 = resolution + 1;
            var r1 = resolution;
            var r2 = resolution + 1;

            //var r0 = resolution;
            //var r1 = resolution - 1;
            //var r2 = resolution;
            for (int x = 0; x < r0; ++x)
            {
                float perc_x = (float)x / (resolution);
                for (int z = 0; z < r0; ++z)
                {
                    float perc_z = (float)z / (resolution);
                    var vert_local_position = new float3(diff3.x * perc_x, 0f, diff3.z * perc_z);
                    var key = min + vert_local_position;
                    key.y = 200000f;
                    //key = math.normalize(key);
                    var height = octaves(key * starting_freq, 6) * intensity;
                    vert_local_position.y = height;
                    verts.Add(vert_local_position);
                }
            }
            int index_incre = 0;
            for (int x = 0; x < r1; ++x)
            {
                for (int z = 0; z < r1; ++z)
                {
                    indices.Add(index_incre);
                    indices.Add(index_incre + 1);
                    indices.Add(index_incre + r2);

                    indices.Add(index_incre + 1);
                    indices.Add(index_incre + r2 + 1);
                    indices.Add(index_incre + r2);
                    index_incre += 1;
                }
                index_incre += 1;
            }
        }
    }

    [BurstCompile]
    struct mesh_triangle : IJob
    {
        public NativeList<float3> verts;
        public NativeList<int> indices;
        public float3 p0;
        public float3 p1;
        public float3 p2;
        public byte edge_lod;
        public int resolution; // number of segments. vertice count per edge is resolution + 1.
        public float starting_freq;
        public float intensity;
        void add_vert(float i, float bot, float3 pos_start, float3 dir)
        {
            float perc = i / bot;
            var vert_local_position = pos_start + dir * perc;
            var key = p0 + vert_local_position;
            //key.y = 200000f;
            var height = mesh0.octaves(key * starting_freq, 6) * intensity;
            vert_local_position.y = height;
            verts.Add(vert_local_position);
        }
        public void Execute()
        {
            var diff0_1 = p1 - p0;
            var diff0_2 = p2 - p0;
            var diff1_2 = p2 - p1;
            NativeArray<float3> p0_2_starts = new NativeArray<float3>(resolution, Allocator.Temp);
            NativeArray<float3> p1_2_ends = new NativeArray<float3>(resolution, Allocator.Temp);
            for (int i = 0; i < resolution; ++i)
            {
                float t = (float)i / resolution;
                p0_2_starts[i] = p0 + diff0_2 * t;
                p1_2_ends[i] = p1 + diff1_2 * t;
            }
            for(int j = 0; j < resolution; ++j)
            {
                var pos_start = p0_2_starts[j];
                var pos_end = p1_2_ends[j];
                var diff = pos_end - pos_start;
                for (int i = 0; i < resolution - j; ++i)
                {
                    //float perc = (float)i / (resolution - j);
                    //var vert_local_position = pos_start + diff * perc;
                    //var key = p0 + vert_local_position;
                    ////key.y = 200000f;
                    //var height = mesh0.octaves(key * starting_freq, 6) * intensity;
                    //vert_local_position.y = height;
                    //verts.Add(vert_local_position);
                    add_vert(i, resolution - j, pos_start, diff);

                    if (j == 0)
                    {
                        // e0
                        if((edge_lod & 0b1) != 0)
                        {
                            //perc = ((float)i + 0.5f) / (resolution - j);
                            //vert_local_position = pos_start + diff * perc;
                            //key = p0 + vert_local_position;
                            ////key.y = 200000f;
                            //height = mesh0.octaves(key * starting_freq, 6) * intensity;
                            //vert_local_position.y = height;
                            //verts.Add(vert_local_position);
                            add_vert(i + 0.5f, resolution - j, pos_start, diff);
                        }
                        if(i == 0 && (edge_lod & 0b100) != 0)
                        {
                            add_vert(i + 0.5f, resolution - j, pos_start, diff);
                        }
                    }

                    if (i == resolution - j - 1) break;


                }
            }

            float3 p0_2_start = p0;
            for(int i = 0; i <= resolution; ++i) 
            {
                float t = (float)i / resolution;
                var p = p0_2_start + diff0_1 * t;
            }

            // inclusive
            var r0 = resolution + 1;
            var r1 = resolution;
            var r2 = resolution + 1;

            //var r0 = resolution;
            //var r1 = resolution - 1;
            //var r2 = resolution;
            for (int x = 0; x < r0; ++x)
            {
                float perc_x = (float)x / (resolution);
                for (int z = 0; z < r0; ++z)
                {
                    float perc_z = (float)z / (resolution);
                    var vert_local_position = new float3(diff3.x * perc_x, 0f, diff3.z * perc_z);
                    var key = p0 + vert_local_position;
                    key.y = 200000f;
                    //key = math.normalize(key);
                    var height = mesh0.octaves(key * starting_freq, 6) * intensity;
                    vert_local_position.y = height;
                    verts.Add(vert_local_position);
                }
            }
            int index_incre = 0;
            for (int x = 0; x < r1; ++x)
            {
                for (int z = 0; z < r1; ++z)
                {
                    indices.Add(index_incre);
                    indices.Add(index_incre + 1);
                    indices.Add(index_incre + r2);

                    indices.Add(index_incre + 1);
                    indices.Add(index_incre + r2 + 1);
                    indices.Add(index_incre + r2);
                    index_incre += 1;
                }
                index_incre += 1;
            }
        }
    }
}
public class NoiseStatics
{

    static float4 mod(float4 x, float4 y)
    {
        return x - y * math.floor(x / y);
    }

    static float3 mod(float3 x, float3 y)
    {
        return x - y * math.floor(x / y);
    }

    static float2 mod289(float2 x)
    {
        return x - math.floor(x / 289.0f) * 289.0f;
    }

    static float3 mod289(float3 x)
    {
        return x - math.floor(x / 289.0f) * 289.0f;
    }

    static float4 mod289(float4 x)
    {
        return x - math.floor(x / 289.0f) * 289.0f;
    }

    static float4 permute(float4 x)
    {
        return mod289(((x * 34.0f) + 1.0f) * x);
    }

    static float3 permute(float3 x)
    {
        return mod289((x * 34.0f + 1.0f) * x);
    }

    static float4 taylorInvSqrt(float4 r)
    {
        return (float4)1.79284291400159 - r * 0.85373472095314f;
    }

    static float3 taylorInvSqrt(float3 r)
    {
        return 1.79284291400159f - 0.85373472095314f * r;
    }

    static float3 fade(float3 t)
    {
        return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
    }

    static float2 fade(float2 t)
    {
        return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
    }


    static float rand3dTo1d(float3 value, float3 dotDir/* = float3(12.9898, 78.233, 37.719)*/)
    {
        //make value smaller to avoid artefacts
        float3 smallValue = math.sin(value);
        //get scalar value from 3d vector
        float random = math.dot(smallValue, dotDir);
        //make value more random by making it bigger and then taking the factional part
        random = math.frac(math.sin(random) * 143758.5453f);
        return random;
    }

    static float rand2dTo1d(float2 value, float2 dotDir/* = float2(12.9898, 78.233)*/)
    {
        float2 smallValue = math.sin(value);
        float random = math.dot(smallValue, dotDir);
        random = math.frac(math.sin(random) * 143758.5453f);
        return random;
    }

    static float rand1dTo1d(float3 value, float mutator/* = 0.546f*/)
    {
        float random = math.frac(math.sin(value.x + mutator) * 143758.5453f);
        return random;
    }

    //to 2d functions

    static float2 rand3dTo2d(float3 value)
    {
        return new float2(
            rand3dTo1d(value, new float3(12.989f, 78.233f, 37.719f)),
            rand3dTo1d(value, new float3(39.346f, 11.135f, 83.155f))
        );
    }

    static  float2 rand2dTo2d(float2 value)
    {
        return new float2(
            rand2dTo1d(value, new float2(12.989f, 78.233f)),
            rand2dTo1d(value, new float2(39.346f, 11.135f))
        );
    }

    float2 rand1dTo2d(float value)
    {
        return new float2(
            rand2dTo1d(value, 3.9812f),
            rand2dTo1d(value, 7.1536f)
        );
    }

    //to 3d functions

    float3 rand3dTo3d(float3 value)
    {
        return new float3(
            rand3dTo1d(value, new float3(12.989f, 78.233f, 37.719f)),
            rand3dTo1d(value, new float3(39.346f, 11.135f, 83.155f)),
            rand3dTo1d(value, new float3(73.156f, 52.235f, 09.151f))
        );
    }

    float3 rand2dTo3d(float2 value)
    {
        return new float3(
            rand2dTo1d(value, new float2(12.989f, 78.233f)),
            rand2dTo1d(value, new float2(39.346f, 11.135f)),
            rand2dTo1d(value, new float2(73.156f, 52.235f))
        );
    }

    float3 rand1dTo3d(float value)
    {
        return new float3(
            rand1dTo1d(value, 3.9812f),
            rand1dTo1d(value, 7.1536f),
            rand1dTo1d(value, 5.7241f)
        );
    }
//}
//public class Perlin
//{

    // Classic Perlin noise
    public static float cnoise(float3 P)
    {
        float3 Pi0 = math.floor(P); // Integer part for indexing
        float3 Pi1 = Pi0 + (float3)1.0; // Integer part + 1
        Pi0 = mod289(Pi0);
        Pi1 = mod289(Pi1);
        float3 Pf0 = math.frac(P); // Fractional part for interpolation
        float3 Pf1 = Pf0 - (float3)1.0; // Fractional part - 1.0
        float4 ix = new float4(Pi0.x, Pi1.x, Pi0.x, Pi1.x);
        float4 iy = new float4(Pi0.y, Pi0.y, Pi1.y, Pi1.y);
        float4 iz0 = (float4)Pi0.z;
        float4 iz1 = (float4)Pi1.z;

        float4 ixy = permute(permute(ix) + iy);
        float4 ixy0 = permute(ixy + iz0);
        float4 ixy1 = permute(ixy + iz1);

        float4 gx0 = ixy0 / 7.0f;
        float4 gy0 = math.frac(math.floor(gx0) / 7.0f) - 0.5f;
        gx0 = math.frac(gx0);
        float4 gz0 = (float4)0.5 - math.abs(gx0) - math.abs(gy0);
        float4 sz0 = math.step(gz0, (float4)0.0);
        gx0 -= sz0 * (math.step((float4)0.0f, gx0) - 0.5f);
        gy0 -= sz0 * (math.step((float4)0.0f, gy0) - 0.5f);

        float4 gx1 = ixy1 / 7.0f;
        float4 gy1 = math.frac(math.floor(gx1) / 7.0f) - 0.5f;
        gx1 = math.frac(gx1);
        float4 gz1 = (float4)0.5f - math.abs(gx1) - math.abs(gy1);
        float4 sz1 = math.step(gz1, (float4)0.0);
        gx1 -= sz1 * (math.step((float4)0.0f, gx1) - 0.5f);
        gy1 -= sz1 * (math.step((float4)0.0f, gy1) - 0.5f);

        float3 g000 = new float3(gx0.x, gy0.x, gz0.x);
        float3 g100 = new float3(gx0.y, gy0.y, gz0.y);
        float3 g010 = new float3(gx0.z, gy0.z, gz0.z);
        float3 g110 = new float3(gx0.w, gy0.w, gz0.w);
        float3 g001 = new float3(gx1.x, gy1.x, gz1.x);
        float3 g101 = new float3(gx1.y, gy1.y, gz1.y);
        float3 g011 = new float3(gx1.z, gy1.z, gz1.z);
        float3 g111 = new float3(gx1.w, gy1.w, gz1.w);

        float4 norm0 = taylorInvSqrt(new float4(math.dot(g000, g000), math.dot(g010, g010), math.dot(g100, g100), math.dot(g110, g110)));
        g000 *= norm0.x;
        g010 *= norm0.y;
        g100 *= norm0.z;
        g110 *= norm0.w;

        float4 norm1 = taylorInvSqrt(new float4(math.dot(g001, g001), math.dot(g011, g011), math.dot(g101, g101), math.dot(g111, g111)));
        g001 *= norm1.x;
        g011 *= norm1.y;
        g101 *= norm1.z;
        g111 *= norm1.w;

        float n000 = math.dot(g000, Pf0);
        float n100 = math.dot(g100, new float3(Pf1.x, Pf0.y, Pf0.z));
        float n010 = math.dot(g010, new float3(Pf0.x, Pf1.y, Pf0.z));
        float n110 = math.dot(g110, new float3(Pf1.x, Pf1.y, Pf0.z));
        float n001 = math.dot(g001, new float3(Pf0.x, Pf0.y, Pf1.z));
        float n101 = math.dot(g101, new float3(Pf1.x, Pf0.y, Pf1.z));
        float n011 = math.dot(g011, new float3(Pf0.x, Pf1.y, Pf1.z));
        float n111 = math.dot(g111, Pf1);

        float3 fade_xyz = fade(Pf0);
        float4 n_z = math.lerp(new float4(n000, n100, n010, n110), new float4(n001, n101, n011, n111), fade_xyz.z);
        float2 n_yz = math.lerp(n_z.xy, n_z.zw, fade_xyz.y);
        float n_xyz = math.lerp(n_yz.x, n_yz.y, fade_xyz.x);
        return 2.2f * n_xyz;
    }

    // Classic Perlin noise, periodic variant
    public static float pnoise(float3 P, float3 rep)
    {
        float3 Pi0 = mod(math.floor(P), rep); // Integer part, modulo period
        float3 Pi1 = mod(Pi0 + (float3)1.0f, rep); // Integer part + 1, mod period
        Pi0 = mod289(Pi0);
        Pi1 = mod289(Pi1);
        float3 Pf0 = math.frac(P); // Fractional part for interpolation
        float3 Pf1 = Pf0 - (float3)1.0f; // Fractional part - 1.0
        float4 ix = new float4(Pi0.x, Pi1.x, Pi0.x, Pi1.x);
        float4 iy = new float4(Pi0.y, Pi0.y, Pi1.y, Pi1.y);
        float4 iz0 = (float4)Pi0.z;
        float4 iz1 = (float4)Pi1.z;

        float4 ixy = permute(permute(ix) + iy);
        float4 ixy0 = permute(ixy + iz0);
        float4 ixy1 = permute(ixy + iz1);

        float4 gx0 = ixy0 / 7.0f;
        float4 gy0 = math.frac(math.floor(gx0) / 7.0f) - 0.5f;
        gx0 = math.frac(gx0);
        float4 gz0 = (float4)0.5 - math.abs(gx0) - math.abs(gy0);
        float4 sz0 = math.step(gz0, (float4)0.0);
        gx0 -= sz0 * (math.step((float4)0.0f, gx0) - 0.5f);
        gy0 -= sz0 * (math.step((float4)1.0f, gy0) - 0.5f);

        float4 gx1 = ixy1 / 7.0f;
        float4 gy1 = math.frac(math.floor(gx1) / 7.0f) - 0.5f;
        gx1 = math.frac(gx1);
        float4 gz1 = (float4)0.5 - math.abs(gx1) - math.abs(gy1);
        float4 sz1 = math.step(gz1, (float4)0.0);
        gx1 -= sz1 * (math.step((float4)0.0f, gx1) - 0.5f);
        gy1 -= sz1 * (math.step((float4)0.0f, gy1) - 0.5f);

        float3 g000 = new float3(gx0.x, gy0.x, gz0.x);
        float3 g100 = new float3(gx0.y, gy0.y, gz0.y);
        float3 g010 = new float3(gx0.z, gy0.z, gz0.z);
        float3 g110 = new float3(gx0.w, gy0.w, gz0.w);
        float3 g001 = new float3(gx1.x, gy1.x, gz1.x);
        float3 g101 = new float3(gx1.y, gy1.y, gz1.y);
        float3 g011 = new float3(gx1.z, gy1.z, gz1.z);
        float3 g111 = new float3(gx1.w, gy1.w, gz1.w);

        float4 norm0 = taylorInvSqrt(new float4(math.dot(g000, g000), math.dot(g010, g010), math.dot(g100, g100), math.dot(g110, g110)));
        g000 *= norm0.x;
        g010 *= norm0.y;
        g100 *= norm0.z;
        g110 *= norm0.w;
        float4 norm1 = taylorInvSqrt(new float4(math.dot(g001, g001), math.dot(g011, g011), math.dot(g101, g101), math.dot(g111, g111)));
        g001 *= norm1.x;
        g011 *= norm1.y;
        g101 *= norm1.z;
        g111 *= norm1.w;

        float n000 = math.dot(g000, Pf0);
        float n100 = math.dot(g100, new float3(Pf1.x, Pf0.y, Pf0.z));
        float n010 = math.dot(g010, new float3(Pf0.x, Pf1.y, Pf0.z));
        float n110 = math.dot(g110, new float3(Pf1.x, Pf1.y, Pf0.z));
        float n001 = math.dot(g001, new float3(Pf0.x, Pf0.y, Pf1.z));
        float n101 = math.dot(g101, new float3(Pf1.x, Pf0.y, Pf1.z));
        float n011 = math.dot(g011, new float3(Pf0.x, Pf1.y, Pf1.z));
        float n111 = math.dot(g111, Pf1);

        float3 fade_xyz = fade(Pf0);
        float4 n_z = math.lerp(new float4(n000, n100, n010, n110), new float4(n001, n101, n011, n111), fade_xyz.z);
        float2 n_yz = math.lerp(n_z.xy, n_z.zw, fade_xyz.y);
        float n_xyz = math.lerp(n_yz.x, n_yz.y, fade_xyz.x);
        return 2.2f * n_xyz;
    }



    // BEGIN JIMMY'S MODIFICATIONS

    public static void PerlinNoise3D_float(float3 input, out float Out)
    {
        Out = cnoise(input);
    }

    public static void PerlinNoise3DPeriodic_float(float3 input, float3 period, out float Out)
    {
        Out = pnoise(input, period);
    }
}