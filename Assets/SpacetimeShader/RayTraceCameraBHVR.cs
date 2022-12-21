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
    public float updateInterval = 15f;
    public int overSample = 4;
    public int maxSoftPasses = 5000;
    public int maxPasses = 100000;
    public string subfolder = "";
    public bool exitOnComplete = false;
    public int completeFace = 0;



    [Header("Physical Parameters")]
    public float diskRadius = 3.0f;
    public float horizonRadius = 0.5f;



    public enum CamChoice { Panorama, Single };
    public CamChoice camChoice = CamChoice.Single;
    public bool is360 = false;


    private Camera _camera;
    private RenderTexture _position;
    private RenderTexture _direction;
    private RenderTexture _color;
    private RenderTexture _isComplete;
    private RenderTexture _timeStep;
    private RenderTexture _errorTolerance;

    public Vector4 _momentum;

    // Private variables
    private float
        startTime = 0f,
        checkTimer = 0f,
        numThreads = 8f,
        coordinateTime = 0f;

    public int currentPass = 0;
    public int currentFrame = 0;

    public int allFacesComplete = 0;


    private Vector2Int
        lastCheck = new Vector2Int(0, 0);

    private bool startRender = true;
    private bool hardCheck = false;

    //public variables
    public bool
        renderComplete = false;

    private void Awake()
    {
        // Get Camera component
        _camera = GetComponent<Camera>();
        switch (camChoice)
        {
            case CamChoice.Single:
                is360 = false;
                break;
            case CamChoice.Panorama:
                is360 = true;
                break;
        }
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
        cameraVectorShader.SetFloat("horizonRadius", horizonRadius);


        // Send render parameters to update shader
        rayUpdateShader.SetTexture(0, "_SkyboxTexture", skyboxTexture);
        rayUpdateShader.SetFloat("escapeDistance", escapeDistance);
        rayUpdateShader.SetFloat("time", coordinateTime);
        rayUpdateShader.SetFloat("diskRadius", diskRadius);
        rayUpdateShader.SetFloat("horizonRadius", horizonRadius);

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
        _momentum[0] = momentum[0];
        _momentum[1] = momentum[1];
        _momentum[2] = momentum[2];
        _momentum[3] = momentum[3];

        Debug.Log("running360");
        if (is360)
        {
            Debug.Log("running2");

            PanoramaCapture unify = multiCam.GetComponent<PanoramaCapture>();
            int frameTrack = unify.fCount;


            // Step through ray trace if not complete


            if (!renderComplete && allFacesComplete <= frameTrack)
            {
                allFacesComplete = frameTrack;
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
                    Debug.Log("running3");
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
                        Debug.Log("checking completeness");
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
        }



        if (!renderComplete && !is360)
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
    }

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

        private void CheckCompleteness(bool maxPasses)
        {

            // Read render texture to texture2D
            Texture2D completeTex = RenderToTexture(_isComplete, TextureFormat.RFloat);


            // Loop over pixels searching for incomplete
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
                if (!is360)
                {
                    Destroy(completeTex);
                    allFacesComplete++;
                    OnComplete();
                }

                if (is360)
                {
                    GameObject multiCam = GameObject.Find("360Cam");
                    PanoramaCapture unify = multiCam.GetComponent<PanoramaCapture>();
                    int frameTrack = unify.fCount;

                    if (allFacesComplete <= frameTrack)
                    {
                        // Run method if not broken
                        allFacesComplete++;
                        Destroy(completeTex);
                        Debug.Log("All pixels rendered successfully.");
                        OnComplete();
                    }
                }

                Debug.Log("All pixels rendered successfully.");
                // OnComplete();
            }
        }

        private void OnComplete()
        {

            // Set complete render flag
            renderComplete = true;


            //SaveToFile(_color); 


            // Debug message
            int elapsedTime = (int)(Time.realtimeSinceStartup - startTime);
            Debug.Log("Render complete!\nTime Elapsed: " + elapsedTime.ToString() + " s");

            // Update coordinate time
            if (is360)
            {
                completeFace++;
            }
            if (!is360)
            {

                completeFace++;
                SaveToFile(_color);
            }
            currentFrame++;
            if (currentFrame >= numFrames)
            {


                if (!is360)
                {
                    SaveToFile(_color);
                }

                // Console readout
                Debug.Log("Render cycle complete!");

                // Quit application
                if (exitOnComplete)
                {

                    Application.Quit();

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif

                }

            }
            else
            {
                // Advance time and reset settings
                coordinateTime += (1f / framesPerSecond);

                ResetSettings();
            }
        }

        private void ResetSettings()
        {
            startRender = true;
            renderComplete = false;
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
                string filename = "SingleCam_" + allFacesComplete + ".jpg";


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



