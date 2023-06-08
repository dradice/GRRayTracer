using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanoramaCapture : MonoBehaviour
{
    public Camera front;
    public Camera back;
    public Camera left;
    public Camera right;
    public Camera up;
    public Camera down;

    public RenderTexture faceF;
    public RenderTexture faceB;
    public RenderTexture faceL;
    public RenderTexture faceR;
    public RenderTexture faceU;
    public RenderTexture faceD;

    public RenderTexture cubeMapLeft;
    public RenderTexture equirectRT;

    public int completeFaces;
    public int frameCount = 0;

    public int cfCount;
    public int cbCount;
    public int clCount;
    public int crCount;
    public int cuCount;
    public int cdCount;

    public bool capture = false;

    // Start is called before the first frame update
    void Start()
    {
        /*GameObject cf = GameObject.Find("CameraF");
        RayTraceCameraBHVR cfFrames = cf.GetComponent<RayTraceCameraBHVR>();
        int cfCount = cfFrames.completeFace;*/
    }

    // Update is called once per frame
    void Update()
    {
        GameObject cf = GameObject.Find("CameraF");
        RayTraceCameraBHVR cfFrames = cf.GetComponent<RayTraceCameraBHVR>();
        cfCount = cfFrames.faceNumber;

        GameObject cb = GameObject.Find("CameraB");
        RayTraceCameraBHVR cbFrames = cb.GetComponent<RayTraceCameraBHVR>();
        cbCount = cbFrames.faceNumber;

        GameObject cl = GameObject.Find("CameraL");
        RayTraceCameraBHVR clFrames = cl.GetComponent<RayTraceCameraBHVR>();
        clCount = clFrames.faceNumber;

        GameObject cr = GameObject.Find("CameraR");
        RayTraceCameraBHVR crFrames = cr.GetComponent<RayTraceCameraBHVR>();
        crCount = crFrames.faceNumber;

        GameObject cu = GameObject.Find("CameraU");
        RayTraceCameraBHVR cuFrames = cu.GetComponent<RayTraceCameraBHVR>();
        cuCount = cuFrames.faceNumber;

        GameObject cd = GameObject.Find("CameraD");
        RayTraceCameraBHVR cdFrames = cd.GetComponent<RayTraceCameraBHVR>();
        cdCount = cdFrames.faceNumber;

        completeFaces = cfCount + cbCount + clCount + crCount + cuCount + cdCount;
        Debug.Log("Complete Faces Panorama Capture = " + completeFaces);
        Debug.Log("f = " + cfCount);
        Debug.Log("b = " + cbCount);
        Debug.Log("l = " + clCount);
        Debug.Log("r = " + crCount);
        Debug.Log("u = " + cuCount);
        Debug.Log("d = " + cdCount);

        capture = cfFrames.capture;

        if (completeFaces == 6)
        {
            Debug.Log("six faces");
            completeFaces = 0;
            Debug.Log("Panorama Capture " + completeFaces);
            Capture();
        }
    }

    void Capture()
    {
        Debug.Log("Capturing");
        Graphics.CopyTexture(faceR, 0, cubeMapLeft, 0);
        Graphics.CopyTexture(faceL, 0, cubeMapLeft, 1);
        Graphics.CopyTexture(faceD, 0, cubeMapLeft, 2);
        Graphics.CopyTexture(faceU, 0, cubeMapLeft, 3);
        Graphics.CopyTexture(faceF, 0, cubeMapLeft, 4);
        Graphics.CopyTexture(faceB, 0, cubeMapLeft, 5);

        cubeMapLeft.ConvertToEquirect(equirectRT, Camera.MonoOrStereoscopicEye.Mono);


        Save(equirectRT);
    }

    public void Save(RenderTexture rt)
    {
        Texture2D tex = new Texture2D(rt.width, rt.height);

        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        RenderTexture.active = null;

        byte[] bytes = tex.EncodeToJPG();

        string path = Application.dataPath + "/360" + "_" + frameCount.ToString() + ".jpeg";
        System.IO.File.WriteAllBytes(path, bytes);
        frameCount++;
    }
}
