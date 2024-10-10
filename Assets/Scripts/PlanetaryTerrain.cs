using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class PlanetaryTerrain : MonoBehaviour
{
    public Transform[] three;
    public float cell_half_extents = 200f;
    public int max_lod = 4;
    public int mesh_gen_count = 0;
    public bool refresh;
    public bool continuous_refresh;
    public List<TerrainMeshStates> meshes = new List<TerrainMeshStates>(8);
    public Material terrain_mat;
    public Transform cam_transform; // pivot transform ref
    public int3 cell_pos; // cell index inside a planet's volume, if the range is just the planet.
    public float radius = 200000f;
    void Start()
    {
        
    }
    private void OnDrawGizmos()
    {
        if (three != null && three.Length == 3)
        {
            for(int i = 0; i < three.Length; i++)
            {
                if (three[i] == null)
                {
                    return;
                }
            }
            Gizmos.DrawLine(three[0].position, three[1].position);
            Gizmos.DrawLine(three[2].position, three[1].position);
            Gizmos.DrawLine(three[0].position, three[2].position);
            // clockwise winding
            //triangle_divide(three[0].position, three[1].position, three[2].position, default, 2);
        }
    }
    static void triangle_divide(float3 p0, float3 p1, float3 p2, NativeList<float3> points, int level)
    {
        // float height = 1f;
        // var p0_nml = math.normalize(p0);
        // //var height = NoisesTest.mesh0.octaves(p0_nml, 4);
        // p0 = p0_nml * height;

        // var p1_nml = math.normalize(p1);
        //// height = NoisesTest.mesh0.octaves(p1_nml, 4);
        // p1 = p1_nml * height;

        // var p2_nml = math.normalize(p2);
        // //height = NoisesTest.mesh0.octaves(p2_nml, 4);
        // p2 = p2_nml * height;


        Gizmos.DrawLine(p0, p1);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p0, p2);
        if (level == 0) return;
        float3 q0 = (p0 + p1) / 2f;
        float3 q1 = (p1 + p2) / 2f;
        float3 q2 = (p0 + p2) / 2f;

        //var height = NoisesTest.mesh0.octaves(q0, 4);
        //q0 = math.normalize(q0) * height;

        //height = NoisesTest.mesh0.octaves(q1, 4);
        //q1 = math.normalize(q1) * height;

        //height = NoisesTest.mesh0.octaves(q2, 4);
        //q2 = math.normalize(q2) * height;


        triangle_divide(p0, q0, q2, points, level - 1);
        triangle_divide(q0, p1, q1, points, level - 1);
        triangle_divide(q0, q1, q2, points, level - 1);
        triangle_divide(q2, q1, p2, points, level - 1);
    }
    static bool triangle_size2cam(float3 p0, float3 p1, float3 p2, TerrainGenParams campos)
    {
        var center = (p0 + p1 + p2) / 3f;
        var to_center = math.distance(center, campos.pivot_pos);
        var dist = math.distance(p0, p1) + math.distance(p0, p2);
        dist /= 2f;
        return to_center > dist;

    }
    
    static void triangle_divide_mesh(float3 p0, float3 p1, float3 p2, TerrainGenParams tgparams, List<TerrainMeshStates> meshes, ref int mesh_added, int level)
    {
        if (level == 0 || triangle_size2cam(p0, p1, p2, tgparams))
        {
            var job = new NoisesTest.mesh_triangle();
            job.p0 = p0;
            job.p1 = p1;
            job.p2 = p2;
            job.resolution = 4;
            job.starting_freq = 0.03f;
            job.intensity = 5f;
            job.verts = new NativeList<float3>(65535, Allocator.TempJob);
            job.indices = new NativeList<int>(65535, Allocator.TempJob);
            job.Run();
            Mesh mesh2use = null;
            if (mesh_added >= meshes.Count)
            {
                var tms = new TerrainMeshStates();
                tms.mesh = new Mesh();
                tms.wrotation = quaternion.identity;
                meshes.Add(tms);
            }
            mesh2use = meshes[mesh_added].mesh;
            mesh2use.SetVertices(job.verts.AsArray());
            mesh2use.SetIndices(job.indices.AsArray(), MeshTopology.Triangles, 0);
            mesh2use.RecalculateNormals();
            mesh2use.RecalculateBounds();

            job.verts.Dispose();
            job.indices.Dispose();

            mesh_added++;
            return;
        }
        float3 q0 = (p0 + p1) / 2f;
        float3 q1 = (p1 + p2) / 2f;
        float3 q2 = (p0 + p2) / 2f;

        triangle_divide_mesh(p0, q0, q2, tgparams, meshes, ref mesh_added, level - 1);
        triangle_divide_mesh(q0, p1, q1, tgparams, meshes, ref mesh_added, level - 1);
        triangle_divide_mesh(q0, q1, q2, tgparams, meshes, ref mesh_added, level - 1);
        triangle_divide_mesh(q2, q1, p2, tgparams, meshes, ref mesh_added, level - 1);
    }
    static Vector3 double3_vec3(double3 val)
    {
        return new Vector3((float)val.x, (float)val.y, (float)val.z);
    }

    // Update is called once per frame
    void Update()
    {
        float3 pivot_pos = cam_transform.position;
        if(math.abs(pivot_pos.x) > cell_half_extents
            || math.abs(pivot_pos.y) > cell_half_extents
            || math.abs(pivot_pos.z) > cell_half_extents)
        {
            if (pivot_pos.x > cell_half_extents)
            {
                pivot_pos.x -= cell_half_extents * 2f;
                cell_pos.x++;
            }
            if (pivot_pos.x < -cell_half_extents)
            {
                pivot_pos.x += cell_half_extents * 2f;
                cell_pos.x--;
            }
            if (pivot_pos.y > cell_half_extents)
            {
                pivot_pos.y -= cell_half_extents * 2f;
                cell_pos.y++;
            }
            if (pivot_pos.y < -cell_half_extents)
            {
                pivot_pos.y += cell_half_extents * 2f;
                cell_pos.y--;
            }
            if (pivot_pos.z > cell_half_extents)
            {
                pivot_pos.z -= cell_half_extents * 2f;
                cell_pos.z++;
            }
            if (pivot_pos.z < -cell_half_extents)
            {
                pivot_pos.z += cell_half_extents * 2f;
                cell_pos.z--;
            }
        }
        cam_transform.position = pivot_pos;
        if (refresh)
        {
            if (continuous_refresh == false)
                refresh = false;
            for (int i = 0; i < meshes.Count; i++)
            {
                meshes[i].Clear();

            }
            //meshes.Clear();
            //int mesh_gen_count = 0;
            mesh_gen_count = 0;
            var tgp = new TerrainGenParams();
            tgp.pivot_pos = cam_transform.position;
            tgp.planet_radius = radius;
            tgp.pivot_cell_coord = cell_pos;

            double3 cell_center = new double3(cell_pos) * cell_half_extents * 2.0;
            var cell_center_dir_normalized = math.normalize(cell_center);
            double3 right_dir = math.cross(cell_center_dir_normalized, new double3(0.0, 1.0, 0.0));
            var right_mag = math.distance(right_dir, 0.0);
            if(right_mag > 0.001f)
            {
                right_dir = math.normalize(right_dir);
            }
            else
            {
                right_dir = new double3(0.0, 0.0, 1.0);
            }

            var up_dir = math.normalize(math.cross(right_dir, cell_center_dir_normalized));
            var up_offset = up_dir * cell_half_extents * 4f;
            var right_offset = right_dir * cell_half_extents * 4f;
            Debug.DrawLine(double3_vec3(cell_center), double3_vec3(cell_center + up_offset), Color.red);
            Debug.DrawLine(double3_vec3(cell_center), double3_vec3(cell_center + right_offset), Color.green);
            //triangle_divide_mesh(three[0].position, three[1].position, three[2].position, tgp,
            //    meshes, ref mesh_gen_count, max_lod);
        }
        for (int i = 0; i < meshes.Count; i++)
        {
            if (meshes[i].mesh.vertexCount > 0)
                //Graphics.DrawMesh(meshes[i].mesh, meshes[i].wposition, meshes[i].wrotation, terrain_mat, 0);
                Graphics.DrawMesh(meshes[i].mesh, transform.position, meshes[i].wrotation, terrain_mat, 0);
        }
    }
}
/*
 * when the camera root moves for a large portion of a 'grid', the LCS will have to re-center.
 * the macro-triangulation should stay unchanged.
 * 
 */
public struct TerrainGenParams
{
    public float3 pivot_pos; // pivot's position in cell space, or CamControl.self.root.transform.position
    public int3 pivot_cell_coord; // pivot's cell coord
    public float planet_radius; // planet radius in meters
}
public struct TerrainMeshStates
{
    public Mesh mesh;
    public float3 wposition;
    public quaternion wrotation;
    public void Clear()
    {
        mesh.Clear();
    }
}