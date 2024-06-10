using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.Diagnostics;

public class VoxelInteractor : MonoBehaviour
{

    public List<GameObject> collidingObjects;
    public enum InteractorType { Add, Remove }
    [SerializeField] InteractorType type;
    GameObject interactorCentreObj;
    Mesh interactorMesh;

    [SerializeField] Vector3Int interactorCentreVoxelsIndex;

    public struct VoxelModel 
    {
        public GameObject voxelGameobject;
        public VoxelGenerator voxelGenerator;
        public byte[,,] voxelArray;
        public Mesh baseMesh;
        public GameObject interactorCentre;
    }

    List<VoxelModel> voxelModels = new List<VoxelModel>();

    private void OnTriggerEnter(Collider col) 
    {
        if (col.gameObject.tag == "Voxel" && !collidingObjects.Contains(col.gameObject))
        {
            collidingObjects.Add(col.gameObject);

            // Puts a placeholder object on the voxel model that gets used for transform manipulations
            interactorCentreObj = new GameObject(gameObject.name.ToString() + " Centre");
            interactorCentreObj.transform.parent = col.gameObject.transform;

            // Creates a reference to the important parts of the voxel gameobject
            VoxelModel voxelModel = new VoxelModel
            {
                voxelGameobject = col.gameObject,
                voxelGenerator = col.gameObject.GetComponent<VoxelGenerator>(),
                voxelArray = col.gameObject.GetComponent<VoxelGenerator>().voxelArray,
                baseMesh = col.gameObject.GetComponent<MeshFilter>().mesh,
                interactorCentre = interactorCentreObj
            };

            voxelModels.Add(voxelModel);

        }
    }

    private void Start() 
    {
        interactorMesh = GetComponent<MeshFilter>().mesh;
    }

    // Update is called once per frame
    void Update()
    {

        if (transform.hasChanged)
        {
            foreach (VoxelModel voxModel in voxelModels)
            {
                List<Vector3Int> voxelsToUpdate = GetVoxelModelIndices(voxModel);

                if (voxelsToUpdate.Count > 0) { voxModel.voxelGenerator.RemoveCubes(voxelsToUpdate); }
                voxelsToUpdate.Clear();

            }

            transform.hasChanged = false;
        }
    }

    List<Vector3Int> GetVoxelModelIndices(VoxelModel voxModel)
    {
        Stopwatch stopwatch = new Stopwatch();
        
        float voxelScale = voxModel.voxelGenerator.voxelScale;

        Vector3 translator = voxModel.baseMesh.bounds.extents;

        Vector3 interactorScale = transform.localScale;
        float interactorRadius = interactorMesh.bounds.extents.x * interactorScale.x;

        // UnityEngine.Debug.Log("Interactor Radius: " + interactorRadius.ToString());

        voxModel.interactorCentre.transform.position = transform.position;

        Vector3 interactorCentreVoxels = voxModel.interactorCentre.transform.localPosition + translator;

        interactorCentreVoxelsIndex = new Vector3Int(
                                                        Mathf.RoundToInt(interactorCentreVoxels.x / voxelScale),
                                                        Mathf.RoundToInt(interactorCentreVoxels.y / voxelScale),
                                                        Mathf.RoundToInt(interactorCentreVoxels.z / voxelScale)
                                                        ); // Scales the position into indices


        // Count the number of indices that make up the radius of the interactor.
        // That defines the maximum area that we'd need to search.
        // int radiusInVoxels = Mathf.RoundToInt(interactorRadius / voxelScale * voxModel.voxelGameobject.transform.localScale.x);
        int radiusInVoxels = Mathf.RoundToInt(interactorRadius / (voxelScale * voxModel.voxelGameobject.transform.localScale.x));

        UnityEngine.Debug.Log("Radius in voxels: " + radiusInVoxels.ToString());

        List<Vector3Int> voxelsToUpdate = new List<Vector3Int>();

        // Get the sizes of the array
        int arrayX = voxModel.voxelArray.GetLength(0);
        int arrayY = voxModel.voxelArray.GetLength(1);
        int arrayZ = voxModel.voxelArray.GetLength(2);

        stopwatch.Start();

        // voxelsToUpdate = DrawSphere(radiusInVoxels, interactorCentreVoxelsIndex.x, interactorCentreVoxelsIndex.y, interactorCentreVoxelsIndex.z);

        // Run through the array throughout the search area
        // This cuts off if the search area would go above or below the range of the voxel array
        int minX = Mathf.Max(interactorCentreVoxelsIndex.x - radiusInVoxels, 0);
        int maxX = Mathf.Min(interactorCentreVoxelsIndex.x + radiusInVoxels + 1, arrayX);
        int minY = Mathf.Max(interactorCentreVoxelsIndex.y - radiusInVoxels, 0);
        int maxY = Mathf.Min(interactorCentreVoxelsIndex.y + radiusInVoxels + 1, arrayY);
        int minZ = Mathf.Max(interactorCentreVoxelsIndex.z - radiusInVoxels, 0);
        int maxZ = Mathf.Min(interactorCentreVoxelsIndex.z + radiusInVoxels + 1, arrayZ);

        for (int i = minX; i < maxX; i++)
        {
            for (int j = minY; j < maxY; j++)
            {
                for (int k = minZ; k < maxZ; k++)
                {
                    switch (type)
                    {
                        case InteractorType.Remove:
                            if (voxModel.voxelArray[i,j,k] == 0)
                            {
                                continue;
                            }
                            break;

                        case InteractorType.Add:
                            if (voxModel.voxelArray[i,j,k] == 1)
                            {
                                continue;
                            }
                            break;
                    }

                    // if a point is found that is within the search area and voxel array, take its index position and real position in the array local space
                    Vector3Int positionIndex = new Vector3Int(i, j, k);
                    Vector3 position = new Vector3(i * voxelScale, j * voxelScale, k * voxelScale);

                    // float dist = Vector3.Dot(position, interactorCentreVoxels);
                    // float radius = (interactorRadius / voxelScale) * (interactorRadius / voxelScale);

                    // UnityEngine.Debug.Log("Dot Distance: " + dist.ToString());
                    // UnityEngine.Debug.Log("RadSq: " + radius.ToString());

                    // if (dist < radius)
                    // {
                    //     voxelsToUpdate.Add(positionIndex);
                    // }
                    
                    // Check that its within a radius distance of the interactor centre, and add it if it is
                    if (Vector3.Distance(interactorCentreVoxels, position) < interactorRadius / voxModel.voxelGameobject.transform.localScale.x)
                    {
                        voxelsToUpdate.Add(positionIndex);
                    }    
                }
            }
        }       
        
        stopwatch.Stop();
        // UnityEngine.Debug.Log("Time to calculate: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");

        return voxelsToUpdate;

    }

    int ReturnArrayLength(int arrayDefault, int position, int distance)
    {
        int min = 0;
        int max = arrayDefault;

        if (position - distance > min)
        {
            min = position - distance;
        }
        if (position + distance < max)
        {
            max = position + distance;
        }

        return max - min;
    }

    private List<Vector3Int> DrawSphere(float radius, int posX, int posY, int posZ)
    {
        // determines how far apart the pixels are
        float density = 1;

        List<Vector3Int> voxelsToAdd = new List<Vector3Int>();

        for (float i = 0; i < 90; i += density)
        {
            float x1 = radius * Mathf.Cos(i * Mathf.PI / 180);
            float y1 = radius * Mathf.Sin(i * Mathf.PI / 180);

            for (float j = 0; j < 45; j += density)
            {
                float x2 = x1 * Mathf.Cos(j * Mathf.PI / 180);
                float y2 = x1 * Mathf.Sin(j * Mathf.PI / 180);
                
                int x = (int)Mathf.Round(x2) + posX;
                int y = (int)Mathf.Round(y1) + posY;
                int z = (int)Mathf.Round(y2) + posZ;             

                voxelsToAdd.Add(new Vector3Int(x, y, z));
                voxelsToAdd.Add(new Vector3Int(x, y, -z));
                voxelsToAdd.Add(new Vector3Int(-x, y, z));
                voxelsToAdd.Add(new Vector3Int(-x, y, z));

                voxelsToAdd.Add(new Vector3Int(z, y, x));
                voxelsToAdd.Add(new Vector3Int(z, y, -x));
                voxelsToAdd.Add(new Vector3Int(-z, y, x));
                voxelsToAdd.Add(new Vector3Int(-z, y, -x));

                voxelsToAdd.Add(new Vector3Int(x, -y, z));
                voxelsToAdd.Add(new Vector3Int(x, -y, -z));
                voxelsToAdd.Add(new Vector3Int(-x, -y, z));
                voxelsToAdd.Add(new Vector3Int(-x, -y, -z));

                voxelsToAdd.Add(new Vector3Int(z, -y, x));
                voxelsToAdd.Add(new Vector3Int(z, -y, -x));
                voxelsToAdd.Add(new Vector3Int(-z, -y, x));
                voxelsToAdd.Add(new Vector3Int(-z, -y, -x));
            }
        }

        return voxelsToAdd;
    }
}
