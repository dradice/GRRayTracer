using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCam360 : MonoBehaviour
{
    public Vector3 cameraPosition = new Vector3(0.0f, 0.0f, 0.0f);
    // Start is called before the first frame update
    void Start()
    {
        Vector3 pos = Camera.main.transform.position;
      
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 pos = Camera.main.transform.position;
        pos = GameObject.Find("360Cam").transform.position;
        transform.position = pos;
        cameraPosition = transform.position;
    }
}
