using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.InputManagerEntry;

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
    public int edge_lod;
    public Transform[] othertwo;

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
            
            //{
            //    var mesh = mfilter.mesh;
            //    mesh.Clear();
            //    var job0 = new mesh0();
            //    job0.verts = new NativeList<float3>(32768, Allocator.TempJob);
            //    job0.indices = new NativeList<int>(32768, Allocator.TempJob);
            //    job0.min = transform.localPosition;/* - new Vector3(50f, 0f ,50f);*/
            //    job0.max = transform.localPosition + new Vector3(100f, 0f, 100f);
            //    job0.resolution = dim;
            //    job0.starting_freq = starting_freq;
            //    job0.intensity = intensity;
            //    job0.Run();
            //    mesh.SetVertices(job0.verts.AsArray());
            //    mesh.SetIndices(job0.indices.AsArray(), MeshTopology.Triangles, 0);
            //    mesh.RecalculateNormals();
            //    mesh.RecalculateBounds();
            //    job0.verts.Dispose();
            //    job0.indices.Dispose();
            //}

            {
                var mesh = mfilter.mesh;
                mesh.Clear();

                var job0 = new mesh_triangle();
                job0.verts = new NativeList<float3>(32768, Allocator.TempJob);
                job0.indices = new NativeList<int>(32768, Allocator.TempJob);
                job0.resolution = dim;
                job0.starting_freq = starting_freq;
                job0.intensity = intensity;
                job0.edge_lod= (byte)edge_lod;
                job0.p0 = PlanetaryTerrain.vec3_double3(transform.localPosition);
                if (othertwo != null && othertwo.Length == 2)
                {
                    job0.p1 = PlanetaryTerrain.vec3_double3(othertwo[0].position);
                    job0.p2 = PlanetaryTerrain.vec3_double3(othertwo[1].position);
                }
                else
                {
                    job0.p1 = PlanetaryTerrain.vec3_double3(transform.localPosition + Vector3.forward * dim);
                    job0.p2 = PlanetaryTerrain.vec3_double3(transform.localPosition + Vector3.right * dim);
                }
                job0.Run();
                mesh.SetVertices(job0.verts.AsArray());
                mesh.SetIndices(job0.indices.AsArray(), MeshTopology.Triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                job0.verts.Dispose();
                job0.indices.Dispose();
                
            }


        }
    }

    

    [BurstCompile]
    public struct mesh_triangle : IJob
    {
        public NativeList<float3> verts;
        public NativeList<int> indices;
        public double3 p0;  // should be in planet space, the magnitude must be equal to planet radius.
        public double3 p1;
        public double3 p2;
        public float planet_radius;
        public byte edge_lod;
        public int resolution; // number of segments. vertice count per edge is resolution + 1.
        public float starting_freq;
        public float intensity;
        public static float octaves(float3 key, int count)
        {
            float sum = 0f;
            float strength = 1f;
            float freq = 1f;
            for (int i = 0; i < count; ++i)
            {
                //sum += NoiseStatics.cnoise(key * freq) * strength;
                //sum += noise.cnoise(key * freq) * strength;
                sum += noise.cnoise(key * freq) * strength;
                strength *= 0.5f;
                freq *= 2f;
            }
            return sum;
        }
        double3 add_vert0(float i, float bot, double3 pos_start, double3 dir, NativeList<double3> _verts)
        {
            float perc = i / bot;
            var vert_local_position = pos_start + dir * perc;
            //var key = p0 + vert_local_position;
            //var height = mesh0.octaves(key * starting_freq, 6) * intensity;
            //vert_local_position.y = height;
            _verts.Add(vert_local_position);
            return vert_local_position;
        }
        
        void add_vert2map(double3 vert, int idx, NativeArray<double3> mapped_verts, ref int increment, NativeArray<int> mapped_indices)
        {
            if (mapped_indices[idx] < 0)
            {
                mapped_indices[idx] = increment;
                mapped_verts[idx] = vert;
                increment++;
            }
        }
        public void Execute()
        {
            const byte EL_Top = 0b1;
            const byte EL_Right = 0b10;
            const byte EL_Left = 0b100;

            
            var diff0_1 = p1 - p0;
            var diff0_2 = p2 - p0;
            var diff1_2 = p2 - p1;
            int mapped_vert_width = (resolution * 2 + 1);
            NativeList<double3> verts_d3 = new NativeList<double3>(10, Allocator.Temp);
            NativeArray<double3> mapped_verts = new NativeArray<double3>(mapped_vert_width * mapped_vert_width, Allocator.Temp);
            NativeArray<int> mapped_indices = new NativeArray<int>(mapped_vert_width * mapped_vert_width, Allocator.Temp);
            for(int i = 0; i < mapped_indices.Length; ++i)
            {
                mapped_indices[i] = -1;
            }
            NativeArray<double3> p0_2_starts = new NativeArray<double3>(resolution + 1, Allocator.Temp);
            NativeArray<double3> p1_2_ends = new NativeArray<double3>(resolution + 1, Allocator.Temp);
            for (int i = 0; i <= resolution; ++i)
            {
                float t = (float)i / resolution;
                p0_2_starts[i] = p0 + diff0_2 * t;
                p1_2_ends[i] = p1 + diff1_2 * t;
            }

            int incre = 0;
            for (int j = 0; j <= resolution; ++j)
            {
                var pos_start = p0_2_starts[j];
                var pos_end = p1_2_ends[j];
                var diff = pos_end - pos_start;

                var tp0 = add_vert0(0, 1, pos_start, diff, verts_d3);
                add_vert2map(tp0, j * 2 * mapped_vert_width, mapped_verts, ref incre, mapped_indices);

                for (int i = 0; i < resolution - j; ++i)
                {
                    int index0 = i * 2 + j * 2 * mapped_vert_width;

                    var tp1 = add_vert0(i + 1, resolution - j, pos_start, diff, verts_d3);
                    add_vert2map(tp1, index0 + 2, mapped_verts, ref incre, mapped_indices);
                }
            }
            if ((edge_lod & EL_Top) != 0) // top
            {
                for (int i = 0; i < resolution; ++i)
                {
                    var tp0 = mapped_verts[i * 2];
                    var tp1 = mapped_verts[i * 2 + 2];

                    add_vert2map((tp0 + tp1) / 2.0, i * 2 + 1, mapped_verts, ref incre, mapped_indices);
                }
            }
            if ((edge_lod & EL_Right) != 0) // right
            {
                for (int i = 0; i < resolution; ++i)
                {
                    int o_index = (resolution - i) * mapped_vert_width * 2 + i * 2;
                    var tp0 = mapped_verts[o_index];
                    var tp1 = mapped_verts[o_index - resolution * 4];

                    add_vert2map((tp0 + tp1) / 2.0, o_index - resolution * 2, mapped_verts, ref incre, mapped_indices);
                }
            }
            if ((edge_lod & EL_Left) != 0) // left
            {
                for (int i = 0; i < resolution; ++i)
                {
                    var tp0 = mapped_verts[i * mapped_vert_width * 2];
                    var tp1 = mapped_verts[(i + 1) * mapped_vert_width * 2];

                    add_vert2map((tp0 + tp1) / 2.0, i * mapped_vert_width * 2 + mapped_vert_width, mapped_verts, ref incre, mapped_indices);
                }
            }

            //for (int i = 0; i < mapped_indices.Length; ++i)
            //{
            //    if (mapped_indices[i] >= 0) Debug.DrawLine(mapped_verts[i], mapped_verts[i] + new float3(0f, 1f, 0f), Color.yellow);
            //}

            // vertex windings
            // first row except the last cell, from p0 to p1.
            // special case, affected by the top edge and the left edge.
            if ((edge_lod & EL_Left) != 0 && (edge_lod & EL_Top) == 0)
            {
                indices.Add(mapped_indices[0]);
                indices.Add(mapped_indices[2]);
                indices.Add(mapped_indices[mapped_vert_width]);

                indices.Add(mapped_indices[mapped_vert_width]);
                indices.Add(mapped_indices[2]);
                indices.Add(mapped_indices[mapped_vert_width * 2]);
            }
            else if ((edge_lod & EL_Left) == 0 && (edge_lod & EL_Top) != 0)
            {
                indices.Add(mapped_indices[0]);
                indices.Add(mapped_indices[1]);
                indices.Add(mapped_indices[mapped_vert_width * 2]);

                indices.Add(mapped_indices[1]);
                indices.Add(mapped_indices[2]);
                indices.Add(mapped_indices[mapped_vert_width * 2]);
            }
            else if (edge_lod == EL_Top + EL_Left)
            {
                indices.Add(mapped_indices[0]);
                indices.Add(mapped_indices[1]);
                indices.Add(mapped_indices[mapped_vert_width]);

                indices.Add(mapped_indices[1]);
                indices.Add(mapped_indices[2]);
                indices.Add(mapped_indices[mapped_vert_width]);

                indices.Add(mapped_indices[mapped_vert_width]);
                indices.Add(mapped_indices[2]);
                indices.Add(mapped_indices[mapped_vert_width * 2]);
            }
            else
            {
                indices.Add(mapped_indices[0]);
                indices.Add(mapped_indices[2]);
                indices.Add(mapped_indices[mapped_vert_width * 2]);
            }
            // the rest of the first row except the last cell.
            if ((edge_lod & EL_Top) != 0)
            {
                for (int i = 1; i < resolution - 1; ++i)
                {
                    indices.Add(mapped_indices[i * 2]);
                    indices.Add(mapped_indices[i * 2 + 1]);
                    indices.Add(mapped_indices[i * 2 + mapped_vert_width * 2]);

                    indices.Add(mapped_indices[i * 2 + 1]);
                    indices.Add(mapped_indices[i * 2 + 2]);
                    indices.Add(mapped_indices[i * 2 + mapped_vert_width * 2]);
                }
            }
            else
            {
                for (int i = 1; i < resolution - 1; ++i)
                {
                    indices.Add(mapped_indices[i * 2]);
                    indices.Add(mapped_indices[i * 2 + 2]);
                    indices.Add(mapped_indices[i * 2 + mapped_vert_width * 2]);
                }
            }
            // the last cell on the first row
            // special case, affected by the top edge and the right edge.
            int onelast = mapped_vert_width - 1 - 2;
            if ((edge_lod & EL_Right) != 0 && (edge_lod & EL_Top) == 0)
            {
                indices.Add(mapped_indices[onelast]);
                indices.Add(mapped_indices[mapped_vert_width - 1]);
                indices.Add(mapped_indices[onelast + mapped_vert_width + 1]);

                indices.Add(mapped_indices[onelast]);
                indices.Add(mapped_indices[onelast + mapped_vert_width + 1]);
                indices.Add(mapped_indices[onelast + mapped_vert_width * 2]);
            }
            else if ((edge_lod & EL_Right) == 0 && (edge_lod & EL_Top) != 0)
            {
                indices.Add(mapped_indices[onelast]);
                indices.Add(mapped_indices[onelast + 1]);
                indices.Add(mapped_indices[onelast + mapped_vert_width * 2]);

                indices.Add(mapped_indices[onelast + 1]);
                indices.Add(mapped_indices[mapped_vert_width - 1]);
                indices.Add(mapped_indices[onelast + mapped_vert_width * 2]);
            }
            else if (edge_lod == EL_Top + EL_Right)
            {
                indices.Add(mapped_indices[onelast]);
                indices.Add(mapped_indices[onelast + 1 + mapped_vert_width]);
                indices.Add(mapped_indices[onelast + 1 + mapped_vert_width * 2 - 1]);

                indices.Add(mapped_indices[onelast]);
                indices.Add(mapped_indices[mapped_vert_width - 2]);
                indices.Add(mapped_indices[mapped_vert_width - 2 + mapped_vert_width]);

                indices.Add(mapped_indices[mapped_vert_width - 2]);
                indices.Add(mapped_indices[mapped_vert_width - 1]);
                indices.Add(mapped_indices[onelast + 1 + mapped_vert_width]);
            }
            else
            {
                indices.Add(mapped_indices[mapped_vert_width - 3]);
                indices.Add(mapped_indices[mapped_vert_width - 1]);
                indices.Add(mapped_indices[mapped_vert_width - 3 + mapped_vert_width * 2]);
            }
            if ((edge_lod & EL_Right) != 0)
            {
                for (int i = 1; i < resolution - 1; ++i)
                {
                    indices.Add(mapped_indices[onelast + 2 * i * (mapped_vert_width - 1)]);
                    indices.Add(mapped_indices[onelast + 2 * i * (mapped_vert_width - 1) + 2]);
                    indices.Add(mapped_indices[onelast + 2 * i * (mapped_vert_width - 1) + mapped_vert_width + 1]);

                    indices.Add(mapped_indices[onelast + 2 * i * (mapped_vert_width - 1)]);
                    indices.Add(mapped_indices[onelast + 2 * i * (mapped_vert_width - 1) + mapped_vert_width + 1]);
                    indices.Add(mapped_indices[onelast + 2 * (i + 1) * (mapped_vert_width - 1) + 2]);
                }
            }
            else
            {
                for (int i = 1; i < resolution - 1; ++i)
                {
                    indices.Add(mapped_indices[onelast + 2 * i * (mapped_vert_width - 1)]);
                    indices.Add(mapped_indices[onelast + 2 * i * (mapped_vert_width - 1) + 2]);
                    indices.Add(mapped_indices[onelast + 2 * (i + 1)* (mapped_vert_width - 1) + 2]);
                }
            }

            // the bottom cell
            // special case, affected by the left edge and the right edge.
            onelast = mapped_vert_width * 2 * (resolution - 1);
            if ((edge_lod & EL_Right) != 0 && (edge_lod & EL_Left) == 0)
            {
                indices.Add(mapped_indices[onelast]);
                indices.Add(mapped_indices[onelast + 2]);
                indices.Add(mapped_indices[onelast + mapped_vert_width + 1]);

                indices.Add(mapped_indices[onelast]);
                indices.Add(mapped_indices[onelast + mapped_vert_width + 1]);
                indices.Add(mapped_indices[onelast + mapped_vert_width * 2]);
            }
            else if ((edge_lod & EL_Right) == 0 && (edge_lod & EL_Left) != 0)
            {
                indices.Add(mapped_indices[onelast]);
                indices.Add(mapped_indices[onelast + 2]);
                indices.Add(mapped_indices[onelast + mapped_vert_width]);

                indices.Add(mapped_indices[onelast + mapped_vert_width]);
                indices.Add(mapped_indices[onelast + 2]);
                indices.Add(mapped_indices[onelast + 2 * mapped_vert_width]);
            }
            else if (edge_lod == EL_Left + EL_Right)
            {
                indices.Add(mapped_indices[onelast]);
                indices.Add(mapped_indices[onelast + 2]);
                indices.Add(mapped_indices[onelast + mapped_vert_width]);

                indices.Add(mapped_indices[onelast + mapped_vert_width]);
                indices.Add(mapped_indices[onelast + 2]);
                indices.Add(mapped_indices[onelast + mapped_vert_width + 1]);

                indices.Add(mapped_indices[onelast + mapped_vert_width]);
                indices.Add(mapped_indices[onelast + mapped_vert_width + 1]);
                indices.Add(mapped_indices[onelast + 2 * mapped_vert_width]);
            }
            else
            {
                indices.Add(mapped_indices[onelast]);
                indices.Add(mapped_indices[onelast + 2]);
                indices.Add(mapped_indices[onelast + mapped_vert_width * 2]);
            }
            if ((edge_lod & EL_Left) != 0)
            {
                for (int i = 1; i < resolution - 1; ++i)
                {
                    indices.Add(mapped_indices[2 * i * mapped_vert_width]);
                    indices.Add(mapped_indices[2 * i * mapped_vert_width + 2]);
                    indices.Add(mapped_indices[2 * i * mapped_vert_width + mapped_vert_width]);

                    indices.Add(mapped_indices[2 * i * mapped_vert_width + mapped_vert_width]);
                    indices.Add(mapped_indices[2 * i * mapped_vert_width + 2]);
                    indices.Add(mapped_indices[2 * (i + 1) * mapped_vert_width]);
                }
            }
            else
            {
                for (int i = 1; i < resolution - 1; ++i)
                {
                    indices.Add(mapped_indices[2 * i * mapped_vert_width]);
                    indices.Add(mapped_indices[2 * i * mapped_vert_width + 2]);
                    indices.Add(mapped_indices[2 * (i + 1) * mapped_vert_width]);
                }
            }
            // the other cells that don't split for adjacent lod.
            for (int j = 0; j < resolution - 1; ++j)
            {
                for (int i = 0; i < resolution - j - 1; ++i)
                {
                    indices.Add(mapped_indices[2 + i * 2 + j * mapped_vert_width * 2]);

                    indices.Add(mapped_indices[2 + i * 2 + (j + 1) * mapped_vert_width * 2]);
                    indices.Add(mapped_indices[i * 2 + (j + 1)* mapped_vert_width * 2]);

                }
            }
            for (int j = 1; j < resolution - 1; ++j)
            {
                for (int i = 1; i < resolution - j - 1; ++i)
                {
                    indices.Add(mapped_indices[i * 2 + j * mapped_vert_width * 2]);
                    indices.Add(mapped_indices[2 + i * 2 + j * mapped_vert_width * 2]);
                    indices.Add(mapped_indices[i * 2 + (j + 1) * mapped_vert_width * 2]);

                }
            }

            int vert_count = 0;
            for (int i = 0; i < mapped_indices.Length; ++i)
            {
                if (mapped_indices[i] >= 0)
                    vert_count++;
            }
            verts.AddReplicate(0f, vert_count);
            float3 base_pos = (float3)(p0 + p1 + p2) / 3f;
            for (int i = 0; i < mapped_indices.Length; ++i)
            {
                if (mapped_indices[i] >= 0)
                {

                    var key = mapped_verts[i];
                    var height = octaves((float3)key * starting_freq, 6) * intensity;
                    var key_normalized = math.normalize(key);
                    key = key_normalized * (planet_radius + height);

                    verts[mapped_indices[i]] = new float3((float)key.x, (float)key.y, (float)key.z) - base_pos;
                }
            }
        }

    }
}
