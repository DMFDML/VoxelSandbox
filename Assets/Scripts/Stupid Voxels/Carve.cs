using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Carve : MonoBehaviour
{

    public GameObject tool;
    public GameObject swarf;
    private Collider[] hitColliders;

    float radius;
        
    public LayerMask carveLayers;

    void Start() 
    {
        radius = (tool.GetComponent<Transform>().localScale.x / 2) + 0.05f;
    }

    private void OnCollisionEnter(Collision col) 
    {
        Destroy(col.gameObject);


    }
}
