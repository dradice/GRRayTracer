using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnifyCameras : MonoBehaviour
{
    public int facesCount = 0;
    public int captureFrame = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        GameObject cf = GameObject.Find("CameraF");
        RayTraceCameraBHVR cfFrames = cf.GetComponent<RayTraceCameraBHVR>();
        int cfCount = cfFrames.completeFace;

        GameObject cb = GameObject.Find("CameraB");
        RayTraceCameraBHVR cbFrames = cb.GetComponent<RayTraceCameraBHVR>();
        int cbCount = cbFrames.completeFace;

        GameObject cl = GameObject.Find("CameraL");
        RayTraceCameraBHVR clFrames = cl.GetComponent<RayTraceCameraBHVR>();
        int clCount = clFrames.completeFace;

        GameObject cr = GameObject.Find("CameraR");
        RayTraceCameraBHVR crFrames = cr.GetComponent<RayTraceCameraBHVR>();
        int crCount = crFrames.completeFace;

        GameObject cu = GameObject.Find("CameraU");
        RayTraceCameraBHVR cuFrames = cu.GetComponent<RayTraceCameraBHVR>();
        int cuCount = cuFrames.completeFace;

        GameObject cd = GameObject.Find("CameraD");
        RayTraceCameraBHVR cdFrames = cd.GetComponent<RayTraceCameraBHVR>();
        int cdCount = cdFrames.completeFace;

        if((cfCount + cbCount + clCount + crCount + cuCount + cdCount) > facesCount)
        {
            facesCount++;
            if(facesCount % 6 == 0)
            {
                captureFrame++;
            }
        }
    }
}
