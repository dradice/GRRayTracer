using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnifyCameras : MonoBehaviour
{
    public int facesCount = 0;
    public int captureFrame = 0;
    public bool is360 = false;
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
        bool is360f = cfFrames.is360;

        GameObject cb = GameObject.Find("CameraB");
        RayTraceCameraBHVR cbFrames = cb.GetComponent<RayTraceCameraBHVR>();
        int cbCount = cbFrames.completeFace;
        bool is360b = cfFrames.is360;

        GameObject cl = GameObject.Find("CameraL");
        RayTraceCameraBHVR clFrames = cl.GetComponent<RayTraceCameraBHVR>();
        int clCount = clFrames.completeFace;
        bool is360l = cfFrames.is360;

        GameObject cr = GameObject.Find("CameraR");
        RayTraceCameraBHVR crFrames = cr.GetComponent<RayTraceCameraBHVR>();
        int crCount = crFrames.completeFace;
        bool is360r = cfFrames.is360;

        GameObject cu = GameObject.Find("CameraU");
        RayTraceCameraBHVR cuFrames = cu.GetComponent<RayTraceCameraBHVR>();
        int cuCount = cuFrames.completeFace;
        bool is360u = cfFrames.is360;

        GameObject cd = GameObject.Find("CameraD");
        RayTraceCameraBHVR cdFrames = cd.GetComponent<RayTraceCameraBHVR>();
        int cdCount = cdFrames.completeFace;
        bool is360d = cfFrames.is360;

        if(is360f || is360b || is360l || is360r || is360u || is360d)
        {
            is360 = true;
        }    

        if (!is360 && (cfCount > facesCount || cbCount > facesCount || clCount > facesCount || crCount > facesCount || cuCount > facesCount || cdCount > facesCount))
        {
           
            facesCount++;
            captureFrame++;
        }

        if (is360 && (cfCount + cbCount + clCount + crCount + cuCount + cdCount) > facesCount)
        {
            facesCount++;
            if(facesCount % 6 == 0)
            {
                captureFrame++;
            }
        }
    }
}
