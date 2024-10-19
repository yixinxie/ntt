using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class PlanetaryTerrain : MonoBehaviour
{
    public Transform[] three;
    public float cell_half_extents = 200f;
    public ushort max_lod = 4;
    public int mesh_gen_count = 0;
    public bool refresh;
    public bool continuous_refresh;
    public List<TerrainMeshStates> meshes = new List<TerrainMeshStates>(8);
    public Material terrain_mat;
    public Transform cam_transform; // pivot transform ref

    public Transform prev_pivot;
    public Transform this_pivot;
    public int3 cell_pos; // cell index inside a planet's volume, if the range is just the planet.
    public float radius = 200000f;
    public float height = 5f;
    public bool draw_dbg_triangle;
    public float draw_dbg_offset;
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
        if (three != null && three.Length == 3 && draw_dbg_triangle)
        {
            for (int i = 0; i < three.Length; i++)
            {
                if (three[i] == null)
                {
                    return;
                }
            }
            Gizmos.DrawLine(three[0].position, three[1].position);
            Gizmos.DrawLine(three[2].position, three[1].position);
            Gizmos.DrawLine(three[0].position, three[2].position);
            //NativeArray<double3> tmp = default;
            //NoisesTest.mesh_triangle.half_fill(vec3_double3(three[0].position), vec3_double3(three[1].position), vec3_double3(three[2].position),
            //    4, ref tmp, 1);
            //for(int i = 0; i < tmp.Length; ++i)
            //{
            //    Gizmos.DrawLine(double3_vec3(tmp[i]), double3_vec3(tmp[i]) + Vector3.up * 5f);
            //}
            //tmp.Dispose();

        }
        
        {
            //lodcmd_test = false;
            var tgp = new TerrainGenParams();
            tgp.planet_radius = radius;
            tgp.cell_half_extents = 0.5f;
            tgp.pivot_pos = prev_pivot.position;
            var patches_prev = new NativeList<TerrainLODInfo>(4, Allocator.Temp);
            patches_prev.Add(default);
            triangle_divide_pass0_lod(0, vec3_double3(three[0].position), vec3_double3(three[1].position), vec3_double3(three[2].position), tgp, patches_prev, max_lod);
            tgp.pivot_pos = this_pivot.position;
            var patches_this = new NativeList<TerrainLODInfo>(4, Allocator.Temp);
            patches_this.Add(default);
            triangle_divide_pass0_lod(0, vec3_double3(three[0].position), vec3_double3(three[1].position), vec3_double3(three[2].position), tgp, patches_this, max_lod);

            NativeList<TerrainPatchGenCmd> gen_list = new NativeList<TerrainPatchGenCmd>(4, Allocator.Temp);
            NativeList<TerrainPatchClearCmd> clear_list = new NativeList<TerrainPatchClearCmd>(4, Allocator.Temp);
            compare_trees(patches_prev, 0, patches_this, 0, gen_list, clear_list);
            if (lodcmd_test)
            {
                lodcmd_test = false;
                var sbuilder = new StringBuilder();
                sbuilder.AppendLine("gen:");
                for (int i = 0; i < gen_list.Length; ++i)
                {

                    sbuilder.AppendLineFormat("{0}", gen_list[i].index);
                }
                Debug.Log(sbuilder.ToString());
            }
            
            // current state
            Gizmos.color = Color.green;
            var ofs = Vector3.forward * -draw_dbg_offset;
            for (int i = 0; i < patches_this.Length; i++)
            {
                var patch = patches_this[i];
                var up_ofs = new Vector3(0, i, 0) * 10f;
                if (patch.expanded == 0)
                {
                    Gizmos.DrawLine(ofs + double3_vec3(patch.p0) + up_ofs, ofs + double3_vec3(patch.p1) + up_ofs);
                    Gizmos.DrawLine(ofs + double3_vec3(patch.p0) + up_ofs, ofs + double3_vec3(patch.p2) + up_ofs);
                    Gizmos.DrawLine(ofs + double3_vec3(patch.p2) + up_ofs, ofs + double3_vec3(patch.p1) + up_ofs);
                }
            }

            // previous state
            Gizmos.color = Color.magenta;
            ofs = Vector3.forward * draw_dbg_offset;
            for (int i = 0; i < patches_prev.Length; i++)
            {
                var patch = patches_prev[i];
                var up_ofs = new Vector3(0, i, 0) * 10f;
                if (patch.expanded == 0)
                {
                    Gizmos.DrawLine(ofs + double3_vec3(patch.p0) + up_ofs, ofs + double3_vec3(patch.p1) + up_ofs);
                    Gizmos.DrawLine(ofs + double3_vec3(patch.p0) + up_ofs, ofs + double3_vec3(patch.p2) + up_ofs);
                    Gizmos.DrawLine(ofs + double3_vec3(patch.p2) + up_ofs, ofs + double3_vec3(patch.p1) + up_ofs);
                }
            }

            // add list
            Gizmos.color = Color.blue;
            ofs = Vector3.right * draw_dbg_offset;
            for (int i = 0; i < gen_list.Length; i++)
            {
                var patch = gen_list[i];
                var up_ofs = new Vector3(0, i, 0)* 10f;
                Gizmos.DrawLine(ofs + double3_vec3(patch.p0) + up_ofs, ofs + double3_vec3(patch.p1) + up_ofs);
                Gizmos.DrawLine(ofs + double3_vec3(patch.p0) + up_ofs, ofs + double3_vec3(patch.p2) + up_ofs);
                Gizmos.DrawLine(ofs + double3_vec3(patch.p2) + up_ofs, ofs + double3_vec3(patch.p1) + up_ofs);
            }
            // draw bounding triangle
            Gizmos.color = Color.gray;
            ofs += Vector3.down * 10f;
            Gizmos.DrawLine(three[0].position + ofs, three[1].position + ofs);
            Gizmos.DrawLine(three[2].position + ofs, three[1].position + ofs);
            Gizmos.DrawLine(three[0].position + ofs, three[2].position + ofs);

            // removal list
            Gizmos.color = Color.red;
            ofs = -Vector3.right * draw_dbg_offset;
            for (int i = 0; i < clear_list.Length; i++)
            {
                var patch_index = clear_list[i];
                var patch = patches_prev[patch_index.index];
                var up_ofs = new Vector3(0, i, 0) * 10f;
                Gizmos.DrawLine(ofs + double3_vec3(patch.p0) + up_ofs, ofs + double3_vec3(patch.p1) + up_ofs);
                Gizmos.DrawLine(ofs + double3_vec3(patch.p0) + up_ofs, ofs + double3_vec3(patch.p2) + up_ofs);
                Gizmos.DrawLine(ofs + double3_vec3(patch.p2) + up_ofs, ofs + double3_vec3(patch.p1) + up_ofs);
            }
            // draw bounding triangle
            Gizmos.color = Color.gray;
            ofs += Vector3.down * 10f;
            Gizmos.DrawLine(three[0].position + ofs, three[1].position + ofs);
            Gizmos.DrawLine(three[2].position + ofs, three[1].position + ofs);
            Gizmos.DrawLine(three[0].position + ofs, three[2].position + ofs);

        }
    }
    public struct TerrainPatchGenCmd
    {
        public ushort index;
        public byte lod_edges;
        public double3 p0, p1, p2;
    }
    public struct TerrainPatchClearCmd
    {
        public ushort index;
    }
    public bool lodcmd_test;
    void recursive_patch_remove(NativeList<TerrainLODInfo> patch_tree, int index, NativeList<TerrainPatchClearCmd> clear_cmds)
    {
        var cur_patch = patch_tree[index];
        if (cur_patch.expanded == 0)
        {
            var cmd = new TerrainPatchClearCmd();
            cmd.index = (ushort)index;
            clear_cmds.Add(cmd);
            return;
        }
        recursive_patch_remove(patch_tree, cur_patch.child_indices[0], clear_cmds);
        recursive_patch_remove(patch_tree, cur_patch.child_indices[1], clear_cmds);
        recursive_patch_remove(patch_tree, cur_patch.child_indices[2], clear_cmds);
        recursive_patch_remove(patch_tree, cur_patch.child_indices[3], clear_cmds);
    }
    void recursive_patch_gen(NativeList<TerrainLODInfo> patch_tree, int index, NativeList<TerrainPatchGenCmd> gen_cmds)
    {
        var cur_patch = patch_tree[index];
        if (cur_patch.expanded == 0)
        {
            var cmd = new TerrainPatchGenCmd();
            cmd.index = (ushort)index;
            cmd.p0 = cur_patch.p0;
            cmd.p1 = cur_patch.p1;
            cmd.p2 = cur_patch.p2;
            gen_cmds.Add(cmd);
            return;
        }
        recursive_patch_gen(patch_tree, cur_patch.child_indices[0], gen_cmds);
        recursive_patch_gen(patch_tree, cur_patch.child_indices[1], gen_cmds);
        recursive_patch_gen(patch_tree, cur_patch.child_indices[2], gen_cmds);
        recursive_patch_gen(patch_tree, cur_patch.child_indices[3], gen_cmds);
    }
    // execute after comparison
    // the return value is the composition of the lowest lod level on each of the three edges.
    public static int3 lod_edge_scan0(NativeList<TerrainLODInfo> expected_patches, int expected_index, int lod_expected)
    {
        var this_patch = expected_patches[expected_index];
        if (this_patch.expanded == 0)
        { 
            return new int3(this_patch.lod, this_patch.lod, this_patch.lod);
        }
        var child0 = expected_patches[this_patch.child_indices[0]];
        var child1 = expected_patches[this_patch.child_indices[1]];
        var child2 = expected_patches[this_patch.child_indices[2]];
        var child3 = expected_patches[this_patch.child_indices[3]];
        if (child2.expanded == 1)
        {
            if (child0.expanded == 0)
            {

            }
            if (child1.expanded == 0)
            {

            }
            if (child3.expanded == 0)
            {

            }
        }
        else
        {
            if (child0.expanded == 1)
            {

            }
            if (child1.expanded == 1)
            {

            }
            if (child3.expanded == 1)
            {

            }
        }
        var r0 = lod_edge_scan0(expected_patches, this_patch.child_indices[0], lod_expected);
        var r1 = lod_edge_scan0(expected_patches, this_patch.child_indices[1], lod_expected);
        var r2 = lod_edge_scan0(expected_patches, this_patch.child_indices[2], lod_expected);
        var r3 = lod_edge_scan0(expected_patches, this_patch.child_indices[3], lod_expected);
        return math.min(math.min(r0, r1), math.min(r2, r3));

    }
    void compare_trees(NativeList<TerrainLODInfo> prev_patches, int prev_index, NativeList<TerrainLODInfo> expected_patches, int expected_index, 
        NativeList<TerrainPatchGenCmd> gen_list
        , NativeList<TerrainPatchClearCmd> clear_list)
    {
        //if (index >= expected_patches.Length) 
        //    return;

        //if (index >= prev_patches.Length) 
        //    return;
        var this_patch = expected_patches[expected_index];
        var prev_patch = prev_patches[prev_index];
        if (prev_patch.expanded != this_patch.expanded)
        {
            if (this_patch.expanded == 0)
            {
                // contraction
                var tpgen = new TerrainPatchGenCmd();
                tpgen.index = (ushort)expected_index;
                tpgen.p0 = this_patch.p0;
                tpgen.p1 = this_patch.p1;
                tpgen.p2 = this_patch.p2;
                gen_list.Add(tpgen);

                recursive_patch_remove(prev_patches, prev_index, clear_list);
                return;
            }
            else
            {
                var tpremove = new TerrainPatchClearCmd();
                tpremove.index = (ushort)prev_index;
                clear_list.Add(tpremove);
                // expansion
                recursive_patch_gen(expected_patches, expected_index, gen_list);
                return;
            }
        }
        else if (this_patch.expanded == 1)
        {
            compare_trees(prev_patches, prev_patch.child_indices[0], expected_patches, this_patch.child_indices[0], gen_list, clear_list);
            compare_trees(prev_patches, prev_patch.child_indices[1], expected_patches, this_patch.child_indices[1], gen_list, clear_list);
            compare_trees(prev_patches, prev_patch.child_indices[2], expected_patches, this_patch.child_indices[2], gen_list, clear_list);
            compare_trees(prev_patches, prev_patch.child_indices[3], expected_patches, this_patch.child_indices[3], gen_list, clear_list);
        }
    }
    static bool triangle_size2cam(double3 p0, double3 p1, double3 p2, TerrainGenParams tgp)
    {
        var center = (p0 + p1 + p2) / 3f;
        var to_center = math.distance(center, vec3_double3(calc_planet_pos(tgp.pivot_pos, tgp.pivot_cell_coord, tgp.cell_half_extents)));
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
    public struct TerrainLODInfo
    {
        public int4 child_indices;
        public byte expanded;
        public ushort lod;
        public byte lod_edges; // edge flags
        public double3 p0, p1, p2;
    }
    
    public static void triangle_divide_pass0_lod(int index, double3 p0, double3 p1, double3 p2, TerrainGenParams tgparams, NativeList<TerrainLODInfo> patch_list, ushort level)
    {
        var patch_info = patch_list[index];
        patch_info.lod = level;
        patch_info.p0 = p0;
        patch_info.p1 = p1;
        patch_info.p2 = p2;
        if (level == 0 || triangle_size2cam(p0, p1, p2, tgparams))
        {
            patch_info.child_indices = new int4(-1, -1, -1, -1);
            patch_list[index] = patch_info;
            return;
        }
        patch_info.expanded = 1;
        double3 q0 = (p0 + p1) / 2.0;
        double3 q1 = (p1 + p2) / 2.0;
        double3 q2 = (p0 + p2) / 2.0;
        //q0 = math.normalize(q0) * tgparams.planet_radius;
        //q1 = math.normalize(q1) * tgparams.planet_radius;
        //q2 = math.normalize(q2) * tgparams.planet_radius;

        patch_list.AddReplicate(default, 4);
        patch_info.child_indices = new int4(
            patch_list.Length - 4,
            patch_list.Length - 3,
            patch_list.Length - 2,
            patch_list.Length - 1);
        patch_list[index] = patch_info;
        var child_indices = patch_info.child_indices;

        ushort one_level_lower = (ushort)(level - 1);
        triangle_divide_pass0_lod(child_indices.x, p0, q0, q2, tgparams, patch_list, one_level_lower);
        triangle_divide_pass0_lod(child_indices.y, q0, p1, q1, tgparams, patch_list, one_level_lower);
        triangle_divide_pass0_lod(child_indices.z, q0, q1, q2, tgparams, patch_list, one_level_lower);
        triangle_divide_pass0_lod(child_indices.w, q2, q1, p2, tgparams, patch_list, one_level_lower);
    }
    static void triangle_divide_mesh(double3 p0, double3 p1, double3 p2, TerrainGenParams tgparams, List<TerrainMeshStates> meshes, ref int mesh_added, int level)
    {
        if (level == 0 || triangle_size2cam(p0, p1, p2, tgparams))
        {
            var job = new NoisesTest.mesh_triangle();
            //job.p0 = tgparams.relative2cell_center(p0);
            //job.p1 = tgparams.relative2cell_center(p1);
            //job.p2 = tgparams.relative2cell_center(p2);
            //unsafe{
            //    job.tgp = &tgparams;
            //}
            job.p0 = p0;
            job.p1 = p1;
            job.p2 = p2;
            job.planet_radius = tgparams.planet_radius;
            //var mesh_base_pos = (job.p0 + job.p1 + job.p2) / 3f;
            job.resolution = 4;
            job.starting_freq = 0.03f;
            job.intensity = tgparams.noise_height;
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
                tmp.wposition = tgparams.relative2cell_center((job.p0 + job.p1 + job.p2) / 3.0);
                //tmp.wposition = -new float3((job.p0 + job.p1 + job.p2) / 3.0);
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
    public int dbg_face_select;
    static Vector3 calc_planet_pos(Vector3 local_pos, int3 cell_coord, float _cell_half_extent)
    {
        return local_pos + new Vector3(cell_coord.x, cell_coord.y, cell_coord.z) * _cell_half_extent * 2.0f;
    }
    void refresh_lods()
    {
        for (int i = 0; i < meshes.Count; i++)
        {
            meshes[i].Clear();

        }

        mesh_gen_count = 0;
        var tgp = new TerrainGenParams();
        tgp.pivot_pos = cam_transform.position;
        tgp.planet_radius = radius;
        tgp.pivot_cell_coord = cell_pos;
        tgp.cell_half_extents = cell_half_extents;
        tgp.noise_height = height;


        int dir_idx = 0;
        double dotted = 0.0;
        var abspos = calc_planet_pos(cam_transform.position, cell_pos, cell_half_extents);
        var pivot_d3 = vec3_double3(abspos);
        for (int i = 0; i < 6; ++i)
        {
            var this_dot = math.dot(axis6[i], pivot_d3);
            if (this_dot > dotted)
            {
                dotted = this_dot;
                dir_idx = i;
            }
        }
        dbg_face_select = dir_idx;

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
            cam_transform.position = pivot_pos;
            refresh_lods();
        }
        if (refresh)
        {
            if (continuous_refresh == false)
                refresh = false;
            refresh_lods();

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
    public float cell_half_extents;
    public float noise_height;
    public float3 relative2cell_center(double3 p)
    {
        var ret = p - new double3(pivot_cell_coord.x, pivot_cell_coord.y, pivot_cell_coord.z) * cell_half_extents * 2f;
        return (float3)ret;
        //p /= cell_half_extents * 2f;

        //p -= pivot_cell_coord;
        //return new float3(p * cell_half_extents * 2f);
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