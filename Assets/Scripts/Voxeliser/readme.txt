### Voxel Generator ###
Generates a voxel grid representing the mesh attached to the GO.

Attach to any meshed gameobject.
Specify the minimum number of voxels per side in the inspector.
Select whether to run on start. Press F to generate if not turned on.

Public functions:
 - (WIP) Add Cube - adds a single voxel in position Vector3(X,Y,Z)
 - (WIP) Add Cubes - adds multiple voxels from a list of Vector3s
 - Remove Cube - removes a single voxel in position Vector3(X,Y,Z)
 - Remove Cubes - removes multiple voxels in a list of Vector3s

Objects:
Defines the position of a voxel:
public struct CubePosition
{
    public Vector3Int position;
}

Defines the set of all voxels:
HashSet<CubePosition> cubePositions = new HashSet<CubePosition>();

Code Path:
1. Checks that the GO has a mesh collider. Adds one if not. Works better if convex but toleratres non-convex.
2. Checks if an array already exists. Exits if true.
3. Creates an empty voxel array as a byte[,,]. Sized against specified min number of voxels in inspector.
4. Finds which voxels are within the mesh.
4a. Loops through the voxel grid. The grid is translated and scaled so that it can be looped by index, but aligns with mesh and calculated voxel scale.
4b. Tests whether each voxel is within the mesh.
 - For non-convex mesh: Achieved through raycasting. This is compatible with non-convex meshes but isn't the fastest.
 - Mesh should be reasonably high quality. If it's got internal geometry you'll get strange artefacts.
 - For convex mesh: Achieved through Physics.OverlapSphere. Faster.
5. For each detected voxel, creates a CubePosition object.
6. Adds each cube position to a Hashset of all cube positions. This contains all voxels after generation.
7. Flags that the array has changed, which is watched by other scripts.
8. CubePositions is passed to the render manager.

---------------------

### Voxel Render Manager ###
Renders the voxel grid.

Attach to a GO that holds a voxel grid component.
Renders when the Voxel Array is flagged as HasChanged. If UpdateEveryFrame = true, will do this automatically.
Otherwise press G to render. 

Objects:
Defines the direction a face points: 
public enum FaceDirection { Left, Right, Down, Up, Back, Front }

Defines properties for a face, which is then parsed to render:
public struct Face
    {
        public int index;
        public Vector3Int location;
        public FaceDirection direction;
        public List<int> vertexIndices;
    }

Collates faces: 
public List<Face> faces = new List<Face>();

Code Path:
1. Checks if this is the first render, and if so saves the original mesh geometry for retrieval
2. Checks that an array is present via the HashSet of cube positions
3. Initialises an empty mesh.
4. Generates a list of the faces to render.
4a. Runs through the voxels held in the cube positions. 
4b. Looks at each neighbour in the voxelArray, and adds a face if it finds no cube there. 
5. For each face, generates vertices, UVs, and triangles.
6. Scales the vertices to match mesh size. Previously all saved via voxel index.
7. Populates and initialises the mesh.
8. Updates the mesh filter with the new mesh. 
