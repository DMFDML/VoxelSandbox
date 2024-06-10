using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{

    private Collider[] hitColliders;

    public float blastRadius;
    public float explosionPower;
    public LayerMask explosionLayers;

    public GameObject particles;

    private void OnCollisionEnter(Collision cols) 
    {
        // particles.gameObject.GetComponent<Renderer>().material = cols.gameObject.GetComponent<MeshRenderer>().material;
        // Instantiate(particles, cols.transform.position, Quaternion.identity);

        Vector3 contact = cols.contacts[0].point;
        Debug.Log(contact);

        destroy(contact);
    }

    void destroy(Vector3 explosionPoint)
    {
        hitColliders = Physics.OverlapSphere(explosionPoint, blastRadius, explosionLayers);

        foreach (Collider hitCol in hitColliders)
        {
            if (hitCol.GetComponent<Rigidbody>() == null)
            {
                hitCol.GetComponent<MeshRenderer>().enabled = true;

                hitCol.gameObject.AddComponent<Rigidbody>();

                hitCol.GetComponent<Rigidbody>().mass = 100;
                hitCol.GetComponent<Rigidbody>().isKinematic = false;
                hitCol.GetComponent<Rigidbody>().velocity = Camera.main.transform.forward * 5;
                hitCol.GetComponent<Rigidbody>().AddExplosionForce(explosionPower, explosionPoint, blastRadius, 1, ForceMode.Impulse);


                // Rigidbody hitColBody = hitCol.GetComponent<Rigidbody>();
                // hitColBody.mass = 500;
                // hitColBody.isKinematic = false;
                // hitColBody.velocity = Camera.main.transform.forward * 5;
                // hitColBody.AddExplosionForce(explosionPower, explosionPoint, blastRadius, 1, ForceMode.Impulse);

            }

        }
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
