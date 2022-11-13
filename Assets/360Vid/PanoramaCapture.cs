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

    public int fCount = 0;

    // Start is called before the first frame update
    void Start()
    {
        GameObject cf = GameObject.Find("CameraF");
        RayTraceCameraBHVR cfFrames = cf.GetComponent<RayTraceCameraBHVR>();
        int cfCount = cfFrames.completeFace;
    }

    // Update is called once per frame
    void Update()
    {
        GameObject multiCam = GameObject.Find("360Cam");
        UnifyCameras unify = multiCam.GetComponent<UnifyCameras>();
        int frameTrack = unify.captureFrame;
        bool is360 = unify.is360;
        if(!is360)
        {
            back = null;
            left = null;
            right = null;
            up = null;
            down = null;
        }
        if (frameTrack > fCount)
        {
            Debug.Log(":)");
            fCount = frameTrack;
            if (is360)
            {
                Capture();
            }
        }

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
        RenderTexture.active = null;

        byte[] bytes = tex.EncodeToJPG();

        string path = Application.dataPath + "/360" + "_" + fCount.ToString() + ".jpeg";
        System.IO.File.WriteAllBytes(path, bytes);
    }
}
