using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cam : MonoBehaviour
{
    public float mainSpeed = 100.0f; 
    public float maxShift = 1000.0f;

    public float Min_x = 0;
    public float Min_y = 0;
    public float Min_z = 10;


    public float Max_x = 100;
    public float Max_y = 100;
    public float Max_z = 100;
   

    public float camSens = 0.25f; 

    void Update()
    {
        Vector3 p = GetBaseInput();
        if (p.sqrMagnitude > 0)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                p = p * Time.deltaTime* maxShift;

            }
            else
            {
                p = p * Time.deltaTime* mainSpeed;

            }
            transform.Translate(p);
            Vector3 newPosition = transform.position;
            
            if (newPosition.x < Min_x)
            {
                Vector3 teleportPosition = new Vector3(Min_x, transform.position.y, transform.position.z);
                transform.position = teleportPosition;
            }
            if (newPosition.x > Max_x)
            {
                Vector3 teleportPosition = new Vector3(Max_x, transform.position.y, transform.position.z);
                transform.position = teleportPosition;
            }
            if (newPosition.y < Min_y)
            {
                Vector3 teleportPosition = new Vector3(transform.position.x, Min_y, transform.position.z);
                transform.position = teleportPosition;
            }
            if (newPosition.y > Max_y)
            {
                Vector3 teleportPosition = new Vector3(transform.position.x, Max_y, transform.position.z);
                transform.position = teleportPosition;
            }
            if (newPosition.z < Min_z)
            {
                Vector3 teleportPosition = new Vector3(transform.position.x, transform.position.y, Min_z);
                transform.position = teleportPosition;
            }
            if (newPosition.z > Max_z)
            {
                Vector3 teleportPosition = new Vector3(transform.position.x, transform.position.y, Max_z);
                transform.position = teleportPosition;
            }
            

        }
    }

    private Vector3 GetBaseInput()
    {
        Vector3 p_Velocity = new Vector3();
        if (Input.GetKey(KeyCode.W))
        {
            p_Velocity += new Vector3(0, 0, 1);
        }
        if (Input.GetKey(KeyCode.S))
        {
            p_Velocity += new Vector3(0, 0, -1);
        }
        if (Input.GetKey(KeyCode.A))
        {
            p_Velocity += new Vector3(-1, 0, 0);
        }
        if (Input.GetKey(KeyCode.D))
        {
            p_Velocity += new Vector3(1, 0, 0);
        }

        if (Input.GetKey(KeyCode.E))
        {
            transform.rotation *= Quaternion.Euler(0, 5f* Time.deltaTime* mainSpeed, 0);
        }
        if (Input.GetKey(KeyCode.Q))
        {
            transform.rotation *= Quaternion.Euler(0, -5f * Time.deltaTime* mainSpeed, 0);
        }
        
        if (Input.GetAxis("Mouse ScrollWheel")!=0)
        {
            p_Velocity += new Vector3(0, 1* Input.GetAxis("Mouse ScrollWheel")*10, 0);
        }
        return p_Velocity;
    }
}
