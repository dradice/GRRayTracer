using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Freefall : MonoBehaviour
{
    public float framerate = 3.0f;
    public float timeStep = 1e-3f;
    public float errorTolerance = 1e-3f;
    public float t_0 = 0.0f;
    public float rho_s;

    public float[] position;
    public float[] momentum;

    public bool frameComplete;
    public int numFramesCheck = 1;
    public int numFrames = 0;

    float eps = 0.5f;

    float a_bh = 0.0f;

    void Start()
    {

        position = new float[4] { 0.0f, (float)transform.position[0], (float)transform.position[1], (float)transform.position[2] };
        momentum = new float[4] { 1.0f, 0.0f, 0.0f, 0.0f };

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

        float g_tt = -(1 - rho_s / rho);
        float g_xx = Pow4((1 + rho_s / rho));
        float g_yy = g_xx;
        float g_zz = g_xx;

        float u_t = Mathf.Sqrt((-1.0f - ((g_xx * Pow2(u[5])) + (g_yy * Pow2(u[6])) + (g_zz * Pow2(u[7])))) / g_tt);
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
    
    void Metric(float[] xyz, ref float[,] g)
    {
        /*float rho = Mathf.Sqrt((x[1] * x[1]) + (x[2] * x[2]) + (x[3] * x[3]));

        float g_tt = -Pow2((1 - rho_s / rho) / (1 + rho_s / rho));
        float g_xx = Pow4((1 + rho_s / rho));
        float g_yy = g_xx;
        float g_zz = g_xx;

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                g[i, j] = 0.0f;

            }
        }
        g[0, 0] = g_tt;
        g[1, 1] = g_xx;
        g[2, 2] = g_yy;
        g[3, 3] = g_zz;*/

        float x = xyz[1];
        float y = xyz[2];
        float z = xyz[3];

        float rad = Mathf.Sqrt(Pow2(x) + Pow2(y) + Pow2(z));
        float r = Mathf.Sqrt((Pow2(rad) - Pow2(a_bh) + Mathf.Sqrt(Pow2(Pow2(rad) - Pow2(a_bh)) + 4.0f * Pow2(a_bh) * Pow2(z))) / 2.0f);

        if (r < eps)
        {
            r = eps / 2.0f + (r * r / (2 * eps));
        }

        // Set covariant components
        // null vector l
        float[] l_lower = new float[4];
        l_lower[0] = 1.0f;
        l_lower[1] = (r * x + (a_bh) * y) / (Pow2(r) + Pow2(a_bh));
        l_lower[2] = (r * y - (a_bh) * x) / (Pow2(r) + Pow2(a_bh));
        l_lower[3] = z / r;

        // g_nm = f*l_n*l_m + eta_nm, where eta_nm is Minkowski metric

        float f = 2.0f * Pow2(r) * r / (Pow2(Pow2(r)) + Pow2(a_bh) * Pow2(z));

        for (int a = 0; a < 4; a++)
        {
            for (int b = 0; b < 4; b++)
            {
                g[a, b] = 0.0f;
            }
        }

        g[0,0] = f * l_lower[0] * l_lower[0] - 1.0f;
        g[0,1] = f * l_lower[0] * l_lower[1];
        g[0,2] = f * l_lower[0] * l_lower[2];
        g[0,3] = f * l_lower[0] * l_lower[3];
        g[1,0] = g[0,1];
        g[1,1] = f * l_lower[1] * l_lower[1] + 1.0f;
        g[1,2] = f * l_lower[1] * l_lower[2];
        g[1,3] = f * l_lower[1] * l_lower[3];
        g[2,0] = g[0,2];
        g[2,1] = g[1,2];
        g[2,2] = f * l_lower[2] * l_lower[2] + 1.0f;
        g[2,3] = f * l_lower[2] * l_lower[3];
        g[3,0] = g[0,3];
        g[3,1] = g[1,3];
        g[3,2] = g[2,3];
        g[3,3] = f * l_lower[3] * l_lower[3] + 1.0f;
    }

    void InverseMetric(float[] xyz, ref float[,] g)
    {
        /* float rho = Mathf.Sqrt((x[1] * x[1]) + (x[2] * x[2]) + (x[3] * x[3]));

         float g_tt = -Pow2((1 - rho_s / rho) / (1 + rho_s / rho));
         float g_xx = Pow4((1 + rho_s / rho));
         float g_yy = g_xx;
         float g_zz = g_xx;

         for (int i = 0; i < 4; i++)
         {
             for (int j = 0; j < 4; j++)
             {
                 g[i, j] = 0.0f;
             }
         }
         g[0, 0] = 1.0f / g_tt;
         g[1, 1] = 1.0f / g_xx;
         g[2, 2] = 1.0f / g_yy;
         g[3, 3] = 1.0f / g_zz;*/

        float x = xyz[1];
        float y = xyz[2];
        float z = xyz[3];

        float rad = Mathf.Sqrt(Pow2(x) + Pow2(y) + Pow2(z));
        float r = Mathf.Sqrt((Pow2(rad) - Pow2(a_bh) + Mathf.Sqrt(Pow2(Pow2(rad) - Pow2(a_bh)) + 4.0f * Pow2(a_bh) * Pow2(z))) / 2.0f);

        if (r < eps)
        {
            r = eps / 2 + (r * r / (2.0f * eps));
        }

        // Set contravariant components
        // null vector l
        float[] l_upper = new float[4];
        l_upper[0] = -1.0f;
        l_upper[1] = (r * x + (-a_bh) * y) / (Pow2(r) + Pow2(a_bh));
        l_upper[2] = (r * y - (a_bh) * x) / (Pow2(r) + Pow2(a_bh));
        l_upper[3] = z / r;

        float f = 2.0f * Pow2(r) * r / (Pow2(Pow2(r)) + Pow2(a_bh) * Pow2(z));

        for(int a = 0; a < 4; a++)
        {
            for (int b = 0; b < 4; b++)
            {
                g[a, b] = 0.0f;
            }
        }

        // g^nm = -f*l^n*l^m + eta^nm, where eta^nm is Minkowski metric
        g[0,0] = -f * l_upper[0] * l_upper[0] - 1.0f;
        g[0,1] = -f * l_upper[0] * l_upper[1];
        g[0,2] = -f * l_upper[0] * l_upper[2];
        g[0,3] = -f * l_upper[0] * l_upper[3];
        g[1,0] = g[0,1];
        g[1,1] = -f * l_upper[1] * l_upper[1] + 1.0f;
        g[1,2] = -f * l_upper[1] * l_upper[2];
        g[1,3] = -f * l_upper[1] * l_upper[3];
        g[2,0] = g[0,2];
        g[2,1] = g[1,2];
        g[2,2] = -f * l_upper[2] * l_upper[2] + 1.0f;
        g[2,3] = -f * l_upper[2] * l_upper[3];
        g[3,0] = g[0,3];
        g[3,1] = g[1,3];
        g[3,2] = g[2,3];
        g[3,3] = -f * l_upper[3] * l_upper[3] + 1.0f;


    }

    void Derivative(float[] x, int a, ref float[,] dg)
    {
        float h = 0.01f;
        float[] deltaPlus_x = new float[4];

        for (int i = 0; i < 4; i++)
        {
            deltaPlus_x[i] = x[i];
        }

        deltaPlus_x[a] += h;
        float[,] gPlus = new float[4, 4];
        Metric(deltaPlus_x, ref gPlus);

        float[] deltaMinus_x = new float[4];

        for (int i = 0; i < 4; i++)
        {
            deltaMinus_x[i] = x[i];
        }

        deltaMinus_x[a] -= h;
        float[,] gMinus = new float[4, 4];
        Metric(deltaMinus_x, ref gMinus);

        for (int b = 0; b < 4; b++)
        {
            for (int c = 0; c < 4; c++)
            {
                dg[b, c] = (gPlus[b, c] - gMinus[b, c]) / (2.0f * h);

            }
        }

    }

    bool MoveCam(ref float dt, float tol, ref float[] x_u, ref float[] u_u)
    {
        float[] rhs_x_u = new float[4];
        float[] rhs_u_u = new float[4];
        float[] rhs_u_d = new float[4];

        float[] x_u_star = new float[4];
        float[] u_d_star = new float[4];
        float[] u_u_star = new float[4];

        float[] x_u_euler = new float[4];
        float[] u_d_euler = new float[4];
        float[] u_u_euler = new float[4];

        float[] u_d = new float[4];

        float[,] g_dd = new float[4, 4];

        Metric(x_u, ref g_dd);

        float[] k = new float[8];

        float[] u_star = new float[8];

        float[] u_euler = new float[8];

        float error = 0.0f;

        float dt_min = 1e-10f;

        bool isGood = false;

        for (int a = 0; a < 4; a++)
        {
            u_d[a] = 0.0f;

            for (int b = 0; b < 4; b++)
            {
                u_d[a] += g_dd[a, b] * u_u[b];
            }

        }

        for (int a = 0; a < 4; a++)
        {
            rhs_x_u[a] = -u_u[a];

            rhs_u_d[a] = 0.0f;

            float[,] dg_dd = new float[4, 4];
            Derivative(x_u, a, ref dg_dd);

            for (int b = 0; b < 4; b++)
            {

                for (int c = 0; c < 4; c++)
                {

                    rhs_u_d[a] -= dt * ((0.5f) * dg_dd[b, c] * u_u[b] * u_u[c]);
                }
            }
        }

        for (int a = 0; a < 4; a++)
        {
            x_u_star[a] = x_u[a] + (0.5f) * dt * rhs_x_u[a];
            u_d_star[a] = u_d[a] + (0.5f) * dt * rhs_u_d[a];
        }

        float[,] g_uu = new float[4, 4];
        InverseMetric(x_u_star, ref g_uu);

        for (int a = 0; a < 4; a++)
        {
            u_u_star[a] = 0.0f;

            for (int b = 0; b < 4; b++)
            {
                u_u_star[a] += g_uu[a, b] * u_d_star[b];
            }
        }

        for (int a = 0; a < 4; a++)
        {
            x_u_euler[a] = x_u[a] + dt * rhs_x_u[a];
            u_d_euler[a] = u_d[a] + dt * rhs_u_d[a];
        }

        for (int a = 0; a < 4; a++)
        {
            rhs_u_d[a] = 0.0f;
            rhs_x_u[a] = -u_u_star[a];
            float[,] dg_dd = new float[4, 4];
            Derivative(x_u_star, a, ref dg_dd);

            for (int b = 0; b < 4; b++)
            {

                for (int c = 0; c < 4; c++)
                {
                    rhs_u_d[a] -= dt * (0.5f) * (dg_dd[b, c] * u_u_star[b] * u_u_star[c]);
                }
            }
        }

        for (int a = 0; a < 4; a++)
        {
            x_u_star[a] = x_u[a] + dt * rhs_x_u[a];
            u_d_star[a] = u_d[a] + dt * rhs_u_d[a];
        }

        for (int i = 1; i < 4; i++)
        {
            error += Mathf.Abs(x_u_star[i] - x_u_euler[i]);
        }

        if (error < tol || dt <= dt_min)
        {
            isGood = true;
            for (int i = 0; i < 4; i++)
            {
                x_u[i] = x_u_star[i];
            }

            InverseMetric(x_u, ref g_uu);

            for (int a = 0; a < 4; a++)
            {
                u_d[a] = u_d_star[a];
            }

            for (int a = 0; a < 4; a++)
            {
                u_u[a] = 0.0f;

                for (int b = 0; b < 4; b++)
                {

                    u_u[a] += g_uu[a, b] * u_d[a];
                    Debug.Log(g_uu[a, b]);
                }
            }

            if (4.0f * error < tol)
            {
                dt = dt * 2.0f;
            }
        }

        else if (error > tol)
        {
            dt *= 0.5f;
        }

        if (dt >= framerate)
        {
            dt = 0.9f * framerate;
        }

        return isGood;

    }


    // Update is called once per frame
    void Update()
    {
        GameObject multiCam = GameObject.Find("360Cam");
        PanoramaCapture cam360 = multiCam.GetComponent<PanoramaCapture>();
        numFrames = cam360.frameCount;

        if (t_0 < framerate)
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
            Debug.Log("good step");
            }
        }

        if (t_0 >= framerate)
        {
            if (numFrames > numFramesCheck)
            {
            numFramesCheck = numFrames;
            Debug.Log("updating cam pos.");
            transform.position = new Vector3(position[1], position[2], position[3]);
            t_0 = 0.0f;
            }
        }
    }
}



