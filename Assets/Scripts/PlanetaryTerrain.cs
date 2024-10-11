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
    NativeArray<double3> axis6;
    NativeArray<byte> faces;
    NativeArray<int4> faces_selectors;
    private void Awake()
    {
        axis6 = new NativeArray<double3>(6, Allocator.Persistent);
        faces = new NativeArray<byte>(24, Allocator.Persistent);

        faces_selectors = new NativeArray<int4>(6, Allocator.Persistent);
        faces_selectors[0] = new int4(0, 3, 4, 7);
        faces_selectors[1] = new int4(0, 1, 2, 3);
        faces_selectors[2] = new int4(0, 1, 4, 5);

        faces_selectors[3] = new int4(1, 2, 5, 6);
        faces_selectors[4] = new int4(4, 5, 6, 7);
        faces_selectors[5] = new int4(2, 3, 7, 6);


        axis6[0] = new double3(1.0, 0.0, 0.0);
        axis6[1] = new double3(0.0, 1.0, 0.0);
        axis6[2] = new double3(0.0, 0.0, 1.0);
        axis6[3] = new double3(-1.0, 0.0, 0.0);
        axis6[4] = new double3(0.0, -1.0, 0.0);
        axis6[5] = new double3(0.0, 0.0, -1.0);

        faces[0] = 1;
        faces[1] = 2;
        faces[2] = 0;

        faces[3] = 1;
        faces[4] = 3;
        faces[5] = 2;

        faces[6] = 1;
        faces[7] = 5;
        faces[8] = 3;

        faces[9] = 1;
        faces[10] = 0;
        faces[11] = 5;
        // y = -1
        faces[12] = 4;
        faces[13] = 0;
        faces[14] = 2;

        faces[15] = 4;
        faces[16] = 2;
        faces[17] = 3;

        faces[18] = 4;
        faces[19] = 3;
        faces[20] = 5;

        faces[21] = 4;
        faces[22] = 5;
        faces[23] = 0;
    }
    private void OnDestroy()
    {
        axis6.Dispose();
        faces.Dispose();
        faces_selectors.Dispose();
    }
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
    static bool triangle_size2cam(double3 p0, double3 p1, double3 p2, TerrainGenParams tgp)
    {
        var center = (p0 + p1 + p2) / 3f;
        var to_center = math.distance(center, tgp.pivot_pos);
        var dist = math.distance(p0, p1) + math.distance(p0, p2);
        dist /= 2f;
        return to_center > dist;

    }
    public struct lfloat
    {
        public long integer_part;
        public float float_part;
        public void average(lfloat v0, lfloat v1)
        {

            double integer_d = (double)v0.integer_part + ((double)(v1.integer_part - v0.integer_part)) / 2.0;
            //integer_d.
            //(v0.x + v1.x)
        }
    }
    public struct lfloat3
    {
        public long x;
        public long y;
        public long z;
        public void average(lfloat3 v0, lfloat3 v1)
        {

            var integer_diff = (v0.x > v1.x) ? v0.x - v1.x : v1.x - v0.x;
            //(v0.x + v1.x)
        }
    }
    static void triangle_divide_mesh(double3 p0, double3 p1, double3 p2, TerrainGenParams tgparams, List<TerrainMeshStates> meshes, ref int mesh_added, int level)
    {
        if (level == 0 || triangle_size2cam(p0, p1, p2, tgparams))
        {
            var job = new NoisesTest.mesh_triangle();
            //job.p0 = tgparams.relative2cell_center(p0);
            //job.p1 = tgparams.relative2cell_center(p1);
            //job.p2 = tgparams.relative2cell_center(p2);

            job.p0 = p0;
            job.p1 = p1;
            job.p2 = p2;
            job.planet_radius = tgparams.planet_radius;
            //var mesh_base_pos = (job.p0 + job.p1 + job.p2) / 3f;
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
                meshes.Add(tms);
            }
            {
                var tmp = meshes[mesh_added];
                tmp.wrotation = quaternion.identity;
                tmp.wposition = new float3((job.p0 + job.p1 + job.p2) / 3.0);
                meshes[mesh_added] = tmp;
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
        double3 q0 = (p0 + p1) / 2.0;
        double3 q1 = (p1 + p2) / 2.0;
        double3 q2 = (p0 + p2) / 2.0;
        q0 = math.normalize(q0) * tgparams.planet_radius;
        q1 = math.normalize(q1) * tgparams.planet_radius;
        q2 = math.normalize(q2) * tgparams.planet_radius;

        triangle_divide_mesh(p0, q0, q2, tgparams, meshes, ref mesh_added, level - 1);
        triangle_divide_mesh(q0, p1, q1, tgparams, meshes, ref mesh_added, level - 1);
        triangle_divide_mesh(q0, q1, q2, tgparams, meshes, ref mesh_added, level - 1);
        triangle_divide_mesh(q2, q1, p2, tgparams, meshes, ref mesh_added, level - 1);
    }
    public static Vector3 double3_vec3(double3 val)
    {
        return new Vector3((float)val.x, (float)val.y, (float)val.z);
    }
    public static double3 vec3_double3(Vector3 val)
    {
        return new double3(val.x, val.y, val.z);
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
        //cam_transform.position = pivot_pos;
        if (refresh)
        {
            if (continuous_refresh == false)
                refresh = false;
            for (int i = 0; i < meshes.Count; i++)
            {
                meshes[i].Clear();

            }

            mesh_gen_count = 0;
            var tgp = new TerrainGenParams();
            tgp.pivot_pos = cam_transform.position;
            tgp.planet_radius = radius;
            tgp.pivot_cell_coord = cell_pos;
            tgp.cell_half_extent = cell_half_extents;
            //double3 cell_center = new double3(cell_pos) * cell_half_extents * 2.0;
            //var cell_center_dir_normalized = math.normalize(cell_center);
            //double3 right_dir = math.cross(cell_center_dir_normalized, new double3(0.0, 1.0, 0.0));
            //var right_mag = math.distance(right_dir, 0.0);
            //if(right_mag > 0.001f)
            //{
            //    right_dir = math.normalize(right_dir);
            //}
            //else
            //{
            //    right_dir = new double3(0.0, 0.0, 1.0);
            //}
            
            int dir_idx = 0;
            double dotted = 0.0;
            var pivot_d3 = vec3_double3(cam_transform.position);
            for (int i = 0; i < 6; ++i)
            {
                var this_dot = math.dot(axis6[i], pivot_d3);
                if(this_dot > dotted)
                {
                    dotted = this_dot;
                    dir_idx = i;
                }
            }

            var faces_selected = faces_selectors[dir_idx];
            for (int i = 0; i < 4; ++i)
            {
                var p0 = axis6[faces[faces_selected[i] * 3]] * radius;
                var p1 = axis6[faces[faces_selected[i] * 3 + 1]] * radius;
                var p2 = axis6[faces[faces_selected[i] * 3 + 2]] * radius;
                //Debug.DrawLine(double3_vec3(p0 * radius), double3_vec3(p1 * radius), Color.green);
                //Debug.DrawLine(double3_vec3(p1 * radius), double3_vec3(p2 * radius), Color.green);
                //Debug.DrawLine(double3_vec3(p0 * radius), double3_vec3(p2 * radius), Color.green);

                //triangle_divide_mesh(tgp.relative2cell_center(p0), tgp.relative2cell_center(p1), tgp.relative2cell_center(p2),
                triangle_divide_mesh(p0, p1, p2,
                    tgp, meshes, ref mesh_gen_count, max_lod);
            }

                //var up_dir = math.normalize(math.cross(right_dir, cell_center_dir_normalized));
                //var up_offset = up_dir * cell_half_extents * 4f;
                //var right_offset = right_dir * cell_half_extents * 4f;
                //Debug.DrawLine(double3_vec3(cell_center), double3_vec3(cell_center + up_offset), Color.red);
                //Debug.DrawLine(double3_vec3(cell_center), double3_vec3(cell_center + right_offset), Color.green);
            //triangle_divide_mesh(three[0].position, three[1].position, three[2].position, tgp,
            //    meshes, ref mesh_gen_count, max_lod);
        }
        for (int i = 0; i < meshes.Count; i++)
        {
            if (meshes[i].mesh.vertexCount > 0)
                //Graphics.DrawMesh(meshes[i].mesh, meshes[i].wposition, meshes[i].wrotation, terrain_mat, 0);
                Graphics.DrawMesh(meshes[i].mesh, meshes[i].wposition, meshes[i].wrotation, terrain_mat, 0);
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
    public float cell_half_extent;
    public float3 relative2cell_center(double3 p)
    {
        p /= cell_half_extent;
        p -= pivot_cell_coord;
        return new float3(p * cell_half_extent);
    }
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