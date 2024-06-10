using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Shoot : MonoBehaviour
{

    public GameObject projectile;
    public Transform point;
    public int speed;

    // Update is called once per frame
    void Update()
    {

        var mouse = Mouse.current;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            GameObject bul = (GameObject)Instantiate(projectile, point.transform.position, Quaternion.identity);
            bul.gameObject.GetComponent<Rigidbody>().velocity = Camera.main.transform.forward * speed;
        }
    }
}
