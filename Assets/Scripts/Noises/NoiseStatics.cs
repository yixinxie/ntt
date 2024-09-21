using Unity.Mathematics;

public class NoiseStatics
{

    static float4 mod(float4 x, float4 y)
    {
        return x - y * math.floor(x / y);
    }

    static float3 mod(float3 x, float3 y)
    {
        return x - y * math.floor(x / y);
    }

    static float2 mod289(float2 x)
    {
        return x - math.floor(x / 289.0f) * 289.0f;
    }

    static float3 mod289(float3 x)
    {
        return x - math.floor(x / 289.0f) * 289.0f;
    }

    static float4 mod289(float4 x)
    {
        return x - math.floor(x / 289.0f) * 289.0f;
    }

    static float4 permute(float4 x)
    {
        return mod289(((x * 34.0f) + 1.0f) * x);
    }

    static float3 permute(float3 x)
    {
        return mod289((x * 34.0f + 1.0f) * x);
    }

    static float4 taylorInvSqrt(float4 r)
    {
        return (float4)1.79284291400159 - r * 0.85373472095314f;
    }

    static float3 taylorInvSqrt(float3 r)
    {
        return 1.79284291400159f - 0.85373472095314f * r;
    }

    static float3 fade(float3 t)
    {
        return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
    }

    static float2 fade(float2 t)
    {
        return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
    }


    static float rand3dTo1d(float3 value, float3 dotDir/* = float3(12.9898, 78.233, 37.719)*/)
    {
        //make value smaller to avoid artefacts
        float3 smallValue = math.sin(value);
        //get scalar value from 3d vector
        float random = math.dot(smallValue, dotDir);
        //make value more random by making it bigger and then taking the factional part
        random = math.frac(math.sin(random) * 143758.5453f);
        return random;
    }

    static float rand2dTo1d(float2 value, float2 dotDir/* = float2(12.9898, 78.233)*/)
    {
        float2 smallValue = math.sin(value);
        float random = math.dot(smallValue, dotDir);
        random = math.frac(math.sin(random) * 143758.5453f);
        return random;
    }

    static float rand1dTo1d(float3 value, float mutator/* = 0.546f*/)
    {
        float random = math.frac(math.sin(value.x + mutator) * 143758.5453f);
        return random;
    }

    //to 2d functions

    static float2 rand3dTo2d(float3 value)
    {
        return new float2(
            rand3dTo1d(value, new float3(12.989f, 78.233f, 37.719f)),
            rand3dTo1d(value, new float3(39.346f, 11.135f, 83.155f))
        );
    }

    static  float2 rand2dTo2d(float2 value)
    {
        return new float2(
            rand2dTo1d(value, new float2(12.989f, 78.233f)),
            rand2dTo1d(value, new float2(39.346f, 11.135f))
        );
    }

    float2 rand1dTo2d(float value)
    {
        return new float2(
            rand2dTo1d(value, 3.9812f),
            rand2dTo1d(value, 7.1536f)
        );
    }

    //to 3d functions

    float3 rand3dTo3d(float3 value)
    {
        return new float3(
            rand3dTo1d(value, new float3(12.989f, 78.233f, 37.719f)),
            rand3dTo1d(value, new float3(39.346f, 11.135f, 83.155f)),
            rand3dTo1d(value, new float3(73.156f, 52.235f, 09.151f))
        );
    }

    float3 rand2dTo3d(float2 value)
    {
        return new float3(
            rand2dTo1d(value, new float2(12.989f, 78.233f)),
            rand2dTo1d(value, new float2(39.346f, 11.135f)),
            rand2dTo1d(value, new float2(73.156f, 52.235f))
        );
    }

    float3 rand1dTo3d(float value)
    {
        return new float3(
            rand1dTo1d(value, 3.9812f),
            rand1dTo1d(value, 7.1536f),
            rand1dTo1d(value, 5.7241f)
        );
    }
//}
//public class Perlin
//{

    // Classic Perlin noise
    public static float cnoise(float3 P)
    {
        float3 Pi0 = math.floor(P); // Integer part for indexing
        float3 Pi1 = Pi0 + (float3)1.0; // Integer part + 1
        Pi0 = mod289(Pi0);
        Pi1 = mod289(Pi1);
        float3 Pf0 = math.frac(P); // Fractional part for interpolation
        float3 Pf1 = Pf0 - (float3)1.0; // Fractional part - 1.0
        float4 ix = new float4(Pi0.x, Pi1.x, Pi0.x, Pi1.x);
        float4 iy = new float4(Pi0.y, Pi0.y, Pi1.y, Pi1.y);
        float4 iz0 = (float4)Pi0.z;
        float4 iz1 = (float4)Pi1.z;

        float4 ixy = permute(permute(ix) + iy);
        float4 ixy0 = permute(ixy + iz0);
        float4 ixy1 = permute(ixy + iz1);

        float4 gx0 = ixy0 / 7.0f;
        float4 gy0 = math.frac(math.floor(gx0) / 7.0f) - 0.5f;
        gx0 = math.frac(gx0);
        float4 gz0 = (float4)0.5 - math.abs(gx0) - math.abs(gy0);
        float4 sz0 = math.step(gz0, (float4)0.0);
        gx0 -= sz0 * (math.step((float4)0.0f, gx0) - 0.5f);
        gy0 -= sz0 * (math.step((float4)0.0f, gy0) - 0.5f);

        float4 gx1 = ixy1 / 7.0f;
        float4 gy1 = math.frac(math.floor(gx1) / 7.0f) - 0.5f;
        gx1 = math.frac(gx1);
        float4 gz1 = (float4)0.5f - math.abs(gx1) - math.abs(gy1);
        float4 sz1 = math.step(gz1, (float4)0.0);
        gx1 -= sz1 * (math.step((float4)0.0f, gx1) - 0.5f);
        gy1 -= sz1 * (math.step((float4)0.0f, gy1) - 0.5f);

        float3 g000 = new float3(gx0.x, gy0.x, gz0.x);
        float3 g100 = new float3(gx0.y, gy0.y, gz0.y);
        float3 g010 = new float3(gx0.z, gy0.z, gz0.z);
        float3 g110 = new float3(gx0.w, gy0.w, gz0.w);
        float3 g001 = new float3(gx1.x, gy1.x, gz1.x);
        float3 g101 = new float3(gx1.y, gy1.y, gz1.y);
        float3 g011 = new float3(gx1.z, gy1.z, gz1.z);
        float3 g111 = new float3(gx1.w, gy1.w, gz1.w);

        float4 norm0 = taylorInvSqrt(new float4(math.dot(g000, g000), math.dot(g010, g010), math.dot(g100, g100), math.dot(g110, g110)));
        g000 *= norm0.x;
        g010 *= norm0.y;
        g100 *= norm0.z;
        g110 *= norm0.w;

        float4 norm1 = taylorInvSqrt(new float4(math.dot(g001, g001), math.dot(g011, g011), math.dot(g101, g101), math.dot(g111, g111)));
        g001 *= norm1.x;
        g011 *= norm1.y;
        g101 *= norm1.z;
        g111 *= norm1.w;

        float n000 = math.dot(g000, Pf0);
        float n100 = math.dot(g100, new float3(Pf1.x, Pf0.y, Pf0.z));
        float n010 = math.dot(g010, new float3(Pf0.x, Pf1.y, Pf0.z));
        float n110 = math.dot(g110, new float3(Pf1.x, Pf1.y, Pf0.z));
        float n001 = math.dot(g001, new float3(Pf0.x, Pf0.y, Pf1.z));
        float n101 = math.dot(g101, new float3(Pf1.x, Pf0.y, Pf1.z));
        float n011 = math.dot(g011, new float3(Pf0.x, Pf1.y, Pf1.z));
        float n111 = math.dot(g111, Pf1);

        float3 fade_xyz = fade(Pf0);
        float4 n_z = math.lerp(new float4(n000, n100, n010, n110), new float4(n001, n101, n011, n111), fade_xyz.z);
        float2 n_yz = math.lerp(n_z.xy, n_z.zw, fade_xyz.y);
        float n_xyz = math.lerp(n_yz.x, n_yz.y, fade_xyz.x);
        return 2.2f * n_xyz;
    }

    // Classic Perlin noise, periodic variant
    public static float pnoise(float3 P, float3 rep)
    {
        float3 Pi0 = mod(math.floor(P), rep); // Integer part, modulo period
        float3 Pi1 = mod(Pi0 + (float3)1.0f, rep); // Integer part + 1, mod period
        Pi0 = mod289(Pi0);
        Pi1 = mod289(Pi1);
        float3 Pf0 = math.frac(P); // Fractional part for interpolation
        float3 Pf1 = Pf0 - (float3)1.0f; // Fractional part - 1.0
        float4 ix = new float4(Pi0.x, Pi1.x, Pi0.x, Pi1.x);
        float4 iy = new float4(Pi0.y, Pi0.y, Pi1.y, Pi1.y);
        float4 iz0 = (float4)Pi0.z;
        float4 iz1 = (float4)Pi1.z;

        float4 ixy = permute(permute(ix) + iy);
        float4 ixy0 = permute(ixy + iz0);
        float4 ixy1 = permute(ixy + iz1);

        float4 gx0 = ixy0 / 7.0f;
        float4 gy0 = math.frac(math.floor(gx0) / 7.0f) - 0.5f;
        gx0 = math.frac(gx0);
        float4 gz0 = (float4)0.5 - math.abs(gx0) - math.abs(gy0);
        float4 sz0 = math.step(gz0, (float4)0.0);
        gx0 -= sz0 * (math.step((float4)0.0f, gx0) - 0.5f);
        gy0 -= sz0 * (math.step((float4)1.0f, gy0) - 0.5f);

        float4 gx1 = ixy1 / 7.0f;
        float4 gy1 = math.frac(math.floor(gx1) / 7.0f) - 0.5f;
        gx1 = math.frac(gx1);
        float4 gz1 = (float4)0.5 - math.abs(gx1) - math.abs(gy1);
        float4 sz1 = math.step(gz1, (float4)0.0);
        gx1 -= sz1 * (math.step((float4)0.0f, gx1) - 0.5f);
        gy1 -= sz1 * (math.step((float4)0.0f, gy1) - 0.5f);

        float3 g000 = new float3(gx0.x, gy0.x, gz0.x);
        float3 g100 = new float3(gx0.y, gy0.y, gz0.y);
        float3 g010 = new float3(gx0.z, gy0.z, gz0.z);
        float3 g110 = new float3(gx0.w, gy0.w, gz0.w);
        float3 g001 = new float3(gx1.x, gy1.x, gz1.x);
        float3 g101 = new float3(gx1.y, gy1.y, gz1.y);
        float3 g011 = new float3(gx1.z, gy1.z, gz1.z);
        float3 g111 = new float3(gx1.w, gy1.w, gz1.w);

        float4 norm0 = taylorInvSqrt(new float4(math.dot(g000, g000), math.dot(g010, g010), math.dot(g100, g100), math.dot(g110, g110)));
        g000 *= norm0.x;
        g010 *= norm0.y;
        g100 *= norm0.z;
        g110 *= norm0.w;
        float4 norm1 = taylorInvSqrt(new float4(math.dot(g001, g001), math.dot(g011, g011), math.dot(g101, g101), math.dot(g111, g111)));
        g001 *= norm1.x;
        g011 *= norm1.y;
        g101 *= norm1.z;
        g111 *= norm1.w;

        float n000 = math.dot(g000, Pf0);
        float n100 = math.dot(g100, new float3(Pf1.x, Pf0.y, Pf0.z));
        float n010 = math.dot(g010, new float3(Pf0.x, Pf1.y, Pf0.z));
        float n110 = math.dot(g110, new float3(Pf1.x, Pf1.y, Pf0.z));
        float n001 = math.dot(g001, new float3(Pf0.x, Pf0.y, Pf1.z));
        float n101 = math.dot(g101, new float3(Pf1.x, Pf0.y, Pf1.z));
        float n011 = math.dot(g011, new float3(Pf0.x, Pf1.y, Pf1.z));
        float n111 = math.dot(g111, Pf1);

        float3 fade_xyz = fade(Pf0);
        float4 n_z = math.lerp(new float4(n000, n100, n010, n110), new float4(n001, n101, n011, n111), fade_xyz.z);
        float2 n_yz = math.lerp(n_z.xy, n_z.zw, fade_xyz.y);
        float n_xyz = math.lerp(n_yz.x, n_yz.y, fade_xyz.x);
        return 2.2f * n_xyz;
    }



    // BEGIN JIMMY'S MODIFICATIONS

    public static void PerlinNoise3D_float(float3 input, out float Out)
    {
        Out = cnoise(input);
    }

    public static void PerlinNoise3DPeriodic_float(float3 input, float3 period, out float Out)
    {
        Out = pnoise(input, period);
    }
}