using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class VoxelRenderManager : MonoBehaviour
{

    VoxelGenerator voxelGenerator;
    Mesh mesh;
    public Mesh baseMesh;

    public byte[,,] voxelArray;
    public float voxelScale = 1;
    public int arrayX;
    public int arrayY;
    public int arrayZ;

    [SerializeField] bool updateEveryFrame = true;

    public enum FaceDirection { Left, Right, Down, Up, Back, Front }

    public HashSet<CubePosition> cubePositions = new HashSet<CubePosition>();
    // public List<CubePosition> cubePositions = new List<CubePosition>();

    // [HideInInspector] public List<CubePosition> cubePositions = new List<CubePosition>();

    int faceIndex = 0;
    [HideInInspector] public struct Face
    {
        public int index;
        public Vector3Int location;
        public FaceDirection direction;
        public List<int> vertexIndices;
    }

    public struct FaceGroup 
    { 
        public List<int> indices;
    }

    [HideInInspector] public List<Face> faces = new List<Face>();

    // ####### Runtime Logic #########
    private void Start()
    {
        voxelGenerator = this.GetComponent<VoxelGenerator>();  
        
    }

    private void Update() 
    {
        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            GenerateVoxelMesh();

            voxelGenerator.hasChanged = false;
        }

        if (updateEveryFrame)
        {
            RecalculateMesh();
        }
    }

    // ########## Public Functions ##########

    public void GenerateVoxelMesh()
    {
        // UnityEngine.Debug.Log("Rendering: " );
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        // Checks to see if the model is already rendered as voxels. If a baseMesh is stored, then it has.
        // Returns either the mesh that's showing, or the stored mesh.
        baseMesh = CheckBaseMesh();
        baseMesh.name = "Original Mesh: " + gameObject.name;
        Vector3 extent = baseMesh.bounds.extents;
                
        // Retrieve variables from the array generator
        voxelArray = voxelGenerator.voxelArray;
        voxelScale = voxelGenerator.voxelScale;
        cubePositions = voxelGenerator.cubePositions;

        if (cubePositions.Count == 0)
        {
            UnityEngine.Debug.Log("No voxel array found. You probably need to generate the array first");
            return;
        }

        // Create lists to store the new mesh
        List<int> newTriangles = new List<int>();
        List<Vector3> newVertices = new List<Vector3>();
        List<Vector2> newUVs = new List<Vector2>();

        // Get array dimensions. Used to control face identification
        arrayX = voxelArray.GetLength(0);
        arrayY = voxelArray.GetLength(1);
        arrayZ = voxelArray.GetLength(2);

        // Get the faces for the voxels
        faces.Clear(); // This is my current fix for the increasing render time. But as I've gone to the trouble of creating them, feels like I shouldn't need to do this.
        // The challenge is that I can easily only add if new, but it's hard to remove if ones have been taken out. Unknown unknowns.      
        RetrieveFaces(cubePositions); // This could be optimised, or even run from the array script. It could run through and change the faces rather than recreating, and then generate verticies afterwards.

        // Generate vertices, triangles, and UVs for each face.
        foreach (Face face in faces)
        {
            CreateFaceVertices(face, newVertices, extent); // This one is different as it can either just create new vertices, or check for duplicates and only add if new.
            newUVs.AddRange(GetUVs());
            newTriangles.AddRange(CreateTriangles(face));
        }

        ScaleVertices(newVertices, extent);

        // Change the lists into arrays and update the mesh
        Vector3[] finalVertices = newVertices.ToArray();
        int[] finaltriangles = newTriangles.ToArray();
        Vector2[] finalUVs = newUVs.ToArray();

        Mesh newMesh = new Mesh();
        newMesh.name = "Voxel Mesh: " + gameObject.name;
        newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; 

        newMesh.vertices = finalVertices;
        newMesh.triangles = finaltriangles;
        newMesh.uv = finalUVs;
        newMesh.RecalculateNormals();

        gameObject.GetComponent<MeshFilter>().mesh = newMesh;
        // gameObject.GetComponent<MeshCollider>().sharedMesh = newMesh;

        // UnityEngine.Debug.Log("Voxels found: " + cubePositions.Count.ToString());
        // UnityEngine.Debug.Log("Faces found: " + faces.Count.ToString());

        stopwatch.Stop();
        UnityEngine.Debug.Log("Time to render: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");

    }

    // ########## Private Functions #########

    void RecalculateMesh()
    {
        if (voxelGenerator.hasChanged)
        {
            GenerateVoxelMesh();
        }

        voxelGenerator.hasChanged = false;

        
    }

    Mesh CheckBaseMesh()
    {
        if (!baseMesh)
        {
            Mesh mesh = this.GetComponent<MeshFilter>().mesh;
            return mesh;
        }

        return baseMesh;
    }
    
    Face CreateFace(Vector3Int location, FaceDirection faceDirection)
    {
        Face face = new Face 
        {
            index = faceIndex,
            location = location,
            direction = faceDirection,
            vertexIndices = new List<int>()
        };

        faceIndex++;

        return face;
    }

    void RetrieveFaces(List<CubePosition> cubePositions)
    {       
        foreach (CubePosition cube in cubePositions)
        {

            // List<int> vertexIndices = new List<int>(); 

            if (cube.position.x == 0) { faces.Add(CreateFace(cube.position, FaceDirection.Left)); } 
            else if (voxelArray[cube.position.x - 1, cube.position.y, cube.position.z] != 1) { faces.Add(CreateFace(cube.position, FaceDirection.Left));  }

            if (cube.position.x == arrayX - 1) { faces.Add(CreateFace(cube.position, FaceDirection.Right)); } 
            else if (voxelArray[cube.position.x + 1, cube.position.y, cube.position.z] != 1) { faces.Add(CreateFace(cube.position, FaceDirection.Right));  }

            if (cube.position.y == 0) { faces.Add(CreateFace(cube.position, FaceDirection.Down)); }
            else if (voxelArray[cube.position.x, cube.position.y - 1, cube.position.z] != 1) { faces.Add(CreateFace(cube.position, FaceDirection.Down)); }

            if (cube.position.y == arrayY - 1) { faces.Add(CreateFace(cube.position, FaceDirection.Up)); }
            else if (voxelArray[cube.position.x, cube.position.y + 1, cube.position.z] != 1) { faces.Add(CreateFace(cube.position, FaceDirection.Up)); }

            if (cube.position.z == 0) { faces.Add(CreateFace(cube.position, FaceDirection.Back)); }
            else if (voxelArray[cube.position.x, cube.position.y, cube.position.z - 1] != 1) { faces.Add(CreateFace(cube.position, FaceDirection.Back)); }

            if (cube.position.z == arrayZ - 1) { faces.Add(CreateFace(cube.position, FaceDirection.Front)); }
            else if (voxelArray[cube.position.x, cube.position.y, cube.position.z +1] != 1) { faces.Add(CreateFace(cube.position, FaceDirection.Front)); }

        }
    }

    void RetrieveFaces(HashSet<CubePosition> cubePositions)
    {       
        foreach (CubePosition cube in cubePositions)
        {
            if (cube.position.x == 0) { faces.Add(CreateFace(cube.position, FaceDirection.Left)); } 
            else if (voxelArray[cube.position.x - 1, cube.position.y, cube.position.z] != 1) { faces.Add(CreateFace(cube.position, FaceDirection.Left));  }

            if (cube.position.x == arrayX - 1) { faces.Add(CreateFace(cube.position, FaceDirection.Right)); } 
            else if (voxelArray[cube.position.x + 1, cube.position.y, cube.position.z] != 1) { faces.Add(CreateFace(cube.position, FaceDirection.Right));  }

            if (cube.position.y == 0) { faces.Add(CreateFace(cube.position, FaceDirection.Down)); }
            else if (voxelArray[cube.position.x, cube.position.y - 1, cube.position.z] != 1) { faces.Add(CreateFace(cube.position, FaceDirection.Down)); }

            if (cube.position.y == arrayY - 1) { faces.Add(CreateFace(cube.position, FaceDirection.Up)); }
            else if (voxelArray[cube.position.x, cube.position.y + 1, cube.position.z] != 1) { faces.Add(CreateFace(cube.position, FaceDirection.Up)); }

            if (cube.position.z == 0) { faces.Add(CreateFace(cube.position, FaceDirection.Back)); }
            else if (voxelArray[cube.position.x, cube.position.y, cube.position.z - 1] != 1) { faces.Add(CreateFace(cube.position, FaceDirection.Back)); }

            if (cube.position.z == arrayZ - 1) { faces.Add(CreateFace(cube.position, FaceDirection.Front)); }
            else if (voxelArray[cube.position.x, cube.position.y, cube.position.z +1] != 1) { faces.Add(CreateFace(cube.position, FaceDirection.Front)); }
        }
    }

    // void RetrieveFacesFromInt(HashSet<CubePosition> cubePositions)
    // {
            // I thought this would save time but it's WAAAYY slower. Literally 30x. Which I think is because the math it runs on each face add is large.
            // If I had bitwise operations it would probably be faster using bitmasking

    //     // Format: Left, Right, Down, Up, Back, Front. (i.e. -ve, +ve for x --> y --> z.)

    //     foreach (CubePosition cube in cubePositions)
    //     {
    //         if (GetDigitAt(cube.neighbours, 5) == 1) { faces.Add(CreateFace(cube.position, FaceDirection.Left)); }
    //         if (GetDigitAt(cube.neighbours, 4) == 1) { faces.Add(CreateFace(cube.position, FaceDirection.Right)); }
    //         if (GetDigitAt(cube.neighbours, 3) == 1) { faces.Add(CreateFace(cube.position, FaceDirection.Down)); }
    //         if (GetDigitAt(cube.neighbours, 2) == 1) { faces.Add(CreateFace(cube.position, FaceDirection.Up)); }
    //         if (GetDigitAt(cube.neighbours, 1) == 1) { faces.Add(CreateFace(cube.position, FaceDirection.Back)); }
    //         if (GetDigitAt(cube.neighbours, 0) == 1) { faces.Add(CreateFace(cube.position, FaceDirection.Front)); }
    //     }
    // }
    
    List<Vector3> GetVertexLocations(Face face)
    {
        List<Vector3> vertices = new List<Vector3>();

        switch (face.direction)
        {           
            case FaceDirection.Left:
                vertices.Add( new Vector3(face.location.x - 0.5f, face.location.y + 0.5f, face.location.z + 0.5f)); // Top Left
                vertices.Add( new Vector3(face.location.x - 0.5f, face.location.y + 0.5f, face.location.z - 0.5f)); // Top Right
                vertices.Add( new Vector3(face.location.x - 0.5f, face.location.y - 0.5f, face.location.z + 0.5f)); // Bottom Left
                vertices.Add( new Vector3(face.location.x - 0.5f, face.location.y - 0.5f, face.location.z - 0.5f)); // Bottom Right
                break;
            case FaceDirection.Right: // Note: corner order swapped to reverse triangles, which reverses normals for rendering
                vertices.Add( new Vector3(face.location.x + 0.5f, face.location.y + 0.5f, face.location.z - 0.5f)); // Top Right
                vertices.Add( new Vector3(face.location.x + 0.5f, face.location.y + 0.5f, face.location.z + 0.5f)); // Top Left
                vertices.Add( new Vector3(face.location.x + 0.5f, face.location.y - 0.5f, face.location.z - 0.5f)); // Bottom Right
                vertices.Add( new Vector3(face.location.x + 0.5f, face.location.y - 0.5f, face.location.z + 0.5f)); // Bottom Left
                break;
            case FaceDirection.Down: // Note: corner order swapped to reverse triangles, which reverses normals for rendering
                vertices.Add( new Vector3(face.location.x + 0.5f, face.location.y - 0.5f, face.location.z - 0.5f)); // Top Right
                vertices.Add( new Vector3(face.location.x + 0.5f, face.location.y - 0.5f, face.location.z + 0.5f)); // Top Left      
                vertices.Add( new Vector3(face.location.x - 0.5f, face.location.y - 0.5f, face.location.z - 0.5f)); // Bottom Right
                vertices.Add( new Vector3(face.location.x - 0.5f, face.location.y - 0.5f, face.location.z + 0.5f)); // Bottom Left
                break;  
            case FaceDirection.Up:
                vertices.Add( new Vector3(face.location.x + 0.5f, face.location.y + 0.5f, face.location.z + 0.5f)); // Top Left
                vertices.Add( new Vector3(face.location.x + 0.5f, face.location.y + 0.5f, face.location.z - 0.5f)); // Top Right
                vertices.Add( new Vector3(face.location.x - 0.5f, face.location.y + 0.5f, face.location.z + 0.5f)); // Bottom Left
                vertices.Add( new Vector3(face.location.x - 0.5f, face.location.y + 0.5f, face.location.z - 0.5f)); // Bottom Right
                break;  
            case FaceDirection.Back:
                vertices.Add( new Vector3(face.location.x - 0.5f, face.location.y + 0.5f, face.location.z - 0.5f)); // Top Left
                vertices.Add( new Vector3(face.location.x + 0.5f, face.location.y + 0.5f, face.location.z - 0.5f)); // Top Right
                vertices.Add( new Vector3(face.location.x - 0.5f, face.location.y - 0.5f, face.location.z - 0.5f)); // Bottom Left
                vertices.Add( new Vector3(face.location.x + 0.5f, face.location.y - 0.5f, face.location.z - 0.5f)); // Bottom Right
                break;  
            case FaceDirection.Front: // Note: corner order swapped to reverse triangles, which reverses normals for rendering
                vertices.Add( new Vector3(face.location.x + 0.5f, face.location.y + 0.5f, face.location.z + 0.5f)); // Top Right
                vertices.Add( new Vector3(face.location.x - 0.5f, face.location.y + 0.5f, face.location.z + 0.5f)); // Top Left
                vertices.Add( new Vector3(face.location.x + 0.5f, face.location.y - 0.5f, face.location.z + 0.5f)); // Bottom Right
                vertices.Add( new Vector3(face.location.x - 0.5f, face.location.y - 0.5f, face.location.z + 0.5f)); // Bottom Left
                break;  
        }

        return vertices;
    }

    void ScaleVertices(List<Vector3> vertices, Vector3 extents)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] = vertices[i] * voxelScale; // Scales it by the scaling factor for the voxel array
            vertices[i] = vertices[i] - extents; // Moves the vertex by the halfsize of the mesh. Mesh is centred on the transform, voxels are cornered on the transform. This moves it from centre to corner.
        }
    }
    
    int CheckAndUpdateVertices(List<Vector3> vertices, Vector3 faceVertex)
    {
        // This version checks for and removes duplicates. It makes teh uv mapping hard.
        // int vertexIndex = vertices.IndexOf(faceVertex);

        // if ( vertexIndex == -1 )
        // {   
        //     vertices.Add(faceVertex);
        //     return vertices.Count - 1;
        // }

        // return vertexIndex;

        // This code doesn't check for or remove duplicates. It creates way more vertices but makes uvs easy.
        // It's also WAAAAY faster computationally. The check above becomes huge when there's loads of vertices.
        vertices.Add(faceVertex);
        return vertices.Count - 1;
        
    }

    void CreateFaceVertices(Face face, List<Vector3> vertices, Vector3 extents)
    {
        List<Vector3> faceVertices = GetVertexLocations(face); // Gets the coordinates of the vertex. Unscaled space.

        foreach (Vector3 vertex in faceVertices)
        {
            int vertexIndex = CheckAndUpdateVertices(vertices, vertex); // Adds the vertex to the list of new vertices
            face.vertexIndices.Add(vertexIndex); // records the index for the vertex to add to triangles
        }
    }
    
    List<Vector2> GetUVs()
    {
        List<Vector2> faceUVs = new List<Vector2>();

        faceUVs.Add(new Vector2(0,0));
        faceUVs.Add(new Vector2(0,1));
        faceUVs.Add(new Vector2(1,1));
        faceUVs.Add(new Vector2(1,0));

        return faceUVs;


    }

    List<int> CreateTriangles(Face face)
    {
        List<int> faceTriangles = new List<int>();

        faceTriangles.Add(face.vertexIndices[0]);
        faceTriangles.Add(face.vertexIndices[1]);
        faceTriangles.Add(face.vertexIndices[2]);
        faceTriangles.Add(face.vertexIndices[2]);
        faceTriangles.Add(face.vertexIndices[1]);
        faceTriangles.Add(face.vertexIndices[3]);

        return faceTriangles;
    }

    public static int GetDigitAt(int number, int position)
    {
        if (position < 0)
        {
            throw new ArgumentOutOfRangeException("position", "Position cannot be negative");
        }

        int digitPosition = (int)Mathf.Pow(10, position); // Calculate the place value of the digit
        return (number / digitPosition) % 10; // Extract the digit at the specified position
    }

    void GroupFaces()
    {
        // For each face direction integer.
        // Lets look at right/left first.
        // If there isn't anything to the left or the front, it might be the start of a face.
        // Go through the voxels to the right, and check their values. If they also have a face on the front, then add to the group of faces.
        // With the slowness of the maths this might take too long... 
    }
}
