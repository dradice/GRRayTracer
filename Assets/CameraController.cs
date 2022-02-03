using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraController : MonoBehaviour
{
    public float panSpeed = 20f;
    public float panBorderThickness = 10f;
    public float rotationSpeed = 5f;
    private float x;
    private float y;

    // Update is called once per frame
    void Update()
    {
  

        Vector3 rotation = transform.eulerAngles;

        Vector3 pos = Camera.main.transform.position;

        x = Input.GetAxis("Mouse Y");
        y = -Input.GetAxis("Mouse X");

        rotation = new Vector3(x, y, 0.0f);

        if (Input.GetKey("w"))
        {
            //pos.z += panSpeed * Time.deltaTime; 
            pos = transform.position + Camera.main.transform.forward * panSpeed * Time.deltaTime;
        }

        if (Input.GetKey("s"))
        {
            pos = transform.position - Camera.main.transform.forward * panSpeed * Time.deltaTime;
        }

        if (Input.GetKey("d"))
        { 
            pos = transform.position + Camera.main.transform.right * panSpeed * Time.deltaTime;
        }

        if (Input.GetKey("a"))
        {
            pos = transform.position - Camera.main.transform.right * panSpeed * Time.deltaTime;

        }

        
      


        transform.position = pos;

        
        transform.eulerAngles = transform.eulerAngles - rotation;
    }
}
