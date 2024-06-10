using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class VoxelInteractionManager : MonoBehaviour
{

    float voxelScale;
    VoxelGenerator voxelGenerator;
    GameObject interactorCentreObj;
    public Mesh baseMesh;
    byte[,,] voxelArray;

    // public GameObject centreB;


    public List<GameObject> collidingInteractors;

    private void OnTriggerEnter(Collider col) 
    {
        if (col.gameObject.tag == "Tool" && !collidingInteractors.Contains(col.gameObject))
        {
            collidingInteractors.Add(col.gameObject);
            interactorCentreObj = new GameObject(col.gameObject.name.ToString() + " Centre");
            interactorCentreObj.transform.parent = transform;
        }
    }

    // private void OnTriggerExit(Collider col) 
    // {

    //     Debug.Log("Trigger exit");
    //     if (collidingInteractors.Contains(col.gameObject)) // This isn't ever leaving right now
    //     {
            
    //         GameObject interactorCentreObj = transform.Find(col.gameObject.name.ToString() + " Centre").gameObject;
    //         Destroy(interactorCentreObj);
    //         collidingInteractors.Remove(col.gameObject);
    //     }
    // }

    private void Start() 
    {
        voxelGenerator = GetComponent<VoxelGenerator>();
        voxelArray = voxelGenerator.voxelArray;
        baseMesh = GetComponent<MeshFilter>().mesh;
    }

    private void Update() 
    {
        foreach (GameObject interactor in collidingInteractors) // I'd like to get this out of update. Or so it reads from a hasChanged flag. Maybe from the interactor transform.
        {
            List<Vector3Int> voxelsToUpdate = new List<Vector3Int>();

            voxelsToUpdate = GetInteractorPoints(interactor); // This only needs to run if the interactor has moved.

            // if (interactor.transform.hasChanged)
            // {
            //     voxelsToUpdate = GetInteractorPoints(interactor); // This only needs to run if the interactor has moved.
            //     interactor.transform.hasChanged = false;
            // }           

            if (voxelsToUpdate.Count > 0) { voxelGenerator.RemoveCubes(voxelsToUpdate); }
            voxelsToUpdate.Clear();
        }

        
    }

    // private bool CheckTriggerExit(GameObject interactor)
    // {
       
    //     Vector3 interactorCentre = interactor.transform.position;
    //     Vector3 interactorScale = interactor.transform.localScale;
    //     Vector3 translator = baseMesh.bounds.extents;

    //     if (interactorCentre.x + translator.x + interactorScale.x < transform.position.x ||
    //         interactorCentre.x - translator.x - interactorScale.x > transform.position.x ||
    //         interactorCentre.y + translator.y + interactorScale.y < transform.position.y ||
    //         interactorCentre.y - translator.y - interactorScale.y > transform.position.y ||
    //         interactorCentre.z + translator.z + interactorScale.z < transform.position.z ||
    //         interactorCentre.z + translator.z - interactorScale.z > transform.position.z

    //         )
    //         {
    //             return true;
    //         }
    //     return false;
    // }


    #region // Public Functions  
    List<Vector3Int> GetInteractorPoints(GameObject interactor) // For Spherical
    {
        UnityEngine.Debug.Log("Interacting with: " + gameObject.name);
        // interactorCentreObj = transform.Find(interactor.name + " Centre").gameObject;
        
        // Get the scale of the voxels and a reference to the voxelArray
        voxelScale = voxelGenerator.voxelScale;
        voxelArray = voxelGenerator.voxelArray;

        // Get the offset for the voxel array from the mesh used to generate it. As the mesh is centred on the transform, this is just the mesh halfsize. 
        Vector3 translator = baseMesh.bounds.extents;

        // Get a reference to the mesh for the interactor
        Mesh mesh = interactor.GetComponent<MeshFilter>().mesh; // Pull this outside the function.
        
        // Radius, scale, centre of the interactor in its own local space
        Vector3 interactorScale = interactor.transform.localScale;
        float interactorRadius = mesh.bounds.extents.x * interactorScale.x;
        Vector3 interactorCentre = interactor.transform.position;

        // Put the gameobject we're using as the mirror for the interactor in the centre of the interactor
        interactorCentreObj.transform.position = interactorCentre;

        // Adjust the interactor centre position to the voxel array local space
        Vector3 interactorCentreVoxels = interactorCentreObj.transform.localPosition + translator; // Gets the position in the byte array local space

        // Find the index in the voxel array that this corresponds to
        Vector3Int interactorCentreVoxelsIndex = new Vector3Int(
                                                        Mathf.RoundToInt(interactorCentreVoxels.x / voxelScale),
                                                        Mathf.RoundToInt(interactorCentreVoxels.y / voxelScale),
                                                        Mathf.RoundToInt(interactorCentreVoxels.z / voxelScale)
                                                        ); // Scales the position into indices

        // Count the number of indices that make up the radius of the interactor.
        // That defines the maximum area that we'd need to search.
        int radiusInVoxels = Mathf.RoundToInt(interactorRadius / voxelScale * transform.localScale.x);

        // Create an empty list that will contain the points we want to change
        List<Vector3Int> voxelsToUpdate = new List<Vector3Int>();

        // Get the sizes of the array
        int arrayX = voxelArray.GetLength(0);
        int arrayY = voxelArray.GetLength(1);
        int arrayZ = voxelArray.GetLength(2);

        // Run through the array throughout the search area
        // This cuts off if the search area would go above or below the range of the voxel array
        for (int i = interactorCentreVoxelsIndex.x - radiusInVoxels; i < interactorCentreVoxelsIndex.x + radiusInVoxels + 1; i++)
        {
            if (i >= 0 && i < arrayX)
            {
                for (int j = interactorCentreVoxelsIndex.y - radiusInVoxels; j < interactorCentreVoxelsIndex.y + radiusInVoxels + 1; j++)
                {
                    if (j >= 0 && j < arrayY)
                    {
                        for (int k = interactorCentreVoxelsIndex.z - radiusInVoxels; k < interactorCentreVoxelsIndex.z + radiusInVoxels + 1; k++)
                        {
                            if (k >= 0 && k < arrayZ)
                            {
                                // if a point is found that is within the search area and voxel array, take its index position and real position in the array local space
                                Vector3Int positionIndex = new Vector3Int(i, j, k);
                                Vector3 position = new Vector3(i * voxelScale, j * voxelScale, k * voxelScale);
                                
                                // Check that its within a radius distance of the interactor centre, and add it if it is
                                if (Vector3.Distance(interactorCentreVoxels, position) < interactorRadius / transform.localScale.x)
                                {
                                    voxelsToUpdate.Add(positionIndex);
                                }                                
                            }
                        }
                    }
                }
            }
        }

        // UnityEngine.Debug.Log("Positions Found: " + voxelsToUpdate.Count.ToString());

        

        // foreach (Vector3Int pos in voxelsToUpdate)
        // {
        //     UnityEngine.Debug.Log(pos.ToString());
        // }

        return voxelsToUpdate;

        // Run the add and remove logic
        
    }

    List<Vector3Int> GetInteractorPointsBox(GameObject interactor) // For box shaped interactor
    {
        UnityEngine.Debug.Log("Interacting with: " + gameObject.name);
        GameObject interactorCentreObj = transform.Find(interactor.name + " Centre").gameObject;
        
        // Get the scale of the voxels and a reference to the voxelArray
        voxelScale = voxelGenerator.voxelScale;
        voxelArray = voxelGenerator.voxelArray;

        // Get the offset for the voxel array from the mesh used to generate it. As the mesh is centred on the transform, this is just the mesh halfsize. 
        Vector3 translator = baseMesh.bounds.extents;

        // Get a reference to the mesh for the interactor
        Mesh mesh = interactor.GetComponent<MeshFilter>().mesh; // Pull this outside the function.
        
        // Radius, scale, centre of the interactor in its own local space
        Vector3 interactorScale = interactor.transform.localScale;
        float interactorRadius = Mathf.Max(mesh.bounds.extents.x * interactorScale.x, 
                                            mesh.bounds.extents.y * interactorScale.y, 
                                            mesh.bounds.extents.z * interactorScale.z);
        Vector3 interactorExtent = Vector3.Scale(mesh.bounds.extents, interactorScale);
        Vector3 interactorCentre = interactor.transform.position;

        // Put the gameobject we're using as the mirror for the interactor in the centre of the interactor
        interactorCentreObj.transform.position = interactorCentre;

        // Adjust the interactor centre position to the voxel array local space
        Vector3 interactorCentreVoxels = interactorCentreObj.transform.localPosition + translator; // Gets the position in the byte array local space

        // Find the index in the voxel array that this corresponds to
        Vector3Int interactorCentreVoxelsIndex = new Vector3Int(
                                                        Mathf.RoundToInt(interactorCentreVoxels.x / voxelScale),
                                                        Mathf.RoundToInt(interactorCentreVoxels.y / voxelScale),
                                                        Mathf.RoundToInt(interactorCentreVoxels.z / voxelScale)
                                                        ); // Scales the position into indices

        // Get the number of voxels that make up the extent in each of its directions
        // iterate through the space. compare the distance between 

        // Get a radius that denotes the max sphere enclosing the interactor box
        int radiusInVoxels = Mathf.RoundToInt(interactorRadius / voxelScale * transform.localScale.x);

        // iterate through the radius space
        // Get the position of the voxels within the radius in the local space of the interactor
        // If that position is lower than the extent.max or greater than extent.min in each dimension, it's within the box 


        // Count the number of indices that make up the radius of the interactor.
        // That defines the maximum area that we'd need to search.
        Vector3Int extentInVoxels = new Vector3Int(     Mathf.RoundToInt(interactorExtent.x / voxelScale * transform.localScale.x),
                                                        Mathf.RoundToInt(interactorExtent.x / voxelScale * transform.localScale.x),
                                                        Mathf.RoundToInt(interactorExtent.x / voxelScale * transform.localScale.x)
                                                        ); // Scales the position into indices

        // Create an empty list that will contain the points we want to change
        List<Vector3Int> voxelsToUpdate = new List<Vector3Int>();

        // Get the sizes of the array
        int arrayX = voxelArray.GetLength(0);
        int arrayY = voxelArray.GetLength(1);
        int arrayZ = voxelArray.GetLength(2);

        // Run through the array throughout the search area
        // This cuts off if the search area would go above or below the range of the voxel array
        for (int i = interactorCentreVoxelsIndex.x - radiusInVoxels; i < interactorCentreVoxelsIndex.x + radiusInVoxels + 1; i++)
        {
            if (i >= 0 && i < arrayX)
            {
                for (int j = interactorCentreVoxelsIndex.y - radiusInVoxels; j < interactorCentreVoxelsIndex.y + radiusInVoxels + 1; j++)
                {
                    if (j >= 0 && j < arrayY)
                    {
                        for (int k = interactorCentreVoxelsIndex.z - radiusInVoxels; k < interactorCentreVoxelsIndex.z + radiusInVoxels + 1; k++)
                        {
                            if (k >= 0 && k < arrayZ)
                            {
                                // if a point is found that is within the search area and voxel array, take its index position and real position in the array local space
                                Vector3Int positionIndex = new Vector3Int(i,j,k);
                                Vector3 position = new Vector3(i * voxelScale, j * voxelScale, k * voxelScale);
                                
                                // Check that its within a radius distance of the interactor centre, and add it if it is
                                if (Vector3.Distance(interactorCentreVoxels, position) < interactorRadius / transform.localScale.x)
                                {
                                    voxelsToUpdate.Add(positionIndex);
                                }                                
                            }
                        }
                    }
                }
            }
        }

        // UnityEngine.Debug.Log("Positions Found: " + voxelsToUpdate.Count.ToString());

        

        // foreach (Vector3Int pos in voxelsToUpdate)
        // {
        //     UnityEngine.Debug.Log(pos.ToString());
        // }

        return voxelsToUpdate;

        // Run the add and remove logic
        
    }

    #endregion

}
