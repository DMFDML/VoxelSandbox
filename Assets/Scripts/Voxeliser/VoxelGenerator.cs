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

[HideInInspector] public struct CubePosition
{
    public Vector3Int position;
    public int neighbours; // Remove this if I end up not doing this approach
}

public class VoxelGenerator : MonoBehaviour
{
    public byte[,,] voxelArray;
    Mesh mesh;
    MeshCollider meshCol;
    public int voxelsOnSmallestSide = 10;
    public float voxelScale;
    public int arrayX;
    public int arrayY;
    public int arrayZ;
    // Collider collider;
    public bool hasChanged = false;
    [SerializeField] bool generateOnStart = true; 

    public List<CubePosition> cubePositionsList = new List<CubePosition>();
    public HashSet<CubePosition> cubePositions = new HashSet<CubePosition>();

    // [SerializeField] float fillProbability = 0.05f;

    // #### Runtime Logic ####

    private void Start()
    {
        meshCol = CheckCollider(); // Checks that there's a mesh collider on the object, and destroys others / adds one if not.
        gameObject.tag = "Voxel";
        CheckRigidbody();

        if (generateOnStart)
        {
            GenerateArray();
        }
    }

    private void Update() 
    {
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            GenerateArray();
        }

        // if (Keyboard.current.wKey.wasPressedThisFrame) // DEBUG: Fills with random to test rendering performance
        // {
        //     voxelArray = FillArrayWithRandom(voxelArray);
        //     RetrieveCubesFromArray(voxelArray);

        //     hasChanged = true;
        // }

        
    }

    // #### Public Functions ####
    public void AddCubes(List<Vector3Int> newCubes)
    {
        foreach (Vector3Int cube in newCubes)
        {
            AddCube(cube);
        }
    }

    public void AddCube(Vector3Int cube)
    {
            voxelArray[cube.x, cube.y, cube.z] = 1;
            
            CubePosition voxel = new CubePosition();
            voxel.position = new Vector3Int(cube.x, cube.y, cube.z);

            cubePositions.Add(voxel);

            hasChanged = true;
    }

    public void RemoveCubes(List<Vector3Int> newCubes)
    {

        // UnityEngine.Debug.Log("Removing Cubes: " + newCubes.Count.ToString());
        // UnityEngine.Debug.Log("Array Size: " + (arrayX * arrayY * arrayZ).ToString() );

        foreach (Vector3Int cube in newCubes)
        {
            RemoveCube(cube);
        }

        hasChanged = true;
    }

    public void RemoveCube(Vector3Int cube) // These should check that the requested cubes are actually present, and ignore if not.
    {
            voxelArray[cube.x, cube.y, cube.z] = 0;
            
            CubePosition voxel = new CubePosition();
            voxel.position = cube;
            // voxel.neighbours = CheckNeighbours(cube);

            if (cubePositions.Contains(voxel))
            {
                cubePositions.Remove(voxel);
            }

            // hasChanged = true;   
    }

    // #### Private Functions ####
    
    MeshCollider CheckCollider() // Puts a mesh collider on the object
    {
        if (!this.gameObject.GetComponent<MeshCollider>())
        {
            // Get all colliders attached to the current GameObject
            Collider[] colliders = GetComponents<Collider>();

            // Loop through each collider and destroy it
            foreach (Collider collider in colliders)
            {
                Destroy(collider);
            }

            gameObject.AddComponent<MeshCollider>();

            // MeshCollider col = gameObject.GetComponent<MeshCollider>();

            // return gameObject.GetComponent<MeshCollider>();
        }

        return gameObject.GetComponent<MeshCollider>();
    }

    byte[,,] GenerateArray() // Initial generation of the array. Use UpdateArray() for later changes. Generates the voxel array and retrives the cubes.
    {

        UnityEngine.Debug.Log("Generating Array: " );
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        // Check if a voxel array of the right size is already present
        if (!CheckArrayNeedsUpdate()) {return voxelArray; }

        cubePositions.Clear();        
        mesh = this.GetComponent<MeshFilter>().mesh;
        UnityEngine.Debug.Log("Mesh size: " + mesh.bounds.size.ToString());
        // UnityEngine.Debug.Log("Mesh size magnitude: " + mesh.bounds.size.magnitude.ToString());
        // Vector3 translator = mesh.bounds.extents; // Required as the mesh is centred on the transform, and our voxel array puts the corner at the centre.

        // Generate a voxel array as a Byte[,,] with cubix voxels, with a min number of them on the smallest side.
        voxelArray = GenerateVoxelArray(mesh, voxelsOnSmallestSide);

        // Loop through the voxel array. For each point, transform in index into a coordinate at the centre of the voxel, in the local space of the mesh. 
        // Then check whether that point is inside the mesh.
        // If it is, change the voxel value to 1 and add it to the Hashmap of cubes.
        RetrieveCubesFromMesh(voxelArray, mesh);

        // ###### Messed with the code from here:

        // List<CubePosition> updatedVoxels = RetrieveCubesFromMeshAsList(voxelArray, mesh);

        // for (int i = 0; i < updatedVoxels.Count; i++)
        // {
        //     CubePosition updatedVox = updatedVoxels[i];
        //     updatedVox.neighbours = CheckNeighbours(updatedVox.position);
        //     cubePositions.Add(updatedVox);
        // }

        // // ###### To here
        hasChanged = true;

        // stopwatch.Stop();
        // UnityEngine.Debug.Log("Time to generate: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");

        return voxelArray;
    }

    bool CheckArrayNeedsUpdate() // Checks whether the array exists, and that its size matches the resolution requested
    {
        if (voxelArray != null)
        {
            int[] numbers = {voxelArray.GetLength(0), voxelArray.GetLength(1), voxelArray.GetLength(2)};
            int min = numbers.Min();

            if (min == voxelsOnSmallestSide)
            {
                UnityEngine.Debug.Log("No update required");
                return false;
            }

            return true;
        }

        return true;
    }

    void RetrieveCubesFromMesh(byte[,,] voxelArray, Mesh mesh) // Parses the voxel array and mesh, and turns any point within the mesh into a 1
    {
        cubePositions.Clear();
        
        Vector3 translator = mesh.bounds.extents; // Required as the mesh is centred on the transform, and our voxel array puts the corner at the centre.
        
        arrayX = voxelArray.GetLength(0);
        arrayY = voxelArray.GetLength(1);
        arrayZ = voxelArray.GetLength(2);

        // Iterate through the array elements
        for (int z = 0; z < arrayZ; z++) {
            for (int y = 0; y < arrayY; y++) {
                for (int x = 0; x < arrayX; x++) {
                    
                    // For each element, create a point that represents each voxel within the mesh bounding box. Equivalent to mesh AABB on local transform.
                    Vector3 point = new Vector3(
                        (x * voxelScale) - translator.x + (0.5f * voxelScale), // 
                        (y * voxelScale) - translator.y + (0.5f * voxelScale), 
                        (z * voxelScale) - translator.z + (0.5f * voxelScale));

                    // Transform that point to the world space. I.e. apply the local transform to it.
                    Vector3 pointInWorldSpace = transform.TransformPoint(point);

                    // Check if the corresponding point is inside the mesh and turn it to a 1 if so. Also add a corresponding cube (voxel = 1) to the hashmap.
                    // Will only return true for the 
                    if (meshCol.convex == false && IsPointInsideMeshBackFaces(pointInWorldSpace))
                    {
                        voxelArray[x,y,z] = 1;

                        CubePosition voxel = new CubePosition {position = new Vector3Int(x, y, z) }; 
                        cubePositions.Add(voxel);
                    }
                }
            }
        }
    }

    List<CubePosition> RetrieveCubesFromMeshAsList(byte[,,] voxelArray, Mesh mesh) // Parses the voxel array and mesh, and turns any point within the mesh into a 1
    {
        cubePositions.Clear();

        List<CubePosition> newCubes = new List<CubePosition>();
        
        Vector3 translator = mesh.bounds.extents; // Required as the mesh is centred on the transform, and our voxel array puts the corner at the centre.
        
        arrayX = voxelArray.GetLength(0);
        arrayY = voxelArray.GetLength(1);
        arrayZ = voxelArray.GetLength(2);

        // Iterate through the array elements
        for (int z = 0; z < arrayZ; z++) {
            for (int y = 0; y < arrayY; y++) {
                for (int x = 0; x < arrayX; x++) {
                    
                    // For each element, create a point that represents each voxel within the mesh bounding box. Equivalent to mesh AABB on local transform.
                    Vector3 point = new Vector3(
                        (x * voxelScale) - translator.x + (0.5f * voxelScale), // 
                        (y * voxelScale) - translator.y + (0.5f * voxelScale), 
                        (z * voxelScale) - translator.z + (0.5f * voxelScale));

                    // Transform that point to the world space. I.e. apply the local transform to it.
                    Vector3 pointInWorldSpace = transform.TransformPoint(point);

                    // Check if the corresponding point is inside the mesh and turn it to a 1 if so. Also add a corresponding cube (voxel = 1) to the hashmap.
                    // Will only return true for the 
                    if (IsPointInsideMeshBackFaces(pointInWorldSpace))
                    {
                        voxelArray[x,y,z] = 1;

                        CubePosition voxel = new CubePosition {position = new Vector3Int(x, y, z) }; 
                        newCubes.Add(voxel);
                    }
                }
            }
        }

        return newCubes;
    }
    void RetrieveCubesFromArray(byte[,,] voxelArray) // Parses a voxel array pre-defined with 0s and 1s, and generates the HashMap of existing voxels.
    {
        cubePositions.Clear();
        
        arrayX = voxelArray.GetLength(0);
        arrayY = voxelArray.GetLength(1);
        arrayZ = voxelArray.GetLength(2);

        // Iterate through the array elements
        for (int z = 0; z < arrayZ; z++) {
            for (int y = 0; y < arrayY; y++) {
                for (int x = 0; x < arrayX; x++) {
                    
                    if (voxelArray[x,y,z] == 1)
                    {
                        CubePosition voxel = new CubePosition {position = new Vector3Int(x, y, z) }; 
                        cubePositions.Add(voxel);
                    }
                }
            }
        }
    }

    void SwitchToMeshCollider()
    {
        if (GetComponent<Collider>())
        {
            Destroy(GetComponent<Collider>());
        }

        MeshCollider meshCollider = this.AddComponent<MeshCollider>();
        meshCollider.convex = true;

        meshCollider.sharedMesh = mesh;
    }

    bool IsPointInsideMesh(Vector3 point)
    {
        Collider collider = GetComponent<Collider>(); // Get collider for this gameobject

        Collider[] hits = Physics.OverlapSphere(point, 0.005f); // get all overlapping colliders

        foreach (Collider hitCollider in hits)
        {
            if (hitCollider == collider)
            {
                return true;
            }
        }

        return false;
    }

    bool IsPointInsideMeshRaycast(Vector3 point)
    {
        Collider collider = GetComponent<Collider>(); // Get collider for this gameobject

        // gameObject.layer = LayerMask.NameToLayer("VoxelGen");

        float distance = 2f * mesh.bounds.size.magnitude;
        Vector3 directionTowardsCentre = (transform.position - point).normalized;

        RaycastHit[] hitsOne = Physics.RaycastAll(point, directionTowardsCentre, distance);

        UnityEngine.Debug.DrawRay(point, directionTowardsCentre, Color.red, Mathf.Infinity);

        if (!IsColliderInArray(hitsOne, collider))
        {

            // UnityEngine.Debug.Log("hit");
            // Vector3 hitLocation = hitOne.point;
            Vector3 newPoint = point + directionTowardsCentre * distance;
            Vector3 newDirection = - directionTowardsCentre;

            RaycastHit[] hitsTwo = Physics.RaycastAll(newPoint, newDirection, distance);
            if(IsColliderInArray(hitsTwo, collider))
            {
                // UnityEngine.Debug.DrawRay(newPoint, newDirection, Color.yellow, Mathf.Infinity);
                return true;          
            }
            return false;
        }
        return false;
    }

    bool IsPointInsideMeshBackFaces(Vector3 point)
    {
        Collider col = GetComponent<Collider>(); // Get collider for this gameobject
        
        // var temp = Physics.queriesHitBackfaces; // set to false initially
        Ray ray = new Ray(point, Vector3.left); // creates a ray that will go backwards from the point

        bool hitFrontFace = false; // initialises that it hasn't hit the front
        RaycastHit hit = default; // creates a pointer

        Physics.queriesHitBackfaces = true;
        bool hitFrontOrBackFace = col.Raycast(ray, out RaycastHit hit2, 100f); // Sends out the ray, testing if it's hitting anything at all

        // UnityEngine.Debug.DrawRay(point, Vector3.forward, Color.red, Mathf.Infinity);

        if (hitFrontOrBackFace) // if it hit something on either the front or the back
        {
            Physics.queriesHitBackfaces = false; // tell it to only hit the front
            hitFrontFace = col.Raycast(ray, out hit2, 100f); // try again, with it only hitting the front
        }

        // Physics.queriesHitBackfaces = temp; // set this back to temp, which I believe is always false?

        if (!hitFrontOrBackFace) // if you didn't hit anything at all
        {
            return false; // return false
        }
        else if (!hitFrontFace) // if you didn't hit the front face
        {
            return true; // Didn't hit the front but did hit the back, so must be inside
        }
        else
        {
            if (hit.distance > hit2.distance) // hit the back before the front
            {
                return true; // must be inside
            } 
            else 
            {
                return false;
            }
        }
    }

    public bool IsColliderInArray(RaycastHit[] array, Collider targetcol)
    {
        if (array == null || targetcol == null)
        {
            return false;
        }

        for (int i = 0; i < array.Length; i++)
        {
            if  (array[i].collider == targetcol)
            {
                return true;
            }
        }

        return false;
    }

    // Creates a blank array of square voxels (by mesh size, not scale) with voxPerSide on the smallest side.
    private byte[,,] GenerateVoxelArray(Mesh mesh, int voxPerSide)
    {
        Bounds bounds = mesh.bounds;
        Vector3 meshSize = bounds.size; // Vector3.Scale(bounds.size, transform.localScale);

        voxelScale = FindVoxelScale(mesh.bounds.size, voxPerSide);

        voxelArray = new byte[
            Mathf.RoundToInt(meshSize.x / voxelScale), 
            Mathf.RoundToInt(meshSize.y / voxelScale), 
            Mathf.RoundToInt(meshSize.z / voxelScale)];

        return voxelArray;
    }

    private float FindVoxelScale(Vector3 size, int voxPerSide)
    {

        float voxelScale = size.x;

        if (size.y < voxelScale) 
        {
            voxelScale = size.y ;
        } 
        
        if (size.z < voxelScale)
        {
            voxelScale = size.z  ;
        }

        return voxelScale / voxPerSide;
    }

    // private byte[,,] FillArrayWithRandom(byte[,,] array)
    // {
    //     // Get the dimensions of the array
    //     int width = array.GetLength(0);
    //     int height = array.GetLength(1);
    //     int depth = array.GetLength(2);

    //     byte[,,] newArray = new byte[
    //         width, 
    //         height, 
    //         depth];

    //     // Loop through each element
    //     for (int x = 0; x < width; x++)
    //     {
    //         for (int y = 0; y < height; y++)
    //         {
    //             for (int z = 0; z < depth; z++)
    //             {
    //                 if (Random.value < fillProbability)
    //                 {
    //                     newArray[x, y, z] = 1;
    //                 }
    //             }
    //         }
    //     }

    //     return newArray;
    // }

    private void CheckRigidbody()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        if (!rb)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.isKinematic = true;
        rb.useGravity = false;
    } 

    private int CheckNeighbours(Vector3Int position)
    {
        int neighbourhood = 111111;

        // Format: Left, Right, Down, Up, Back, Front. (i.e. -ve, +ve for x --> y --> z.)

        if (position.x == 0 || voxelArray[position.x - 1, position.y, position.z] == 0) {SetDigitAt(neighbourhood, 5, 0); } // Sets left digit
        if (position.x == arrayX - 1 || voxelArray[position.x + 1, position.y, position.z] == 0) {SetDigitAt(neighbourhood, 4, 0); } // Sets right digit

        if (position.y == 0 || voxelArray[position.x, position.y - 1, position.z] == 0) {SetDigitAt(neighbourhood, 3, 0); } // Sets down digit
        if (position.y == arrayY - 1 || voxelArray[position.x, position.y + 1, position.z] == 0) {SetDigitAt(neighbourhood, 2, 0); } // Sets up digit

        if (position.z == 0 || voxelArray[position.x, position.y, position.z - 1] == 0) {SetDigitAt(neighbourhood, 1, 0); } // Sets back digit
        if (position.z == arrayZ - 1 || voxelArray[position.x, position.y, position.z + 1] == 0) {SetDigitAt(neighbourhood, 0, 0); } // Sets front digit

        return neighbourhood;
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

    public static int SetDigitAt(int number, int position, int newDigit)

    // Important! This starts at the rightmost digit rather than the left when indexing, and uses pos - 1 notation. 
    // I.e. the rightmost digit is digit 0. The right-but-one is digit 1. 
    {
        if (position < 0 || newDigit < 0 || newDigit > 9)
        {
            throw new ArgumentOutOfRangeException("position or newDigit", "Position must be non-negative and newDigit must be between 0 and 9");
        }

        int placeValue = (int)Mathf.Pow(10, position);
        int leftPart = number / (placeValue * 10); // Extract the digits before the target position
        int rightPart = number % placeValue; // Extract the digits after the target position

        return leftPart * (placeValue * 10) + newDigit * placeValue + rightPart;
    }

}
