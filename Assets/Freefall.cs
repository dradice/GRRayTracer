using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Freefall : MonoBehaviour
{
    public float framerate = 1000.0f;
    public float timeStep = 1e-4f;
    public float errorTolerance = 1e-3f;
    public float t_0 = 0.0f;
    public float rho_s;

    public float[] position;
    public float[] momentum;

    public bool frameComplete;
    public int numFramesCheck = 0;
    public int numFrames;

    void Start()
    {
        RayTraceCameraBHVR rtcBHVR = GetComponent<RayTraceCameraBHVR>();
        rho_s = rtcBHVR.horizonRadius;
        frameComplete = rtcBHVR.renderComplete;

        position = new float[4] { 0.0f, (float)Camera.main.transform.position[0], (float)Camera.main.transform.position[1], (float)Camera.main.transform.position[2] };
        momentum = new float[4] { 0.0f, 0.0f, 0.0f, 0.0f };
    }

    float Pow7(float input)
    {
        return input * input * input * input * input * input * input;
    }

    float Pow2(float input)
    {
        return input * input;
    }

    float Pow3(float input)
    {
        return input * input * input;
    }

    float Pow4(float input)
    {
        return input * input * input * input;
    }

    void NullCondition(ref float[] u)
    {
      
        float rho = Mathf.Sqrt((u[1] * u[1]) + (u[2] * u[2]) + (u[3] * u[3]));

        float g_tt = -(1 - rho_s / rho) / (1 + rho_s / rho) * (1 - rho_s / rho) / (1 + rho_s / rho);
        float g_xx = Pow4((1 + rho_s / rho));
        float g_yy = g_xx;
        float g_zz = g_xx;

        float u_t = -1.0f - Mathf.Sqrt(-((g_xx * Pow2(u[5])) + (g_yy * Pow2(u[6])) + (g_zz * Pow2(u[7]))) / g_tt);
        u[4] = u_t;
    }

    void SchwarzchildChristoffel(float x, float y, float z, ref float[,,] Christoffel)
    {

        float rho = Mathf.Sqrt((x * x) + (y * y) + (z * z));

        float a = (rho - rho_s);
        float b = Pow7(rho + rho_s);
        float c = Pow3(rho);
        float d = 1 + (rho_s / rho);
        float e = (1 - Pow2(rho_s / rho));

        float g_xtt = (2 * c * rho_s * a * x) / (b);
        float g_ytt = (2 * c * rho_s * a * y) / (b);
        float g_ztt = (2 * c * rho_s * a * z) / (b);

        float g_ttx = (2 * rho_s * x) / (c * e);
        float g_tty = (2 * rho_s * y) / (c * e);
        float g_ttz = (2 * rho_s * z) / (c * e);

        float g_xxx = -(2 * rho_s * x) / (c * d);
        float g_yxy = g_xxx;
        float g_zxz = g_xxx;
        float g_xyy = -1.0f * g_xxx;
        float g_xzz = -1.0f * g_xxx;

        float g_yxx = (2 * rho_s * y) / (c * d);
        float g_xxy = -1.0f * g_yxx;
        float g_yyy = -1.0f * g_yxx;
        float g_zyz = -1.0f * g_yxx;
        float g_yzz = g_yxx;

        float g_zxx = (2 * rho_s * z) / (c * d);
        float g_xxz = -1.0f * g_zxx;
        float g_zyy = g_zxx;
        float g_yyz = -1.0f * g_zxx;
        float g_zzz = -1.0f * g_zxx;

        float[,] c0 = new float[4, 4]
        {
         {0.0f, g_ttx, g_tty, g_ttz},
         {g_ttx, 0.0f, 0.0f, 0.0f},
         {g_tty, 0.0f, 0.0f, 0.0f},
         {g_ttz, 0.0f, 0.0f, 0.0f}
        };

        float[,] c1 = new float[4, 4]
        {
         {g_xtt, 0.0f, 0.0f, 0.0f},
         {0.0f, g_xxx, g_xxy, g_xxz},
         {0.0f, g_xxy, g_xyy, 0.0f},
         {0.0f, g_xxz, 0.0f, g_xzz}
        };

        float[,] c2 = new float[4, 4]
        {
         {g_ytt, 0.0f, 0.0f, 0.0f},
         {0.0f, g_yxx, g_yxy, 0.0f},
         {0.0f, g_yxy, g_yyy, g_yyz},
         {0.0f, 0.0f, g_yyz, g_yzz}
        };

        float[,] c3 = new float[4, 4]
        {
         {g_ztt, 0.0f, 0.0f, 0.0f},
         {0.0f, g_zxx, 0.0f, g_zxz},
         {0.0f, 0.0f, g_zyy, g_zyz},
         {0.0f, g_zxz, g_zyz, g_zzz}
        };

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                Christoffel[0, i, j] = c0[i, j];
                Christoffel[1, i, j] = c1[i, j];
                Christoffel[2, i, j] = c2[i, j];
                Christoffel[3, i, j] = c3[i, j];
            }
        }
    }



bool MoveCam(ref float dt, float tol, ref float[] x, ref float[] u)
{
    float[] k = new float[8];

    float[] u_star = new float[8];

    float[] u_euler = new float[8];

    float error;

    float[] u_trial = new float[8];

    float dt_min = 1e-10f;

    float[,,] Christoffel = new float[4, 4, 4];

    bool isGood = false;

    SchwarzchildChristoffel(x[1], x[2], x[3], ref Christoffel);


    for (int a = 0; a < 4; a++)
    {
        k[a] = -u[a];
        k[a + 4] = 0.0f;

        for (int b = 0; b < 4; b++)
        {

            for (int c = 0; c < 4; c++)
            {
                k[a + 4] += (Christoffel[a, b, c]) * u[b] * u[c];
            }
        }
    }

    for (int i = 0; i < 4; i++)
    {
        u_star[i] = x[i] + (0.5f) * dt * k[i];
        u_euler[i] = x[i] + dt * k[i];
    }

    for (int i = 4; i < 8; i++)
    {
        u_star[i] = u[i - 4] + (0.5f) * dt * k[i];
        u_euler[i] = u[i - 4] + dt * k[i];
    }

    SchwarzchildChristoffel(u_star[1], u_star[2], u_star[3], ref Christoffel);

    for (int a = 0; a < 4; a++)
    {
        k[a] = -u_star[a + 4];
        k[a + 4] = 0.0f;

        for (int b = 0; b < 4; b++)
        {

            for (int c = 0; c < 4; c++)
            {
                k[a + 4] += (Christoffel[a, b, c]) * u_star[b + 4] * u_star[c + 4];
            }
        }
    }

    for (int i = 0; i < 4; i++)
    {
        u_trial[i] = x[i] + dt * k[i];
    }

    for (int i = 4; i < 8; i++)
    {
        u_trial[i] = u[i - 4] + dt * k[i];
    }

    error = 0.0f;

    for (int i = 1; i < 4; i++)
    {
        error += Mathf.Abs(u_trial[i] - u_euler[i]);
    }

    if (error < tol || dt <= dt_min)
    {
        isGood = true;

        for (int i = 0; i < 4; i++)
        {
            x[i] = u_trial[i];
        }

            NullCondition(ref u_trial);
        for (int i = 4; i < 8; i++)
        {
            u[i - 4] = u_trial[i];
        }
        if (4 * error < tol)
        {
            dt = dt * 2.0f;
        }
    }
    else if (error > tol)
    {

        dt *= 0.5f;
    }
    return isGood;


}

    // Update is called once per frame
    void Update()
    {
            RayTraceCameraBHVR rtcBHVR = GetComponent<RayTraceCameraBHVR>();
            numFrames = rtcBHVR.currentFrame;

        if (numFrames > numFramesCheck)
        {
            float[] x = position;
            float[] u = momentum;

            float dt = timeStep;
            float tol = errorTolerance;

            bool isGood = MoveCam(ref dt, tol, ref x, ref u);

            bool updatePosition = false;

            timeStep = dt;

            if (isGood)
            {
                t_0 += timeStep;
                position = x;
                momentum = u;

            }

            if (t_0 >= 1.0f / framerate)
            {
                transform.position = new Vector3(position[1], position[2], position[3]);
                numFramesCheck += 1;
                t_0 = 0.0f;
            }


        }
        
    }
}

