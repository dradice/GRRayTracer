using System.Collections;
using System.IO;
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
    public bool captureImage;

    public bool capture = false;

    public string subfolder = "";

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
        RayTraceCameraBHVR cfCam = cf.GetComponent<RayTraceCameraBHVR>();
        captureImage = cfCam.captureImage;

        if(captureImage)
        {
            Debug.Log("capture");
            Capture();
            RestartCameras();
        }
    }

    void RestartCameras()
    {
        GameObject cf = GameObject.Find("CameraF");
        RayTraceCameraBHVR cfCam = cf.GetComponent<RayTraceCameraBHVR>();
        cfCam.startRender = true;
        cfCam.renderComplete = false;

        GameObject cb = GameObject.Find("CameraB");
        RayTraceCameraBHVR cbCam = cb.GetComponent<RayTraceCameraBHVR>();
        cbCam.startRender = true;
        cbCam.renderComplete = false;

        GameObject cl = GameObject.Find("CameraL");
        RayTraceCameraBHVR clCam = cl.GetComponent<RayTraceCameraBHVR>();
        clCam.startRender = true;
        clCam.renderComplete = false;

        GameObject cr = GameObject.Find("CameraR");
        RayTraceCameraBHVR crCam = cr.GetComponent<RayTraceCameraBHVR>();
        crCam.startRender = true;
        crCam.renderComplete = false;

        GameObject cu = GameObject.Find("CameraU");
        RayTraceCameraBHVR cuCam = cu.GetComponent<RayTraceCameraBHVR>();
        cuCam.startRender = true;
        cuCam.renderComplete = false;

        GameObject cd = GameObject.Find("CameraD");
        RayTraceCameraBHVR cdCam = cd.GetComponent<RayTraceCameraBHVR>();
        cdCam.startRender = true;
        cdCam.renderComplete = false;
    }

    void Capture()
    {
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
        Texture2D flippedTex = FlipTexture(tex);
        RenderTexture.active = null;

        byte[] bytes = flippedTex.EncodeToJPG();

        try
        {
            string filename = "360_" + frameCount + ".jpg";

            string fullPath = Application.dataPath + "/PanoramaFrames/";
            fullPath = string.IsNullOrEmpty(subfolder) ? fullPath : fullPath + subfolder + "/";

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            File.WriteAllBytes(fullPath + filename, bytes);
            Debug.Log("File saved.");
        }

        catch
        {
            Debug.LogWarning("ERROR: Failure to save file.");
        }
        frameCount++;
    }

    Texture2D FlipTexture(Texture2D original)
    {
        Texture2D flipped = new Texture2D(original.width, original.height);

        int xN = original.width;
        int yN = original.height;


        for (int i = 0; i < xN; i++)
        {
            for (int j = 0; j < yN; j++)
            {
                flipped.SetPixel(i, yN - j - 1, original.GetPixel(i, j));
            }
        }
        flipped.Apply();

        return flipped;
    }
}
