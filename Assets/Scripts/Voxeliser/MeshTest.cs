using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshTest : MonoBehaviour
{

    Mesh baseMesh;
    public VoxelRenderManager voxelRenderer;
    MeshFilter meshFilter;

    // Start is called before the first frame update
    void Start()
    {
        meshFilter = this.GetComponent<MeshFilter>();
    }

    // Update is called once per frame
    void Update()
    {
        baseMesh = voxelRenderer.baseMesh;
        meshFilter.mesh = baseMesh;
    }
}
