using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipControlBGTest : MonoBehaviour
{
    // Start is called before the first frame update
    public Mesh mesh;
    public Material mat;
    public const int dim = 10;
    public float spacing = 50f;
    public Matrix4x4[] matrices;

    void Start()
    {
        float step = spacing;
        Vector3 pos = -Vector3.one * dim * spacing / 2f;
        matrices = new Matrix4x4[dim * dim * dim];
        for (int z = 0; z < dim; z++)
        {
            for (int y = 0; y < dim; y++)
            {
                for (int x = 0; x < dim; x++)
                {
                    var tpos = pos + new Vector3(x, y, z) * step;
                    matrices[x + y* dim + z * dim * dim] = Matrix4x4.TRS(tpos, Quaternion.identity, Vector3.one);
                }
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        Graphics.DrawMeshInstanced(mesh, 0, mat, matrices);
    }
}
