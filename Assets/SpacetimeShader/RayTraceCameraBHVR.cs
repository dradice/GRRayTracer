using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class RayTraceCameraBHVR : MonoBehaviour
{
#pragma shader_feature STEREO_CUBEMAP_RENDER

    [Header("Shaders")]
    public ComputeShader cameraVectorShader;
    public ComputeShader rayUpdateShader;

    [Header("Textures")]
    public Texture skyboxTexture;

    [Header("Step Size Parameters")]
    public float timeStep = 0.001f;
    public float escapeDistance = 10000f;
    public float errorTolerance = 0.001f;

    [Header("Renderer Settings")]
    public int numFrames = 1000000;
    public float framesPerSecond = 30f;
    public float updateInterval = 1.0f;
    public int overSample = 4;
    public int maxSoftPasses = 5000;
    public int maxPasses = 100000;
    public string subfolder = "";
    public bool exitOnComplete = false;

    [Header("Physical Parameters")]
    public float diskRadius = 3.0f;

    private Camera _camera;
    private RenderTexture _position;
    private RenderTexture _direction;
    private RenderTexture _color;
    private RenderTexture _isComplete;
    private RenderTexture _timeStep;
    private RenderTexture _errorTolerance;

    public Vector4 _momentum;

    public enum CameraState { Single, Panorama};
    public CameraState cameraState = CameraState.Single;
    public int faceNumber = 0;

    // Private variables
    private float
        startTime = 0f,
        checkTimer = 0f,
        numThreads = 8f,
        coordinateTime = 0f;

    public int currentPass = 0;
    public int currentFrame = 0;

    public bool fComplete = false;
    public bool bComplete = false;
    public bool lComplete = false;
    public bool rComplete = false;
    public bool uComplete = false;
    public bool dComplete = false;

    public bool render = false;

    public bool captureImage = false;

    private Vector2Int
        lastCheck = new Vector2Int(0, 0);

    public bool startRender = true;
    private bool hardCheck = false;

    //public variables
    public bool renderComplete = false;
    public int completeFacesRayTraceCameraBHVR;
    public bool capture = false;
    public int completeCheck = 0;
    public int totalChecks;

    private void Awake()
    {
        // Get Camera component
        _camera = GetComponent<Camera>();
        

    }

    private void InitRenderTextures()
    {
        SetupTexture(ref _position, RenderTextureFormat.ARGBFloat, overSample * Screen.width, overSample * Screen.height);
        SetupTexture(ref _direction, RenderTextureFormat.ARGBFloat, overSample * Screen.width, overSample * Screen.height);
        SetupTexture(ref _color, RenderTextureFormat.ARGBFloat, overSample * Screen.width, overSample * Screen.height);
        SetupTexture(ref _isComplete, RenderTextureFormat.RInt, overSample * Screen.width, overSample * Screen.height);
        SetupTexture(ref _timeStep, RenderTextureFormat.RFloat, overSample * Screen.width, overSample * Screen.height);
        SetupTexture(ref _errorTolerance, RenderTextureFormat.RFloat, overSample * Screen.width, overSample * Screen.height);
    }

    private void SetupTexture(ref RenderTexture texture, RenderTextureFormat format, int width, int height)
    {

        if (texture == null || texture.width != width || texture.height != height)
        {
            // Release render texture if we already have one
            if (texture != null)
                texture.Release();

            // Get a render target for Ray Tracing
            texture = new RenderTexture(width, height, 0,
                format, RenderTextureReadWrite.Linear);
            texture.enableRandomWrite = true;
            texture.Create();
        }

        // Set boundary method to clamp
        texture.wrapMode = TextureWrapMode.Clamp;
    }

    private void SetShaderParameters()
    {
        // Pre-convert camera position to spherical
        Vector3 cart = _camera.transform.position;

        // Send camera vectors and matrices to initialization shader
        cameraVectorShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        cameraVectorShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        cameraVectorShader.SetVector("_CameraPositionCartesian", cart);
        cameraVectorShader.SetVector("_Momentum", _momentum);
        cameraVectorShader.SetFloat("timeStep", timeStep);
        cameraVectorShader.SetFloat("errorTolerance", errorTolerance);

        // Send render parameters to update shader
        rayUpdateShader.SetTexture(0, "_SkyboxTexture", skyboxTexture);
        rayUpdateShader.SetFloat("escapeDistance", escapeDistance);
        rayUpdateShader.SetFloat("time", coordinateTime);
        rayUpdateShader.SetFloat("diskRadius", diskRadius);

    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(_color, destination);
    }

    // Update is called once per frame
    void Update()
    {
        GameObject multiCam = GameObject.Find("360Cam");
        Freefall freefall = multiCam.GetComponent<Freefall>();
        float[] momentum = freefall.momentum;
        _momentum[0] = 1.0f;
        _momentum[1] = 0.0f;
        _momentum[2] = 0.0f;
        _momentum[3] = 0.0f;

        if (renderComplete)
        {
            CheckComplete(true);
            return;
        }

        /*if (cameraState == CameraState.Panorama)
        {
            CheckFaces();
        }*/

        /*if(completeFacesRayTraceCameraBHVR == 6 && totalChecks == 6)
        {
            OnComplete();
        }*/

        if (startRender)
        {
            //Reset variables
            startTime = Time.realtimeSinceStartup;
            lastCheck = Vector2Int.zero;
            startRender = false;
            renderComplete = false;
            currentPass = 0;
            currentFrame++;
            captureImage = false;
            render = false;
                
           

            //Initialize shaders
            SetShaderParameters();
            GenerateCameraVectors();
        }

        if (!renderComplete)
        {
            Debug.Log("Update called");
            //March rays
            UpdateRay();
            currentPass++;

            if (currentPass >= maxPasses)
            {
                Debug.Log("checking true");
                CheckCompleteness(true);
            }
            else if (Time.time > updateInterval)
            {
                Debug.Log("checking false");
                updateInterval = Mathf.FloorToInt(Time.time) + 1;
                CheckCompleteness(false);
            }
        }
    }


        /*if ((!renderComplete)
        {
            if (startRender)
            {

                // Read out to console
                Debug.Log("Beginning render.");

                // Reset variables
                startTime = Time.realtimeSinceStartup;
                lastCheck = Vector2Int.zero;
                startRender = false;
                currentPass = 0;

                // Initialize shaders
                SetShaderParameters();
                GenerateCameraVectors();

            }
            else
            {
                // March rays
                UpdateRay();
                currentPass++;

                // Check if hard check pass is surpassed
                if (!hardCheck && currentPass >= maxSoftPasses)
                {
                    hardCheck = true;
                    Debug.Log("Maximum soft passes exceeded, checking for stranded rays.");
                    rayUpdateShader.SetBool("hardCheck", true);
                }

                // Check if maximum passes is surpassed
                if (currentPass >= maxPasses)
                {

                    Debug.Log("Maximum passes exceeded, timing out.");
                    CheckCompleteness(true);
                }
            }

            // Check for render completeness once per second
            if (Time.time - checkTimer > updateInterval)
            {
                checkTimer = Time.time;
                CheckCompleteness(false);
            }
        }
    }*/

        private void GenerateCameraVectors()
        {

            // Make sure we have a current render target
            InitRenderTextures();

            // Set textures and dispatch the compute shader
            cameraVectorShader.SetTexture(0, "Position", _position);
            cameraVectorShader.SetTexture(0, "Direction", _direction);
            cameraVectorShader.SetTexture(0, "Color", _color);
            cameraVectorShader.SetTexture(0, "isComplete", _isComplete);
            cameraVectorShader.SetTexture(0, "TimeStep", _timeStep);
            cameraVectorShader.SetTexture(0, "ErrorTolerance", _errorTolerance);
            int threadGroupsX = Mathf.CeilToInt(overSample * Screen.width / numThreads);
            int threadGroupsY = Mathf.CeilToInt(overSample * Screen.height / numThreads);
            cameraVectorShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        }

        private void UpdateRay()
        {

            // Set textures and dispatch the compute shader
            rayUpdateShader.SetTexture(0, "Position", _position);
            rayUpdateShader.SetTexture(0, "Direction", _direction);
            rayUpdateShader.SetTexture(0, "Color", _color);
            rayUpdateShader.SetTexture(0, "isComplete", _isComplete);
            rayUpdateShader.SetTexture(0, "TimeStep", _timeStep);
            rayUpdateShader.SetTexture(0, "ErrorTolerance", _errorTolerance);
            int threadGroupsX = Mathf.CeilToInt(overSample * Screen.width / numThreads);
            int threadGroupsY = Mathf.CeilToInt(overSample * Screen.height / numThreads);
            rayUpdateShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        }

        private Texture2D RenderToTexture(RenderTexture rt, TextureFormat format)
        {
            // Read render texture to texture2D
            RenderTexture.active = rt;
            Texture2D outTex = new Texture2D(rt.width, rt.height, format, false);
            outTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            outTex.Apply(false);
            RenderTexture.active = null;

            return outTex;
        }

    private void CheckComplete(bool maxpasses)
    {
        GameObject cf = GameObject.Find("CameraF");
        RayTraceCameraBHVR cfFrames = cf.GetComponent<RayTraceCameraBHVR>();
        bool cfComplete = cfFrames.renderComplete;

        GameObject cb = GameObject.Find("CameraB");
        RayTraceCameraBHVR cbFrames = cb.GetComponent<RayTraceCameraBHVR>();
        bool cbComplete = cbFrames.renderComplete;

        GameObject cl = GameObject.Find("CameraL");
        RayTraceCameraBHVR clFrames = cl.GetComponent<RayTraceCameraBHVR>();
        bool clComplete = clFrames.renderComplete;

        GameObject cr = GameObject.Find("CameraR");
        RayTraceCameraBHVR crFrames = cr.GetComponent<RayTraceCameraBHVR>();
        bool crComplete = crFrames.renderComplete;

        GameObject cu = GameObject.Find("CameraU");
        RayTraceCameraBHVR cuFrames = cu.GetComponent<RayTraceCameraBHVR>();
        bool cuComplete = cuFrames.renderComplete;

        GameObject cd = GameObject.Find("CameraD");
        RayTraceCameraBHVR cdFrames = cd.GetComponent<RayTraceCameraBHVR>();
        bool cdComplete = cdFrames.renderComplete;

        if (!render)
        {
            /*if(!renderComplete)
            {
                CheckCompleteness(maxpasses);
            }*/

            if (cfComplete && cbComplete && clComplete && crComplete && cuComplete && cdComplete)
            {
                render = true;
            }
        }
        else
        {
            bool cfCheckAll = cfFrames.render;
            bool cbCheckAll = cbFrames.render;
            bool clCheckAll = clFrames.render;
            bool crCheckAll = crFrames.render;
            bool cuCheckAll = cuFrames.render;
            bool cdCheckAll = cdFrames.render;

            if (cfCheckAll && cbCheckAll && clCheckAll && crCheckAll && cuCheckAll && cdCheckAll)
            {
                //startRender = true;

                captureImage = true;
            }
        }
        
    }
    private void CheckCompleteness(bool maxPasses)
    {

        // Read render texture to texture2D
        Texture2D completeTex = RenderToTexture(_isComplete, TextureFormat.RFloat);


        // Loop over pixels searching for incomplete
        if (cameraState == CameraState.Single)
        {
            if (!maxPasses)
            {
                for (int i = lastCheck.x; i < _isComplete.width; i++)
                {
                    for (int j = lastCheck.y; j < _isComplete.width; j++)
                    {
                        if (completeTex.GetPixel(i, j).r == 0)
                        {
                            //Debug.Log("Incomplete on pixel: (" + i.ToString() + ", " + j.ToString() + ")");
                            Destroy(completeTex);
                            lastCheck = new Vector2Int(i, j);
                            return;
                        }
                    }
                }
            }


            else if (maxPasses)
            {
                Destroy(completeTex);
                OnComplete();
            }
        }

        else
        {
            if (!maxPasses)
            {
                for (int i = lastCheck.x; i < _isComplete.width; i++)
                {
                    for (int j = lastCheck.y; j < _isComplete.width; j++)
                    {
                        if (completeTex.GetPixel(i, j).r == 0)
                        {
                            Debug.Log("Incomplete on pixel: (" + i.ToString() + ", " + j.ToString() + ")");
                            Destroy(completeTex);
                            lastCheck = new Vector2Int(i, j);
                            return;
                        }
                    }
                }
            }


            else if (maxPasses)
            {
                renderComplete = true;
                Destroy(completeTex);
                //OnComplete();
            }  
        }
    }

   private void OnComplete()
   {
        Debug.Log("OnComplete");
        if (cameraState == CameraState.Single)
        {
            // Set complete render flag
            renderComplete = true;
            // Debug message
            int elapsedTime = (int)(Time.realtimeSinceStartup - startTime);
            Debug.Log("Render complete!\nTime Elapsed: " + elapsedTime.ToString() + " s");
            Debug.Log("Render cycle complete!");
            currentFrame++;
            SaveToFile(_color);
        }
        else
        {
            renderComplete = true;
            /*CheckFaces();*/
            Debug.Log("Face render cycle complete!");
            /*if (completeFacesRayTraceCameraBHVR == 6)
            {
                completeCheck++;
                Debug.Log("Total Checks = " + totalChecks);
                if (totalChecks == 6)
                {
                    Debug.Log("capture complete");
                    capture = true;
                    // Advance time and reset settings
                    coordinateTime += (1f / framesPerSecond);
                    currentFrame++;
                    ResetSettings();
                }
            } */
        }
   }

   private void CheckFaces()
   {
        /* GameObject cf = GameObject.Find("CameraF");
         RayTraceCameraBHVR cfFrames = cf.GetComponent<RayTraceCameraBHVR>();
         int cfCount = cfFrames.faceNumber;

         GameObject cb = GameObject.Find("CameraB");
         RayTraceCameraBHVR cbFrames = cb.GetComponent<RayTraceCameraBHVR>();
         int cbCount = cbFrames.faceNumber;

         GameObject cl = GameObject.Find("CameraL");
         RayTraceCameraBHVR clFrames = cl.GetComponent<RayTraceCameraBHVR>();
         int clCount = clFrames.faceNumber;

         GameObject cr = GameObject.Find("CameraR");
         RayTraceCameraBHVR crFrames = cr.GetComponent<RayTraceCameraBHVR>();
         int crCount = crFrames.faceNumber;

         GameObject cu = GameObject.Find("CameraU");
         RayTraceCameraBHVR cuFrames = cu.GetComponent<RayTraceCameraBHVR>();
         int cuCount = cuFrames.faceNumber;

         GameObject cd = GameObject.Find("CameraD");
         RayTraceCameraBHVR cdFrames = cd.GetComponent<RayTraceCameraBHVR>();
         int cdCount = cdFrames.faceNumber;

         completeFacesRayTraceCameraBHVR = cfCount + cbCount + clCount + crCount + cuCount + cdCount;

         Debug.Log("There are " + completeFacesRayTraceCameraBHVR + " faces ");

         Debug.Log("Still checking, total checks = " + totalChecks);

         if (completeFacesRayTraceCameraBHVR == 6)
         {
             completeCheck++;

             int cfCheck = cfFrames.completeCheck;

             int cbCheck = cbFrames.completeCheck;

             int clCheck = clFrames.completeCheck;

             int crCheck = crFrames.completeCheck;

             int cuCheck = cuFrames.completeCheck;

             int cdCheck = cdFrames.completeCheck;

             totalChecks = cfCheck + cbCheck + clCheck + crCheck + cuCheck + cdCheck;
             Debug.Log("Total Checks = " + totalChecks);
             if (totalChecks == 6)
             {
                 Debug.Log("capture complete");
                 capture = true;
                 // Advance time and reset settings
                 coordinateTime += (1f / framesPerSecond);
                 currentFrame++;
                 ResetSettings();
             }
         }*/

        GameObject cf = GameObject.Find("CameraF");
        RayTraceCameraBHVR cfFrames = cf.GetComponent<RayTraceCameraBHVR>();
        bool cfComplete = cfFrames.renderComplete;

        GameObject cb = GameObject.Find("CameraB");
        RayTraceCameraBHVR cbFrames = cb.GetComponent<RayTraceCameraBHVR>();
        bool cbComplete = cbFrames.renderComplete;

        GameObject cl = GameObject.Find("CameraL");
        RayTraceCameraBHVR clFrames = cl.GetComponent<RayTraceCameraBHVR>();
        bool clComplete = clFrames.renderComplete;

        GameObject cr = GameObject.Find("CameraR");
        RayTraceCameraBHVR crFrames = cr.GetComponent<RayTraceCameraBHVR>();
        bool crComplete = crFrames.renderComplete;

        GameObject cu = GameObject.Find("CameraU");
        RayTraceCameraBHVR cuFrames = cu.GetComponent<RayTraceCameraBHVR>();
        bool cuComplete = cuFrames.renderComplete;

        GameObject cd = GameObject.Find("CameraD");
        RayTraceCameraBHVR cdFrames = cd.GetComponent<RayTraceCameraBHVR>();
        bool cdComplete = cdFrames.renderComplete;

        if(cfComplete && cbComplete && clComplete && crComplete && cuComplete && cdComplete)
        {
            Debug.Log("all complete");
            currentFrame++;
            /*ResetSettings();*/
            //Debug.Log("Reseting");
            /*faceNumber = 0;
            totalChecks = 0;
            completeCheck = 0;*/
            startRender = true;
            renderComplete = false;
            hardCheck = false;
            completeFacesRayTraceCameraBHVR = 0;
        }
    }

   private void ResetSettings()
   {
        Debug.Log("Reseting");
        renderComplete = false;
        completeCheck = 0;
        startRender = true;
        render = false;
        hardCheck = false;
   }

    private void SaveToFile(RenderTexture saveTexture)
    {

            // Create texture2D from render texture
            Texture2D colorTex = RenderToTexture(saveTexture, TextureFormat.RGBAFloat);

            // Encode to image format
            byte[] bytes;

            bytes = colorTex.EncodeToJPG();



            Destroy(colorTex);

            // Save to file
            try
            {

                // Set up filename and save
                string filename = "SingleCam_" + currentFrame + ".jpg";


                // Set up path to directory
                string fullPath = Application.dataPath + "/SingleCamFrames/";
                fullPath = string.IsNullOrEmpty(subfolder) ? fullPath : fullPath + subfolder + "/";

                // Ensure existence of directory
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }

                // Save file
                File.WriteAllBytes(fullPath + filename, bytes);
                Debug.Log("File saved.");

            }
            catch
            {

                Debug.LogWarning("ERROR: Failure to save file.");
            }
    }

    
}



